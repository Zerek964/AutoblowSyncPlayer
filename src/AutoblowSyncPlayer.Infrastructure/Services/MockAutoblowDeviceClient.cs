using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;
using Microsoft.Extensions.Logging;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Mock implementation of IAutoblowDeviceClient for testing without real hardware.
/// Simulates successful device operations.
/// </summary>
public class MockAutoblowDeviceClient : IAutoblowDeviceClient
{
    private string? _deviceToken;
    private bool _connected;
    private bool _scriptLoaded;
    private bool _scriptPlaying;

    public bool HasToken => !string.IsNullOrEmpty(_deviceToken);
    public string? CurrentCluster => _connected ? "mock-cluster.autoblowapi.com" : null;
    public bool IsConnected => _connected;

    public void SetDeviceToken(string deviceToken)
    {
        _deviceToken = deviceToken;
    }

    public Task<DeviceConnectionResult?> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!HasToken)
            return Task.FromResult<DeviceConnectionResult?>(null);

        _connected = true;
        return Task.FromResult<DeviceConnectionResult?>(new DeviceConnectionResult
        {
            IsConnected = true,
            Cluster = "mock-cluster.autoblowapi.com",
            DeviceType = "autoblow-ultra",
            IsSupported = true
        });
    }

    public Task<ScriptUploadResult> UploadScriptAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!_connected)
            return Task.FromResult(new ScriptUploadResult { Success = false, ErrorMessage = "Not connected." });

        _scriptLoaded = false;
        return Task.FromResult(new ScriptUploadResult
        {
            Success = true,
            SyncScriptToken = "mock-sync-token-" + Guid.NewGuid().ToString("N")[..8]
        });
    }

    public Task<bool> LoadScriptAsync(string syncScriptToken, CancellationToken cancellationToken = default)
    {
        if (!_connected) return Task.FromResult(false);

        _scriptLoaded = true;
        return Task.FromResult(true);
    }

    public Task<bool> WaitForScriptLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (!_connected) return Task.FromResult(false);

        // Mock: script is "loaded" after upload, so just set the state
        _scriptLoaded = true;
        return Task.FromResult(true);
    }

    public Task<bool> StartScriptAsync(long startTimeMs, CancellationToken cancellationToken = default)
    {
        if (!_connected || !_scriptLoaded) return Task.FromResult(false);

        _scriptPlaying = true;
        return Task.FromResult(true);
    }

    public Task<bool> StopScriptAsync(CancellationToken cancellationToken = default)
    {
        if (!_connected) return Task.FromResult(false);

        _scriptPlaying = false;
        return Task.FromResult(true);
    }

    public Task<bool> SetOffsetAsync(long offsetMs, CancellationToken cancellationToken = default)
    {
        if (!_connected) return Task.FromResult(false);
        return Task.FromResult(true);
    }

    public Task<bool> SetLoopAsync(bool enableLoop, CancellationToken cancellationToken = default)
    {
        if (!_connected) return Task.FromResult(false);
        return Task.FromResult(true);
    }

    public Task<DeviceStateResponse?> GetDeviceStateAsync(CancellationToken cancellationToken = default)
    {
        if (!_connected) return Task.FromResult<DeviceStateResponse?>(null);

        return Task.FromResult<DeviceStateResponse?>(new DeviceStateResponse
        {
            State = _scriptPlaying ? "SYNC_SCRIPT_PLAYING" : "SYNC_SCRIPT_PAUSED",
            ScriptPositionMs = _scriptPlaying ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() % 10000 : 0
        });
    }

    public Task<string?> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!_connected) return Task.FromResult<string?>(null);
        return Task.FromResult<string?>("{\"mock\":\"device-info\"}");
    }
}
