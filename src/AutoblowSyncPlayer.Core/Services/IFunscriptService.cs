using AutoblowSyncPlayer.Core.Models;

namespace AutoblowSyncPlayer.Core.Services;

/// <summary>
/// Service for parsing and validating funscript files.
/// </summary>
public interface IFunscriptService
{
    /// <summary>
    /// Parse and validate a funscript file from the given path.
    /// </summary>
    Task<FunscriptValidationResult> ValidateAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Parse a funscript from raw JSON text.
    /// </summary>
    Task<FunscriptValidationResult> ValidateFromTextAsync(string jsonText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a matching funscript file exists alongside a video file.
    /// </summary>
    string? FindMatchingScriptPath(string videoFilePath);
}
