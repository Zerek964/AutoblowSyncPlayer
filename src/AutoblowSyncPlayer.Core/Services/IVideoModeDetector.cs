using AutoblowSyncPlayer.Core.Models;

namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Detects the video mode (Standard2D, Flat3D, VR180, VR360) for a given video file.
/// </summary>
public interface IVideoModeDetector
{
    /// <summary>
    /// Detects the video mode for the specified video file.
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Video metadata containing the detected mode and confidence.</returns>
    Task<VideoMetadata> DetectModeAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Detects the video mode with a fallback mode when detection confidence is low.
    /// </summary>
    /// <param name="filePath">Path to the video file.</param>
    /// <param name="fallbackMode">Mode to use if detection confidence is below threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Video metadata containing the detected or fallback mode.</returns>
    Task<VideoMetadata> GetModeWithFallbackAsync(string filePath, VideoMode fallbackMode, CancellationToken cancellationToken = default);
}
