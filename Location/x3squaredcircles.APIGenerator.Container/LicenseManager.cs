using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Manages license acquisition, validation, and automatic NOOP mode for expired licenses
    /// </summary>
    public class LicenseManager : IDisposable
    {
        private readonly Configuration _config;
        private readonly Logger _logger;
        private readonly HttpClient _httpClient;
        private string _licenseToken;
        private DateTime _licenseExpiry;
        private bool _disposed;

        public bool IsLicenseValid => !string.IsNullOrEmpty(_licenseToken) && DateTime.UtcNow < _licenseExpiry;
        public bool IsInNoOpMode { get; private set; }

        public LicenseManager(Configuration config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(_config.LicenseTimeout)
            };

            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"{_config.ToolName}/{GetVersion()}");
        }

        /// <summary>
        /// Acquire a license from the license server
        /// </summary>
        /// <returns>True if license acquired successfully, false if should run in NOOP mode</returns>
        public async Task<bool> AcquireLicenseAsync()
        {
            _logger.LogStartPhase("License Acquisition");

            try
            {
                using var operation = _logger.TimeOperation("License Acquisition");

                var licenseResponse = await RequestLicenseWithRetryAsync();

                if (licenseResponse.Success)
                {
                    _licenseToken = licenseResponse.Token;
                    _licenseExpiry = licenseResponse.ExpiryTime;

                    _logger.LogLicenseStatus("ACQUIRED",
                        $"Token: {MaskToken(_licenseToken)}, Expires: {_licenseExpiry:yyyy-MM-dd HH:mm:ss UTC}");

                    _logger.LogEndPhase("License Acquisition", true);
                    return true;
                }
                else if (licenseResponse.IsExpired)
                {
                    _logger.LogLicenseStatus("EXPIRED", "License has expired - entering NOOP mode");
                    IsInNoOpMode = true;
                    _logger.LogEndPhase("License Acquisition", true);
                    return false; // Continue in NOOP mode
                }
                else if (licenseResponse.IsBurstCapacityExceeded)
                {
                    _logger.LogLicenseStatus("BURST_EXCEEDED", "Burst capacity exceeded - waiting for available license");

                    var waitSuccess = await WaitForAvailableLicenseAsync();
                    if (waitSuccess)
                    {
                        return await AcquireLicenseAsync(); // Retry acquisition
                    }
                    else
                    {
                        _logger.Error("License acquisition failed: Timeout waiting for available license");
                        _logger.LogEndPhase("License Acquisition", false);
                        throw new LicenseException("Failed to acquire license within timeout period", 2);
                    }
                }
                else
                {
                    _logger.Error($"License acquisition failed: {licenseResponse.ErrorMessage}");
                    _logger.LogEndPhase("License Acquisition", false);
                    throw new LicenseException($"License server error: {licenseResponse.ErrorMessage}", 2);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Error("License server unreachable", ex);
                _logger.LogLicenseStatus("SERVER_UNREACHABLE", "Will retry with exponential backoff");

                var retrySuccess = await RetryLicenseServerConnectionAsync();
                if (retrySuccess)
                {
                    return await AcquireLicenseAsync(); // Retry acquisition
                }
                else
                {
                    _logger.Error("License server remained unreachable after retries");
                    _logger.LogEndPhase("License Acquisition", false);
                    throw new LicenseException("License server unreachable", 2);
                }
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.Error("License server request timeout");
                _logger.LogEndPhase("License Acquisition", false);
                throw new LicenseException("License server timeout", 2);
            }
            catch (LicenseException)
            {
                throw; // Re-throw license exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error during license acquisition", ex);
                _logger.LogEndPhase("License Acquisition", false);
                throw new LicenseException($"Unexpected license error: {ex.Message}", 2);
            }
        }

        /// <summary>
        /// Release the current license back to the server
        /// </summary>
        public async Task ReleaseLicenseAsync()
        {
            if (string.IsNullOrEmpty(_licenseToken) || IsInNoOpMode)
                return;

            try
            {
                _logger.Debug("Releasing license");

                var releaseRequest = new LicenseReleaseRequest
                {
                    Token = _licenseToken,
                    ToolName = _config.ToolName
                };

                var json = JsonSerializer.Serialize(releaseRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.LicenseServer}/api/license/release", content);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogLicenseStatus("RELEASED", $"Token: {MaskToken(_licenseToken)}");
                }
                else
                {
                    _logger.Warn($"Failed to release license: HTTP {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"Error releasing license: {ex.Message}");
            }
            finally
            {
                _licenseToken = null;
                _licenseExpiry = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Validate that the current license is still valid
        /// </summary>
        public async Task<bool> ValidateLicenseAsync()
        {
            if (string.IsNullOrEmpty(_licenseToken))
                return false;

            if (DateTime.UtcNow >= _licenseExpiry)
            {
                _logger.LogLicenseStatus("EXPIRED", "License expired during execution - entering NOOP mode");
                IsInNoOpMode = true;
                return false;
            }

            try
            {
                var validateRequest = new LicenseValidateRequest
                {
                    Token = _licenseToken,
                    ToolName = _config.ToolName
                };

                var json = JsonSerializer.Serialize(validateRequest);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_config.LicenseServer}/api/license/validate", content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    var validateResponse = JsonSerializer.Deserialize<LicenseValidateResponse>(responseBody);

                    if (validateResponse.IsValid)
                    {
                        _logger.Debug("License validation successful");
                        return true;
                    }
                    else
                    {
                        _logger.LogLicenseStatus("INVALID", "License became invalid - entering NOOP mode");
                        IsInNoOpMode = true;
                        return false;
                    }
                }
                else
                {
                    _logger.Warn($"License validation failed: HTTP {response.StatusCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.Warn($"License validation error: {ex.Message}");
                return false;
            }
        }

        private async Task<LicenseResponse> RequestLicenseWithRetryAsync()
        {
            var request = new LicenseRequest
            {
                ToolName = _config.ToolName,
                RequestedBy = Environment.UserName,
                Repository = _config.RepoUrl,
                Branch = _config.Branch,
                BuildId = Environment.GetEnvironmentVariable("BUILD_BUILDID") ??
                         Environment.GetEnvironmentVariable("GITHUB_RUN_ID") ??
                         Environment.GetEnvironmentVariable("BUILD_NUMBER") ??
                         "local"
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync($"{_config.LicenseServer}/api/license/acquire", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<LicenseResponse>(responseBody);
            }
            else
            {
                var errorResponse = JsonSerializer.Deserialize<LicenseErrorResponse>(responseBody);
                return new LicenseResponse
                {
                    Success = false,
                    ErrorMessage = errorResponse?.Message ?? $"HTTP {response.StatusCode}",
                    IsExpired = errorResponse?.ErrorCode == "LICENSE_EXPIRED",
                    IsBurstCapacityExceeded = errorResponse?.ErrorCode == "BURST_CAPACITY_EXCEEDED"
                };
            }
        }

        private async Task<bool> WaitForAvailableLicenseAsync()
        {
            var maxWaitTime = TimeSpan.FromSeconds(_config.LicenseTimeout);
            var startTime = DateTime.UtcNow;
            var retryInterval = TimeSpan.FromSeconds(_config.LicenseRetryInterval);

            _logger.Info($"Waiting for available license (max {maxWaitTime.TotalMinutes:F1} minutes)");

            while (DateTime.UtcNow - startTime < maxWaitTime)
            {
                await Task.Delay(retryInterval);

                try
                {
                    var response = await _httpClient.GetAsync($"{_config.LicenseServer}/api/license/availability");
                    if (response.IsSuccessStatusCode)
                    {
                        var responseBody = await response.Content.ReadAsStringAsync();
                        var availability = JsonSerializer.Deserialize<LicenseAvailabilityResponse>(responseBody);

                        if (availability.AvailableCount > 0)
                        {
                            _logger.Info("License became available");
                            return true;
                        }

                        var elapsed = DateTime.UtcNow - startTime;
                        _logger.LogProgress("License Wait", (int)elapsed.TotalSeconds, (int)maxWaitTime.TotalSeconds,
                            $"Available: {availability.AvailableCount}, Queue: {availability.QueueLength}");
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Error checking license availability: {ex.Message}");
                }
            }

            return false;
        }

        private async Task<bool> RetryLicenseServerConnectionAsync()
        {
            var maxRetries = 3;
            var baseDelay = TimeSpan.FromSeconds(_config.LicenseRetryInterval);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));

                _logger.Debug($"License server retry attempt {attempt}/{maxRetries} in {delay.TotalSeconds:F1}s");
                await Task.Delay(delay);

                try
                {
                    var response = await _httpClient.GetAsync($"{_config.LicenseServer}/api/health");
                    if (response.IsSuccessStatusCode)
                    {
                        _logger.Info("License server connection restored");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Retry {attempt} failed: {ex.Message}");
                }
            }

            return false;
        }

        private static string MaskToken(string token)
        {
            if (string.IsNullOrEmpty(token) || token.Length < 8)
                return "[MASKED]";

            return $"{token.Substring(0, 4)}...{token.Substring(token.Length - 4)}";
        }

        private static string GetVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return assembly.GetName().Version?.ToString() ?? "1.0.0";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    ReleaseLicenseAsync().Wait(TimeSpan.FromSeconds(10));
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"Error during license disposal: {ex.Message}");
                }

                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        // Data Transfer Objects for License API
        private class LicenseRequest
        {
            public string ToolName { get; set; }
            public string RequestedBy { get; set; }
            public string Repository { get; set; }
            public string Branch { get; set; }
            public string BuildId { get; set; }
        }

        private class LicenseResponse
        {
            public bool Success { get; set; }
            public string Token { get; set; }
            public DateTime ExpiryTime { get; set; }
            public string ErrorMessage { get; set; }
            public bool IsExpired { get; set; }
            public bool IsBurstCapacityExceeded { get; set; }
        }

        private class LicenseErrorResponse
        {
            public string Message { get; set; }
            public string ErrorCode { get; set; }
        }

        private class LicenseReleaseRequest
        {
            public string Token { get; set; }
            public string ToolName { get; set; }
        }

        private class LicenseValidateRequest
        {
            public string Token { get; set; }
            public string ToolName { get; set; }
        }

        private class LicenseValidateResponse
        {
            public bool IsValid { get; set; }
            public DateTime ExpiryTime { get; set; }
        }

        private class LicenseAvailabilityResponse
        {
            public int AvailableCount { get; set; }
            public int QueueLength { get; set; }
        }
    }

    /// <summary>
    /// Exception thrown for license-related errors
    /// </summary>
    public class LicenseException : Exception
    {
        public int ExitCode { get; }

        public LicenseException(string message, int exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        public LicenseException(string message, int exitCode, Exception innerException) : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }
}