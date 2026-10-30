namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Request body for PUT /autoblow/sync-script/start
/// </summary>
public sealed class ScriptStartRequest
{
    /// <summary>
    /// Start position in milliseconds.
    /// </summary>
    public long StartTimeMs { get; init; }
}
