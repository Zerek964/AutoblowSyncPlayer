using AutoblowSyncPlayer.Core.Models;

namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Abstraction over the Autoblow HTTP API device client.
/// </summary>
public interface IAutoblowDeviceClient
{
    /// <summary>
    /// Check connection, discover cluster, and verify device type.
    /// Returns null if the device is offline or unsupported.
    /// </summary>
    Task<DeviceConnectionResult?> CheckConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a funscript file to the device.
    /// </summary>
    Task<ScriptUploadResult> UploadScriptAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Load the uploaded script by token on the device.
    /// </summary>
    Task<bool> LoadScriptAsync(string syncScriptToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Wait for the device to finish processing an uploaded script by polling device state.
    /// Returns true when state indicates the script is loaded (SYNC_SCRIPT_LOADED or SYNC_SCRIPT_PAUSED).
    /// </summary>
    Task<bool> WaitForScriptLoadedAsync(CancellationToken cancellationToken = default);

    /// <script>
    /// Start script playback at the specified timestamp (ms).
    /// </script>
    Task<bool> StartScriptAsync(long startTimeMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stop/pause the script on the device.
    /// </summary>
    Task<bool> StopScriptAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the synchronization offset in milliseconds.
    /// </summary>
    Task<bool> SetOffsetAsync(long offsetMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Enable or disable script looping.
    /// </summary>
    Task<bool> SetLoopAsync(bool enableLoop, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current device state.
    /// </summary>
    Task<Core.Models.DeviceStateResponse?> GetDeviceStateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get device info.
    /// </summary>
    Task<string?> GetDeviceInfoAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the device token to use for API calls.
    /// </summary>
    void SetDeviceToken(string deviceToken);

    /// <summary>
    /// Whether a device token has been set.
    /// </summary>
    bool HasToken { get; }

    /// <summary>
    /// The currently discovered API cluster (if any).
    /// </summary>
    string? CurrentCluster { get; }

    /// <summary>
    /// Whether the device is connected and compatible.
    /// </summary>
    bool IsConnected { get; }
}

public sealed class DeviceConnectionResult
{
    public bool IsConnected { get; init; }
    public string? Cluster { get; init; }
    public string? DeviceType { get; init; }
    public bool IsSupported { get; init; }
}

public sealed class ScriptUploadResult
{
    public bool Success { get; init; }
    public string? SyncScriptToken { get; init; }
    public string? ErrorMessage { get; init; }
}
