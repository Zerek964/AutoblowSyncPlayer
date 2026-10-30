using AutoblowSyncPlayer.Core.Models;

namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Abstraction for the video player engine.
/// </summary>
public interface IVideoPlayerService
{
    /// <summary>
    /// Initializes the underlying video engine on the calling thread.
    /// Must be called on the UI thread before binding to a WPF VideoView.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Current playback position in milliseconds.
    /// </summary>
    long PositionMs { get; }

    /// <summary>
    /// Total duration in milliseconds.
    /// </summary>
    long DurationMs { get; }

    /// <summary>
    /// Whether the player is currently playing.
    /// </summary>
    bool IsPlaying { get; }

    /// <summary>
    /// Exposes the underlying native media player object for UI binding.
    /// Returns LibVLC MediaPlayer instance when using LibVlcVideoPlayer.
    /// </summary>
    object? MediaPlayerHandle { get; }

    /// <summary>
    /// Load a video file from the given path.
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <param name="videoMode">Video mode to use for rendering. Stereoscopic options are applied based on this mode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<bool> LoadVideoAsync(string filePath, VideoMode videoMode = VideoMode.Standard2D, CancellationToken cancellationToken = default);

    /// <summary>
    /// Start or resume playback.
    /// </summary>
    Task PlayAsync();

    /// <summary>
    /// Pause playback.
    /// </summary>
    Task PauseAsync();

    /// <summary>
    /// Stop playback and reset to start.
    /// </summary>
    Task StopAsync();

    /// <summary>
    /// Seek to a position in milliseconds.
    /// </summary>
    Task SeekToAsync(long positionMs);

    /// <summary>
    /// Volume level 0-100.
    /// </summary>
    int Volume { get; set; }

    /// <summary>
    /// Whether audio is muted.
    /// </summary>
    bool IsMuted { get; set; }

    /// <summary>
    /// Gets the current video rendering mode (e.g., Standard2D, Vr360Mono).
    /// </summary>
    VideoMode CurrentVideoMode { get; }

    /// <summary>
    /// Gets the current stereoscopic eye setting.
    /// </summary>
    StereoEye CurrentStereoEye { get; }

    /// <summary>
    /// Updates the 360° camera viewpoint for VR/panoramic video modes.
    /// </summary>
    /// <param name="yaw">Yaw angle in degrees.</param>
    /// <param name="pitch">Pitch angle in degrees.</param>
    /// <param name="roll">Roll angle in degrees.</param>
    /// <param name="fov">Field of view in degrees.</param>
    /// <returns>Task representing the async operation.</returns>
    Task UpdateViewpointAsync(float yaw, float pitch, float roll, float fov);

    /// <summary>
    /// Switches the video mode (e.g., from Standard2D to a VR mode).
    /// </summary>
    /// <param name="mode">Target video mode.</param>
    /// <param name="stereoEye">Stereo eye setting for stereoscopic modes.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetVideoModeAsync(VideoMode mode, StereoEye stereoEye = StereoEye.Auto);

    /// <summary>
    /// Event fired when the position changes.
    /// </summary>
    event EventHandler<long>? PositionChanged;

    /// <summary>
    /// Event fired when the duration becomes available or changes (e.g., after media parsing completes).
    /// </summary>
    event EventHandler<long>? DurationChanged;

    /// <summary>
    /// Event fired when playback ends.
    /// </summary>
    event EventHandler? PlaybackEnded;

    /// <summary>
    /// Event fired when play/pause state changes.
    /// </summary>
    event EventHandler<bool>? IsPlayingChanged;

    /// <summary>
    /// Dispose the player and release resources.
    /// </summary>
    void Dispose();
}
