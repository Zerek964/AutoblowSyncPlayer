namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Supported video modes for standard 2D, flat 3D, VR180, and VR360 content.
/// </summary>
public enum VideoMode
{
    /// <summary>
    /// Standard 2D video (no special projection or stereoscopic content).
    /// </summary>
    Standard2D,

    /// <summary>
    /// Flat 3D side-by-side, left eye on left half.
    /// </summary>
    Flat3dSbsLeft,

    /// <summary>
    /// Flat 3D side-by-side, right eye on left half.
    /// </summary>
    Flat3dSbsRight,

    /// <summary>
    /// Flat 3D over-under (top-bottom), left eye on top half.
    /// </summary>
    Flat3dOverUnderLeft,

    /// <summary>
    /// Flat 3D over-under (top-bottom), right eye on top half.
    /// </summary>
    Flat3dOverUnderRight,

    /// <summary>
    /// VR180 monoscopic (single-eye 180° panorama).
    /// </summary>
    Vr180Mono,

    /// <summary>
    /// VR180 side-by-side stereoscopic.
    /// </summary>
    Vr180Sbs,

    /// <summary>
    /// VR180 over-under (top-bottom) stereoscopic.
    /// </summary>
    Vr180OverUnder,

    /// <summary>
    /// VR360 monoscopic (single-eye 360° panorama).
    /// </summary>
    Vr360Mono,

    /// <summary>
    /// VR360 side-by-side stereoscopic.
    /// </summary>
    Vr360Sbs,

    /// <summary>
    /// VR360 over-under (top-bottom) stereoscopic.
    /// </summary>
    Vr360OverUnder,

    /// <summary>
    /// VR180 fisheye (dual fisheye projection).
    /// </summary>
    Vr180Fisheye,
}
