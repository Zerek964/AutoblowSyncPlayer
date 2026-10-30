using System.Text.Json;
using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;
using Microsoft.Extensions.Logging;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Implementation of <see cref="ISettingsService"/> using JSON file storage.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly string _settingsPath;
    private readonly ILogger<SettingsService> _logger;
    private SettingsModel _settings;

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsService"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public SettingsService(ILogger<SettingsService> logger)
    {
        _logger = logger;
        _settings = new SettingsModel();

        // Determine settings file path: %LOCALAPPDATA%\AutoblowSyncPlayer\settings.json
        string appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AutoblowSyncPlayer");

        _settingsPath = Path.Combine(appDataFolder, "settings.json");
        Load();
    }

    /// <inheritdoc/>
    public string? DeviceToken
    {
        get => _settings.DeviceToken;
        set => _settings.DeviceToken = value;
    }

    /// <inheritdoc/>
    public string? LastDeviceCluster
    {
        get => _settings.LastDeviceCluster;
        set => _settings.LastDeviceCluster = value;
    }

    /// <inheritdoc/>
    public string? LastVideoPath
    {
        get => _settings.LastVideoPath;
        set => _settings.LastVideoPath = value;
    }

    /// <inheritdoc/>
    public string? LastScriptPath
    {
        get => _settings.LastScriptPath;
        set => _settings.LastScriptPath = value;
    }

    /// <inheritdoc/>
    public int SyncOffsetMs
    {
        get => _settings.SyncOffsetMs;
        set => _settings.SyncOffsetMs = value;
    }

    /// <inheritdoc/>
    public bool AutoDetectScript
    {
        get => _settings.AutoDetectScript;
        set => _settings.AutoDetectScript = value;
    }

    /// <inheritdoc/>
    public bool UploadConfirmation
    {
        get => _settings.UploadConfirmation;
        set => _settings.UploadConfirmation = value;
    }

    /// <inheritdoc/>
    public bool LoggingEnabled
    {
        get => _settings.LoggingEnabled;
        set => _settings.LoggingEnabled = value;
    }

    /// <inheritdoc/>
    public int WindowWidth
    {
        get => _settings.WindowWidth;
        set => _settings.WindowWidth = value;
    }

    /// <inheritdoc/>
    public int WindowHeight
    {
        get => _settings.WindowHeight;
        set => _settings.WindowHeight = value;
    }

    /// <inheritdoc/>
    public bool ResumeLastPosition
    {
        get => _settings.ResumeLastPosition;
        set => _settings.ResumeLastPosition = value;
    }

    /// <inheritdoc/>
    public int PollingIntervalMs
    {
        get => _settings.PollingIntervalMs;
        set => _settings.PollingIntervalMs = value;
    }

    /// <inheritdoc/>
    public bool RememberWindowSize
    {
        get => _settings.RememberWindowSize;
        set => _settings.RememberWindowSize = value;
    }

    /// <inheritdoc/>
    public bool StartMinimized
    {
        get => _settings.StartMinimized;
        set => _settings.StartMinimized = value;
    }

    /// <inheritdoc/>
    public double Volume
    {
        get => _settings.Volume;
        set => _settings.Volume = value;
    }

    /// <inheritdoc/>
    public float CameraYaw
    {
        get => _settings.CameraYaw;
        set => _settings.CameraYaw = value;
    }

    /// <inheritdoc/>
    public float CameraPitch
    {
        get => _settings.CameraPitch;
        set => _settings.CameraPitch = value;
    }

    /// <inheritdoc/>
    public float CameraRoll
    {
        get => _settings.CameraRoll;
        set => _settings.CameraRoll = value;
    }

    /// <inheritdoc/>
    public float CameraFov
    {
        get => _settings.CameraFov;
        set => _settings.CameraFov = value;
    }

    /// <inheritdoc/>
    public float MouseSensitivity
    {
        get => _settings.MouseSensitivity;
        set => _settings.MouseSensitivity = value;
    }

    /// <inheritdoc/>
    public bool InvertVerticalMouse
    {
        get => _settings.InvertVerticalMouse;
        set => _settings.InvertVerticalMouse = value;
    }

    /// <inheritdoc/>
    public bool EnableMovementSmoothing
    {
        get => _settings.EnableMovementSmoothing;
        set => _settings.EnableMovementSmoothing = value;
    }

    /// <inheritdoc/>
    public VideoMode LastVideoMode
    {
        get => _settings.LastVideoMode;
        set => _settings.LastVideoMode = value;
    }

    /// <inheritdoc/>
    public void Save()
    {
        try
        {
            string directory = Path.GetDirectoryName(_settingsPath)!;
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            File.WriteAllText(_settingsPath, json);
            _logger.LogInformation("Settings saved to {SettingsPath}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save settings to {SettingsPath}", _settingsPath);
        }
    }

    /// <summary>
    /// Loads settings from persistent storage.
    /// </summary>
    private void Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                _logger.LogInformation("No settings file found at {SettingsPath}, using defaults", _settingsPath);
                return;
            }

            string json = File.ReadAllText(_settingsPath);
            _settings = JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
            _logger.LogInformation("Settings loaded from {SettingsPath}", _settingsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings from {SettingsPath}, using defaults", _settingsPath);
            _settings = new SettingsModel();
        }
    }
}
