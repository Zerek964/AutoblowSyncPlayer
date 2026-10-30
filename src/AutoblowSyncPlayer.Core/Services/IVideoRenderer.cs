using AutoblowSyncPlayer.Core.Models;

namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Manages video rendering mode switches (2D ↔ 3D/VR) and viewpoint updates.
/// Responsible for telling the underlying player which projection and stereoscopic mode to use.
/// </summary>
public interface IVideoRenderer
{
    /// <summary>
    /// Gets the currently active video mode.
    /// </summary>
    VideoMode CurrentMode { get; }

    /// <summary>
    /// Gets the currently active stereoscopic eye setting.
    /// </summary>
    StereoEye CurrentStereoEye { get; }

    /// <summary>
    /// Initializes the renderer with the specified video player handle.
    /// </summary>
    /// <param name="playerHandle">Native player handle from the video player service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async initialization.</returns>
    Task InitializeAsync(object playerHandle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Switches the video mode (e.g., from Standard2D to Vr360Mono).
    /// </summary>
    /// <param name="mode">Target video mode.</param>
    /// <param name="stereoEye">Stereo eye setting for stereoscopic modes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SetModeAsync(VideoMode mode, StereoEye stereoEye = StereoEye.Auto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the 360° viewpoint for VR/panoramic video modes.
    /// </summary>
    /// <param name="yaw">Yaw angle in degrees.</param>
    /// <param name="pitch">Pitch angle in degrees.</param>
    /// <param name="roll">Roll angle in degrees.</param>
    /// <param name="fov">Field of view in degrees.</param>
    /// <returns>Task representing the async operation.</returns>
    Task UpdateViewpointAsync(float yaw, float pitch, float roll, float fov);

    /// <summary>
    /// Disposes renderer resources.
    /// </summary>
    /// <returns>Task representing the async disposal.</returns>
    Task DisposeAsync();

    /// <summary>
    /// Event fired when the video mode changes.
    /// </summary>
    event EventHandler<VideoModeChangedEventArgs>? ModeChanged;
}
