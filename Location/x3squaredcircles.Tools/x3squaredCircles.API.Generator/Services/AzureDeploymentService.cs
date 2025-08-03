using Azure.Identity;
using Azure.ResourceManager;
using Azure.ResourceManager.Resources;
using Azure.ResourceManager.Resources.Models;
using Azure.Storage.Blobs;
using x3squaredcirecles.API.Generator.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;

namespace x3squaredcirecles.API.Generator.APIGenerator.Services;

public class AzureDeploymentService
{
    private readonly ILogger<AzureDeploymentService> _logger;
    private readonly HttpClient _httpClient;

    public AzureDeploymentService(ILogger<AzureDeploymentService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task DeployFunctionAppAsync(AssemblyInfo assemblyInfo, GeneratedAssets generatedAssets, GeneratorOptions options)
    {
        try
        {
            _logger.LogInformation("Starting Azure deployment for {FunctionApp}", generatedAssets.FunctionAppName);

            // Create Azure Resource Manager client
            var credential = new DefaultAzureCredential();
            var armClient = new ArmClient(credential);

            // Get the subscription and resource group
            var subscription = await armClient.GetDefaultSubscriptionAsync();
            var resourceGroup = await subscription.GetResourceGroupAsync(options.ResourceGroup);

            if (resourceGroup == null)
            {
                throw new InvalidOperationException($"Resource group '{options.ResourceGroup}' not found in subscription");
            }

            _logger.LogInformation("Deploying to resource group: {ResourceGroup}", options.ResourceGroup);

            // Deploy Bicep template
            await DeployBicepTemplateAsync(resourceGroup.Value, generatedAssets, options);

            // Upload function code if provided
            if (!string.IsNullOrEmpty(options.FunctionCodePath))
            {
                await UploadFunctionCodeAsync(generatedAssets, options);
            }
            else
            {
                _logger.LogInformation("No function code path provided, skipping code deployment");
            }

            _logger.LogInformation("Azure deployment completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure deployment failed");
            throw;
        }
    }

    private async Task DeployBicepTemplateAsync(ResourceGroupResource resourceGroup, GeneratedAssets generatedAssets, GeneratorOptions options)
    {
        try
        {
            _logger.LogInformation("Deploying Bicep infrastructure template");

            // Create deployment name
            var deploymentName = $"{generatedAssets.FunctionAppName}-{DateTime.UtcNow:yyyyMMdd-HHmmss}";

            // Convert Bicep to ARM template (simplified approach)
            var armTemplate = ConvertBicepToArmTemplate(generatedAssets.BicepTemplate, generatedAssets.FunctionAppName);

            // Create deployment content
            var deploymentContent = new ArmDeploymentContent(new ArmDeploymentProperties(ArmDeploymentMode.Incremental)
            {
                Template = BinaryData.FromString(armTemplate),
                Parameters = BinaryData.FromString(GetDeploymentParameters(generatedAssets, options))
            });

            _logger.LogDebug("Starting ARM template deployment: {DeploymentName}", deploymentName);

            // Start the deployment
            var deploymentOperation = await resourceGroup.GetArmDeployments().CreateOrUpdateAsync(
                waitUntil: Azure.WaitUntil.Completed,
                deploymentName: deploymentName,
                content: deploymentContent);

            if (deploymentOperation.HasCompleted)
            {
                _logger.LogInformation("Bicep template deployment completed successfully");
            }
            else
            {
                _logger.LogWarning("Bicep template deployment is still in progress");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy Bicep template");
            throw;
        }
    }

    private async Task UploadFunctionCodeAsync(GeneratedAssets generatedAssets, GeneratorOptions options)
    {
        try
        {
            _logger.LogInformation("Uploading function code from: {CodePath}", options.FunctionCodePath);

            string zipFilePath;

            // Handle directory vs zip file
            if (Directory.Exists(options.FunctionCodePath))
            {
                _logger.LogDebug("Code path is directory, creating zip package");
                zipFilePath = await CreateZipPackageAsync(options.FunctionCodePath!);
            }
            else if (File.Exists(options.FunctionCodePath) && Path.GetExtension(options.FunctionCodePath).Equals(".zip", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Code path is zip file, using directly");
                zipFilePath = options.FunctionCodePath!;
            }
            else
            {
                throw new FileNotFoundException($"Function code path not found or invalid: {options.FunctionCodePath}");
            }

            // Deploy using publish profile if provided
            if (!string.IsNullOrEmpty(options.PublishProfile))
            {
                await DeployUsingPublishProfileAsync(zipFilePath, options.PublishProfile, generatedAssets.FunctionAppName);
            }
            else
            {
                // Deploy using direct SCM API (simplified approach)
                await DeployUsingSCMApiAsync(zipFilePath, generatedAssets.FunctionAppName, options);
            }

            // Clean up temporary zip if we created one
            if (Directory.Exists(options.FunctionCodePath) && zipFilePath != options.FunctionCodePath)
            {
                File.Delete(zipFilePath);
            }

            _logger.LogInformation("Function code deployment completed successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload function code");
            throw;
        }
    }

    private async Task<string> CreateZipPackageAsync(string directoryPath)
    {
        var tempZipPath = Path.Combine(Path.GetTempPath(), $"function-app-{Guid.NewGuid()}.zip");

        _logger.LogDebug("Creating zip package: {ZipPath}", tempZipPath);

        // Create zip from directory
        ZipFile.CreateFromDirectory(directoryPath, tempZipPath, CompressionLevel.Optimal, false);

        var fileInfo = new FileInfo(tempZipPath);
        _logger.LogDebug("Created zip package: {Size} bytes", fileInfo.Length);

        return tempZipPath;
    }

    private async Task DeployUsingPublishProfileAsync(string zipFilePath, string publishProfilePath, string functionAppName)
    {
        try
        {
            _logger.LogInformation("Deploying using publish profile: {ProfilePath}", publishProfilePath);

            // Parse publish profile
            var publishProfile = await ParsePublishProfileAsync(publishProfilePath);

            // Create deployment request
            using var zipContent = new ByteArrayContent(await File.ReadAllBytesAsync(zipFilePath));
            zipContent.Headers.Add("Content-Type", "application/zip");

            // Build deployment URL
            var deployUrl = $"https://{publishProfile.PublishUrl}/api/zipdeploy";

            // Add authentication
            var authBytes = Encoding.ASCII.GetBytes($"{publishProfile.UserName}:{publishProfile.Password}");
            var authHeader = Convert.ToBase64String(authBytes);

            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
            client.Timeout = TimeSpan.FromMinutes(10); // Long timeout for large deployments

            _logger.LogDebug("Uploading to: {DeployUrl}", deployUrl);

            // Upload zip
            var response = await client.PostAsync(deployUrl, zipContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Code deployment successful via publish profile");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Deployment failed: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy using publish profile");
            throw;
        }
    }

    private async Task DeployUsingSCMApiAsync(string zipFilePath, string functionAppName, GeneratorOptions options)
    {
        try
        {
            _logger.LogInformation("Deploying using SCM API (requires Function App to exist)");
            _logger.LogWarning("Note: You may need to provide --publish-profile for authentication");

            // This is a simplified approach that assumes the Function App already exists
            // and the user has the necessary credentials

            var scmUrl = $"https://{functionAppName}.scm.azurewebsites.net/api/zipdeploy";

            using var zipContent = new ByteArrayContent(await File.ReadAllBytesAsync(zipFilePath));
            zipContent.Headers.Add("Content-Type", "application/zip");

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(10);

            _logger.LogDebug("Attempting deployment to: {ScmUrl}", scmUrl);
            _logger.LogWarning("This may fail without proper authentication. Consider using --publish-profile option.");

            // This will likely fail without proper authentication
            // but we'll attempt it for completeness
            var response = await client.PostAsync(scmUrl, zipContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Code deployment successful via SCM API");
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                throw new InvalidOperationException(
                    "Authentication failed. Please provide --publish-profile option or ensure your Function App allows anonymous deployments.");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Deployment failed: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deploy using SCM API");
            _logger.LogInformation("Tip: Download publish profile from Azure portal and use --publish-profile option");
            throw;
        }
    }

    private async Task<PublishProfile> ParsePublishProfileAsync(string publishProfilePath)
    {
        var content = await File.ReadAllTextAsync(publishProfilePath);
        return ParsePublishProfileXml(content);
    }

    private PublishProfile ParsePublishProfileXml(string xmlContent)
    {
        var doc = XDocument.Parse(xmlContent);
        var profile = doc.Descendants("publishProfile")
            .FirstOrDefault(p => p.Attribute("publishMethod")?.Value == "MSDeploy");

        if (profile == null)
        {
            throw new InvalidOperationException("Could not find MSDeploy publish profile");
        }

        return new PublishProfile
        {
            PublishUrl = profile.Attribute("publishUrl")?.Value ?? "",
            UserName = profile.Attribute("userName")?.Value ?? "",
            Password = profile.Attribute("userPWD")?.Value ?? "",
            SiteName = profile.Attribute("msdeploySite")?.Value ?? ""
        };
    }

    private string ConvertBicepToArmTemplate(string bicepTemplate, string functionAppName)
    {
        // Simplified ARM template conversion
        // In a real implementation, you'd use the Bicep CLI or Azure Bicep SDK
        var armTemplate = new StringBuilder();

        armTemplate.AppendLine("{");
        armTemplate.AppendLine("  \"$schema\": \"https://schema.management.azure.com/schemas/2019-04-01/deploymentTemplate.json#\",");
        armTemplate.AppendLine("  \"contentVersion\": \"1.0.0.0\",");
        armTemplate.AppendLine("  \"parameters\": {");
        armTemplate.AppendLine("    \"location\": {");
        armTemplate.AppendLine("      \"type\": \"string\",");
        armTemplate.AppendLine("      \"defaultValue\": \"[resourceGroup().location]\"");
        armTemplate.AppendLine("    },");
        armTemplate.AppendLine("    \"functionAppName\": {");
        armTemplate.AppendLine("      \"type\": \"string\",");
        armTemplate.AppendLine($"      \"defaultValue\": \"{functionAppName}\"");
        armTemplate.AppendLine("    }");
        armTemplate.AppendLine("  },");
        armTemplate.AppendLine("  \"variables\": {");
        armTemplate.AppendLine("    \"storageAccountName\": \"[concat(parameters('functionAppName'), 'storage')]\",");
        armTemplate.AppendLine("    \"appServicePlanName\": \"[concat(parameters('functionAppName'), '-plan')]\"");
        armTemplate.AppendLine("  },");
        armTemplate.AppendLine("  \"resources\": [");

        // Storage Account with blob storage support
        armTemplate.AppendLine("    {");
        armTemplate.AppendLine("      \"type\": \"Microsoft.Storage/storageAccounts\",");
        armTemplate.AppendLine("      \"apiVersion\": \"2021-06-01\",");
        armTemplate.AppendLine("      \"name\": \"[variables('storageAccountName')]\",");
        armTemplate.AppendLine("      \"location\": \"[parameters('location')]\",");
        armTemplate.AppendLine("      \"sku\": {");
        armTemplate.AppendLine("        \"name\": \"Standard_LRS\"");
        armTemplate.AppendLine("      },");
        armTemplate.AppendLine("      \"kind\": \"StorageV2\",");
        armTemplate.AppendLine("      \"properties\": {");
        armTemplate.AppendLine("        \"supportsHttpsTrafficOnly\": true,");
        armTemplate.AppendLine("        \"allowBlobPublicAccess\": false");
        armTemplate.AppendLine("      }");
        armTemplate.AppendLine("    },");

        // App Service Plan
        armTemplate.AppendLine("    {");
        armTemplate.AppendLine("      \"type\": \"Microsoft.Web/serverfarms\",");
        armTemplate.AppendLine("      \"apiVersion\": \"2021-02-01\",");
        armTemplate.AppendLine("      \"name\": \"[variables('appServicePlanName')]\",");
        armTemplate.AppendLine("      \"location\": \"[parameters('location')]\",");
        armTemplate.AppendLine("      \"sku\": {");
        armTemplate.AppendLine("        \"name\": \"Y1\",");
        armTemplate.AppendLine("        \"tier\": \"Dynamic\"");
        armTemplate.AppendLine("      }");
        armTemplate.AppendLine("    },");

        // Function App with blob storage connection
        armTemplate.AppendLine("    {");
        armTemplate.AppendLine("      \"type\": \"Microsoft.Web/sites\",");
        armTemplate.AppendLine("      \"apiVersion\": \"2021-02-01\",");
        armTemplate.AppendLine("      \"name\": \"[parameters('functionAppName')]\",");
        armTemplate.AppendLine("      \"location\": \"[parameters('location')]\",");
        armTemplate.AppendLine("      \"kind\": \"functionapp\",");
        armTemplate.AppendLine("      \"dependsOn\": [");
        armTemplate.AppendLine("        \"[resourceId('Microsoft.Web/serverfarms', variables('appServicePlanName'))]\",");
        armTemplate.AppendLine("        \"[resourceId('Microsoft.Storage/storageAccounts', variables('storageAccountName'))]\"");
        armTemplate.AppendLine("      ],");
        armTemplate.AppendLine("      \"properties\": {");
        armTemplate.AppendLine("        \"serverFarmId\": \"[resourceId('Microsoft.Web/serverfarms', variables('appServicePlanName'))]\",");
        armTemplate.AppendLine("        \"siteConfig\": {");
        armTemplate.AppendLine("          \"appSettings\": [");
        armTemplate.AppendLine("            {");
        armTemplate.AppendLine("              \"name\": \"AzureWebJobsStorage\",");
        armTemplate.AppendLine("              \"value\": \"[concat('DefaultEndpointsProtocol=https;AccountName=', variables('storageAccountName'), ';EndpointSuffix=', environment().suffixes.storage, ';AccountKey=', listKeys(resourceId('Microsoft.Storage/storageAccounts', variables('storageAccountName')), '2021-06-01').keys[0].value)]\"");
        armTemplate.AppendLine("            },");
        armTemplate.AppendLine("            {");
        armTemplate.AppendLine("              \"name\": \"WEBSITE_CONTENTAZUREFILECONNECTIONSTRING\",");
        armTemplate.AppendLine("              \"value\": \"[concat('DefaultEndpointsProtocol=https;AccountName=', variables('storageAccountName'), ';EndpointSuffix=', environment().suffixes.storage, ';AccountKey=', listKeys(resourceId('Microsoft.Storage/storageAccounts', variables('storageAccountName')), '2021-06-01').keys[0].value)]\"");
        armTemplate.AppendLine("            },");
        armTemplate.AppendLine("            {");
        armTemplate.AppendLine("              \"name\": \"BLOB_STORAGE_CONNECTION_STRING\",");
        armTemplate.AppendLine("              \"value\": \"[concat('DefaultEndpointsProtocol=https;AccountName=', variables('storageAccountName'), ';EndpointSuffix=', environment().suffixes.storage, ';AccountKey=', listKeys(resourceId('Microsoft.Storage/storageAccounts', variables('storageAccountName')), '2021-06-01').keys[0].value)]\"");
        armTemplate.AppendLine("            },");
        armTemplate.AppendLine("            {");
        armTemplate.AppendLine("              \"name\": \"FUNCTIONS_EXTENSION_VERSION\",");
        armTemplate.AppendLine("              \"value\": \"~4\"");
        armTemplate.AppendLine("            },");
        armTemplate.AppendLine("            {");
        armTemplate.AppendLine("              \"name\": \"FUNCTIONS_WORKER_RUNTIME\",");
        armTemplate.AppendLine("              \"value\": \"dotnet-isolated\"");
        armTemplate.AppendLine("            },");
        armTemplate.AppendLine("            {");
        armTemplate.AppendLine("              \"name\": \"TOKEN_SECRET\",");
        armTemplate.AppendLine("              \"value\": \"[uniqueString(resourceGroup().id, parameters('functionAppName'))]\"");
        armTemplate.AppendLine("            }");
        armTemplate.AppendLine("          ]");
        armTemplate.AppendLine("        }");
        armTemplate.AppendLine("      }");
        armTemplate.AppendLine("    }");
        armTemplate.AppendLine("  ]");
        armTemplate.AppendLine("}");

        return armTemplate.ToString();
    }

    private string GetDeploymentParameters(GeneratedAssets generatedAssets, GeneratorOptions options)
    {
        return @"{
  ""$schema"": ""https://schema.management.azure.com/schemas/2019-04-01/deploymentParameters.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {}
}";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

// Supporting classes for publish profile parsing
public class PublishProfile
{
    public string PublishUrl { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string SiteName { get; set; } = string.Empty;
}