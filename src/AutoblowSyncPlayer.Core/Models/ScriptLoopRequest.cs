namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Request body for PUT /autoblow/sync-script/loop
/// </summary>
public sealed class ScriptLoopRequest
{
    public bool EnableLoop { get; init; }
}
