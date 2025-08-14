using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Manages secrets retrieval from various key vault providers (Azure, AWS, HashiCorp)
    /// </summary>
    public class KeyVaultManager : IDisposable
    {
        private readonly Configuration _config;
        private readonly Logger _logger;
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, string> _secretCache;
        private bool _disposed;

        public KeyVaultManager(Configuration config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _secretCache = new Dictionary<string, string>();
        }

        /// <summary>
        /// Retrieve a secret from the configured key vault
        /// </summary>
        /// <param name="secretName">Name of the secret to retrieve</param>
        /// <returns>The secret value, or null if not found</returns>
        public async Task<string> GetSecretAsync(string secretName)
        {
            if (string.IsNullOrWhiteSpace(secretName))
                return null;

            if (string.IsNullOrWhiteSpace(_config.VaultType))
            {
                _logger.Debug($"No vault configured, cannot retrieve secret: {secretName}");
                return null;
            }

            // Check cache first
            if (_secretCache.TryGetValue(secretName, out var cachedValue))
            {
                _logger.Debug($"Retrieved secret from cache: {secretName}");
                return cachedValue;
            }

            try
            {
                using var operation = _logger.TimeOperation($"Secret Retrieval [{secretName}]");

                string secretValue = _config.VaultType.ToLowerInvariant() switch
                {
                    "azure" => await GetAzureKeyVaultSecretAsync(secretName),
                    "aws" => await GetAwsSecretsManagerSecretAsync(secretName),
                    "hashicorp" => await GetHashiCorpVaultSecretAsync(secretName),
                    _ => throw new KeyVaultException($"Unsupported vault type: {_config.VaultType}", 9)
                };

                if (!string.IsNullOrEmpty(secretValue))
                {
                    _secretCache[secretName] = secretValue;
                    _logger.LogStructured("KeyVault", "SecretRetrieved", "SUCCESS", new { SecretName = secretName });
                }
                else
                {
                    _logger.Warn($"Secret not found in vault: {secretName}");
                }

                return secretValue;
            }
            catch (KeyVaultException)
            {
                throw; // Re-throw vault exceptions as-is
            }
            catch (Exception ex)
            {
                _logger.Error($"Unexpected error retrieving secret: {secretName}", ex);
                throw new KeyVaultException($"Failed to retrieve secret {secretName}: {ex.Message}", 9);
            }
        }

        /// <summary>
        /// Get PAT token, either from direct config or vault
        /// </summary>
        public async Task<string> GetPatTokenAsync()
        {
            // Direct PAT token takes precedence
            if (!string.IsNullOrWhiteSpace(_config.PatToken))
            {
                _logger.Debug("Using PAT token from configuration");
                return _config.PatToken;
            }

            // Try to get from vault
            if (!string.IsNullOrWhiteSpace(_config.PatSecretName))
            {
                _logger.Debug($"Retrieving PAT token from vault: {_config.PatSecretName}");
                return await GetSecretAsync(_config.PatSecretName);
            }

            // Try automatic detection from pipeline context
            var automaticPat = DetectPipelineAccessToken();
            if (!string.IsNullOrWhiteSpace(automaticPat))
            {
                _logger.Debug("Using automatic pipeline access token");
                return automaticPat;
            }

            _logger.Debug("No PAT token available");
            return null;
        }

        /// <summary>
        /// Get template PAT token, either from direct config or vault
        /// </summary>
        public async Task<string> GetTemplatePatTokenAsync()
        {
            // Direct template PAT takes precedence
            if (!string.IsNullOrWhiteSpace(_config.TemplatePat))
            {
                _logger.Debug("Using template PAT from configuration");
                return _config.TemplatePat;
            }

            // Try to get from vault
            if (!string.IsNullOrWhiteSpace(_config.TemplatePATVaultKey))
            {
                _logger.Debug($"Retrieving template PAT from vault: {_config.TemplatePATVaultKey}");
                return await GetSecretAsync(_config.TemplatePATVaultKey);
            }

            // Fall back to regular PAT
            _logger.Debug("No template-specific PAT, falling back to regular PAT");
            return await GetPatTokenAsync();
        }

        private async Task<string> GetAzureKeyVaultSecretAsync(string secretName)
        {
            _logger.Debug($"Retrieving secret from Azure Key Vault: {secretName}");

            // Get access token for Azure Key Vault
            var accessToken = await GetAzureAccessTokenAsync();

            var requestUrl = $"{_config.VaultUrl}/secrets/{secretName}?api-version=7.4";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessToken}");

            var response = await _httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var secretResponse = JsonSerializer.Deserialize<AzureKeyVaultSecretResponse>(responseBody);
                return secretResponse?.Value;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Secret not found
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new KeyVaultException($"Azure Key Vault error: {response.StatusCode} - {errorBody}", 9);
            }
        }

        private async Task<string> GetAwsSecretsManagerSecretAsync(string secretName)
        {
            _logger.Debug($"Retrieving secret from AWS Secrets Manager: {secretName}");

            // AWS Secrets Manager API requires AWS SDK or direct REST calls with AWS signature
            // For simplicity, using AWS CLI if available, otherwise direct REST with temporary credentials

            var request = new AwsSecretsManagerRequest
            {
                SecretId = secretName
            };

            var json = JsonSerializer.Serialize(request);
            var content = new StringContent(json, Encoding.UTF8, "application/x-amz-json-1.1");

            // Add AWS authentication headers
            await AddAwsAuthenticationHeadersAsync(content, "secretsmanager.GetSecretValue");

            var requestUrl = $"https://secretsmanager.{_config.AwsRegion}.amazonaws.com/";
            var response = await _httpClient.PostAsync(requestUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var secretResponse = JsonSerializer.Deserialize<AwsSecretsManagerResponse>(responseBody);
                return secretResponse?.SecretString;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new KeyVaultException($"AWS Secrets Manager error: {response.StatusCode} - {errorBody}", 9);
            }
        }

        private async Task<string> GetHashiCorpVaultSecretAsync(string secretName)
        {
            _logger.Debug($"Retrieving secret from HashiCorp Vault: {secretName}");

            var requestUrl = $"{_config.VaultUrl}/v1/secret/data/{secretName}";
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Vault-Token", _config.VaultToken);

            var response = await _httpClient.GetAsync(requestUrl);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var vaultResponse = JsonSerializer.Deserialize<HashiCorpVaultResponse>(responseBody);

                // HashiCorp Vault stores secrets in data.data object
                if (vaultResponse?.Data?.Data != null && vaultResponse.Data.Data.TryGetValue("value", out var element))
                {
                    return element.GetString();
                }

                return null;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null; // Secret not found
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new KeyVaultException($"HashiCorp Vault error: {response.StatusCode} - {errorBody}", 9);
            }
        }

        private async Task<string> GetAzureAccessTokenAsync()
        {
            var tokenUrl = $"https://login.microsoftonline.com/{_config.AzureTenantId}/oauth2/v2.0/token";

            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _config.AzureClientId,
                ["client_secret"] = _config.AzureClientSecret,
                ["scope"] = "https://vault.azure.net/.default",
                ["grant_type"] = "client_credentials"
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(tokenUrl, content);

            if (response.IsSuccessStatusCode)
            {
                var responseBody = await response.Content.ReadAsStringAsync();
                var tokenResponse = JsonSerializer.Deserialize<AzureTokenResponse>(responseBody);
                return tokenResponse?.AccessToken;
            }
            else
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                throw new KeyVaultException($"Azure authentication failed: {response.StatusCode} - {errorBody}", 9);
            }
        }

        private async Task AddAwsAuthenticationHeadersAsync(StringContent content, string action)
        {
            // Simplified AWS authentication - in production, use AWS SDK
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ");
            var date = DateTime.UtcNow.ToString("yyyyMMdd");

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("X-Amz-Target", action);
            _httpClient.DefaultRequestHeaders.Add("X-Amz-Date", timestamp);
            _httpClient.DefaultRequestHeaders.Add("Authorization",
                $"AWS4-HMAC-SHA256 Credential={_config.AwsAccessKeyId}/{date}/{_config.AwsRegion}/secretsmanager/aws4_request, SignedHeaders=host;x-amz-date;x-amz-target, Signature=[SIGNATURE_PLACEHOLDER]");

            // Note: Real implementation would calculate proper AWS signature
            await Task.CompletedTask;
        }

        private string DetectPipelineAccessToken()
        {
            // Azure DevOps
            var azureToken = Environment.GetEnvironmentVariable("SYSTEM_ACCESSTOKEN");
            if (!string.IsNullOrWhiteSpace(azureToken))
            {
                _logger.Debug("Detected Azure DevOps access token");
                return azureToken;
            }

            // GitHub Actions
            var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
            if (!string.IsNullOrWhiteSpace(githubToken))
            {
                _logger.Debug("Detected GitHub Actions token");
                return githubToken;
            }

            // Jenkins
            var jenkinsToken = Environment.GetEnvironmentVariable("JENKINS_TOKEN") ??
                              Environment.GetEnvironmentVariable("BUILD_TOKEN");
            if (!string.IsNullOrWhiteSpace(jenkinsToken))
            {
                _logger.Debug("Detected Jenkins token");
                return jenkinsToken;
            }

            return null;
        }

        /// <summary>
        /// Clear the secret cache
        /// </summary>
        public void ClearCache()
        {
            _secretCache.Clear();
            _logger.Debug("Secret cache cleared");
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                // Clear sensitive data from memory
                _secretCache.Clear();
                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        // Data Transfer Objects for various vault APIs
        private class AzureKeyVaultSecretResponse
        {
            public string Value { get; set; }
            public string Id { get; set; }
            public Dictionary<string, object> Attributes { get; set; }
        }

        private class AzureTokenResponse
        {
            public string AccessToken { get; set; }
            public string TokenType { get; set; }
            public int ExpiresIn { get; set; }
        }

        private class AwsSecretsManagerRequest
        {
            public string SecretId { get; set; }
        }

        private class AwsSecretsManagerResponse
        {
            public string SecretString { get; set; }
            public string SecretBinary { get; set; }
            public string Name { get; set; }
            public string VersionId { get; set; }
        }

        private class HashiCorpVaultResponse
        {
            public VaultData Data { get; set; }
        }

        private class VaultData
        {
            public Dictionary<string, JsonElement> Data { get; set; }
            public Dictionary<string, object> Metadata { get; set; }
        }
    }

    /// <summary>
    /// Exception thrown for key vault related errors
    /// </summary>
    public class KeyVaultException : Exception
    {
        public int ExitCode { get; }

        public KeyVaultException(string message, int exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        public KeyVaultException(string message, int exitCode, Exception innerException) : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }
}