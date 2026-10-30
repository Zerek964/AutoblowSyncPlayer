using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;

namespace AutoblowSyncPlayer.App.ViewModels;

/// <summary>
/// Main window view model - manages video playback, funscript loading, device connection, and sync state.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IVideoPlayerService _videoPlayer;
    private readonly IFunscriptService _funscriptService;
    private readonly IAutoblowDeviceClient _deviceClient;
    private readonly ISyncService _syncService;
    private readonly ICredentialStorage _credentialStorage;
    private readonly ISettingsService _settingsService;
    private readonly IHeatmapCalculator _heatmapCalculator;
    private readonly ILogger<MainViewModel> _logger;

    #region Observable Properties

    [ObservableProperty]
    private string _statusMessage = "Ready. Load a video to begin.";

    [ObservableProperty]
    private bool _isVideoLoaded;

    [ObservableProperty]
    private bool _isScriptValid;

    [ObservableProperty]
    private bool _isDeviceConnected;

    [ObservableProperty]
    private bool _isPlaying;

    [ObservableProperty]
    private double _videoProgress;

    [ObservableProperty]
    private string _videoFileName = string.Empty;

    [ObservableProperty]
    private string _scriptFileName = string.Empty;

    [ObservableProperty]
    private string _scriptTitle = string.Empty;

    [ObservableProperty]
    private string _scriptDescription = string.Empty;

    [ObservableProperty]
    private string _scriptDuration = "00:00:00";

    [ObservableProperty]
    private int _scriptActionCount = 0;

    [ObservableProperty]
    private string _scriptDeviceType = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _syncStatus = "Idle";

    private long _syncOffsetMs;
    private bool _videoProgressUpdatingProgrammatically = false;
    private volatile bool _isSeekInProgress = false; // Blocks OnVideoPositionChanged while seek executes. Volatile for cross-thread visibility (LibVLC fires events from internal threads)
    private volatile bool _isStopping = false; // Guard flag to prevent event handlers from executing during Stop. Volatile for cross-thread visibility (LibVLC fires events from internal threads)
    public long SyncOffsetMs
    {
        get => _syncOffsetMs;
        set
        {
            if (_syncOffsetMs != value)
            {
                _syncOffsetMs = value;
                _syncService.SetOffset(value);
                OnPropertyChanged();
            }
        }
    }

    [ObservableProperty]
    private string _deviceToken = string.Empty;

    [ObservableProperty]
    private bool _isConnecting = false;

    [ObservableProperty]
    private string _videoDuration = "00:00:00";

    [ObservableProperty]
    private string _videoPosition = "00:00:00";

    [ObservableProperty]
    private string _deviceInfo = string.Empty;

    [ObservableProperty]
    private string _deviceCluster = string.Empty;

    [ObservableProperty]
    private string _deviceType = string.Empty;

    [ObservableProperty]
    private bool _isScriptUploaded;

    [ObservableProperty]
    private string _scriptUploadStatus = string.Empty;

    [ObservableProperty]
    private bool _isUploadInProgress;

    [ObservableProperty]
    private bool _hasLoadError = false;

    [ObservableProperty]
    private string _loadErrorMessage = string.Empty;

    [ObservableProperty]
    private double _volume = 70;

    partial void OnVolumeChanged(double value)
    {
        _videoPlayer.Volume = (int)value;
    }

    [ObservableProperty]
    private bool _isMuted;

    partial void OnIsMutedChanged(bool value)
    {
        _videoPlayer.IsMuted = value;
    }

    [ObservableProperty]
    private bool _isFullscreen;

    [ObservableProperty]
    private string _videoTimestamp = "00:00:00";

    [ObservableProperty]
    private string _deviceTimestamp = "00:00:00";

    [ObservableProperty]
    private string _syncDifference = "0ms";

    [ObservableProperty]
    private bool _isSyncReady;

    [ObservableProperty]
    private string _scriptDurationWarning = string.Empty;

    [ObservableProperty]
    private IReadOnlyList<Core.Models.HeatmapActivity>? _heatmapActivities;

    [ObservableProperty]
    private bool _hasScript;

    [ObservableProperty]
    private bool _scriptExceedsVideo;

    partial void OnIsScriptValidChanged(bool value)
    {
        if (value && _scriptDurationMs > 0)
        {
            // Check for script vs video duration mismatch using cached duration
            double scriptDurationSec = _scriptDurationMs / 1000.0;
            double videoDurationSec = _videoPlayer.DurationMs / 1000.0;

            if (videoDurationSec > 0 && Math.Abs(scriptDurationSec - videoDurationSec) > 10)
            {
                double diffSec = Math.Abs(scriptDurationSec - videoDurationSec);
                ScriptDurationWarning = $"⚠ Script/video duration difference: {diffSec:F0}s";
            }
            else
            {
                ScriptDurationWarning = string.Empty;
            }
        }
        else
        {
            ScriptDurationWarning = string.Empty;
        }

        UpdateSyncReadiness();
    }

    partial void OnIsDeviceConnectedChanged(bool value)
    {
        UpdateSyncReadiness();
    }

    partial void OnIsScriptUploadedChanged(bool value)
    {
        UpdateSyncReadiness();
    }

    partial void OnIsVideoLoadedChanged(bool value)
    {
        UpdateSyncReadiness();
    }

    private void UpdateSyncReadiness()
    {
        IsSyncReady = IsVideoLoaded && IsScriptValid && IsDeviceConnected && IsScriptUploaded;
    }

    #endregion

    private string? _validatedScriptPath;
    private long _scriptDurationMs = 0;
    private string? _videoPath;

    #region VR3D Properties

    /// <summary>
    /// All available video modes for the ComboBox selector.
    /// </summary>
    public static readonly VideoMode[] AllVideoModes = Enum.GetValues<VideoMode>();

    [ObservableProperty]
    private VideoMode _videoMode = VideoMode.Standard2D;

    partial void OnVideoModeChanged(VideoMode value)
    {
        // Apply mode change when user selects from ComboBox
        _ = ApplyVideoModeAsync(value);
    }

    private async Task ApplyVideoModeAsync(VideoMode mode)
    {
        try
        {
            CameraModeEnabled = mode != VideoMode.Standard2D;
            
            if (_videoRenderer is not null)
            {
                await _videoRenderer.SetModeAsync(mode, StereoEye);
            }
            if (_videoPlayer is not null)
            {
                await _videoPlayer.SetVideoModeAsync(mode, StereoEye);
            }
            
            StatusMessage = $"Video mode changed to {mode}";
            _logger.LogInformation("Video mode changed to {Mode}", mode);
            
            // Update settings
            _settingsService.LastVideoMode = mode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing video mode");
            StatusMessage = $"Video mode change failed: {ex.Message}";
        }
    }

    [ObservableProperty]
    private StereoEye _stereoEye = StereoEye.Auto;

    [ObservableProperty]
    private float _cameraYaw;

    [ObservableProperty]
    private float _cameraPitch;

    [ObservableProperty]
    private float _cameraRoll;

    [ObservableProperty]
    private float _cameraFov = 90f;

    [ObservableProperty]
    private float _mouseSensitivity = 1.0f;

    partial void OnMouseSensitivityChanged(float value)
    {
        if (_cameraController != null)
        {
            _cameraController.MouseSensitivity = value;
        }
    }

    [ObservableProperty]
    private bool _invertVertical;

    partial void OnInvertVerticalChanged(bool value)
    {
        if (_cameraController != null)
        {
            _cameraController.InvertVertical = value;
        }
    }

    [ObservableProperty]
    private bool _cameraModeEnabled;

    partial void OnCameraModeEnabledChanged(bool value)
    {
        if (value && _videoMode == VideoMode.Standard2D)
        {
            // If enabling camera mode but still on Standard2D, suggest switching
            StatusMessage = "Camera mode enabled. Select a VR/3D video mode to interact.";
        }
        else if (!value)
        {
            StatusMessage = "Camera mode disabled.";
        }
    }

    /// <summary>
    /// Formatted string showing current camera view info.
    /// </summary>
    public string CameraViewInfo
    {
        get => $"Yaw: {CameraYaw:F1}° | Pitch: {CameraPitch:F1}° | FOV: {CameraFov:F0}°";
    }

    #endregion

    private readonly ICameraController _cameraController;
    private readonly IVideoModeDetector _videoModeDetector;
    private readonly IVideoRenderer _videoRenderer;

    public MainViewModel(
        IVideoPlayerService videoPlayer,
        IFunscriptService funscriptService,
        IAutoblowDeviceClient deviceClient,
        ISyncService syncService,
        ICredentialStorage credentialStorage,
        ISettingsService settingsService,
        IHeatmapCalculator heatmapCalculator,
        ICameraController cameraController,
        IVideoModeDetector videoModeDetector,
        IVideoRenderer videoRenderer,
        ILogger<MainViewModel> logger)
    {
        _videoPlayer = videoPlayer;
        _funscriptService = funscriptService;
        _deviceClient = deviceClient;
        _syncService = syncService;
        _credentialStorage = credentialStorage;
        _settingsService = settingsService;
        _heatmapCalculator = heatmapCalculator;
        _cameraController = cameraController;
        _videoModeDetector = videoModeDetector;
        _videoRenderer = videoRenderer;
        _logger = logger;

        // Subscribe to video player events
        _videoPlayer.PositionChanged += OnVideoPositionChanged;
        _videoPlayer.PlaybackEnded += OnVideoPlaybackEnded;
        _videoPlayer.IsPlayingChanged += OnIsPlayingChanged;
        _videoPlayer.DurationChanged += OnDurationChanged;

        // Subscribe to sync status changes
        _syncService.SyncStatusChanged += UpdateSyncStatusFromService;

        // Initialize camera controller settings from properties
        _cameraController.MouseSensitivity = MouseSensitivity;
        _cameraController.InvertVertical = InvertVertical;
    }

    /// <summary>
    /// Loads persisted settings into the view model.
    /// </summary>
    /// <param name="settings">Settings service instance.</param>
    public async Task LoadSettings(ISettingsService settings)
    {
        try
        {
            DeviceToken = settings.DeviceToken ?? string.Empty;
            DeviceCluster = settings.LastDeviceCluster ?? string.Empty;
            SyncOffsetMs = (long)settings.SyncOffsetMs;
            Volume = settings.Volume;

            // Load last video path if available
            if (!string.IsNullOrEmpty(settings.LastVideoPath) && File.Exists(settings.LastVideoPath))
            {
                try
                {
                    // Detect mode before loading so stereoscopic options are applied correctly
                    VideoMode detectedMode = VideoMode.Standard2D;
                    try
                    {
                        var metadata = await _videoModeDetector.DetectModeAsync(settings.LastVideoPath);
                        detectedMode = metadata.DetectedMode;
                    }
                    catch
                    {
                        // Fallback to saved mode if detection fails
                        detectedMode = settings.LastVideoMode;
                    }

                    bool loaded = await _videoPlayer.LoadVideoAsync(settings.LastVideoPath, detectedMode);
                    if (loaded)
                    {
                        VideoFileName = Path.GetFileName(settings.LastVideoPath);
                        IsVideoLoaded = true;
                        _videoPath = settings.LastVideoPath;
                        VideoDuration = FormatTimeSpan(_videoPlayer.DurationMs);
                        _logger.LogInformation("Restored last video: {VideoPath}", settings.LastVideoPath);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to restore video - file may be corrupted: {VideoPath}", settings.LastVideoPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error restoring video, clearing path");
                    // Don't set error state - just log and continue without video
                }
            }

            // Load last script path if available
            if (!string.IsNullOrEmpty(settings.LastScriptPath) && File.Exists(settings.LastScriptPath))
            {
                try
                {
                    await ValidateScriptFileAsync(settings.LastScriptPath);
                    _logger.LogInformation("Restored last script: {ScriptPath}", settings.LastScriptPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error restoring script, clearing path");
                    // Don't set error state - just log and continue without script
                }
            }

            _logger.LogInformation("Settings loaded successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings");
            // Set error state so user can see what went wrong
            HasLoadError = true;
            LoadErrorMessage = $"Failed to load previous session: {ex.Message}";
            StatusMessage = "Session restore failed (see details below)";
        }
    }

    /// <summary>
    /// Clears the load error state after user acknowledges it.
    /// </summary>
    [RelayCommand]
    private void ClearLoadError()
    {
        HasLoadError = false;
        LoadErrorMessage = string.Empty;
        StatusMessage = "Ready. Load a video to begin.";
    }

    /// <summary>
    /// Saves current view model state to settings.
    /// </summary>
    /// <param name="settings">Settings service instance.</param>
    public void SaveSettings(ISettingsService settings)
    {
        settings.DeviceToken = DeviceToken;
        settings.LastDeviceCluster = DeviceCluster;
        settings.SyncOffsetMs = (int)SyncOffsetMs; // Cast long to int
        settings.Volume = Volume;

        // Save current video path if loaded
        if (!string.IsNullOrEmpty(_videoPath))
        {
            settings.LastVideoPath = _videoPath;
        }

        // Save current script path if loaded
        if (!string.IsNullOrEmpty(_validatedScriptPath))
        {
            settings.LastScriptPath = _validatedScriptPath;
        }

        _logger.LogInformation("Settings saved");
    }

    private void UpdateSyncStatusFromService(string status)
    {
        SyncStatus = status;
    }

    #region Video Commands

    /// <summary>
    /// Open video file dialog and load the selected video.
    /// </summary>
    [RelayCommand]
    private async Task LoadVideoAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Video Files|*.mp4;*.avi;*.mkv;*.mov;*.wmv|All Files|*.*",
            Title = "Select Video File"
        };

        if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.FileName))
            return;

        await LoadVideoFromPathAsync(dialog.FileName);
    }

    /// <summary>
    /// Load a video from the specified file path (used by drag-and-drop and other code-behind handlers).
    /// </summary>
    /// <param name="path">Full path to the video file.</param>
    public async Task LoadVideoAsync(string path)
    {
        await LoadVideoFromPathAsync(path);
    }

    /// <summary>
    /// Core video loading logic shared by dialog and direct-path calls.
    /// Detects video mode BEFORE loading so stereoscopic options are applied at Media creation time.
    /// </summary>
    /// <param name="path">Full path to the video file.</param>
    private async Task LoadVideoFromPathAsync(string path)
    {
        try
        {
            StatusMessage = $"Loading video: {Path.GetFileName(path)}";
            
            // Detect video mode BEFORE loading so stereoscopic options are applied at Media creation
            VideoMode detectedMode = VideoMode.Standard2D;
            try
            {
                StatusMessage = $"Loading video: {Path.GetFileName(path)} — detecting mode...";
                var metadata = await _videoModeDetector.DetectModeAsync(path);
                detectedMode = metadata.DetectedMode;
                _logger.LogInformation("Pre-detected video mode: {Mode} (confidence: {Confidence:P1})", detectedMode, metadata.Confidence);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mode detection failed before load, falling back to Standard2D");
            }
            
            // Load video with detected mode so stereoscopic options are applied at Media creation
            var loaded = await _videoPlayer.LoadVideoAsync(path, detectedMode);

            if (loaded)
            {
                IsVideoLoaded = true;
                VideoFileName = Path.GetFileName(path);
                _videoPath = path; // Store the full path
                VideoDuration = FormatTimeSpan(_videoPlayer.DurationMs);
                
                // Set the video mode (this updates UI and applies camera/renderer settings)
                CameraModeEnabled = detectedMode != VideoMode.Standard2D;
                VideoMode = detectedMode;
                _settingsService.LastVideoMode = detectedMode;

                // Reset script state for new video
                IsScriptValid = false;
                IsScriptUploaded = false;
                ScriptFileName = string.Empty;
                ScriptTitle = string.Empty;
                ScriptDescription = string.Empty;
                ScriptDuration = "00:00:00";
                ScriptActionCount = 0;
                ScriptDeviceType = string.Empty;
                ValidationMessage = string.Empty;
                ScriptUploadStatus = string.Empty;
                _validatedScriptPath = null;
                HasScript = false;
                HeatmapActivities = null;
                ScriptExceedsVideo = false;

                // Auto-detect matching funscript (only if setting is enabled)
                if (_settingsService.AutoDetectScript)
                {
                    AutoDetectScript(path);
                }
            }
            else
            {
                StatusMessage = "Failed to load video file.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading video");
            StatusMessage = $"Error loading video: {ex.Message}";
        }
    }

    /// <summary>
    /// Start synchronized playback of video and device.
    /// Handles both initial play and resume from pause.
    /// </summary>
    [RelayCommand]
    private async Task PlayAsync()
    {
        if (!IsVideoLoaded || !IsScriptValid || !IsDeviceConnected)
        {
            var reasons = new List<string>();
            if (!IsVideoLoaded) reasons.Add("video not loaded");
            if (!IsScriptValid) reasons.Add("script not validated");
            if (!IsDeviceConnected) reasons.Add("device not connected");
            if (IsScriptValid && !IsScriptUploaded) reasons.Add("script not uploaded to device");
            
            StatusMessage = $"Cannot play: {string.Join(", ", reasons)}. ";
            
            // Offer to upload script if valid but not uploaded
            if (IsScriptValid && IsDeviceConnected && !IsScriptUploaded)
            {
                var result = MessageBox.Show("Script needs to be uploaded to the device. Upload now?",
                    "Upload Required", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    await UploadScriptAsync();
                }
            }
            
            return;
        }

        try
        {
            StatusMessage = "Starting synchronized playback...";
            
            // Get current video position for sync start
            long currentPositionMs = _videoPlayer.PositionMs;
            
            // Start device sync at current position
            var syncSuccess = await _syncService.StartPlaybackAsync(currentPositionMs);
            
            // Start video playback regardless of sync success (video is the master clock)
            await _videoPlayer.PlayAsync();

            if (syncSuccess)
            {
                IsPlaying = true;
                StatusMessage = "Synchronized playback started.";
                _syncService.StartMonitoring();
            }
            else
            {
                StatusMessage = "Video playing, device sync may not be active.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting playback");
            StatusMessage = $"Playback error: {ex.Message}";
        }
    }

    /// <summary>
    /// Pause synchronized playback - pauses both video and device simultaneously.
    /// </summary>
    [RelayCommand]
    private async Task PauseAsync()
    {
        try
        {
            StatusMessage = "Pausing synchronized playback...";
            
            // Pause device first (stops movement)
            await _syncService.PausePlaybackAsync();
            
            // Pause video immediately after
            await _videoPlayer.PauseAsync();
            
            IsPlaying = false;
            StatusMessage = "Synchronized playback paused.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing playback");
            StatusMessage = $"Pause error: {ex.Message}";
        }
    }

    /// <summary>
    /// Stop playback and reset to beginning - stops video, device, and sync monitoring.
    /// </summary>
    [RelayCommand]
    private async Task StopAsync()
       {
        try
        {
            StatusMessage = "Stopping synchronized playback...";
            
            // Set guard flag BEFORE stopping to prevent any event handlers
            // from executing while we're in the middle of the stop sequence.
            // LibVLC can fire events from internal threads that try to
            // Dispatcher.Invoke() on the UI thread, causing a deadlock
            // if the UI thread is blocked waiting for Stop() to complete.
            _isStopping = true;
            
            // Stop sync monitoring first
            _syncService.StopMonitoring();
            
            // Stop the device
            await _syncService.VideoEndedAsync();
            
            // Stop video - LibVLC Stop() can block while draining internal buffers
            // but event handlers are now guarded so they won't deadlock the UI thread
            await _videoPlayer.StopAsync();
            
            IsPlaying = false;
            VideoProgress = 0;
            VideoPosition = "00:00:00";
            VideoTimestamp = "00:00:00";
            SyncDifference = "0ms";
            StatusMessage = "Playback stopped.";
            
            // Clear guard flag after everything is done
            _isStopping = false;
        }
        catch (Exception ex)
        {
            _isStopping = false; // Ensure guard is cleared even on error
            _logger.LogError(ex, "Error stopping playback");
            StatusMessage = $"Stop error: {ex.Message}";
        }
    }

    /// <summary>
    /// Seek to a position in the video.
    /// </summary>
    [RelayCommand]
    private async Task SeekToPositionAsync(double value)
    {
        if (!IsVideoLoaded) return;

        long positionMs = (long)(value * _videoPlayer.DurationMs / 100.0);

        _isSeekInProgress = true;
        try
        {
            await _videoPlayer.SeekToAsync(positionMs);

            // Immediately set the slider to the target position so it reflects
            // where the user clicked.
            _videoProgressUpdatingProgrammatically = true;
            VideoProgress = value;
            _videoProgressUpdatingProgrammatically = false;
            VideoPosition = FormatTimeSpan(positionMs);
            VideoTimestamp = FormatTimeSpan(positionMs);

            // Device seek runs here so the seek guard stays active during the
            // entire operation, preventing re-entrant seeks while LibVLC settles.
            await _syncService.SeekAsync(positionMs, IsPlaying);
        }
        finally
        {
            _isSeekInProgress = false;
        }
    }

    #endregion

    #region Funscript Commands

    /// <summary>
    /// Load a funscript file manually.
    /// </summary>
    [RelayCommand]
    private async Task LoadScriptAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Funscript Files|*.funscript;*.json|All Files|*.*",
            Title = "Select Funscript File"
        };

        if (dialog.ShowDialog() != true || string.IsNullOrEmpty(dialog.FileName))
            return;

        await ValidateScriptFileAsync(dialog.FileName);
    }

    private async void AutoDetectScript(string videoFilePath)
    {
        var scriptPath = _funscriptService.FindMatchingScriptPath(videoFilePath);

        if (!string.IsNullOrEmpty(scriptPath))
        {
            StatusMessage = $"Found matching script: {Path.GetFileName(scriptPath)}";
            await ValidateScriptFileAsync(scriptPath);
        }
        else
        {
            StatusMessage = "Video loaded. No matching funscript found - load one manually.";
        }
    }

    private async Task ValidateScriptFileAsync(string filePath)
    {
        try
        {
            var result = await _funscriptService.ValidateAsync(filePath);

            if (result.IsValid)
            {
                IsScriptValid = true;
                HasScript = true;
                _validatedScriptPath = filePath;
                _scriptDurationMs = result.DurationMs;
                ScriptFileName = Path.GetFileName(filePath);
                ScriptTitle = result.Script?.Name ?? Path.GetFileNameWithoutExtension(filePath);
                ScriptDescription = result.Script?.Description ?? string.Empty;
                ScriptDuration = FormatTimeSpan(result.DurationMs);
                ScriptActionCount = result.Script?.Actions.Count ?? 0;
                ScriptDeviceType = result.Script?.DeviceType ?? "Unknown";
                ValidationMessage = $"✓ Valid script: {result.Script?.Name ?? "Untitled"} ({result.Script?.Actions.Count} actions, {FormatTimeSpan(result.DurationMs)})";
                StatusMessage = $"Script validated: {result.Script?.Name ?? "Untitled"} ({result.Script?.Actions.Count} actions)";
                IsScriptUploaded = false;
                ScriptUploadStatus = string.Empty;

                // Auto-upload script if device is already connected
                if (IsDeviceConnected)
                {
                    _logger.LogInformation("Device connected, auto-uploading newly validated script");
                    await AutoUploadScriptAsync();
                }

                // Calculate heatmap activities
                _logger.LogInformation("[HEATMAP] CalculateActivities check - Script: {HasScript}, VideoDurationMs: {DurationMs}", result.Script != null, _videoPlayer.DurationMs);
                if (result.Script != null && _videoPlayer.DurationMs > 0)
                {
                    _logger.LogInformation("[HEATMAP] Calculating heatmap with duration {DurationMs}ms", _videoPlayer.DurationMs);
                    var activities = _heatmapCalculator.CalculateActivities(
                        result.Script, _videoPlayer.DurationMs, 1000);
                    HeatmapActivities = activities;
                    _logger.LogInformation("[HEATMAP] Calculated {Count} heatmap activities", activities.Count);

                    // Check if script exceeds video duration
                    ScriptExceedsVideo = result.DurationMs > _videoPlayer.DurationMs;
                }
                else
                {
                    _logger.LogWarning("[HEATMAP] Skipping heatmap calculation - Script: {HasScript}, VideoDurationMs: {DurationMs}", result.Script != null, _videoPlayer.DurationMs);
                    HeatmapActivities = Array.Empty<Core.Models.HeatmapActivity>();
                    ScriptExceedsVideo = false;
                }
            }
            else
            {
                IsScriptValid = false;
                HasScript = false;
                _validatedScriptPath = null;
                _scriptDurationMs = 0;
                HeatmapActivities = null;
                ScriptExceedsVideo = false;
                var criticalErrors = result.Errors.Where(e => e.IsCritical).ToList();
                ValidationMessage = $"✗ Invalid script: {criticalErrors.Count} critical error(s)";
                StatusMessage = $"Script invalid: {string.Join("; ", criticalErrors.Select(e => e.Message))}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating script");
            IsScriptValid = false;
            HasScript = false;
            _validatedScriptPath = null;
            _scriptDurationMs = 0;
            HeatmapActivities = null;
            ScriptExceedsVideo = false;
            StatusMessage = $"Script error: {ex.Message}";
        }
    }

    #endregion

    #region Device Commands

    /// <summary>
    /// Connect to the Autoblow device using saved or entered token.
    /// </summary>
    [RelayCommand]
    private async Task ConnectDeviceAsync()
    {
        try
        {
            IsConnecting = true;
            StatusMessage = "Connecting to device...";

            // Use saved token if available
            var token = DeviceToken.Trim();
            if (string.IsNullOrEmpty(token))
            {
                token = _credentialStorage.GetDeviceToken() ?? string.Empty;
                DeviceToken = token;
            }

            if (string.IsNullOrEmpty(token))
            {
                StatusMessage = "No device token provided.";
                MessageBox.Show("Please enter your device token.", "Token Required", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _deviceClient.SetDeviceToken(token);
            var connectionResult = await _deviceClient.CheckConnectionAsync();

            if (connectionResult != null && connectionResult.IsConnected)
            {
                IsDeviceConnected = true;
                DeviceType = connectionResult.DeviceType ?? "Unknown";
                DeviceCluster = connectionResult.Cluster ?? "Unknown";
                DeviceInfo = $"{DeviceType} - Cluster: {DeviceCluster}";
                _credentialStorage.SaveDeviceToken(token);
                StatusMessage = $"Connected to {DeviceType} in cluster: {DeviceCluster}";
                
                // Auto-upload script if one is validated
                await AutoUploadScriptAsync();
                
                MessageBox.Show($"Successfully connected to device!\n\nDevice: {DeviceType}\nCluster: {DeviceCluster}",
                    "Connection Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                IsDeviceConnected = false;
                StatusMessage = "Failed to connect to device.";
                MessageBox.Show("Could not connect to the device. Check your token and network connection.",
                    "Connection Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error connecting to device");
            IsDeviceConnected = false;
            StatusMessage = $"Connection error: {ex.Message}";
            MessageBox.Show($"Connection error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsConnecting = false;
        }
    }

    /// <summary>
    /// Disconnect from the device.
    /// </summary>
    [RelayCommand]
    private async Task DisconnectDeviceAsync()
    {
        try
        {
            await _deviceClient.StopScriptAsync();
            _syncService.StopMonitoring();
            IsDeviceConnected = false;
            IsScriptUploaded = false;
            DeviceInfo = string.Empty;
            DeviceCluster = string.Empty;
            DeviceType = string.Empty;
            ScriptUploadStatus = string.Empty;
            StatusMessage = "Device disconnected.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting");
            StatusMessage = $"Disconnect error: {ex.Message}";
        }
    }

    /// <summary>
    /// Save the device token to both secure DPAPI storage and settings.json.
    /// </summary>
    [RelayCommand]
    private void SaveTokenAsync()
    {
        if (!string.IsNullOrEmpty(DeviceToken))
        {
            _credentialStorage.SaveDeviceToken(DeviceToken);
            _settingsService.DeviceToken = DeviceToken;
            _settingsService.Save();
            _logger.LogInformation("Device token saved to credential storage and settings file");
            MessageBox.Show("Device token saved securely.", "Token Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    /// <summary>
    /// Delete the saved device token.
    /// </summary>
    [RelayCommand]
    private void DeleteTokenAsync()
    {
        _credentialStorage.DeleteDeviceToken();
        DeviceToken = string.Empty;
        MessageBox.Show("Device token deleted.", "Token Deleted", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    #endregion

    #region Script Upload Commands

    /// <summary>
    /// Upload the validated script to the connected device.
    /// </summary>
    [RelayCommand]
    private async Task UploadScriptAsync()
    {
        if (IsUploadInProgress)
        {
            MessageBox.Show("An upload operation is already in progress. Please wait for it to complete.",
                "Upload In Progress", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        if (!IsDeviceConnected)
        {
            StatusMessage = "Cannot upload: device not connected.";
            MessageBox.Show("Please connect to the device first.", "Device Required",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (string.IsNullOrEmpty(_validatedScriptPath))
        {
            StatusMessage = "No validated script to upload.";
            return;
        }

        IsUploadInProgress = true;
        try
        {
            ScriptUploadStatus = "Uploading script to device...";
            StatusMessage = $"Uploading {ScriptFileName} to device...";
            _logger.LogInformation("Uploading script: {ScriptPath}", _validatedScriptPath);

            var uploadResult = await _deviceClient.UploadScriptAsync(_validatedScriptPath);

            if (uploadResult.Success && !string.IsNullOrEmpty(uploadResult.SyncScriptToken))
            {
                // After upload, poll device state until script is loaded
                // Per API contract: "After upload, poll /state until SYNC_SCRIPT_LOADED or SYNC_SCRIPT_PAUSED"
                ScriptUploadStatus = "Waiting for device to process script...";
                StatusMessage = "Device is processing the uploaded script...";
                var loadSuccess = await _deviceClient.WaitForScriptLoadedAsync();

                if (loadSuccess)
                {
                    IsScriptUploaded = true;
                    ScriptUploadStatus = $"✓ Uploaded and loaded: {ScriptFileName}";
                    StatusMessage = $"Script '{ScriptTitle}' ready for playback.";
                    var token = uploadResult.SyncScriptToken;
                    _logger.LogInformation("Script uploaded and loaded successfully, token: {SyncScriptToken}", token);
                    MessageBox.Show($"Script '{ScriptTitle}' uploaded and loaded successfully.\nReady for synchronized playback.",
                        "Script Ready", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    ScriptUploadStatus = $"⚠ Upload succeeded but timed out waiting for device to load script";
                    StatusMessage = "Script uploaded but device did not load it in time.";
                    MessageBox.Show("The script was uploaded but the device did not load it within the expected time.\n\nThis is usually non-critical — the device may still be processing the script in the background.\n\nYou can try playing the video anyway and the script should sync.",
                        "Script Load Timeout", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                IsScriptUploaded = false;
                ScriptUploadStatus = $"✗ Upload failed: {uploadResult.ErrorMessage ?? "Unknown error"}";
                StatusMessage = $"Script upload failed: {uploadResult.ErrorMessage}";
                _logger.LogError("Script upload failed: {Error}", uploadResult.ErrorMessage);
                MessageBox.Show($"Failed to upload script: {uploadResult.ErrorMessage}",
                    "Upload Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading script");
            IsScriptUploaded = false;
            ScriptUploadStatus = $"✗ Upload error: {ex.Message}";
            StatusMessage = $"Upload error: {ex.Message}";
            MessageBox.Show($"Error uploading script: {ex.Message}", "Upload Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsUploadInProgress = false;
        }
    }

    /// <summary>
    /// Upload and load script automatically (upload + load in one step).
    /// Called when connecting after script is already validated.
    /// </summary>
    private async Task AutoUploadScriptAsync()
    {
        if (!string.IsNullOrEmpty(_validatedScriptPath) && IsDeviceConnected)
        {
            await UploadScriptAsync();
        }
    }

    #endregion

    #region Sync Commands

    /// <summary>
    /// Adjust the sync offset.
    /// </summary>
    [RelayCommand]
    private void AdjustSyncOffsetAsync(double value)
    {
        SyncOffsetMs = (long)(value * 1000);
        _syncService.SetOffset(SyncOffsetMs);
        SyncStatus = $"Offset: {SyncOffsetMs}ms";
    }

    /// <summary>
    /// Restart playback from the beginning.
    /// </summary>
    [RelayCommand]
    private async Task RestartAsync()
    {
        try
        {
            // Stop sync monitoring
            _syncService.StopMonitoring();
            
            // Stop the device
            await _syncService.VideoEndedAsync();
            
            // Stop video and reset position
            await _videoPlayer.StopAsync();
            
            IsPlaying = false;
            VideoProgress = 0;
            VideoPosition = "00:00:00";
            VideoTimestamp = "00:00:00";
            DeviceTimestamp = "00:00:00";
            SyncDifference = "0ms";
            StatusMessage = "Playback restarted from beginning.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restarting playback");
            StatusMessage = $"Restart error: {ex.Message}";
        }
    }

    /// <summary>
    /// Reset sync offset to zero.
    /// </summary>
    [RelayCommand]
    private void ResetOffset()
    {
        SyncOffsetMs = 0;
        _syncService.SetOffset(0);
        SyncStatus = $"Offset: 0ms";
        StatusMessage = "Sync offset reset to 0ms.";
    }

    /// <summary>
    /// Refresh connection status by re-checking device connection.
    /// </summary>
    [RelayCommand]
    private async Task RefreshConnectionAsync()
    {
        if (!IsDeviceConnected || string.IsNullOrEmpty(DeviceToken))
        {
            StatusMessage = "No device connected to refresh.";
            return;
        }

        try
        {
            StatusMessage = "Refreshing connection status...";
            IsConnecting = true;
            
            var connectionResult = await _deviceClient.CheckConnectionAsync();
            
            if (connectionResult != null && connectionResult.IsConnected)
            {
                DeviceCluster = connectionResult.Cluster ?? DeviceCluster;
                DeviceType = connectionResult.DeviceType ?? DeviceType;
                DeviceInfo = $"{DeviceType} - Cluster: {DeviceCluster}";
                StatusMessage = $"Connection refreshed: {DeviceType} in {DeviceCluster}";
            }
            else
            {
                IsDeviceConnected = false;
                StatusMessage = "Device appears to be offline.";
                DeviceInfo = "Offline";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing connection");
            StatusMessage = $"Connection refresh failed: {ex.Message}";
        }
        finally
        {
            IsConnecting = false;
        }
    }

    /// <summary>
    /// Toggle fullscreen mode.
    /// </summary>
    [RelayCommand]
    private void ToggleFullscreen()
    {
        IsFullscreen = !IsFullscreen;
        StatusMessage = IsFullscreen ? "Fullscreen mode enabled" : "Fullscreen mode disabled";
    }

    /// <summary>
    /// Toggle mute state.
    /// </summary>
    [RelayCommand]
    private void ToggleMute()
    {
        IsMuted = !IsMuted;
        StatusMessage = IsMuted ? "Audio muted" : "Audio unmuted";
    }

    /// <summary>
    /// Increase volume.
    /// </summary>
    [RelayCommand]
    private void VolumeUp()
    {
        if (Volume < 100)
        {
            Volume = Math.Min(100, Volume + 10);
        }
    }

    /// <summary>
    /// Decrease volume.
    /// </summary>
    [RelayCommand]
    private void VolumeDown()
    {
        if (Volume > 0)
        {
            Volume = Math.Max(0, Volume - 10);
        }
    }

    /// <summary>
    /// Perform emergency stop.
    /// </summary>
    [RelayCommand]
    private async Task EmergencyStopAsync()
    {
        try
        {
            await _syncService.EmergencyStopAsync();
            await _videoPlayer.StopAsync();
            IsPlaying = false;
            _syncService.StopMonitoring();
            StatusMessage = "Emergency stop executed.";
            MessageBox.Show("All playback and device movement stopped immediately.",
                "Emergency Stop", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during emergency stop");
            StatusMessage = $"Emergency stop error: {ex.Message}";
        }
    }

    /// <summary>
    /// Exit the application.
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        Application.Current.Shutdown();
    }

    /// <summary>
    /// Show the About dialog.
    /// </summary>
    [RelayCommand]
    private void ShowAbout()
    {
        var aboutDialog = new Views.AboutDialog();
        aboutDialog.ShowDialog();
    }

    /// <summary>
    /// Show the Settings dialog.
    /// </summary>
    [RelayCommand]
    private void ShowSettings()
    {
        var settingsDialog = new Views.SettingsDialog(_settingsService);
        settingsDialog.ShowDialog();
    }

    #endregion

    #region VR3D Commands

    /// <summary>
    /// Switch the video mode (e.g., Standard2D → Vr360Mono) and apply to the renderer.
    /// The actual application is handled by OnVideoModeChanged.
    /// </summary>
    [RelayCommand]
    private void SetVideoMode(VideoMode mode)
    {
        VideoMode = mode;
        // OnVideoModeChanged handles applying the mode to renderer/player
    }

    /// <summary>
    /// Switch the stereo eye setting (Auto/Left/Right) for 3D modes.
    /// </summary>
    [RelayCommand]
    private async Task SetStereoEyeAsync(StereoEye eye)
    {
        try
        {
            StereoEye = eye;
            await _videoRenderer.SetModeAsync(VideoMode, eye);
            
            StatusMessage = $"Stereo eye changed to {eye}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing stereo eye");
            StatusMessage = $"Stereo eye change failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Reset the camera to default forward-facing orientation.
    /// </summary>
    [RelayCommand]
    private async Task ResetCameraAsync()
    {
        try
        {
            await _cameraController.ResetToDefaultAsync();
            
            // Update UI properties
            CameraYaw = _cameraController.Yaw;
            CameraPitch = _cameraController.Pitch;
            CameraRoll = _cameraController.Roll;
            CameraFov = _cameraController.FieldOfView;
            
            await _videoPlayer.UpdateViewpointAsync(CameraYaw, CameraPitch, CameraRoll, CameraFov);
            
            StatusMessage = "Camera reset to default view";
            OnPropertyChanged(nameof(CameraViewInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting camera");
            StatusMessage = $"Camera reset failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle mouse drag delta for camera rotation.
    /// </summary>
    public async Task HandleMouseDeltaAsync(double deltaX, double deltaY)
    {
        if (!CameraModeEnabled) return;
        
        try
        {
            await _cameraController.HandleMouseDeltaAsync((float)deltaX, (float)deltaY);
            
            // Update UI properties to reflect new camera state
            CameraYaw = _cameraController.Yaw;
            CameraPitch = _cameraController.Pitch;
            CameraRoll = _cameraController.Roll;
            
            // Push viewpoint to video player
            await _videoPlayer.UpdateViewpointAsync(CameraYaw, CameraPitch, CameraRoll, CameraFov);
            
            OnPropertyChanged(nameof(CameraViewInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling mouse delta");
            StatusMessage = $"Camera movement failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Handle mouse wheel delta for FOV zoom.
    /// </summary>
    public async Task HandleWheelDeltaAsync(double delta)
    {
        if (!CameraModeEnabled) return;
        
        try
        {
            await _cameraController.HandleWheelDeltaAsync((float)delta);
            
            // Update UI properties
            CameraFov = _cameraController.FieldOfView;
            
            // Push viewpoint to video player
            await _videoPlayer.UpdateViewpointAsync(CameraYaw, CameraPitch, CameraRoll, CameraFov);
            
            OnPropertyChanged(nameof(CameraViewInfo));
            StatusMessage = $"FOV: {CameraFov:F0}°";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling wheel delta");
            StatusMessage = $"Zoom failed: {ex.Message}";
        }
    }

    /// <summary>
    /// Detect video mode from the currently loaded video file.
    /// </summary>
    [RelayCommand]
    private async Task DetectVideoModeAsync()
    {
        if (!IsVideoLoaded || string.IsNullOrEmpty(_videoPath))
        {
            StatusMessage = "No video loaded to detect mode.";
            return;
        }

        try
        {
            StatusMessage = "Detecting video mode...";
            var metadata = await _videoModeDetector.DetectModeAsync(_videoPath);
            
            // Setting VideoMode triggers OnVideoModeChanged which applies the mode
            // Setting CameraModeEnabled here (before VideoMode) ensures it's ready when the handler fires
            CameraModeEnabled = metadata.DetectedMode != VideoMode.Standard2D;
            VideoMode = metadata.DetectedMode;
            
            StatusMessage = $"Detected mode: {metadata.DetectedMode} (confidence: {metadata.Confidence:P1})";
            _logger.LogInformation("Auto-detected video mode: {Mode}, confidence: {Confidence:P1}", 
                metadata.DetectedMode, metadata.Confidence);
            
            // Save detected mode to settings
            _settingsService.LastVideoMode = metadata.DetectedMode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting video mode");
            StatusMessage = $"Mode detection failed: {ex.Message}";
        }
    }

    #endregion

    #region Event Handlers

    private void OnVideoPositionChanged(object? sender, long positionMs)
    {
        if (_isStopping) return;
        if (_isSeekInProgress) return;

        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            VideoPosition = FormatTimeSpan(positionMs);
            VideoTimestamp = FormatTimeSpan(positionMs);
            if (_videoPlayer.DurationMs > 0)
            {
                _videoProgressUpdatingProgrammatically = true;
                VideoProgress = (double)positionMs / _videoPlayer.DurationMs * 100;
                _videoProgressUpdatingProgrammatically = false;
            }
            long estimatedDevicePosition = positionMs + SyncOffsetMs;
            SyncDifference = $"{SyncOffsetMs:+#;(#)}ms";
        });
    }

    private async void OnDurationChanged(object? sender, long durationMs)
    {
        if (_isStopping) return;
        
        _logger.LogInformation("[HEATMAP] DurationChanged event fired. Duration: {DurationMs}ms, IsScriptValid: {IsScriptValid}, ScriptPath: {ScriptPath}", durationMs, IsScriptValid, _validatedScriptPath);
        
        if (durationMs <= 0)
        {
            _logger.LogWarning("[HEATMAP] Skipping heatmap recalculation - duration is <= 0");
            return;
        }

        if (!IsScriptValid || string.IsNullOrEmpty(_validatedScriptPath))
        {
            _logger.LogInformation("[HEATMAP] Skipping heatmap recalculation - no valid script loaded");
            return;
        }

        try
        {
            _logger.LogInformation("[HEATMAP] Recalculating heatmap for script: {ScriptPath}", _validatedScriptPath);
            var result = await _funscriptService.ValidateAsync(_validatedScriptPath);
            
            if (result.Script == null)
            {
                _logger.LogError("[HEATMAP] Script validation returned null script during recalculation");
                HeatmapActivities = Array.Empty<Core.Models.HeatmapActivity>();
                return;
            }

            var activities = _heatmapCalculator.CalculateActivities(result.Script, durationMs, 1000);
            _logger.LogInformation("[HEATMAP] Heatmap recalculated successfully. Activities count: {Count}", activities.Count);
            
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                HeatmapActivities = activities;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[HEATMAP] Error recalculating heatmap on duration change");
            HeatmapActivities = Array.Empty<Core.Models.HeatmapActivity>();
        }
    }

    private async void OnVideoPlaybackEnded(object? sender, EventArgs e)
    {
        if (_isStopping) return;
        
        await _syncService.VideoEndedAsync();
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            IsPlaying = false;
            _syncService.StopMonitoring();
            StatusMessage = "Video playback ended.";
        });
    }

    private void OnIsPlayingChanged(object? sender, bool playing)
    {
        if (_isStopping) return;

        // Use BeginInvoke (non-blocking) to post UI updates back to the dispatcher thread.
        // InvokeAsync would block the calling LibVLC thread and can deadlock when the UI thread
        // is busy (e.g., during a seek or heatmap recalculation).
        Application.Current.Dispatcher.BeginInvoke(() =>
        {
            IsPlaying = playing;

            // When video play/pause state changes (via video controls or other means),
            // coordinate with sync service to keep device in sync
            if (playing && IsDeviceConnected && IsScriptUploaded)
            {
                // Video started playing - tell sync service to resume at current position
                _ = SyncVideoStartAsync();
            }
            else if (!playing)
            {
                // Video paused - tell sync service to pause
                _ = SyncVideoPauseAsync();
            }

            StatusMessage = playing ? "Playing..." : "Paused.";
        });
    }
    
    private async Task SyncVideoStartAsync()
    {
        try
        {
            if (!IsDeviceConnected) return;
            
            long currentPos = _videoPlayer.PositionMs;
            var success = await _syncService.StartPlaybackAsync(currentPos);
            
            if (success)
            {
                _syncService.StartMonitoring();
                SyncStatus = _syncService.SyncStatus;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing video start");
        }
    }
    
    private async Task SyncVideoPauseAsync()
    {
        try
        {
            if (!IsDeviceConnected) return;
            
            await _syncService.PausePlaybackAsync();
            SyncStatus = _syncService.SyncStatus;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing video pause");
        }
    }

    #endregion

    #region Property Changed Handlers

    /// <summary>
    /// Handle changes to VideoProgress - when user drags the slider, seek to new position.
    /// </summary>
    partial void OnVideoProgressChanged(double value)
    {
        if (_videoProgressUpdatingProgrammatically) return;
        if (_isSeekInProgress) return;
        if (!IsVideoLoaded) return;

        long targetMs = (long)(value * _videoPlayer.DurationMs / 100.0);
        long currentMs = _videoPlayer.PositionMs;

        if (Math.Abs(targetMs - currentMs) > 1000)
        {
            _ = SeekToPositionAsync(value);
        }
    }

    #endregion

    #region Helpers

    private static string FormatTimeSpan(long milliseconds)
    {
        var ts = TimeSpan.FromMilliseconds(milliseconds);
        return ts.ToString(@"hh\:mm\:ss");
    }

    #endregion
}
