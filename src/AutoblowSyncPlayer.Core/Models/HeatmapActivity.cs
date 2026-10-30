namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Represents a single time bucket of funscript activity data for heatmap rendering.
/// </summary>
public sealed class HeatmapActivity
{
    /// <summary>
    /// Start time of this bucket in milliseconds.
    /// </summary>
    public long StartMs { get; init; }

    /// <summary>
    /// End time of this bucket in milliseconds (exclusive).
    /// </summary>
    public long EndMs { get; init; }

    /// <summary>
    /// Normalized activity level between 0.0 (no activity) and 1.0 (maximum activity).
    /// </summary>
    public double ActivityLevel { get; init; }
}
