using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Detects video mode (Standard2D, VR180, VR360, etc.) using LibVLC metadata
/// and heuristic analysis.
/// </summary>
public class VideoModeDetector : IVideoModeDetector
{
    private readonly ILogger<VideoModeDetector> _logger;

    // Confidence thresholds
    private const float HighConfidence = 0.85f;
    private const float MediumConfidence = 0.6f;

    // Aspect ratio thresholds for detection
    private const float WideAspectRatioMin = 1.7f;  // Near 2:1 suggests SBS or 360°
    private const float TallAspectRatioMax = 0.6f;  // Portrait suggests Over-Under

    // Filename patterns: evaluated in priority order (most specific first).
    // Order matters — GetFilenameHint returns the first match.
    private static readonly (string Pattern, VideoMode Mode, float ConfidenceBoost)[] FilenameHints =
    [
        // VR180 + stereoscopic (most specific)
        ("vr180 sbs", VideoMode.Vr180Sbs, 0.25f),
        ("vr180 side by side", VideoMode.Vr180Sbs, 0.25f),
        ("vr180 over under", VideoMode.Vr180OverUnder, 0.25f),
        ("vr180 ou", VideoMode.Vr180OverUnder, 0.25f),

        // VR360 + stereoscopic
        ("vr360 sbs", VideoMode.Vr360Sbs, 0.25f),
        ("vr360 side by side", VideoMode.Vr360Sbs, 0.25f),
        ("vr360 over under", VideoMode.Vr360OverUnder, 0.25f),
        ("vr360 ou", VideoMode.Vr360OverUnder, 0.25f),

        // VR180 monoscopic
        ("vr180", VideoMode.Vr180Mono, 0.2f),
        ("180vr", VideoMode.Vr180Mono, 0.2f),

        // VR360 monoscopic
        ("vr360", VideoMode.Vr360Mono, 0.2f),
        ("360vr", VideoMode.Vr360Mono, 0.2f),
        ("360 degree", VideoMode.Vr360Mono, 0.15f),

        // Stereoscopic format (without explicit VR type — assume VR360)
        ("sbs", VideoMode.Vr360Sbs, 0.15f),
        ("side by side", VideoMode.Vr360Sbs, 0.15f),
        ("over under", VideoMode.Vr360OverUnder, 0.15f),
        ("ou3d", VideoMode.Vr360OverUnder, 0.15f),

        // Generic 3D (lowest priority — broadest match)
        ("3d", VideoMode.Vr360Mono, 0.1f),
    ];

    public VideoModeDetector(ILogger<VideoModeDetector> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Detects the video mode for the specified video file.
    /// </summary>
    public async Task<VideoMetadata> DetectModeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            var metadata = AnalyzeFile(filePath);
            var detectedMode = DetermineMode(metadata, filePath);
            var confidence = CalculateConfidence(metadata, filePath);

            var result = new VideoMetadata(
                filePath,
                detectedMode,
                confidence,
                metadata.Width,
                metadata.Height,
                metadata.DurationMs,
                metadata.HasAudio,
                metadata.FrameRate
            );

            _logger.LogInformation(
                "Detected mode {Mode} for {FilePath} (confidence: {Confidence:F2}, resolution: {Width}x{Height})",
                result.DetectedMode,
                filePath,
                result.Confidence,
                metadata.Width,
                metadata.Height
            );

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// Detects the video mode with a fallback mode when detection confidence is low.
    /// </summary>
    public async Task<VideoMetadata> GetModeWithFallbackAsync(string filePath, VideoMode fallbackMode, CancellationToken cancellationToken = default)
    {
        var result = await DetectModeAsync(filePath, cancellationToken).ConfigureAwait(false);

        if (result.Confidence < MediumConfidence)
        {
            _logger.LogInformation(
                "Low confidence ({Confidence:F2}) for {FilePath}, falling back to {FallbackMode}",
                result.Confidence,
                filePath,
                fallbackMode
            );

            return new VideoMetadata(
                result.FilePath,
                fallbackMode,
                0.5f,
                result.Width,
                result.Height,
                result.DurationMs,
                result.HasAudio,
                result.FrameRate
            );
        }

        return result;
    }

    #region Private Methods

    /// <summary>
    /// Raw file properties for analysis. Uses LibVLC media parsing + reflection to reliably get video dimensions.
    /// Falls back to filename heuristics when LibVLC detection fails.
    /// </summary>
    private static (int Width, int Height, long DurationMs, bool HasAudio, double FrameRate) AnalyzeFile(string filePath)
    {
        try
        {
            // Ensure LibVLC native libraries can be found (same setup as LibVlcVideoPlayer)
            try
            {
                var libVlcDll = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
                var libVlcPath = Path.Combine(libVlcDll, "libvlc");
                if (Directory.Exists(libVlcPath))
                {
                    Environment.CurrentDirectory = libVlcPath;
                }
            }
            catch
            {
                // Best effort - LibVLC constructor may still work
            }

            // FIX Attempt 7: Use dummy video output to prevent LibVLC from creating
            // a native popup window during analysis. The --vout=dummy flag tells LibVLC
            // to process video internally without rendering to any window.
            using var libVlc = new LibVLC(new[] { "--vout=dummy", "--avcodec-hw=none" });
            using var media = new Media(libVlc, Path.GetFullPath(filePath), FromType.FromPath);

            // Parse media synchronously to populate track information before reading tracks
            // We use a MediaPlayer and briefly play to trigger the Vout event which guarantees dimensions are known
            using var mediaPlayer = new MediaPlayer(libVlc);
            
            var tcs = new TaskCompletionSource<bool>();
            bool voutFired = false;
            
            // Subscribe to Vout event which fires when video output is created (dimensions known)
            mediaPlayer.Vout += (sender, args) =>
            {
                if (!voutFired)
                {
                    voutFired = true;
                    tcs.TrySetResult(true);
                }
            };
            
            mediaPlayer.Media = media;
            mediaPlayer.Play();
            
            // Wait for Vout event (dimensions ready) or timeout after 10 seconds
            var completedTask = Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(10)));
            completedTask.Wait();
            
            // Stop playback immediately - we just needed to trigger dimension extraction
            mediaPlayer.Stop();
            mediaPlayer.Media = null;

            var width = 0;
            var height = 0;
            var durationMs = media.Duration;
            var hasAudio = false;
            var frameRate = 0.0;

            // Get track information from media
            var tracks = media.Tracks;
            if (tracks != null && tracks.Length > 0)
            {
                foreach (var track in tracks)
                {
                    if (track.TrackType == TrackType.Audio)
                    {
                        hasAudio = true;
                    }

                    // Use reflection to access internal Data field for video dimensions
                    if (track.TrackType == TrackType.Video && width == 0)
                    {
                        var videoDimensions = ExtractVideoDimensions(track);
                        if (videoDimensions != null)
                        {
                            width = videoDimensions.Value.Width;
                            height = videoDimensions.Value.Height;
                            frameRate = videoDimensions.Value.FrameRate;
                        }
                    }
                }
            }

            return (width, height, durationMs, hasAudio, frameRate);
        }
        catch
        {
            // If LibVLC fails, return defaults
            return (0, 0, 0, false, 0.0);
        }
    }

    /// <summary>
    /// Extracts video dimensions from a MediaTrack using reflection to access internal fields.
    /// </summary>
    private static (int Width, int Height, double FrameRate)? ExtractVideoDimensions(MediaTrack track)
    {
        try
        {
            // MediaTrack.Data is an internal field of type MediaTrackData
            var dataType = track.GetType().GetField(
                "Data",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            if (dataType == null) return null;

            var dataValue = dataType.GetValue(track);
            if (dataValue == null) return null;

            // MediaTrackData.Video is an internal field of type VideoTrack
            var videoField = dataValue.GetType().GetField(
                "Video",
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic);

            if (videoField == null) return null;

            var videoTrack = videoField.GetValue(dataValue);
            if (videoTrack == null) return null;

            // VideoTrack.Width and VideoTrack.Height are UInt32 fields
            var widthField = videoTrack.GetType().GetField("Width", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var heightField = videoTrack.GetType().GetField("Height", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (widthField == null || heightField == null) return null;

            var widthValue = widthField.GetValue(videoTrack);
            var heightValue = heightField.GetValue(videoTrack);

            var width = widthValue is uint w ? (int)w : 0;
            var height = heightValue is uint h ? (int)h : 0;

            // Extract framerate if available
            var frameRate = 0.0;
            var frameRateNumField = videoTrack.GetType().GetField("FrameRateNum", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            var frameRateDenField = videoTrack.GetType().GetField("FrameRateDen", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

            if (frameRateNumField != null && frameRateDenField != null)
            {
                var numValue = frameRateNumField.GetValue(videoTrack);
                var denValue = frameRateDenField.GetValue(videoTrack);
                
                if (numValue is uint num && denValue is uint den && den > 0)
                {
                    frameRate = (double)num / den;
                }
            }

            return (width, height, frameRate);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Determines the video mode based on file properties and filename hints.
    /// When dimensions are unavailable (detection failed), relies heavily on filename hints.
    /// </summary>
    private static VideoMode DetermineMode(
        (int Width, int Height, long DurationMs, bool HasAudio, double FrameRate) metadata,
        string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();

        // Check filename hints first for suggestive signals
        var filenameHint = GetFilenameHint(fileName);

        // Calculate aspect ratio
        var aspectRatio = metadata.Height > 0
            ? (float)metadata.Width / metadata.Height
            : 1.78f; // Default 16:9
        var hasDimensions = metadata.Width > 0 && metadata.Height > 0;

        // Detection logic (priority order):
        // 1. Strong filename hint (e.g., "vr360" in filename)
        // 2. Aspect ratio + resolution heuristics
        // 3. Default to Standard2D

        // When dimensions are unavailable, trust filename hints more
        if (!hasDimensions)
        {
            if (filenameHint is not null)
            {
                return filenameHint.Value;
            }
            // No dimensions and no hint — check for common VR patterns in filename
            if (fileName.Contains("vr") || fileName.Contains("360") || fileName.Contains("3d"))
            {
                return VideoMode.Vr360Mono;
            }
            return VideoMode.Standard2D;
        }

        // VR180 detection: filename hint (checked FIRST because it's more specific than VR360)
        if (filenameHint is not null && IsVr180Mode(filenameHint.Value))
        {
            return filenameHint.Value;
        }

        // VR360 detection: wide videos with filename hint
        if (filenameHint is not null && aspectRatio >= WideAspectRatioMin)
        {
            if (filenameHint.Value == VideoMode.Vr360Mono || filenameHint.Value == VideoMode.Vr360Sbs)
            {
                return filenameHint.Value;
            }
        }

        // SBS detection: very wide aspect ratio (~2:1) without VR hint
        if (aspectRatio >= 1.9f && aspectRatio <= 2.1f && metadata.Width >= 1920)
        {
            // Check for VR180 in filename first (more specific than VR360)
            if (fileName.Contains("vr180") || fileName.Contains("180vr"))
            {
                return VideoMode.Vr180Sbs;
            }
            // Could be SBS VR360 content
            if (fileName.Contains("360"))
            {
                return VideoMode.Vr360Sbs;
            }
            // Generic "vr" without specific type - default to VR360 (more common)
            if (fileName.Contains("vr"))
            {
                return VideoMode.Vr360Sbs;
            }
            // Equirectangular 2:1 is also VR360 mono (very common format: 3840x1920, 4096x2048)
            if (fileName.Contains("3d"))
            {
                return VideoMode.Flat3dSbsLeft;
            }
            return VideoMode.Vr360Mono;
        }

        // Over-Under detection: tall aspect ratio (~0.5)
        if (aspectRatio <= 0.55f && aspectRatio >= 0.45f)
        {
            // Check for VR180 in filename first
            if (fileName.Contains("vr180") || fileName.Contains("180vr"))
            {
                return VideoMode.Vr180OverUnder;
            }
            if (fileName.Contains("360"))
            {
                return VideoMode.Vr360OverUnder;
            }
            // Generic "vr" without specific type - default to VR360 (more common)
            if (fileName.Contains("vr"))
            {
                return VideoMode.Vr360OverUnder;
            }
            return VideoMode.Flat3dOverUnderLeft;
        }

        // Wide video with VR hint but normal SBS ratio - could be Vr360Mono equirectangular
        if (filenameHint == VideoMode.Vr360Mono && aspectRatio >= 1.9f)
        {
            return VideoMode.Vr360Mono;
        }

        // Any remaining filename hint — trust it over the default
        if (filenameHint is not null && filenameHint.Value != VideoMode.Standard2D)
        {
            return filenameHint.Value;
        }

        // Default: standard 2D
        return VideoMode.Standard2D;
    }

    /// <summary>
    /// Calculates confidence score for the detected mode.
    /// </summary>
    private static float CalculateConfidence(
        (int Width, int Height, long DurationMs, bool HasAudio, double FrameRate) metadata,
        string filePath)
    {
        var fileName = Path.GetFileNameWithoutExtension(filePath).ToLowerInvariant();
        var confidence = 0.3f; // Base confidence

        // Boost for filename hints
        var hint = GetFilenameHint(fileName);
        if (hint is not null)
        {
            confidence += 0.3f;
        }

        // Boost for matching aspect ratio
        var aspectRatio = metadata.Height > 0
            ? (float)metadata.Width / metadata.Height
            : 1.78f;

        if (hint.HasValue && aspectRatio >= WideAspectRatioMin)
        {
            confidence += 0.2f;
        }

        // Boost for having valid metadata from LibVLC
        if (metadata.Width > 0 && metadata.Height > 0)
        {
            confidence += 0.1f;
        }

        // Cap at 0.95 (never 100% - user can always override)
        return Math.Min(confidence, 0.95f);
    }

    /// <summary>
    /// Checks filename for known VR/3D patterns. Patterns are evaluated in priority order (most specific first).
    /// </summary>
    private static VideoMode? GetFilenameHint(string fileName)
    {
        foreach (var (pattern, mode, _) in FilenameHints)
        {
            if (fileName.Contains(pattern))
            {
                return mode;
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if the given video mode is a VR180 variant.
    /// </summary>
    private static bool IsVr180Mode(VideoMode mode)
    {
        return mode is VideoMode.Vr180Mono or VideoMode.Vr180Sbs or VideoMode.Vr180OverUnder or VideoMode.Vr180Fisheye;
    }

    #endregion
}
