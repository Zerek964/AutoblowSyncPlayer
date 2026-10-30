namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Which eye to render for stereoscopic (3D/VR) content.
/// </summary>
public enum StereoEye
{
    /// <summary>
    /// Automatically choose the eye based on the current video mode.
    /// </summary>
    Auto,

    /// <summary>
    /// Force left eye for stereoscopic content.
    /// </summary>
    Left,

    /// <summary>
    /// Force right eye for stereoscopic content.
    /// </summary>
    Right,
}
