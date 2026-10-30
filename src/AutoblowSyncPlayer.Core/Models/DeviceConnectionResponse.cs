using System.Text.Json.Serialization;

namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Response from GET /autoblow/connected
/// </summary>
public sealed class DeviceConnectionResponse
{
    [JsonPropertyName("connected")]
    public bool IsConnected { get; init; }

    [JsonPropertyName("cluster")]
    public string? Cluster { get; init; }

    [JsonPropertyName("deviceType")]
    public string? DeviceType { get; init; }
}
