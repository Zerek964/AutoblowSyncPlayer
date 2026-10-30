using AutoblowSyncPlayer.Core.Models;

namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Service for calculating funscript activity data for heatmap visualization.
/// </summary>
public interface IHeatmapCalculator
{
    /// <summary>
    /// Calculate activity data for a funscript mapped to a video duration.
    /// </summary>
    /// <param name="funscript">The parsed funscript containing actions.</param>
    /// <param name="videoDurationMs">Total video duration in milliseconds.</param>
    /// <param name="bucketCount">Number of time buckets to divide the timeline into.</param>
    /// <returns>Read-only list of activity buckets normalized to 0.0-1.0.</returns>
    IReadOnlyList<HeatmapActivity> CalculateActivities(Funscript funscript, long videoDurationMs, int bucketCount);

    /// <summary>
    /// Get activity level at a specific millisecond position from precomputed activities.
    /// </summary>
    /// <param name="activities">Precomputed activity buckets.</param>
    /// <param name="positionMs">Position in milliseconds.</param>
    /// <returns>Activity level between 0.0 and 1.0, or 0.0 if no activities exist.</returns>
    double GetActivityAtPosition(IReadOnlyList<HeatmapActivity> activities, long positionMs);
}
