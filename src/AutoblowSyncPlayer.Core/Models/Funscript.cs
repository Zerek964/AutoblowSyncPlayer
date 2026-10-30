using System.Text.Json.Serialization;

namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Represents a complete funscript file as parsed from JSON.
/// </summary>
public sealed class Funscript
{
    /// <summary>
    /// List of all actions in the script.
    /// </summary>
    [JsonPropertyName("actions")]
    public IReadOnlyList<FunscriptAction> Actions { get; init; } = Array.Empty<FunscriptAction>();

    /// <summary>
    /// Optional: device type the script was created for.
    /// </summary>
    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; init; }

    /// <summary>
    /// Optional: human-readable name.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>
    /// Optional: description text.
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>
    /// Optional: tags array.
    /// </summary>
    [JsonPropertyName("tags")]
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>
    /// Total duration of the script in milliseconds (derived from the last action timestamp).
    /// </summary>
    public long DurationMs => Actions.Count > 0 ? Actions[^1].At : 0;

    /// <summary>
    /// Number of actions in the script.
    /// </summary>
    public int ActionCount => Actions.Count;
}
