namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Represents a single action within a funscript.
/// </summary>
public sealed class FunscriptAction
{
    /// <summary>
    /// Timestamp in milliseconds when this action should execute.
    /// </summary>
    public long At { get; init; }

    /// <summary>
    /// Target position (0-100) for the device.
    /// </summary>
    public int Pos { get; init; }
}
