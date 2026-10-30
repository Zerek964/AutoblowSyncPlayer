using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Calculates funscript activity data for heatmap visualization.
/// Uses speed-based activity calculation: |pos2-pos1| / (at2-at1) per time bucket.
/// </summary>
public sealed class HeatmapCalculator : IHeatmapCalculator
{
    private const int DefaultBucketCount = 1000;

    /// <inheritdoc />
    public IReadOnlyList<HeatmapActivity> CalculateActivities(Funscript funscript, long videoDurationMs, int bucketCount)
    {
        if (funscript == null)
            throw new ArgumentNullException(nameof(funscript));

        if (videoDurationMs <= 0)
            return Array.Empty<HeatmapActivity>();

        if (funscript.Actions.Count == 0)
            return Array.Empty<HeatmapActivity>();

        // Clamp bucket count to reasonable range
        bucketCount = Math.Max(1, Math.Min(bucketCount, 2000));

        long bucketDuration = videoDurationMs / bucketCount;

        // Raw accumulated speed per bucket
        var rawSpeeds = new double[bucketCount];

        // Calculate speed between consecutive action pairs and distribute across buckets
        for (int i = 1; i < funscript.Actions.Count; i++)
        {
            var prevAction = funscript.Actions[i - 1];
            var currAction = funscript.Actions[i];

            long timeDelta = currAction.At - prevAction.At;
            if (timeDelta <= 0)
                continue;

            // Speed = position change per millisecond
            double speed = Math.Abs(currAction.Pos - prevAction.Pos) / (double)timeDelta;

            // Distribute speed across all buckets this interval spans
            long startBucket = Math.Max(0, prevAction.At / bucketDuration);
            long endBucket = Math.Min(bucketCount - 1, (currAction.At - 1) / bucketDuration);

            for (long b = startBucket; b <= endBucket; b++)
            {
                rawSpeeds[b] += speed;
            }
        }

        // Normalize using 95th percentile clamping
        var maxSpeed = GetPercentile(rawSpeeds, 0.95);
        if (maxSpeed <= 0)
        {
            // All speeds are zero (single action or no movement)
            // Return flat activities
            var emptyActivities = new HeatmapActivity[bucketCount];
            for (int i = 0; i < bucketCount; i++)
            {
                emptyActivities[i] = new HeatmapActivity
                {
                    StartMs = i * bucketDuration,
                    EndMs = (i + 1) * bucketDuration,
                    ActivityLevel = 0.0
                };
            }
            return emptyActivities;
        }

        // Build final activities
        var activities = new HeatmapActivity[bucketCount];
        for (int i = 0; i < bucketCount; i++)
        {
            // Clamp to 95th percentile, then normalize to 0-1
            double normalized = Math.Min(rawSpeeds[i] / maxSpeed, 1.0);
            activities[i] = new HeatmapActivity
            {
                StartMs = i * bucketDuration,
                EndMs = (i + 1) * bucketDuration,
                ActivityLevel = normalized
            };
        }

        return activities;
    }

    /// <inheritdoc />
    public double GetActivityAtPosition(IReadOnlyList<HeatmapActivity> activities, long positionMs)
    {
        if (activities == null || activities.Count == 0)
            return 0.0;

        // Binary search for the bucket containing positionMs
        int left = 0;
        int right = activities.Count - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            var bucket = activities[mid];

            if (positionMs < bucket.StartMs)
            {
                right = mid - 1;
            }
            else if (positionMs >= bucket.EndMs)
            {
                left = mid + 1;
            }
            else
            {
                return bucket.ActivityLevel;
            }
        }

        // Position outside range
        return 0.0;
    }

    /// <summary>
    /// Calculates the percentile value from an array of doubles.
    /// </summary>
    private static double GetPercentile(double[] values, double percentile)
    {
        if (values.Length == 0)
            return 0.0;

        var sorted = new double[values.Length];
        Array.Copy(values, sorted, values.Length);
        Array.Sort(sorted);

        double rank = percentile * (sorted.Length - 1);
        int lower = (int)Math.Floor(rank);
        int upper = (int)Math.Ceiling(rank);

        if (lower == upper)
            return sorted[lower];

        double fraction = rank - lower;
        return sorted[lower] * (1.0 - fraction) + sorted[upper] * fraction;
    }
}
