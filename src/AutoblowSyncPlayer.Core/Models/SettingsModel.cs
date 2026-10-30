namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Serializable settings for the application.
/// </summary>
public class SettingsModel
{
    /// <summary>
    /// Gets or sets the saved device token.
    /// </summary>
    public string? DeviceToken { get; set; }

    /// <summary>
    /// Gets or sets the last connected device cluster.
    /// </summary>
    public string? LastDeviceCluster { get; set; }

    /// <summary>
    /// Gets or sets the path to the last loaded video file.
    /// </summary>
    public string? LastVideoPath { get; set; }

    /// <summary>
    /// Gets or sets the path to the last loaded Funscript file.
    /// </summary>
    public string? LastScriptPath { get; set; }

    /// <summary>
    /// Gets or sets the sync offset in milliseconds.
    /// </summary>
    public int SyncOffsetMs { get; set; }

    /// <summary>
    /// Gets or sets whether to auto-detect .funscript files matching video filename.
    /// </summary>
    public bool AutoDetectScript { get; set; }

    /// <summary>
    /// Gets or sets whether to show confirmation before uploading script to device.
    /// </summary>
    public bool UploadConfirmation { get; set; } = true;

    /// <summary>
    /// Gets or sets whether detailed logging is enabled.
    /// </summary>
    public bool LoggingEnabled { get; set; }

    /// <summary>
    /// Gets or sets the last window width.
    /// </summary>
    public int WindowWidth { get; set; } = 1280;

    /// <summary>
    /// Gets or sets the last window height.
    /// </summary>
    public int WindowHeight { get; set; } = 720;

    /// <summary>
    /// Gets or sets whether to resume playback from last position on restart.
    /// </summary>
    public bool ResumeLastPosition { get; set; }

    /// <summary>
    /// Gets or sets the device polling interval in milliseconds.
    /// </summary>
    public int PollingIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets whether to remember window size on close.
    /// </summary>
    public bool RememberWindowSize { get; set; }

    /// <summary>
    /// Gets or sets whether to start minimized to tray.
    /// </summary>
    public bool StartMinimized { get; set; }

    /// <summary>
    /// Gets or sets the volume level (0-100).
    /// </summary>
    public double Volume { get; set; } = 70;

    // -- Camera / 3D/VR Settings --

    /// <summary>
    /// Gets or sets the default camera yaw angle in degrees.
    /// </summary>
    public float CameraYaw { get; set; }

    /// <summary>
    /// Gets or sets the default camera pitch angle in degrees.
    /// </summary>
    public float CameraPitch { get; set; }

    /// <summary>
    /// Gets or sets the default camera roll angle in degrees.
    /// </summary>
    public float CameraRoll { get; set; }

    /// <summary>
    /// Gets or sets the default field of view in degrees.
    /// </summary>
    public float CameraFov { get; set; } = 90f;

    /// <summary>
    /// Gets or sets the mouse sensitivity multiplier for camera rotation.
    /// </summary>
    public float MouseSensitivity { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether to invert the vertical mouse axis for camera pitch.
    /// </summary>
    public bool InvertVerticalMouse { get; set; }

    /// <summary>
    /// Gets or sets whether to enable smoothing for camera movement.
    /// </summary>
    public bool EnableMovementSmoothing { get; set; } = true;

    /// <summary>
    /// Gets or sets the last used video mode (for restoring on app restart).
    /// </summary>
    public VideoMode LastVideoMode { get; set; }
}
