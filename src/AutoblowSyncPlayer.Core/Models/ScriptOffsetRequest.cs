namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Request body for PUT /autoblow/sync-script/offset
/// </summary>
public sealed class ScriptOffsetRequest
{
    /// <summary>
    /// Offset in milliseconds. Positive means device runs ahead of video.
    /// </summary>
    public long OffsetTimeMs { get; init; }
}
