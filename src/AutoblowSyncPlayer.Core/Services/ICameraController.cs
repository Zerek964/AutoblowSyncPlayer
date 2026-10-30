namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Controls 360° camera orientation for VR/3D video playback.
/// This controller is architecturally isolated from sync-related functionality.
/// </summary>
public interface ICameraController
{
    /// <summary>
    /// Gets or sets the camera yaw angle in degrees.
    /// </summary>
    float Yaw { get; set; }

    /// <summary>
    /// Gets or sets the camera pitch angle in degrees.
    /// </summary>
    float Pitch { get; set; }

    /// <summary>
    /// Gets or sets the camera roll angle in degrees.
    /// </summary>
    float Roll { get; set; }

    /// <summary>
    /// Gets or sets the field of view in degrees.
    /// </summary>
    float FieldOfView { get; set; }

    /// <summary>
    /// Gets or sets the mouse sensitivity multiplier.
    /// </summary>
    float MouseSensitivity { get; set; }

    /// <summary>
    /// Gets or sets whether to invert vertical mouse input.
    /// </summary>
    bool InvertVertical { get; set; }

    /// <summary>
    /// Sets the camera viewpoint to the specified orientation.
    /// </summary>
    /// <param name="yaw">Yaw angle in degrees.</param>
    /// <param name="pitch">Pitch angle in degrees.</param>
    /// <param name="roll">Roll angle in degrees.</param>
    /// <param name="fov">Field of view in degrees.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetViewpointAsync(float yaw, float pitch, float roll, float fov);

    /// <summary>
    /// Resets the camera to default forward-facing orientation.
    /// </summary>
    /// <returns>Task representing the async operation.</returns>
    Task ResetToDefaultAsync();

    /// <summary>
    /// Applies a mouse delta to rotate the camera.
    /// </summary>
    /// <param name="deltaX">Horizontal mouse delta.</param>
    /// <param name="deltaY">Vertical mouse delta.</param>
    /// <returns>Task representing the async operation.</returns>
    Task HandleMouseDeltaAsync(float deltaX, float deltaY);

    /// <summary>
    /// Applies a wheel delta to zoom the camera.
    /// </summary>
    /// <param name="delta">Mouse wheel delta.</param>
    /// <returns>Task representing the async operation.</returns>
    Task HandleWheelDeltaAsync(float delta);
}
