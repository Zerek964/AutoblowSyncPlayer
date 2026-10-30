using AutoblowSyncPlayer.Core.Services;
using Microsoft.Extensions.Logging;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Basic synchronization service implementation.
/// Coordinates video playback with the Autoblow device.
/// </summary>
public class SyncService : ISyncService, IDisposable
{
    private readonly IAutoblowDeviceClient _deviceClient;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<SyncService> _logger;
    private Timer? _monitorTimer;

    private const long DeadbandMs = 2000; // 2 second deadband before adjusting
    private const long CooldownMs = 5000; // 5 second cooldown after offset adjustment to prevent repeated restarts

    public string SyncStatus { get; private set; } = "Idle";
    public long OffsetMs { get; private set; }

    /// <summary>
    /// Raised when the synchronization status changes.
    /// </summary>
    public event Action<string>? SyncStatusChanged;

    private bool _isPlaying;
    private bool _isMonitoring;
    private long _lastVideoPositionMs;
    private DateTime _lastOffsetAdjustment = DateTime.MinValue;

    public SyncService(IAutoblowDeviceClient deviceClient, ISettingsService settingsService, ILogger<SyncService> logger)
    {
        _deviceClient = deviceClient;
        _settingsService = settingsService;
        _logger = logger;
    }

    private int GetMonitorIntervalMs()
    {
        int interval = _settingsService.PollingIntervalMs;
        return Math.Max(1000, Math.Min(interval, 30000)); // Clamp between 1-30 seconds
    }

    private void SetSyncStatus(string status)
    {
        SyncStatus = status;
        SyncStatusChanged?.Invoke(status);
    }

    public async Task<bool> StartPlaybackAsync(long videoPositionMs, CancellationToken cancellationToken = default)
    {
        if (!_deviceClient.IsConnected)
        {
            SetSyncStatus("Error: Device not connected.");
            _logger.LogWarning("Cannot start playback - device not connected.");
            return false;
        }

        _isPlaying = true;
        _lastVideoPositionMs = videoPositionMs;
        SetSyncStatus("Starting playback...");

        var success = await _deviceClient.StartScriptAsync(videoPositionMs, cancellationToken);

        if (success)
        {
            SetSyncStatus($"Playing (offset: {OffsetMs:+0;-0;0}ms)");
            _logger.LogInformation("Started synchronized playback at {PositionMs}ms.", videoPositionMs);
        }
        else
        {
            SetSyncStatus("Error: Failed to start script on device.");
            _isPlaying = false;
            _logger.LogError("Failed to start script playback.");
        }

        return success;
    }

    public async Task PausePlaybackAsync(CancellationToken cancellationToken = default)
    {
        _isPlaying = false;
        SetSyncStatus("Pausing...");

        await _deviceClient.StopScriptAsync(cancellationToken);

        SetSyncStatus($"Paused (offset: {OffsetMs:+0;-0;0}ms)");
        _logger.LogInformation("Paused synchronized playback at {PositionMs}ms.", _lastVideoPositionMs);
    }

    public async Task<bool> ResumePlaybackAsync(long videoPositionMs, CancellationToken cancellationToken = default)
    {
        return await StartPlaybackAsync(videoPositionMs, cancellationToken);
    }

    public async Task SeekAsync(long newVideoPositionMs, bool isPlaying, CancellationToken cancellationToken = default)
    {
        _lastVideoPositionMs = newVideoPositionMs;

        if (isPlaying && _deviceClient.IsConnected)
        {
            SetSyncStatus("Seeking...");
            await _deviceClient.StartScriptAsync(newVideoPositionMs, cancellationToken);
            SetSyncStatus($"Seeked to {newVideoPositionMs}ms");
            _logger.LogInformation("Seeked to {PositionMs}ms during playback.", newVideoPositionMs);
        }
        else
        {
            _logger.LogInformation("Seeked to {PositionMs}ms (paused, device not synced).", newVideoPositionMs);
        }
    }

    public async Task VideoEndedAsync(CancellationToken cancellationToken = default)
    {
        if (!_isPlaying)
        {
            _logger.LogDebug("VideoEndedAsync called but already stopped, ignoring.");
            return;
        }

        _isPlaying = false;
        SetSyncStatus("Video ended - stopping device.");

        await _deviceClient.StopScriptAsync(cancellationToken);

        SetSyncStatus("Stopped");
        _logger.LogInformation("Video ended, device script stopped.");
    }

    public async Task EmergencyStopAsync(CancellationToken cancellationToken = default)
    {
        _isPlaying = false;
        SetSyncStatus("EMERGENCY STOP");

        try
        {
            await _deviceClient.StopScriptAsync(cancellationToken);
            SetSyncStatus("Emergency Stop confirmed");
            _logger.LogWarning("Emergency stop executed.");
        }
        catch (Exception ex)
        {
            SetSyncStatus("WARNING: Emergency Stop may have failed!");
            _logger.LogError(ex, "Emergency stop failed - device may continue movement!");
        }
    }

    public void SetOffset(long offsetMs)
    {
        OffsetMs = offsetMs;
        _logger.LogInformation("Sync offset set to {OffsetMs}ms.", offsetMs);
    }

    public void StartMonitoring()
    {
        if (_isMonitoring) return;

        _isMonitoring = true;
        int interval = GetMonitorIntervalMs();
        _monitorTimer = new Timer(MonitorCallback, null, interval, interval);
        _logger.LogInformation("Started sync monitoring (interval: {IntervalMs}ms, deadband: {DeadbandMs}ms).", interval, DeadbandMs);
    }

    public void StopMonitoring()
    {
        if (!_isMonitoring) return;

        _isMonitoring = false;
        _monitorTimer?.Dispose();
        _monitorTimer = null;
        _logger.LogInformation("Stopped sync monitoring.");
    }

    private async void MonitorCallback(object? senderState)
    {
        if (!_isMonitoring || !_isPlaying) return;

        try
        {
            var deviceState = await _deviceClient.GetDeviceStateAsync();

            if (deviceState is not null && _isPlaying)
            {
                var drift = Math.Abs((deviceState.ScriptPositionMs ?? 0) - _lastVideoPositionMs);

                if (drift > DeadbandMs)
                {
                    // Check cooldown - prevent repeated offset adjustments too frequently
                    var timeSinceLastAdjustment = (DateTime.UtcNow - _lastOffsetAdjustment).TotalMilliseconds;
                    
                    if (timeSinceLastAdjustment >= CooldownMs)
                    {
                        SetSyncStatus($"Drift corrected: {drift}ms (offset: {OffsetMs:+0;-0;0}ms)");
                        _lastOffsetAdjustment = DateTime.UtcNow;
                        _logger.LogDebug("Sync drift corrected: {DriftMs}ms. Cooldown: {CooldownMs}ms.", drift, CooldownMs);
                    }
                    else
                    {
                        var remainingCooldown = CooldownMs - timeSinceLastAdjustment;
                        SetSyncStatus($"Drift: {drift}ms (cooldown: {remainingCooldown:F0}ms)");
                        _logger.LogDebug("Sync drift {DriftMs}ms within cooldown. Remaining: {RemainingMs}ms.", drift, remainingCooldown);
                    }
                }
                else
                {
                    SetSyncStatus($"Sync OK (drift: {drift}ms, offset: {OffsetMs:+0;-0;0}ms)");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during sync monitoring.");
        }
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
