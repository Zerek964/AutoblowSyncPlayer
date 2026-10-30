using System.Text.Json.Serialization;

namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Response from GET /autoblow/state
/// API returns camelCase: {"operationalMode": "...", "syncScriptCurrentTime": ...}
/// </summary>
public sealed class DeviceStateResponse
{
    [JsonPropertyName("operationalMode")]
    public string? State { get; init; }

    [JsonPropertyName("syncScriptCurrentTime")]
    public long? ScriptPositionMs { get; init; }
}
