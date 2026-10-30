using AutoblowSyncPlayer.Core.Models;
using AutoblowSyncPlayer.Core.Services;
using Microsoft.Extensions.Logging;

namespace AutoblowSyncPlayer.Infrastructure.Services;

/// <summary>
/// Real implementation of the Autoblow HTTP API client.
/// </summary>
public class AutoblowDeviceClient : IAutoblowDeviceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AutoblowDeviceClient> _logger;
    private string? _deviceToken;
    private string? _cluster;
    private bool _isConnected;
    private DateTime _rateLimitReset = DateTime.MinValue;

    private const string BaseUrl = "https://latency.autoblowapi.com";
    private const int TimeoutSeconds = 90;

    public AutoblowDeviceClient(ILogger<AutoblowDeviceClient> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
        };
    }

    public bool HasToken => !string.IsNullOrEmpty(_deviceToken);
    public string? CurrentCluster => _cluster;
    public bool IsConnected => _isConnected;

    public void SetDeviceToken(string deviceToken)
    {
        _deviceToken = deviceToken;
    }

    public async Task<DeviceConnectionResult?> CheckConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!HasToken)
        {
            _logger.LogWarning("Cannot check connection - no device token set.");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/autoblow/connected");
            request.Headers.Add("x-device-token", _deviceToken!);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            // Read rate-limit response headers where available
            HandleRateLimitHeaders(response);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Connection check failed with status {StatusCode}.", response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var connResponse = System.Text.Json.JsonSerializer.Deserialize<DeviceConnectionResponse>(json);

            if (connResponse is null || !connResponse.IsConnected)
            {
                _isConnected = false;
                return new DeviceConnectionResult
                {
                    IsConnected = false,
                    Cluster = connResponse?.Cluster,
                    DeviceType = connResponse?.DeviceType,
                    IsSupported = false
                };
            }

            _cluster = connResponse.Cluster;
            _isConnected = true;
            var isSupported = connResponse.DeviceType is not null &&
                             connResponse.DeviceType.Contains("ultra", System.StringComparison.OrdinalIgnoreCase);

            _logger.LogInformation("Device connected. Type: {DeviceType}, Cluster: {Cluster}.", connResponse.DeviceType, _cluster);

            return new DeviceConnectionResult
            {
                IsConnected = true,
                Cluster = connResponse.Cluster,
                DeviceType = connResponse.DeviceType,
                IsSupported = isSupported
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check device connection.");
            _isConnected = false;
            return null;
        }
    }

    public async Task<ScriptUploadResult> UploadScriptAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!HasToken)
        {
            return new ScriptUploadResult { Success = false, ErrorMessage = "No device token configured." };
        }

        if (_cluster is null)
        {
            return new ScriptUploadResult { Success = false, ErrorMessage = "Cluster not discovered." };
        }

        try
        {
            using var content = new MultipartFormDataContent();
            using var fileContent = new StreamContent(new FileStream(filePath, FileMode.Open, FileAccess.Read));
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            content.Add(fileContent, "file", Path.GetFileName(filePath));

            using var request = new HttpRequestMessage(HttpMethod.Put, $"https://{_cluster}/autoblow/sync-script/upload-funscript")
            {
                Content = content
            };
            request.Headers.Add("x-device-token", _deviceToken!);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                return new ScriptUploadResult
                {
                    Success = false,
                    ErrorMessage = $"Upload failed: {response.StatusCode} - {errorBody}"
                };
            }

            // Read rate-limit response headers
            HandleRateLimitHeaders(response);

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var uploadResponse = System.Text.Json.JsonSerializer.Deserialize<ScriptUploadResponse>(json);

            return new ScriptUploadResult
            {
                Success = true,
                SyncScriptToken = uploadResponse?.SyncScriptToken
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Script upload failed.");
            return new ScriptUploadResult { Success = false, ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Loads an uploaded script by token on the device.
    /// Retries up to 12 times over ~60 seconds as the device needs time to process the upload.
    /// </summary>
    public async Task<bool> LoadScriptAsync(string syncScriptToken, CancellationToken cancellationToken = default)
    {
        if (!HasToken || _cluster is null) return false;

        const int maxRetries = 12;
        const int retryDelayMs = 5000;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var body = System.Text.Json.JsonSerializer.Serialize(new { syncScriptToken });
                using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

                using var request = new HttpRequestMessage(HttpMethod.Put, $"https://{_cluster}/autoblow/sync-script/load-token")
                {
                    Content = content
                };
                request.Headers.Add("x-device-token", _deviceToken!);

                var response = await _httpClient.SendAsync(request, cancellationToken);
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                HandleRateLimitHeaders(response);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Script loaded successfully on attempt {Attempt}", attempt);
                    return true;
                }

                _logger.LogWarning("Load script attempt {Attempt}/{MaxAttempts} failed with status {StatusCode}, body: {Body}. Retrying in {DelayMs}ms...",
                    attempt, maxRetries, response.StatusCode, responseBody, retryDelayMs);

                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelayMs, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Load script attempt {Attempt}/{MaxAttempts} threw exception. Retrying...", attempt, maxRetries);

                if (attempt < maxRetries)
                {
                    await Task.Delay(retryDelayMs, cancellationToken);
                }
            }
        }

        _logger.LogError("Script load failed after {MaxAttempts} attempts.", maxRetries);
        return false;
    }

    /// <summary>
    /// Wait for the device to finish processing an uploaded script by polling device state.
    /// After upload, the device needs time to process the script. This method polls /autoblow/state
    /// until the device reports SYNC_SCRIPT_LOADED or SYNC_SCRIPT_PAUSED, or until timeout.
    /// </summary>
    public async Task<bool> WaitForScriptLoadedAsync(CancellationToken cancellationToken = default)
    {
        if (!HasToken || _cluster is null) return false;

        const int maxAttempts = 120;
        const int pollIntervalMs = 5000;
        const string loadedState = "SYNC_SCRIPT_LOADED";
        const string pausedState = "SYNC_SCRIPT_PAUSED";

        _logger.LogInformation("Beginning script load polling: up to {MaxAttempts} attempts ({TotalMinutes}min total)",
            maxAttempts, maxAttempts * pollIntervalMs / 1000 / 60);

        // Also check syncScriptPositionMs > 0 as a sign the device has started processing
        // Per API contract: "syncScriptPositionMs is 0 before script is uploaded"
        bool sawPositivePosition = false;

        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var state = await GetDeviceStateAsync(cancellationToken);

                if (state is not null)
                {
                    _logger.LogInformation("State poll attempt {Attempt}/{MaxAttempts}: device state is \"{State}\", positionMs={Pos}",
                        attempt, maxAttempts, state.State, state.ScriptPositionMs);

                    if (state.State?.Equals(loadedState, System.StringComparison.OrdinalIgnoreCase) == true ||
                        state.State?.Equals(pausedState, System.StringComparison.OrdinalIgnoreCase) == true)
                    {
                        _logger.LogInformation("Script loaded successfully after {Attempts} polls (matched state string)", attempt);
                        return true;
                    }

                    // Fallback: if device reports a positive script position, it has loaded the script
                    if (state.ScriptPositionMs > 0)
                    {
                        sawPositivePosition = true;
                        _logger.LogInformation("Device reports positive script position ({Pos}ms), script is loaded", state.ScriptPositionMs);
                    }

                    // If we've seen a positive position after upload succeeded, consider it loaded
                    // The device may take a while to transition to SYNC_SCRIPT_LOADED state
                    if (sawPositivePosition && attempt > 2)
                    {
                        _logger.LogInformation("Considering script loaded based on positive position after {Attempts} polls", attempt);
                        return true;
                    }
                }
                else
                {
                    _logger.LogWarning("State poll attempt {Attempt}/{MaxAttempts}: unable to retrieve device state (GetDeviceStateAsync returned null)", attempt, maxAttempts);
                }

                if (attempt < maxAttempts)
                {
                    _logger.LogInformation("Waiting {IntervalMs}ms before next poll...", pollIntervalMs);
                    await Task.Delay(pollIntervalMs, cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "State poll attempt {Attempt}/{MaxAttempts} threw exception. Retrying...", attempt, maxAttempts);

                if (attempt < maxAttempts)
                {
                    await Task.Delay(pollIntervalMs, cancellationToken);
                }
            }
        }

        _logger.LogError("Script load wait failed after {MaxAttempts} state polls ({TotalSeconds}s). The script may still be processing on the device.",
            maxAttempts, maxAttempts * pollIntervalMs / 1000);
        return false;
    }

    public async Task<bool> StartScriptAsync(long startTimeMs, CancellationToken cancellationToken = default)
    {
        if (!HasToken || _cluster is null) return false;

        try
        {
            var body = System.Text.Json.JsonSerializer.Serialize(new { startTimeMs });
            using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Put, $"https://{_cluster}/autoblow/sync-script/start")
            {
                Content = content
            };
            request.Headers.Add("x-device-token", _deviceToken!);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            // Read rate-limit response headers
            HandleRateLimitHeaders(response);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start script failed.");
            return false;
        }
    }

    public async Task<bool> StopScriptAsync(CancellationToken cancellationToken = default)
    {
        if (!HasToken || _cluster is null) return false;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Put, $"https://{_cluster}/autoblow/sync-script/stop");
            request.Headers.Add("x-device-token", _deviceToken!);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            // Read rate-limit response headers
            HandleRateLimitHeaders(response);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stop script failed.");
            return false;
        }
    }

    public async Task<bool> SetOffsetAsync(long offsetMs, CancellationToken cancellationToken = default)
    {
        if (!HasToken || _cluster is null) return false;

        try
        {
            var body = System.Text.Json.JsonSerializer.Serialize(new { offsetTimeMs = offsetMs });
            using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Put, $"https://{_cluster}/autoblow/sync-script/offset")
            {
                Content = content
            };
            request.Headers.Add("x-device-token", _deviceToken!);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            // Read rate-limit response headers
            HandleRateLimitHeaders(response);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set offset failed.");
            return false;
        }
    }

    public async Task<bool> SetLoopAsync(bool enableLoop, CancellationToken cancellationToken = default)
    {
        if (!HasToken || _cluster is null) return false;

        try
        {
            var body = System.Text.Json.JsonSerializer.Serialize(new { enableLoop });
            using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Put, $"https://{_cluster}/autoblow/sync-script/loop")
            {
                Content = content
            };
            request.Headers.Add("x-device-token", _deviceToken!);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            // Read rate-limit response headers
            HandleRateLimitHeaders(response);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set loop failed.");
            return false;
        }
    }

    public async Task<DeviceStateResponse?> GetDeviceStateAsync(CancellationToken cancellationToken = default)
    {
        if (!HasToken || _cluster is null) return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{_cluster}/autoblow/state");
            request.Headers.Add("x-device-token", _deviceToken!);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            // Read rate-limit response headers
            HandleRateLimitHeaders(response);

            _logger.LogInformation("GET /autoblow/state responded with HTTP {StatusCode}", response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("GET /autoblow/state failed with HTTP {StatusCode}. Body: {Body}", response.StatusCode, errorBody);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogInformation("GET /autoblow/state raw response: {Json}", json);

            var result = System.Text.Json.JsonSerializer.Deserialize<DeviceStateResponse>(json);
            _logger.LogInformation("Deserialized DeviceStateResponse: State={State}, ScriptPositionMs={Pos}",
                result?.State, result?.ScriptPositionMs);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get device state failed.");
            return null;
        }
    }

    public async Task<string?> GetDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        if (!HasToken || _cluster is null) return null;

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://{_cluster}/autoblow/info");
            request.Headers.Add("x-device-token", _deviceToken!);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            // Read rate-limit response headers
            HandleRateLimitHeaders(response);

            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get device info failed.");
            return null;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    /// <summary>
    /// Read rate-limit response headers where available and log them.
    /// </summary>
    private void HandleRateLimitHeaders(System.Net.Http.HttpResponseMessage response)
    {
        // Standard rate limit headers
        if (response.Headers.TryGetValues("X-RateLimit-Limit", out var limit))
        {
            _logger.LogDebug("Rate limit header - Limit: {Limit}", limit.FirstOrDefault());
        }

        if (response.Headers.TryGetValues("X-RateLimit-Remaining", out var remaining))
        {
            _logger.LogDebug("Rate limit header - Remaining: {Remaining}", remaining.FirstOrDefault());
        }

        if (response.Headers.TryGetValues("X-RateLimit-Reset", out var reset))
        {
            _logger.LogDebug("Rate limit header - Reset: {Reset}", reset.FirstOrDefault());
            if (long.TryParse(reset.FirstOrDefault(), out var resetUnix))
            {
                _rateLimitReset = DateTimeOffset.FromUnixTimeSeconds(resetUnix).UtcDateTime;
            }
        }

        // RFC 7231 Retry-After header (common on 429 Too Many Requests)
        if (response.Headers.RetryAfter != null && response.Headers.RetryAfter.Date.HasValue)
        {
            _logger.LogWarning("Rate limit exceeded. Retry-After: {RetryAfter}", response.Headers.RetryAfter.ToString());
            _rateLimitReset = response.Headers.RetryAfter.Date.Value.UtcDateTime;
        }
    }
}
