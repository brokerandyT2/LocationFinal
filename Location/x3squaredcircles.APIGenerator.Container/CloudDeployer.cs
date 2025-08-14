using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Handles cloud-specific deployment operations for generated APIs
    /// </summary>
    public class CloudDeployer : IDisposable
    {
        private readonly Configuration _config;
        private readonly Logger _logger;
        private readonly KeyVaultManager _keyVaultManager;
        private readonly HttpClient _httpClient;
        private bool _disposed;

        public CloudDeployer(Configuration config, Logger logger, KeyVaultManager keyVaultManager)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keyVaultManager = keyVaultManager ?? throw new ArgumentNullException(nameof(keyVaultManager));
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        }

        /// <summary>
        /// Deploy generated API to the configured cloud provider
        /// </summary>
        /// <param name="projectPath">Path to generated project</param>
        /// <param name="entities">Discovered entities</param>
        /// <param name="deploymentTag">Tag for deployment</param>
        /// <returns>Deployment information</returns>
        public async Task<DeploymentResult> DeployAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            _logger.LogStartPhase("Cloud Deployment");

            try
            {
                using var operation = _logger.TimeOperation("Cloud Deployment");

                var result = _config.SelectedCloud switch
                {
                    "azure" => await DeployToAzureAsync(projectPath, entities, deploymentTag),
                    "aws" => await DeployToAwsAsync(projectPath, entities, deploymentTag),
                    "gcp" => await DeployToGcpAsync(projectPath, entities, deploymentTag),
                    "oracle" => await DeployToOracleAsync(projectPath, entities, deploymentTag),
                    _ => throw new CloudDeploymentException($"Unsupported cloud provider: {_config.SelectedCloud}", 8)
                };

                _logger.LogEndPhase("Cloud Deployment", result.Success);
                return result;
            }
            catch (Exception ex)
            {
                _logger.Error("Cloud deployment failed", ex);
                _logger.LogEndPhase("Cloud Deployment", false);
                throw new CloudDeploymentException($"Deployment failed: {ex.Message}", 8);
            }
        }

        private async Task<DeploymentResult> DeployToAzureAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            _logger.LogCloudOperation("Deploying to Azure");

            var result = new DeploymentResult
            {
                Cloud = "azure",
                DeploymentTag = deploymentTag,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Authenticate with Azure
                await AuthenticateAzureAsync();

                // Create or update resource group
                await CreateAzureResourceGroupAsync();

                // Deploy based on language
                var deploymentInfo = _config.SelectedLanguage switch
                {
                    "csharp" => await DeployAzureFunctionAppAsync(projectPath, entities, deploymentTag),
                    "java" => await DeployAzureSpringAppAsync(projectPath, entities, deploymentTag),
                    "python" => await DeployAzureFunctionAppPythonAsync(projectPath, entities, deploymentTag),
                    "javascript" => await DeployAzureFunctionAppNodeAsync(projectPath, entities, deploymentTag),
                    "typescript" => await DeployAzureFunctionAppNodeAsync(projectPath, entities, deploymentTag),
                    _ => throw new CloudDeploymentException($"Language {_config.SelectedLanguage} not supported on Azure", 8)
                };

                result.ServiceName = deploymentInfo.ServiceName;
                result.ServiceUrl = deploymentInfo.Url;
                result.ResourceId = deploymentInfo.ResourceId;
                result.Success = true;

                _logger.LogDeploymentInfo(result.ServiceName, result.ServiceUrl, "SUCCESS");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.Error($"Azure deployment failed: {ex.Message}");
                throw;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
            }

            return result;
        }

        private async Task<DeploymentResult> DeployToAwsAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            _logger.LogCloudOperation("Deploying to AWS");

            var result = new DeploymentResult
            {
                Cloud = "aws",
                DeploymentTag = deploymentTag,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Authenticate with AWS
                await AuthenticateAwsAsync();

                // Deploy based on language
                var deploymentInfo = _config.SelectedLanguage switch
                {
                    "csharp" => await DeployAwsLambdaAsync(projectPath, entities, deploymentTag),
                    "java" => await DeployAwsLambdaJavaAsync(projectPath, entities, deploymentTag),
                    "python" => await DeployAwsLambdaPythonAsync(projectPath, entities, deploymentTag),
                    "javascript" => await DeployAwsLambdaNodeAsync(projectPath, entities, deploymentTag),
                    "typescript" => await DeployAwsLambdaNodeAsync(projectPath, entities, deploymentTag),
                    "go" => await DeployAwsLambdaGoAsync(projectPath, entities, deploymentTag),
                    _ => throw new CloudDeploymentException($"Language {_config.SelectedLanguage} not supported on AWS", 8)
                };

                result.ServiceName = deploymentInfo.ServiceName;
                result.ServiceUrl = deploymentInfo.Url;
                result.ResourceId = deploymentInfo.ResourceId;
                result.Success = true;

                _logger.LogDeploymentInfo(result.ServiceName, result.ServiceUrl, "SUCCESS");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.Error($"AWS deployment failed: {ex.Message}");
                throw;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
            }

            return result;
        }

        private async Task<DeploymentResult> DeployToGcpAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            _logger.LogCloudOperation("Deploying to Google Cloud Platform");

            var result = new DeploymentResult
            {
                Cloud = "gcp",
                DeploymentTag = deploymentTag,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Authenticate with GCP
                await AuthenticateGcpAsync();

                // Deploy based on language
                var deploymentInfo = _config.SelectedLanguage switch
                {
                    "csharp" => await DeployGcpCloudRunAsync(projectPath, entities, deploymentTag),
                    "java" => await DeployGcpCloudRunJavaAsync(projectPath, entities, deploymentTag),
                    "python" => await DeployGcpCloudFunctionPythonAsync(projectPath, entities, deploymentTag),
                    "javascript" => await DeployGcpCloudFunctionNodeAsync(projectPath, entities, deploymentTag),
                    "typescript" => await DeployGcpCloudFunctionNodeAsync(projectPath, entities, deploymentTag),
                    "go" => await DeployGcpCloudFunctionGoAsync(projectPath, entities, deploymentTag),
                    _ => throw new CloudDeploymentException($"Language {_config.SelectedLanguage} not supported on GCP", 8)
                };

                result.ServiceName = deploymentInfo.ServiceName;
                result.ServiceUrl = deploymentInfo.Url;
                result.ResourceId = deploymentInfo.ResourceId;
                result.Success = true;

                _logger.LogDeploymentInfo(result.ServiceName, result.ServiceUrl, "SUCCESS");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.Error($"GCP deployment failed: {ex.Message}");
                throw;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
            }

            return result;
        }

        private async Task<DeploymentResult> DeployToOracleAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            _logger.LogCloudOperation("Deploying to Oracle Cloud Infrastructure");

            var result = new DeploymentResult
            {
                Cloud = "oracle",
                DeploymentTag = deploymentTag,
                StartTime = DateTime.UtcNow
            };

            try
            {
                // Authenticate with OCI
                await AuthenticateOciAsync();

                // Deploy to OCI Functions
                var deploymentInfo = await DeployOciFunctionAsync(projectPath, entities, deploymentTag);

                result.ServiceName = deploymentInfo.ServiceName;
                result.ServiceUrl = deploymentInfo.Url;
                result.ResourceId = deploymentInfo.ResourceId;
                result.Success = true;

                _logger.LogDeploymentInfo(result.ServiceName, result.ServiceUrl, "SUCCESS");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                _logger.Error($"OCI deployment failed: {ex.Message}");
                throw;
            }
            finally
            {
                result.EndTime = DateTime.UtcNow;
                result.Duration = result.EndTime - result.StartTime;
            }

            return result;
        }

        // Azure-specific deployment methods
        private async Task AuthenticateAzureAsync()
        {
            _logger.Debug("Authenticating with Azure");

            var tokenUrl = $"https://login.microsoftonline.com/{_config.AzureTenantId}/oauth2/v2.0/token";
            var parameters = new Dictionary<string, string>
            {
                ["client_id"] = _config.AzureClientId,
                ["client_secret"] = _config.AzureClientSecret,
                ["scope"] = "https://management.azure.com/.default",
                ["grant_type"] = "client_credentials"
            };

            var content = new FormUrlEncodedContent(parameters);
            var response = await _httpClient.PostAsync(tokenUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                throw new CloudDeploymentException("Azure authentication failed", 10);
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<AzureTokenResponse>(responseBody);

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {tokenResponse.AccessToken}");
        }

        private async Task CreateAzureResourceGroupAsync()
        {
            _logger.Debug($"Creating/updating resource group: {_config.AzureResourceGroup}");

            var resourceGroupUrl = $"https://management.azure.com/subscriptions/{_config.AzureSubscription}/resourcegroups/{_config.AzureResourceGroup}?api-version=2021-04-01";

            var resourceGroup = new
            {
                location = _config.AzureRegion,
                tags = new { createdBy = "api-generator" }
            };

            var json = JsonSerializer.Serialize(resourceGroup);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(resourceGroupUrl, content);
            response.EnsureSuccessStatusCode();
        }

        private async Task<ServiceDeploymentInfo> DeployAzureFunctionAppAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionAppName = $"api-{SanitizeName(deploymentTag)}-{DateTime.UtcNow:yyyyMMddHHmm}";

            _logger.LogCloudOperation($"Deploying Azure Function App: {functionAppName}");

            // Create Function App
            var createUrl = $"https://management.azure.com/subscriptions/{_config.AzureSubscription}/resourceGroups/{_config.AzureResourceGroup}/providers/Microsoft.Web/sites/{functionAppName}?api-version=2021-02-01";

            var functionApp = new
            {
                location = _config.AzureRegion,
                kind = "functionapp",
                properties = new
                {
                    serverFarmId = $"/subscriptions/{_config.AzureSubscription}/resourceGroups/{_config.AzureResourceGroup}/providers/Microsoft.Web/serverfarms/api-generator-plan",
                    httpsOnly = true,
                    siteConfig = new
                    {
                        appSettings = new[]
                        {
                            new { name = "FUNCTIONS_WORKER_RUNTIME", value = "dotnet" },
                            new { name = "FUNCTIONS_EXTENSION_VERSION", value = "~4" },
                            new { name = "AzureWebJobsStorage", value = "DefaultEndpointsProtocol=https;AccountName=apigenstore;AccountKey=..." }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(functionApp);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(createUrl, content);
            response.EnsureSuccessStatusCode();

            // Deploy code (simplified - would use ZIP deployment in real implementation)
            await DeployCodeToAzureFunctionAsync(functionAppName, projectPath);

            return new ServiceDeploymentInfo
            {
                ServiceName = functionAppName,
                Url = $"https://{functionAppName}.azurewebsites.net",
                ResourceId = $"/subscriptions/{_config.AzureSubscription}/resourceGroups/{_config.AzureResourceGroup}/providers/Microsoft.Web/sites/{functionAppName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployAzureSpringAppAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var appName = $"spring-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying Azure Spring Apps: {appName}");

            // Azure Spring Apps deployment logic here
            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = appName,
                Url = $"https://{appName}.azuremicroservices.io",
                ResourceId = $"/subscriptions/{_config.AzureSubscription}/resourceGroups/{_config.AzureResourceGroup}/providers/Microsoft.AppPlatform/Spring/{appName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployAzureFunctionAppPythonAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionAppName = $"python-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying Azure Function App (Python): {functionAppName}");

            // Python-specific Azure Functions deployment
            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionAppName,
                Url = $"https://{functionAppName}.azurewebsites.net",
                ResourceId = $"/subscriptions/{_config.AzureSubscription}/resourceGroups/{_config.AzureResourceGroup}/providers/Microsoft.Web/sites/{functionAppName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployAzureFunctionAppNodeAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionAppName = $"node-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying Azure Function App (Node.js): {functionAppName}");

            // Node.js-specific Azure Functions deployment
            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionAppName,
                Url = $"https://{functionAppName}.azurewebsites.net",
                ResourceId = $"/subscriptions/{_config.AzureSubscription}/resourceGroups/{_config.AzureResourceGroup}/providers/Microsoft.Web/sites/{functionAppName}"
            };
        }

        // AWS-specific deployment methods
        private async Task AuthenticateAwsAsync()
        {
            _logger.Debug("Authenticating with AWS");

            // AWS authentication using access keys
            _httpClient.DefaultRequestHeaders.Clear();
            // AWS signature would be calculated here
            await Task.CompletedTask;
        }

        private async Task<ServiceDeploymentInfo> DeployAwsLambdaAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying AWS Lambda (.NET): {functionName}");

            // Lambda deployment logic
            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{functionName}.execute-api.{_config.AwsRegion}.amazonaws.com/prod",
                ResourceId = $"arn:aws:lambda:{_config.AwsRegion}:{_config.AwsAccountId}:function:{functionName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployAwsLambdaJavaAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"java-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying AWS Lambda (Java): {functionName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{functionName}.execute-api.{_config.AwsRegion}.amazonaws.com/prod",
                ResourceId = $"arn:aws:lambda:{_config.AwsRegion}:{_config.AwsAccountId}:function:{functionName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployAwsLambdaPythonAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"python-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying AWS Lambda (Python): {functionName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{functionName}.execute-api.{_config.AwsRegion}.amazonaws.com/prod",
                ResourceId = $"arn:aws:lambda:{_config.AwsRegion}:{_config.AwsAccountId}:function:{functionName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployAwsLambdaNodeAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"node-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying AWS Lambda (Node.js): {functionName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{functionName}.execute-api.{_config.AwsRegion}.amazonaws.com/prod",
                ResourceId = $"arn:aws:lambda:{_config.AwsRegion}:{_config.AwsAccountId}:function:{functionName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployAwsLambdaGoAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"go-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying AWS Lambda (Go): {functionName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{functionName}.execute-api.{_config.AwsRegion}.amazonaws.com/prod",
                ResourceId = $"arn:aws:lambda:{_config.AwsRegion}:{_config.AwsAccountId}:function:{functionName}"
            };
        }

        // GCP-specific deployment methods
        private async Task AuthenticateGcpAsync()
        {
            _logger.Debug("Authenticating with Google Cloud Platform");

            // GCP authentication using service account
            await Task.CompletedTask;
        }

        private async Task<ServiceDeploymentInfo> DeployGcpCloudRunAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var serviceName = $"api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying GCP Cloud Run: {serviceName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = serviceName,
                Url = $"https://{serviceName}-{_config.GcpRegion}.run.app",
                ResourceId = $"projects/{_config.GcpProjectId}/locations/{_config.GcpRegion}/services/{serviceName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployGcpCloudRunJavaAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var serviceName = $"java-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying GCP Cloud Run (Java): {serviceName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = serviceName,
                Url = $"https://{serviceName}-{_config.GcpRegion}.run.app",
                ResourceId = $"projects/{_config.GcpProjectId}/locations/{_config.GcpRegion}/services/{serviceName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployGcpCloudFunctionPythonAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"python-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying GCP Cloud Function (Python): {functionName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{_config.GcpRegion}-{_config.GcpProjectId}.cloudfunctions.net/{functionName}",
                ResourceId = $"projects/{_config.GcpProjectId}/locations/{_config.GcpRegion}/functions/{functionName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployGcpCloudFunctionNodeAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"node-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying GCP Cloud Function (Node.js): {functionName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{_config.GcpRegion}-{_config.GcpProjectId}.cloudfunctions.net/{functionName}",
                ResourceId = $"projects/{_config.GcpProjectId}/locations/{_config.GcpRegion}/functions/{functionName}"
            };
        }

        private async Task<ServiceDeploymentInfo> DeployGcpCloudFunctionGoAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"go-api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying GCP Cloud Function (Go): {functionName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{_config.GcpRegion}-{_config.GcpProjectId}.cloudfunctions.net/{functionName}",
                ResourceId = $"projects/{_config.GcpProjectId}/locations/{_config.GcpRegion}/functions/{functionName}"
            };
        }

        // Oracle Cloud-specific deployment methods
        private async Task AuthenticateOciAsync()
        {
            _logger.Debug("Authenticating with Oracle Cloud Infrastructure");

            // OCI authentication
            await Task.CompletedTask;
        }

        private async Task<ServiceDeploymentInfo> DeployOciFunctionAsync(string projectPath, List<DiscoveredEntity> entities, string deploymentTag)
        {
            var functionName = $"api-{SanitizeName(deploymentTag)}";

            _logger.LogCloudOperation($"Deploying OCI Function: {functionName}");

            await Task.Delay(1000); // Placeholder

            return new ServiceDeploymentInfo
            {
                ServiceName = functionName,
                Url = $"https://{functionName}.{_config.OciRegion}.functions.oci.oraclecloud.com",
                ResourceId = $"ocid1.fnfunc.oc1.{_config.OciRegion}.{functionName}"
            };
        }

        // Helper methods
        private async Task DeployCodeToAzureFunctionAsync(string functionAppName, string projectPath)
        {
            _logger.Debug($"Deploying code to {functionAppName}");

            // ZIP deployment would happen here
            await Task.Delay(1000);
        }

        private string SanitizeName(string name)
        {
            return System.Text.RegularExpressions.Regex.Replace(name, @"[^a-zA-Z0-9\-]", "").ToLowerInvariant();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        // Data Transfer Objects
        private class AzureTokenResponse
        {
            public string AccessToken { get; set; }
            public string TokenType { get; set; }
            public int ExpiresIn { get; set; }
        }

        private class ServiceDeploymentInfo
        {
            public string ServiceName { get; set; }
            public string Url { get; set; }
            public string ResourceId { get; set; }
        }
    }

    public class DeploymentResult
    {
        public string Cloud { get; set; }
        public string DeploymentTag { get; set; }
        public string ServiceName { get; set; }
        public string ServiceUrl { get; set; }
        public string ResourceId { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public class CloudDeploymentException : Exception
    {
        public int ExitCode { get; }

        public CloudDeploymentException(string message, int exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        public CloudDeploymentException(string message, int exitCode, Exception innerException) : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }
}