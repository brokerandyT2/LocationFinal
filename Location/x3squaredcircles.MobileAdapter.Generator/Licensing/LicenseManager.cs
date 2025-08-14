using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using x3squaredcircles.MobileAdapter.Generator.Configuration;
using x3squaredcircles.MobileAdapter.Generator.Logging;

namespace x3squaredcircles.MobileAdapter.Generator.Licensing
{
    public class LicenseManager
    {
        private readonly ILogger _logger;
        private readonly HttpClient _httpClient;

        public LicenseManager(ILogger logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public async Task<LicenseValidationResult> ValidateLicenseAsync(GeneratorConfiguration config)
        {
            try
            {
                _logger.LogInfo("Validating license...");

                var licenseRequest = new LicenseRequest
                {
                    ToolName = config.ToolName,
                    RequestedBy = Environment.UserName,
                    RequestTime = DateTime.UtcNow,
                    Repository = config.RepoUrl,
                    Branch = config.Branch
                };

                var response = await RequestLicenseWithRetryAsync(config, licenseRequest);

                if (response == null)
                {
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "License server unreachable after retries"
                    };
                }

                return ProcessLicenseResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError("License validation failed", ex);
                return new LicenseValidationResult
                {
                    IsValid = false,
                    ErrorMessage = $"License validation error: {ex.Message}"
                };
            }
        }

        private async Task<LicenseResponse> RequestLicenseWithRetryAsync(GeneratorConfiguration config, LicenseRequest request)
        {
            var maxRetries = config.LicenseTimeout / config.LicenseRetryInterval;
            var currentRetry = 0;

            while (currentRetry <= maxRetries)
            {
                try
                {
                    _logger.LogDebug($"License request attempt {currentRetry + 1}/{maxRetries + 1}");

                    var json = JsonSerializer.Serialize(request);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.LicenseRetryInterval));
                    var response = await _httpClient.PostAsync($"{config.LicenseServer}/api/license/request", content, cts.Token);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseJson = await response.Content.ReadAsStringAsync();
                        return JsonSerializer.Deserialize<LicenseResponse>(responseJson);
                    }

                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        _logger.LogWarning("Burst capacity exceeded, waiting for available license...");
                    }
                    else
                    {
                        _logger.LogWarning($"License server returned {response.StatusCode}: {response.ReasonPhrase}");
                    }
                }
                catch (TaskCanceledException)
                {
                    _logger.LogWarning($"License request timeout (attempt {currentRetry + 1})");
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogWarning($"License server connection failed: {ex.Message}");
                }

                if (currentRetry < maxRetries)
                {
                    _logger.LogInfo($"Retrying license request in {config.LicenseRetryInterval} seconds...");
                    await Task.Delay(TimeSpan.FromSeconds(config.LicenseRetryInterval));
                }

                currentRetry++;
            }

            return null;
        }

        private LicenseValidationResult ProcessLicenseResponse(LicenseResponse response)
        {
            switch (response.Status)
            {
                case LicenseStatus.Valid:
                    _logger.LogInfo($"License validated successfully. Expires: {response.ExpiresAt}");
                    return new LicenseValidationResult
                    {
                        IsValid = true,
                        LicenseId = response.LicenseId,
                        ExpiresAt = response.ExpiresAt
                    };

                case LicenseStatus.Expired:
                    _logger.LogWarning("License expired. Running in NOOP mode - analysis only, no file generation.");
                    return new LicenseValidationResult
                    {
                        IsValid = true,
                        IsNoOpMode = true,
                        IsExpired = true,
                        ErrorMessage = "License expired"
                    };

                case LicenseStatus.Unavailable:
                    _logger.LogError("No licenses available");
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = "No licenses available"
                    };

                case LicenseStatus.Invalid:
                    _logger.LogError($"License validation failed: {response.ErrorMessage}");
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = response.ErrorMessage
                    };

                default:
                    _logger.LogError($"Unknown license status: {response.Status}");
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        ErrorMessage = $"Unknown license status: {response.Status}"
                    };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class LicenseRequest
    {
        public string ToolName { get; set; }
        public string RequestedBy { get; set; }
        public DateTime RequestTime { get; set; }
        public string Repository { get; set; }
        public string Branch { get; set; }
    }

    public class LicenseResponse
    {
        public LicenseStatus Status { get; set; }
        public string LicenseId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public bool IsNoOpMode { get; set; }
        public bool IsExpired { get; set; }
        public string LicenseId { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string ErrorMessage { get; set; }
    }

    public enum LicenseStatus
    {
        Valid,
        Expired,
        Unavailable,
        Invalid
    }
}