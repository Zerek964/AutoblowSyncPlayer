using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;
using LibVLCSharp.Shared;
using Microsoft.Extensions.Logging;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Manages video rendering mode switches (2D ↔ 3D/VR) and viewpoint updates.
/// Wraps a LibVLC MediaPlayer handle and applies projection/viewpoint changes.
/// </summary>
public class VideoRenderer : IVideoRenderer
{
    private readonly ILogger<VideoRenderer> _logger;
    private MediaPlayer? _mediaPlayer;

    public VideoMode CurrentMode { get; private set; } = VideoMode.Standard2D;
    public StereoEye CurrentStereoEye { get; private set; } = StereoEye.Auto;

    public event EventHandler<VideoModeChangedEventArgs>? ModeChanged;

    public VideoRenderer(ILogger<VideoRenderer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Initializes the renderer with the LibVLC MediaPlayer handle.
    /// </summary>
    public async Task InitializeAsync(object playerHandle, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (playerHandle is MediaPlayer mediaPlayer)
            {
                _mediaPlayer = mediaPlayer;
                _logger.LogInformation("VideoRenderer initialized with MediaPlayer handle.");
            }
            else
            {
                _logger.LogWarning(
                    "VideoRenderer received invalid player handle of type {Type}. Renderer will be non-functional.",
                    playerHandle?.GetType().Name ?? "null"
                );
            }
        }, cancellationToken);
    }

    /// <summary>
    /// Switches the video mode and applies appropriate LibVLC settings.
    /// </summary>
    public async Task SetModeAsync(VideoMode mode, StereoEye stereoEye = StereoEye.Auto, CancellationToken cancellationToken = default)
    {
        await Task.Run(() =>
        {
            if (mode == CurrentMode && stereoEye == CurrentStereoEye)
            {
                return;
            }

            var previousMode = CurrentMode;
            CurrentMode = mode;
            CurrentStereoEye = stereoEye;

            _logger.LogInformation(
                "Switching video mode: {PreviousMode} → {NewMode} (stereo eye: {StereoEye})",
                previousMode,
                mode,
                stereoEye
            );

            // Apply mode-specific settings to the MediaPlayer
            ApplyModeSettings(mode, stereoEye);

            // Fire mode changed event
            ModeChanged?.Invoke(this, new VideoModeChangedEventArgs(previousMode, mode));
        }, cancellationToken);
    }

    /// <summary>
    /// Updates the 360° viewpoint for VR/panoramic video modes.
    /// </summary>
    public async Task UpdateViewpointAsync(float yaw, float pitch, float roll, float fov)
    {
        await Task.Run(() =>
        {
            // Only apply viewpoint for VR modes
            if (CurrentMode == VideoMode.Standard2D)
            {
                _logger.LogDebug(
                    "Viewpoint update skipped for Standard2D mode (yaw: {Yaw}, pitch: {Pitch})",
                    yaw,
                    pitch
                );
                return;
            }

            _mediaPlayer?.UpdateViewpoint(yaw, pitch, roll, fov, absolute: true);
        });
    }

    /// <summary>
    /// Disposes renderer resources.
    /// </summary>
    public async Task DisposeAsync()
    {
        await Task.Run(() =>
        {
            _mediaPlayer = null;
            CurrentMode = VideoMode.Standard2D;
            CurrentStereoEye = StereoEye.Auto;
            _logger.LogInformation("VideoRenderer disposed.");
        });
    }

    #region Private Methods

    /// <summary>
    /// Applies LibVLC-specific settings based on the video mode.
    /// For VR modes, this enables 360° projection and sets appropriate viewpoint.
    /// </summary>
    private void ApplyModeSettings(VideoMode mode, StereoEye stereoEye)
    {
        if (_mediaPlayer == null)
        {
            return;
        }

        switch (mode)
        {
            // Standard 2D video - no special projection
            case VideoMode.Standard2D:
                // Reset viewpoint to defaults for 2D
                _mediaPlayer.UpdateViewpoint(0, 0, 0, 90, absolute: true);
                break;

            // VR180 modes - equirectangular 180° projection
            case VideoMode.Vr180Mono:
            case VideoMode.Vr180Sbs:
            case VideoMode.Vr180OverUnder:
            case VideoMode.Vr180Fisheye:
                ApplyVRProjection(mode, stereoEye, is180: true);
                break;

            // VR360 modes - full equirectangular 360° projection
            case VideoMode.Vr360Mono:
            case VideoMode.Vr360Sbs:
            case VideoMode.Vr360OverUnder:
                ApplyVRProjection(mode, stereoEye, is180: false);
                break;

            // Flat 3D modes - SBS or Over-Under without VR projection
            case VideoMode.Flat3dSbsLeft:
            case VideoMode.Flat3dSbsRight:
            case VideoMode.Flat3dOverUnderLeft:
            case VideoMode.Flat3dOverUnderRight:
                ApplyFlat3DProjection(mode, stereoEye);
                break;

            default:
                _logger.LogWarning("Unhandled video mode: {Mode}", mode);
                break;
        }
    }

    /// <summary>
    /// Applies VR projection settings for 360° or VR180 content.
    /// </summary>
    private void ApplyVRProjection(VideoMode mode, StereoEye stereoEye, bool is180)
    {
        if (_mediaPlayer == null)
        {
            return;
        }

        // Reset viewpoint to center for VR mode
        var initialYaw = 0f;
        var initialPitch = 0f;
        var initialRoll = 0f;
        var initialFov = is180 ? 90f : 80f; // Slightly wider FOV for 360°

        _mediaPlayer.UpdateViewpoint(initialYaw, initialPitch, initialRoll, initialFov, absolute: true);

        _logger.LogInformation(
            "Applied VR{Degree} projection for mode {Mode}",
            is180 ? "180" : "360",
            mode
        );
    }

    /// <summary>
    /// Applies flat 3D projection settings for SBS or Over-Under without VR.
    /// Flat 3D modes don't need VR projection; LibVLC handles SBS/OU rendering natively.
    /// </summary>
    private void ApplyFlat3DProjection(VideoMode mode, StereoEye stereoEye)
    {
        if (_mediaPlayer == null)
        {
            return;
        }

        // Reset viewpoint to defaults for flat modes (no VR projection needed)
        _mediaPlayer.UpdateViewpoint(0, 0, 0, 90, absolute: true);

        _logger.LogInformation(
            "Applied flat 3D projection for mode {Mode}. LibVLC will render SBS/OU natively.",
            mode
        );
    }

    #endregion
}
