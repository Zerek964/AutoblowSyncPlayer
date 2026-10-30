using System.ComponentModel;

namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Event arguments for video mode changes.
/// </summary>
/// <param name="PreviousMode">The previous video mode.</param>
/// <param name="NewMode">The new video mode.</param>
public sealed class VideoModeChangedEventArgs(VideoMode previousMode, VideoMode newMode) : EventArgs
{
    /// <summary>
    /// Gets the previous video mode.
    /// </summary>
    public VideoMode PreviousMode { get; } = previousMode;

    /// <summary>
    /// Gets the new video mode.
    /// </summary>
    public VideoMode NewMode { get; } = newMode;
}
