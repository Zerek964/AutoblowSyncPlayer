using LibVLCSharp.Shared;
using AutoblowSyncPlayer.Core.Services;
using AutoblowSyncPlayer.Core.Models;
using Microsoft.Extensions.Logging;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// LibVLC-based video player service implementation.
/// </summary>
public class LibVlcVideoPlayer : IVideoPlayerService
{
    private readonly ILogger<LibVlcVideoPlayer> _logger;
    private LibVLC? _libVlc;
    private MediaPlayer? _mediaPlayer;
    private Media? _currentMedia;
    private Media? _previousMedia; // Tracks old media for delayed disposal after atomic swap

    // Cached position updated by TimeChanged events. Provides a safe non-blocking read
    // for UI thread without hitting _mediaPlayer.Time (which can block and deadlock).
    // Uses Interlocked for thread-safe cross-thread access.
    private long _cachedPositionMs;

    public long PositionMs => Interlocked.Read(ref _cachedPositionMs);

    public long DurationMs => _mediaPlayer?.Length ?? 0;
    public bool IsPlaying => _mediaPlayer?.IsPlaying == true;
    public object? MediaPlayerHandle => _mediaPlayer;

    public event EventHandler<long>? PositionChanged;
    public event EventHandler<long>? DurationChanged;
    public event EventHandler? PlaybackEnded;
    public event EventHandler<bool>? IsPlayingChanged;

    public int Volume
    {
        get => _mediaPlayer?.Volume ?? 100;
        set => _mediaPlayer!.Volume = value;
    }

    public bool IsMuted
    {
        get => _mediaPlayer?.Volume == 0;
        set => _mediaPlayer!.Volume = value ? 0 : 100;
    }

    // -- VR3D Properties --

    public VideoMode CurrentVideoMode { get; private set; } = VideoMode.Standard2D;
    public StereoEye CurrentStereoEye { get; private set; } = StereoEye.Auto;

    // Event gate flag to prevent cross-thread deadlock during blocking native calls.
    // When Stop()/SeekToAsync() calls UnsubscribeFromLibVlcEvents(), this flag is set to false.
    // LibVLC events still fire (handlers are registered once on init), but immediately
    // return without doing any work (no Dispatcher.Invoke, no managed event invocation).
    // This prevents the deadlock where LibVLC internal threads try to marshal back to
    // a UI thread blocked inside Stop()/Time assignment.
    private volatile bool _eventsEnabled = true;

    public LibVlcVideoPlayer(ILogger<LibVlcVideoPlayer> logger)
    {
        _logger = logger;
        // Only create the LibVLC instance here. MediaPlayer creation is deferred
        // until Initialize() is called from MainWindow, after the VideoView exists.
        // This prevents LibVLC from creating its own native video output window.
        EnsureLibVlcInitialized();
    }

    public void Initialize()
    {
        // Create MediaPlayer now that VideoView exists (called from MainWindow constructor
        // before WireMediaPlayer). This ensures LibVLC renders into the WPF control
        // rather than creating its own native video output window.
        EnsureMediaPlayerInitialized();

        // Subscribe to LibVLC events ONCE during initialization.
        // Subscribing here (not in LoadVideoAsync) prevents handler accumulation
        // where each video load adds duplicate handlers.
        // Handlers are stored as fields so they can be unsubscribed during Stop() to prevent
        // cross-thread deadlock (LibVLC fires events from internal threads that try to
        // marshal back to UI thread via Dispatcher.Invoke while UI is blocked in Stop()).
        SubscribeToLibVlcEvents();
    }

    /// <summary>
    /// Subscribes to all LibVLC MediaPlayer events. Called once during initialization.
    /// Handlers are registered once and never removed — instead, a volatile flag gates
    /// whether event processing occurs, preventing cross-thread deadlock without handler accumulation.
    /// </summary>
    private void SubscribeToLibVlcEvents()
    {
        if (_mediaPlayer == null) return;

        // Capture 'this' reference for closures to avoid capturing mutable '_mediaPlayer' directly
        var player = this;

        _mediaPlayer.TimeChanged += (s, e) =>
        {
            if (!_eventsEnabled) return;

            // Use e.Time directly from event args to avoid blocking P/Invoke round-trip
            // to native LibVLC. Reading _mediaPlayer.Time during Stop() causes deadlock
            // because Stop() holds internal LibVLC locks and waits for event threads.
            long currentPos = e.Time;
            Interlocked.Exchange(ref _cachedPositionMs, currentPos);
            player.PositionChanged?.Invoke(player, currentPos);
        };
        _mediaPlayer.LengthChanged += (s, e) =>
        {
            if (!_eventsEnabled) return;

            // Use e.Length directly from event args to avoid blocking P/Invoke round-trip.
            long newDuration = e.Length;
            if (newDuration > 0)
            {
                player.DurationChanged?.Invoke(player, newDuration);
            }
        };
        _mediaPlayer.EncounteredError += (s, e) =>
        {
            _logger.LogError("Playback error");
        };
        _mediaPlayer.Stopped += (s, e) =>
        {
            if (!_eventsEnabled) return;
            player.PlaybackEnded?.Invoke(player, EventArgs.Empty);
            player.IsPlayingChanged?.Invoke(player, false);
        };
        _mediaPlayer.Playing += (s, e) =>
        {
            if (!_eventsEnabled) return;
            player.IsPlayingChanged?.Invoke(player, true);
        };
        _mediaPlayer.Paused += (s, e) =>
        {
            if (!_eventsEnabled) return;
            player.IsPlayingChanged?.Invoke(player, false);
        };
    }

    /// <summary>
    /// Disables LibVLC event processing to prevent cross-thread deadlock during blocking native calls.
    /// Sets the volatile flag to false so event handlers return immediately without doing any work.
    /// This is safe because handlers remain registered (no accumulation) but short-circuit.
    /// </summary>
    private void UnsubscribeFromLibVlcEvents()
    {
        _eventsEnabled = false;
    }

    public async Task<bool> LoadVideoAsync(string filePath, VideoMode videoMode = VideoMode.Standard2D, CancellationToken cancellationToken = default)
    {
        // MediaPlayer must exist before we load media. Ensure it's created.
        // By this point, MainWindow should have called Initialize() and wired
        // the VideoView, so the MediaPlayer will render into the WPF control.
        EnsureInitialized();

        // Store mode before background task so UI thread can apply it during assignment
        _currentVideoMode = videoMode;

        // FIX Attempt 6: Save reference to old media BEFORE background task.
        // We will dispose it AFTER assigning new media to MediaPlayer, so the
        // MediaPlayer never has a null Media reference during the transition.
        // This prevents LibVLC from creating a fallback native video window.
        Media? oldMedia = _currentMedia;
        _previousMedia = oldMedia;
        _currentMedia = null;

        // Media creation runs on background thread to avoid blocking UI.
        // The Media object itself doesn't need the UI thread — only the
        // MediaPlayer.Media assignment does.
        bool mediaResult = await Task.Run(() =>
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Video file not found: {FilePath}", filePath);
                    return false;
                }

                _currentMedia = new Media(_libVlc!, System.IO.Path.GetFullPath(filePath), FromType.FromPath);
                
                // Disable hardware decoding to prevent D3D11 native crashes
                // (D3D11.DLL_unloaded, c00000005) when Direct3D resources are unloaded
                _currentMedia.AddOption(":avcodec-hw=disable");
                _currentMedia.AddOption(":avcodec-hwdecode=disable");
                
                if (IsStereoMode(videoMode))
                {
                    int stereoMode = videoMode switch
                    {
                        VideoMode.Flat3dSbsLeft or VideoMode.Flat3dSbsRight => 2,
                        VideoMode.Flat3dOverUnderLeft or VideoMode.Flat3dOverUnderRight => 1,
                        VideoMode.Vr180Sbs or VideoMode.Vr360Sbs => 2,
                        VideoMode.Vr180OverUnder or VideoMode.Vr360OverUnder => 1,
                        _ => 0
                    };
                    _currentMedia.AddOption(":video-filter=stereoscopy");
                    _currentMedia.AddOption($":stereoscopy={stereoMode}");
                    _logger.LogInformation("Applied stereoscopic mode {StereoMode} for video mode {VideoMode} at media creation", stereoMode == 2 ? "SBS" : "OU", videoMode);
                }

                _logger.LogInformation("Media prepared: {FilePath}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to prepare media: {FilePath}", filePath);
                return false;
            }
        }, cancellationToken);

        if (!mediaResult) return false;

        // Atomic media swap on UI thread: assign new media to MediaPlayer FIRST,
        // then dispose old media. MediaPlayer never has null Media, so LibVLC
        // keeps the video surface attached throughout the transition.
        _mediaPlayer!.Media = _currentMedia;

        // Dispose old media AFTER the MediaPlayer already points to the new one.
        if (_previousMedia != null)
        {
            _previousMedia.Dispose();
            _previousMedia = null;
        }

        // Set internal state
        CurrentVideoMode = _currentVideoMode;

        _logger.LogInformation("Loaded video: {FilePath} (Duration: {DurationMs}ms)", filePath, _mediaPlayer!.Length);
        return true;
    }

    /// <summary>
    /// Stores the video mode to be applied during Media assignment on the UI thread.
    /// </summary>
    private VideoMode _currentVideoMode = VideoMode.Standard2D;

    public Task PlayAsync()
    {
        _mediaPlayer?.Play();
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        _mediaPlayer?.Pause();
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
        // CRITICAL FIX: Disable event processing before calling Stop().
        //
        // LibVLC's Stop() is a synchronous native call that fires internal events (Stopped, etc.)
        // from C-level threads. When called from the UI thread, these events try to marshal
        // back via Dispatcher.Invoke() in the event handlers — but the UI thread is blocked
        // waiting for Stop() to return, causing a cross-thread deadlock.
        //
        // By disabling events first, LibVLC event handlers short-circuit and return immediately,
        // preventing the deadlock. We re-enable events immediately after.
        _eventsEnabled = false;
        try
        {
            _mediaPlayer?.Stop();
            Interlocked.Exchange(ref _cachedPositionMs, 0);
        }
        finally
        {
            _eventsEnabled = true;
        }
        return Task.CompletedTask;
    }

    public Task SeekToAsync(long positionMs)
    {
        if (_mediaPlayer?.Length > 0)
        {
            // Disable event processing to prevent cross-thread deadlock.
            // Setting MediaPlayer.Time can block the calling (UI) thread while
            // LibVLC drains buffers. During this time, LibVLC fires TimeChanged
            // events from internal threads that try to Dispatcher.Invoke back to
            // the UI thread — causing a deadlock identical to the Stop() issue.
            _eventsEnabled = false;
            try
            {
                _mediaPlayer.Time = positionMs;
            }
            finally
            {
                _eventsEnabled = true;
            }
        }
        return Task.CompletedTask;
    }

    public Task UpdateViewpointAsync(float yaw, float pitch, float roll, float fov)
    {
        _mediaPlayer?.UpdateViewpoint(yaw, pitch, roll, fov);
        return Task.CompletedTask;
    }

    public Task SetVideoModeAsync(VideoMode mode, StereoEye stereoEye = StereoEye.Auto)
    {
        CurrentVideoMode = mode;
        CurrentStereoEye = stereoEye;
        _logger.LogInformation("Video mode changed to {Mode} with stereo eye {Eye}", mode, stereoEye);

        // If video is not playing, re-load with the correct mode so stereoscopic options
        // are applied at Media creation time (required for LibVLC to honor the option)
        if (!_mediaPlayer!.IsPlaying)
        {
            _logger.LogInformation("Video not playing, re-creating Media with stereoscopic options for mode {Mode}", mode);
            ReCreateMediaWithMode(mode);
        }
        else
        {
            _logger.LogWarning("Video is playing - stereoscopic mode change cannot be applied until playback stops");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Re-creates the current Media with stereoscopic options for the specified mode.
    /// Preserves the current file path and event subscriptions.
    /// </summary>
    private void ReCreateMediaWithMode(VideoMode mode)
    {
        if (_currentMedia is null || _mediaPlayer is null)
            return;

        try
        {
            // Get the MRL from the current media
            string mrl = _currentMedia.Mrl;
            _currentMedia.Dispose();
            _currentMedia = new Media(_libVlc!, mrl, FromType.FromLocation);
            
            // Add stereoscopic options before assigning to MediaPlayer
            if (IsStereoMode(mode))
            {
                int stereoMode = mode switch
                {
                    VideoMode.Flat3dSbsLeft or VideoMode.Flat3dSbsRight => 2,
                    VideoMode.Flat3dOverUnderLeft or VideoMode.Flat3dOverUnderRight => 1,
                    VideoMode.Vr180Sbs or VideoMode.Vr360Sbs => 2,
                    VideoMode.Vr180OverUnder or VideoMode.Vr360OverUnder => 1,
                    _ => 0
                };
                _currentMedia.AddOption(":video-filter=stereoscopy");
                _currentMedia.AddOption($":stereoscopy={stereoMode}");
                _logger.LogInformation("Re-created Media with stereoscopic mode {StereoMode} for mode {VideoMode}", stereoMode == 2 ? "SBS" : "OU", mode);
            }
            
            _mediaPlayer.Media = _currentMedia;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to re-create Media with new mode {Mode}", mode);
        }
    }

    /// <summary>
    /// Checks if the video mode requires stereoscopic rendering.
    /// </summary>
    private static bool IsStereoMode(VideoMode mode)
    {
        return mode is VideoMode.Flat3dSbsLeft or VideoMode.Flat3dSbsRight
            or VideoMode.Flat3dOverUnderLeft or VideoMode.Flat3dOverUnderRight
            or VideoMode.Vr180Sbs or VideoMode.Vr180OverUnder
            or VideoMode.Vr360Sbs or VideoMode.Vr360OverUnder;
    }

    /// <summary>
    /// Applies stereoscopic rendering mode to the current Media using LibVLC input options.
    /// LibVLC stereoscopy modes: 0=mono, 1=top-bottom (OU), 2=left-right (SBS).
    /// </summary>
    private void ApplyStereoscopicMode(VideoMode mode)
    {
        if (_currentMedia == null)
        {
            _logger.LogWarning("Cannot apply stereoscopic mode: no Media loaded.");
            return;
        }

        try
        {
            // Determine the stereoscopic mode based on video layout
            int stereoMode = mode switch
            {
                VideoMode.Flat3dSbsLeft or VideoMode.Flat3dSbsRight => 2,
                VideoMode.Flat3dOverUnderLeft or VideoMode.Flat3dOverUnderRight => 1,
                VideoMode.Vr180Sbs or VideoMode.Vr360Sbs => 2,
                VideoMode.Vr180OverUnder or VideoMode.Vr360OverUnder => 1,
                _ => 0
            };

            // Add stereoscopic deinterleave option to Media
            // This tells LibVLC to deinterleave the stereoscopic content
            string option = $":stereoscopy={stereoMode}";
            _currentMedia.AddOption(option);

            _logger.LogInformation(
                "Applied stereoscopic mode {Mode} via option {Option} for video mode {VideoMode}",
                stereoMode == 2 ? "SBS" : "OU",
                option,
                mode
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply stereoscopic mode for {Mode}. Video may render incorrectly.", mode);
        }
    }

    public void Dispose()
    {
        _mediaPlayer?.Dispose();
        _currentMedia?.Dispose();
        _libVlc?.Dispose();

        _mediaPlayer = null;
        _currentMedia = null;
        _libVlc = null;
    }

    /// <summary>
    /// Finds the LibVLC native libraries directory.
    /// In single-file publish mode, natives are extracted to a subfolder (e.g., libvlc/win-x64/).
    /// In development mode, the natives are in the same directory as the executable.
    /// </summary>
    private static string? FindLibVlcNativeDirectory()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;

        // Check for single-file publish native subfolders (libvlc/win-x64 or libvlc/win-x86)
        string[] candidateSubDirs = ["libvlc/win-x64", "libvlc/win-x86"];

        foreach (string subDir in candidateSubDirs)
        {
            string candidate = Path.Combine(baseDir, subDir);
            if (Directory.Exists(candidate))
            {
                // Verify it contains the required native DLL
                if (File.Exists(Path.Combine(candidate, "libvlc.dll")))
                {
                    return candidate;
                }
            }
        }

        // Fall back to base directory (development mode)
        if (File.Exists(Path.Combine(baseDir, "libvlc.dll")))
        {
            return baseDir;
        }

        return null;
    }

    /// <summary>
    /// Ensures LibVLC instance is initialized. Safe to call multiple times.
    /// Only creates the LibVLC instance — MediaPlayer is created separately
    /// via EnsureMediaPlayerInitialized() to allow proper VideoView wiring.
    /// </summary>
    private void EnsureLibVlcInitialized()
    {
        if (_libVlc is not null)
            return;

        string? nativeDir = FindLibVlcNativeDirectory();
        if (nativeDir is not null)
        {
            _logger.LogInformation("Setting LibVLC native directory: {NativeDir}", nativeDir);
            Environment.CurrentDirectory = nativeDir;
        }

        _libVlc = new LibVLC();
    }

    /// <summary>
    /// Ensures MediaPlayer is initialized. Must be called after VideoView is wired.
    /// MediaPlayer must be created on the same thread that hosts the VideoView,
    /// otherwise LibVLC creates its own native video output window.
    /// </summary>
    private void EnsureMediaPlayerInitialized()
    {
        if (_mediaPlayer is not null)
            return;

        EnsureLibVlcInitialized();
        _mediaPlayer = new MediaPlayer(_libVlc!);
    }

    /// <summary>
    /// Ensures both LibVLC and MediaPlayer are initialized.
    /// Used internally when MediaPlayer access is required.
    /// </summary>
    private void EnsureInitialized()
    {
        EnsureLibVlcInitialized();
        EnsureMediaPlayerInitialized();
    }
}
