using System.Text.Json.Serialization;

namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Response from PUT /autoblow/sync-script/upload-funscript
/// </summary>
public sealed class ScriptUploadResponse
{
    [JsonPropertyName("syncScriptToken")]
    public string? SyncScriptToken { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }
}
