namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Service responsible for synchronizing video playback with the Autoblow device script.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Current synchronization status message.
    /// </summary>
    string SyncStatus { get; }

    /// <summary>
    /// Raised when the synchronization status changes.
    /// </summary>
    event Action<string> SyncStatusChanged;

    /// <summary>
    /// Start synchronized playback from the given video position (ms).
    /// </summary>
    Task<bool> StartPlaybackAsync(long videoPositionMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pause both video and device script.
    /// </summary>
    Task PausePlaybackAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Resume synchronized playback from the current video position.
    /// </summary>
    Task<bool> ResumePlaybackAsync(long videoPositionMs, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle a seek event - synchronize device to new position.
    /// </summary>
    Task SeekAsync(long newVideoPositionMs, bool isPlaying, CancellationToken cancellationToken = default);

    /// <summary>
    /// Handle video ended event - stop device script.
    /// </summary>
    Task VideoEndedAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Emergency stop - immediately stop all device movement.
    /// </summary>
    Task EmergencyStopAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Set the synchronization offset in milliseconds.
    /// </summary>
    void SetOffset(long offsetMs);

    /// <summary>
    /// Current offset in milliseconds.
    /// </summary>
    long OffsetMs { get; }

    /// <summary>
    /// Start the sync service monitoring.
    /// </summary>
    void StartMonitoring();

    /// <summary>
    /// Stop the sync service monitoring.
    /// </summary>
    void StopMonitoring();
}
