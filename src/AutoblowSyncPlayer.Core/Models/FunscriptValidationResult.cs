namespace AutoblowSyncPlayer.Core.Models;

/// <summary>
/// Result of a funscript validation operation.
/// </summary>
public sealed class FunscriptValidationResult
{
    public bool IsValid { get; init; }
    public IReadOnlyList<FunscriptValidationError> Errors { get; init; } = Array.Empty<FunscriptValidationError>();
    public Funscript? Script { get; init; }
    public long DurationMs => Script?.DurationMs ?? 0;
    public int ActionCount => Script?.ActionCount ?? 0;
}
