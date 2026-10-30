namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Metadata discovered during video mode detection.
/// </summary>
/// <param name="FilePath">Path to the video file.</param>
/// <param name="DetectedMode">Video mode detected from file analysis.</param>
/// <param name="Confidence">Detection confidence level (0.0–1.0).</param>
/// <param name="Width">Video width in pixels.</param>
/// <param name="Height">Video height in pixels.</param>
/// <param name="DurationMs">Video duration in milliseconds.</param>
/// <param name="HasAudio">Whether the video contains audio tracks.</param>
/// <param name="FrameRate">Frames per second of the video.</param>
public record VideoMetadata(
    string FilePath,
    VideoMode DetectedMode,
    double Confidence,
    int Width,
    int Height,
    long DurationMs,
    bool HasAudio,
    double FrameRate);
