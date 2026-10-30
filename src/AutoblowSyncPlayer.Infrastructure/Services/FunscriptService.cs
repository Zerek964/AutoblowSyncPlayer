using System.Text.Json;
using System.Text.Json.Serialization;
using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Implementation of IFunscriptService that parses and validates .funscript JSON files.
/// </summary>
public class FunscriptService : IFunscriptService
{
    private const int MaxActions = 100_000;
    private const long MaxTimestampMs = 16_777_214;
    private const long MaxFileSizeBytes = 60 * 1024 * 1024; // 60 MB

    public async Task<FunscriptValidationResult> ValidateAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            return new FunscriptValidationResult
            {
                IsValid = false,
                Errors = [new FunscriptValidationError { Message = $"File not found: {filePath}", IsCritical = true }]
            };
        }

        var fileLength = new FileInfo(filePath).Length;
        if (fileLength > MaxFileSizeBytes)
        {
            return new FunscriptValidationResult
            {
                IsValid = false,
                Errors = [new FunscriptValidationError { Message = $"File size exceeds {MaxFileSizeBytes / (1024 * 1024)} MB limit.", IsCritical = true }]
            };
        }

        var jsonText = await File.ReadAllTextAsync(filePath, cancellationToken);
        return await ValidateFromTextAsync(jsonText, cancellationToken);
    }

    public async Task<FunscriptValidationResult> ValidateFromTextAsync(string jsonText, CancellationToken cancellationToken = default)
    {
        var errors = new List<FunscriptValidationError>();

        Funscript? script = null;

        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            script = await Task.Run(() => JsonSerializer.Deserialize<Funscript>(jsonText, options), cancellationToken);
        }
        catch (JsonException ex)
        {
            return new FunscriptValidationResult
            {
                IsValid = false,
                Errors = [new FunscriptValidationError { Message = $"Invalid JSON: {ex.Message}", IsCritical = true }]
            };
        }

        if (script is null)
        {
            return new FunscriptValidationResult
            {
                IsValid = false,
                Errors = [new FunscriptValidationError { Message = "Failed to parse funscript.", IsCritical = true }]
            };
        }

        if (script.Actions.Count == 0)
        {
            errors.Add(new FunscriptValidationError { Message = "Script contains no actions.", IsCritical = true });
            return new FunscriptValidationResult { IsValid = false, Errors = errors };
        }

        if (script.Actions.Count > MaxActions)
        {
            errors.Add(new FunscriptValidationError { Message = $"Script exceeds maximum of {MaxActions} actions.", IsCritical = true });
        }

        bool hasUnsorted = false;
        bool hasDuplicateTimestamps = false;
        long? previousTimestamp = null;

        foreach (var action in script.Actions)
        {
            if (action.Pos < 0 || action.Pos > 100)
            {
                errors.Add(new FunscriptValidationError
                {
                    Message = $"Action at {action.At}ms has invalid position {action.Pos} (must be 0-100).",
                    IsCritical = true
                });
            }

            if (action.At < 0)
            {
                errors.Add(new FunscriptValidationError
                {
                    Message = $"Action has negative timestamp {action.At}ms.",
                    IsCritical = true
                });
            }

            if (action.At > MaxTimestampMs)
            {
                errors.Add(new FunscriptValidationError
                {
                    Message = $"Action timestamp {action.At}ms exceeds maximum of {MaxTimestampMs}ms.",
                    IsCritical = true
                });
            }

            if (previousTimestamp.HasValue)
            {
                if (action.At < previousTimestamp.Value)
                {
                    if (!hasUnsorted)
                    {
                        errors.Add(new FunscriptValidationError
                        {
                            Message = "Actions are not sorted by timestamp.",
                            IsCritical = true
                        });
                        hasUnsorted = true;
                    }
                }
                else if (action.At == previousTimestamp.Value)
                {
                    if (!hasDuplicateTimestamps)
                    {
                        errors.Add(new FunscriptValidationError
                        {
                            Message = "Duplicate timestamps detected in actions.",
                            IsCritical = false
                        });
                        hasDuplicateTimestamps = true;
                    }
                }
            }

            previousTimestamp = action.At;
        }

        return new FunscriptValidationResult
        {
            IsValid = errors.Count == 0 || !errors.Any(e => e.IsCritical),
            Errors = errors,
            Script = script
        };
    }

    public string? FindMatchingScriptPath(string videoFilePath)
    {
        if (!File.Exists(videoFilePath))
            return null;

        var directory = Path.GetDirectoryName(videoFilePath);
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(videoFilePath);
        var scriptPath = Path.Combine(directory!, fileNameWithoutExt + ".funscript");

        return File.Exists(scriptPath) ? scriptPath : null;
    }
}
