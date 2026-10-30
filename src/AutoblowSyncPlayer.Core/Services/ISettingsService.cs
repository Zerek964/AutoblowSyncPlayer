namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Service for persisting application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets or sets the saved device token.
    /// </summary>
    string? DeviceToken { get; set; }

    /// <summary>
    /// Gets or sets the last connected device cluster.
    /// </summary>
    string? LastDeviceCluster { get; set; }

    /// <summary>
    /// Gets or sets the path to the last loaded video file.
    /// </summary>
    string? LastVideoPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the last loaded Funscript file.
    /// </summary>
    string? LastScriptPath { get; set; }

    /// <summary>
    /// Gets or sets the sync offset in milliseconds.
    /// </summary>
    int SyncOffsetMs { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-detect .funscript files matching video filename.
    /// </summary>
    bool AutoDetectScript { get; set; }

    /// <summary>
    /// Gets or sets whether to show confirmation before uploading script to device.
    /// </summary>
    bool UploadConfirmation { get; set; }

    /// <summary>
    /// Gets or sets whether detailed logging is enabled.
    /// </summary>
    bool LoggingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last window width.
    /// </summary>
    int WindowWidth { get; set; }

    /// <summary>
    /// Gets or sets the last window height.
    /// </summary>
    int WindowHeight { get; set; }

    /// <summary>
    /// Gets or sets whether to resume playback from last position on restart.
    /// </summary>
    bool ResumeLastPosition { get; set; }

    /// <summary>
    /// Gets or sets the device polling interval in milliseconds.
    /// </summary>
    int PollingIntervalMs { get; set; }

    /// <summary>
    /// Gets or sets whether to remember window size on close.
    /// </summary>
    bool RememberWindowSize { get; set; }

    /// <summary>
    /// Gets or sets whether to start minimized to tray.
    /// </summary>
    bool StartMinimized { get; set; }

    /// <summary>
    /// Gets or sets the volume level (0-100).
    /// </summary>
    double Volume { get; set; }

    /// <summary>
    /// Gets or sets the default camera yaw angle in degrees.
    /// </summary>
    float CameraYaw { get; set; }

    /// <summary>
    /// Gets or sets the default camera pitch angle in degrees.
    /// </summary>
    float CameraPitch { get; set; }

    /// <summary>
    /// Gets or sets the default camera roll angle in degrees.
    /// </summary>
    float CameraRoll { get; set; }

    /// <summary>
    /// Gets or sets the default field of view in degrees.
    /// </summary>
    float CameraFov { get; set; }

    /// <summary>
    /// Gets or sets the mouse sensitivity multiplier for camera rotation.
    /// </summary>
    float MouseSensitivity { get; set; }

    /// <summary>
    /// Gets or sets whether to invert the vertical mouse axis for camera pitch.
    /// </summary>
    bool InvertVerticalMouse { get; set; }

    /// <summary>
    /// Gets or sets whether to enable smoothing for camera movement.
    /// </summary>
    bool EnableMovementSmoothing { get; set; }

    /// <summary>
    /// Gets or sets the last used video mode.
    /// </summary>
    Models.VideoMode LastVideoMode { get; set; }

    /// <summary>
    /// Saves all settings to persistent storage.
    /// </summary>
    void Save();
}
