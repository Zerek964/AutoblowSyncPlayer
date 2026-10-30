namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Represents a validation error found in a funscript file.
/// </summary>
public sealed class FunscriptValidationError
{
    public string Message { get; init; } = string.Empty;
    public bool IsCritical { get; init; }
}
