using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Threading.Channels;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Camera controller implementation that manages 360° viewpoint state
/// and forwards changes to the video player.
/// Architecturally isolated from sync-related functionality.
/// </summary>
public class CameraController : ICameraController
{
    private readonly IVideoPlayerService _videoPlayer;
    private readonly ILogger<CameraController> _logger;

    // Viewpoint state
    private float _yaw;
    private float _pitch;
    private float _roll;
    private float _fieldOfView;

    // Configuration
    private float _mouseSensitivity = 1.0f;
    private bool _invertVertical;
    private bool _enableSmoothing = true;

    // Current video mode for angle clamping
    private VideoMode _currentMode = VideoMode.Standard2D;

    // Channel for debouncing viewpoint updates
    private readonly Channel<ViewpointUpdate> _updateChannel;
    private readonly CancellationTokenSource _processingCts;
    private readonly Task _processingTask;

    // Smoothing state
    private float _targetYaw;
    private float _targetPitch;
    private float _targetRoll;
    private float _targetFov;

    private record ViewpointUpdate(float Yaw, float Pitch, float Roll, float Fov);

    public float Yaw
    {
        get => _yaw;
        set
        {
            _yaw = value;
            _targetYaw = value;
            QueueUpdate();
        }
    }

    public float Pitch
    {
        get => _pitch;
        set
        {
            _pitch = value;
            _targetPitch = value;
            QueueUpdate();
        }
    }

    public float Roll
    {
        get => _roll;
        set
        {
            _roll = value;
            _targetRoll = value;
            QueueUpdate();
        }
    }

    public float FieldOfView
    {
        get => _fieldOfView;
        set
        {
            _fieldOfView = value;
            _targetFov = value;
            QueueUpdate();
        }
    }

    public float MouseSensitivity
    {
        get => _mouseSensitivity;
        set => _mouseSensitivity = Math.Clamp(value, 0.1f, 3.0f);
    }

    public bool InvertVertical
    {
        get => _invertVertical;
        set => _invertVertical = value;
    }

    public bool EnableSmoothing
    {
        get => _enableSmoothing;
        set => _enableSmoothing = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CameraController"/> class.
    /// </summary>
    /// <param name="videoPlayer">The video player service for sending viewpoint updates.</param>
    /// <param name="logger">Logger instance.</param>
    public CameraController(IVideoPlayerService videoPlayer, ILogger<CameraController> logger)
    {
        _videoPlayer = videoPlayer;
        _logger = logger;

        // Default FOV
        _fieldOfView = 90f;
        _targetFov = 90f;

        // Create channel for debounced updates (unbounded to avoid backpressure on mouse input)
        _updateChannel = Channel.CreateUnbounded<ViewpointUpdate>(new UnboundedChannelOptions
        {
            SingleReader = true,
        });

        _processingCts = new CancellationTokenSource();
        _processingTask = Task.Run(() => ProcessUpdateLoop(_processingCts.Token));
    }

    /// <summary>
    /// Sets the camera viewpoint to the specified orientation.
    /// </summary>
    public async Task SetViewpointAsync(float yaw, float pitch, float roll, float fov)
    {
        var clampedPitch = ClampPitch(pitch, _currentMode);
        var clampedYaw = ClampYaw(yaw, _currentMode);
        var clampedFov = Math.Clamp(fov, 30f, 120f);

        if (_enableSmoothing)
        {
            _targetYaw = clampedYaw;
            _targetPitch = clampedPitch;
            _targetRoll = roll;
            _targetFov = clampedFov;
        }
        else
        {
            _yaw = clampedYaw;
            _pitch = clampedPitch;
            _roll = roll;
            _fieldOfView = clampedFov;

            await SendViewpointUpdateAsync(clampedYaw, clampedPitch, roll, clampedFov).ConfigureAwait(false);
        }

        QueueUpdate();
    }

    /// <summary>
    /// Resets the camera to default forward-facing orientation (yaw=0, pitch=0, roll=0, fov=90).
    /// </summary>
    public async Task ResetToDefaultAsync()
    {
        if (_enableSmoothing)
        {
            _targetYaw = 0f;
            _targetPitch = 0f;
            _targetRoll = 0f;
            _targetFov = 90f;
        }
        else
        {
            _yaw = 0f;
            _pitch = 0f;
            _roll = 0f;
            _fieldOfView = 90f;

            await SendViewpointUpdateAsync(0f, 0f, 0f, 90f).ConfigureAwait(false);
        }

        QueueUpdate();
        _logger.LogInformation("Camera reset to default viewpoint");
    }

    /// <summary>
    /// Applies a mouse delta to rotate the camera based on sensitivity and invert settings.
    /// </summary>
    public async Task HandleMouseDeltaAsync(float deltaX, float deltaY)
    {
        var sensitivity = _mouseSensitivity;
        var yawDelta = deltaX * sensitivity * 0.5f;
        var pitchDelta = deltaY * sensitivity * 0.5f;

        if (_invertVertical)
        {
            pitchDelta = -pitchDelta;
        }

        var newYaw = _yaw + yawDelta;
        var newPitch = _pitch + pitchDelta;

        // Apply mode-specific clamping
        newPitch = ClampPitch(newPitch, _currentMode);
        newYaw = ClampYaw(newYaw, _currentMode);

        if (_enableSmoothing)
        {
            _targetYaw = newYaw;
            _targetPitch = newPitch;
        }
        else
        {
            _yaw = newYaw;
            _pitch = newPitch;
            await SendViewpointUpdateAsync(_yaw, _pitch, _roll, _fieldOfView).ConfigureAwait(false);
        }

        QueueUpdate();
    }

    /// <summary>
    /// Applies a wheel delta to zoom (change FOV).
    /// </summary>
    public async Task HandleWheelDeltaAsync(float delta)
    {
        const float ZoomSensitivity = 5f;
        var newFov = _fieldOfView - (delta * ZoomSensitivity);
        newFov = Math.Clamp(newFov, 30f, 120f);

        if (_enableSmoothing)
        {
            _targetFov = (float)newFov;
        }
        else
        {
            _fieldOfView = (float)newFov;
            await SendViewpointUpdateAsync(_yaw, _pitch, _roll, _fieldOfView).ConfigureAwait(false);
        }

        QueueUpdate();
    }

    /// <summary>
    /// Sets the current video mode, which affects angle clamping limits.
    /// </summary>
    public void SetVideoMode(VideoMode mode)
    {
        _currentMode = mode;
        _logger.LogDebug("Video mode set to {Mode}, adjusting clamping limits", mode);

        // Clamp current viewpoint to new mode limits
        _pitch = ClampPitch(_pitch, mode);
        _yaw = ClampYaw(_yaw, mode);
        _targetPitch = _pitch;
        _targetYaw = _yaw;
    }

    /// <summary>
    /// Stops the background processing loop and disposes resources.
    /// </summary>
    public void Dispose()
    {
        _processingCts.Cancel();
    }

    #region Private Methods

    private void QueueUpdate()
    {
        _ = _updateChannel.Writer.TryWrite(new ViewpointUpdate(_targetYaw, _targetPitch, _targetRoll, _targetFov));
    }

    private async Task ProcessUpdateLoop(CancellationToken cancellationToken)
    {
        var lastSentTime = DateTimeOffset.Now;
        const int MinUpdateIntervalMs = 16; // ~60 FPS cap on LibVLC updates

        await foreach (var update in _updateChannel.Reader.ReadAllAsync(cancellationToken))
        {
            // Rate-limit updates to LibVLC
            var elapsed = (DateTimeOffset.Now - lastSentTime).TotalMilliseconds;
            if (elapsed < MinUpdateIntervalMs)
            {
                await Task.Delay((int)(MinUpdateIntervalMs - elapsed), cancellationToken).ConfigureAwait(false);
            }

            // Apply smoothing interpolation
            if (_enableSmoothing)
            {
                const float SmoothingFactor = 0.3f;
                _yaw = Lerp(_yaw, _targetYaw, SmoothingFactor);
                _pitch = Lerp(_pitch, _targetPitch, SmoothingFactor);
                _roll = Lerp(_roll, _targetRoll, SmoothingFactor);
                _fieldOfView = Lerp(_fieldOfView, _targetFov, SmoothingFactor);
            }
            else
            {
                _yaw = _targetYaw;
                _pitch = _targetPitch;
                _roll = _targetRoll;
                _fieldOfView = _targetFov;
            }

            await SendViewpointUpdateAsync(_yaw, _pitch, _roll, _fieldOfView).ConfigureAwait(false);
            lastSentTime = DateTimeOffset.Now;
        }
    }

    private async Task SendViewpointUpdateAsync(float yaw, float pitch, float roll, float fov)
    {
        try
        {
            await _videoPlayer.UpdateViewpointAsync(yaw, pitch, roll, fov).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send viewpoint update");
        }
    }

    /// <summary>
    /// Clamps pitch angle based on video mode limits.
    /// VR360: ±90°, VR180: ±90°, Standard2D: 0 (no effect)
    /// </summary>
    private static float ClampPitch(float pitch, VideoMode mode)
    {
        return mode switch
        {
            VideoMode.Standard2D => 0f,
            VideoMode.Vr180Mono or VideoMode.Vr180Sbs or VideoMode.Vr180OverUnder or VideoMode.Vr180Fisheye
                => Math.Clamp(pitch, -90f, 90f),
            VideoMode.Vr360Mono or VideoMode.Vr360Sbs or VideoMode.Vr360OverUnder
                => Math.Clamp(pitch, -90f, 90f),
            _ => Math.Clamp(pitch, -90f, 90f),
        };
    }

    /// <summary>
    /// Clamps yaw angle based on video mode limits.
    /// VR360: unrestricted (normalized to -180..180), VR180: ±90°
    /// </summary>
    private static float ClampYaw(float yaw, VideoMode mode)
    {
        return mode switch
        {
            VideoMode.Standard2D => 0f,
            VideoMode.Vr180Mono or VideoMode.Vr180Sbs or VideoMode.Vr180OverUnder or VideoMode.Vr180Fisheye
                => Math.Clamp(yaw, -90f, 90f),
            VideoMode.Vr360Mono or VideoMode.Vr360Sbs or VideoMode.Vr360OverUnder
                => NormalizeYaw(yaw),
            _ => NormalizeYaw(yaw),
        };
    }

    /// <summary>
    /// Normalizes yaw to the range [-180, 180].
    /// </summary>
    private static float NormalizeYaw(float yaw)
    {
        while (yaw > 180f) yaw -= 360f;
        while (yaw < -180f) yaw += 360f;
        return yaw;
    }

    /// <summary>
    /// Linear interpolation between two values.
    /// </summary>
    private static float Lerp(float start, float end, float t)
    {
        return start + (end - start) * t;
    }

    #endregion
}
