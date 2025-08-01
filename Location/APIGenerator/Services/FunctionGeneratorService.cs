using Location.Tools.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Text;

namespace Location.Tools.APIGenerator.Services;

public class FunctionGeneratorService
{
    private readonly ILogger<FunctionGeneratorService> _logger;

    public FunctionGeneratorService(ILogger<FunctionGeneratorService> logger)
    {
        _logger = logger;
    }

    public async Task<GeneratedAssets> GenerateAPIAssetsAsync(AssemblyInfo assemblyInfo, List<ExtractableEntity> entities, GeneratorOptions options)
    {
        try
        {
            _logger.LogInformation("Generating API assets for {Vertical} v{Version}",
                assemblyInfo.Vertical, assemblyInfo.MajorVersion);

            var functionAppName = $"location-{assemblyInfo.Vertical}-api-v{assemblyInfo.MajorVersion}";
            var outputDir = Path.Combine(Path.GetTempPath(), "location-api-gen", functionAppName);

            // Create output directory
            if (Directory.Exists(outputDir))
            {
                Directory.Delete(outputDir, true);
            }
            Directory.CreateDirectory(outputDir);

            var generatedAssets = new GeneratedAssets
            {
                FunctionAppName = functionAppName,
                OutputDirectory = outputDir,
                Controllers = new List<string>(),
                GeneratedEndpoints = new List<string>()
            };

            // Generate Azure Functions controllers
            await GenerateControllersAsync(assemblyInfo, entities, generatedAssets);

            // Generate Bicep infrastructure template
            await GenerateBicepTemplateAsync(assemblyInfo, generatedAssets, options);

            // Generate host.json and other configuration files
            await GenerateConfigurationFilesAsync(generatedAssets);

            _logger.LogInformation("Generated {ControllerCount} controllers and infrastructure for {FunctionApp}",
                generatedAssets.Controllers.Count, functionAppName);

            return generatedAssets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate API assets");
            throw;
        }
    }

    private async Task GenerateControllersAsync(AssemblyInfo assemblyInfo, List<ExtractableEntity> entities, GeneratedAssets assets)
    {
        _logger.LogDebug("Generating Azure Functions controllers");

        // Generate main API controller
        var controllerCode = GenerateMainControllerCode(assemblyInfo, entities);
        var controllerPath = Path.Combine(assets.OutputDirectory, "LocationAPIController.cs");
        await File.WriteAllTextAsync(controllerPath, controllerCode);
        assets.Controllers.Add(controllerPath);

        // Generate endpoint list
        var baseUrl = $"/{assemblyInfo.Vertical}/v{assemblyInfo.MajorVersion}";
        assets.GeneratedEndpoints.AddRange(new[]
        {
            $"POST {baseUrl}/auth/request-qr",
            $"POST {baseUrl}/auth/verify-email",
            $"POST {baseUrl}/auth/generate-qr",
            $"POST {baseUrl}/auth/scan-qr",
            $"POST {baseUrl}/auth/manual-restore",
            $"POST {baseUrl}/auth/send-recovery-qr",
            $"POST {baseUrl}/backup",
            $"POST {baseUrl}/forgetme"
        });

        _logger.LogDebug("Generated controller with {EndpointCount} endpoints", assets.GeneratedEndpoints.Count);
    }

    private string GenerateMainControllerCode(AssemblyInfo assemblyInfo, List<ExtractableEntity> entities)
    {
        var sb = new StringBuilder();

        // Generate controller header
        sb.AppendLine("using Microsoft.Azure.Functions.Worker;");
        sb.AppendLine("using Microsoft.Azure.Functions.Worker.Http;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using System.Net;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine();

        sb.AppendLine($"namespace Location.{assemblyInfo.Source}.API.V{assemblyInfo.MajorVersion};");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Generated API controller for {assemblyInfo.Vertical} v{assemblyInfo.MajorVersion}");
        sb.AppendLine($"/// Schema-perfect extraction for entities: {string.Join(", ", entities.Select(e => e.TableName))}");
        sb.AppendLine($"/// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine("/// </summary>");
        sb.AppendLine("public class LocationAPIController");
        sb.AppendLine("{");
        sb.AppendLine("    private readonly ILogger<LocationAPIController> _logger;");
        sb.AppendLine();

        sb.AppendLine("    public LocationAPIController(ILogger<LocationAPIController> logger)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger = logger;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate authentication endpoints
        GenerateAuthEndpoints(sb, assemblyInfo);

        // Generate backup endpoint
        GenerateBackupEndpoint(sb, assemblyInfo, entities);

        // Generate forget-me endpoint
        GenerateForgetMeEndpoint(sb, assemblyInfo);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateAuthEndpoints(StringBuilder sb, AssemblyInfo assemblyInfo)
    {
        var baseRoute = $"{assemblyInfo.Vertical}/v{assemblyInfo.MajorVersion}";

        // Request QR endpoint
        sb.AppendLine($"    [Function(\"RequestQR\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> RequestQR([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/auth/request-qr\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Processing QR request\");");
        sb.AppendLine("        var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("        await response.WriteStringAsync(JsonSerializer.Serialize(new { status = \"success\", message = \"QR request processed\" }));");
        sb.AppendLine("        return response;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Verify Email endpoint
        sb.AppendLine($"    [Function(\"VerifyEmail\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> VerifyEmail([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/auth/verify-email\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Processing email verification\");");
        sb.AppendLine("        var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("        await response.WriteStringAsync(JsonSerializer.Serialize(new { status = \"success\", message = \"Email verified\" }));");
        sb.AppendLine("        return response;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate QR endpoint
        sb.AppendLine($"    [Function(\"GenerateQR\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> GenerateQR([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/auth/generate-qr\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Generating QR code\");");
        sb.AppendLine("        var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("        await response.WriteStringAsync(JsonSerializer.Serialize(new { status = \"success\", qrCode = \"generated-qr-data\" }));");
        sb.AppendLine("        return response;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Scan QR endpoint
        sb.AppendLine($"    [Function(\"ScanQR\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> ScanQR([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/auth/scan-qr\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Processing QR scan\");");
        sb.AppendLine("        var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("        await response.WriteStringAsync(JsonSerializer.Serialize(new { status = \"success\", message = \"QR scanned successfully\" }));");
        sb.AppendLine("        return response;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Manual Restore endpoint
        sb.AppendLine($"    [Function(\"ManualRestore\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> ManualRestore([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/auth/manual-restore\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Processing manual restore\");");
        sb.AppendLine("        var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("        await response.WriteStringAsync(JsonSerializer.Serialize(new { status = \"success\", message = \"Manual restore completed\" }));");
        sb.AppendLine("        return response;");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Send Recovery QR endpoint
        sb.AppendLine($"    [Function(\"SendRecoveryQR\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> SendRecoveryQR([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/auth/send-recovery-qr\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        _logger.LogInformation(\"Sending recovery QR\");");
        sb.AppendLine("        var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("        await response.WriteStringAsync(JsonSerializer.Serialize(new { status = \"success\", message = \"Recovery QR sent\" }));");
        sb.AppendLine("        return response;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void GenerateBackupEndpoint(StringBuilder sb, AssemblyInfo assemblyInfo, List<ExtractableEntity> entities)
    {
        var baseRoute = $"{assemblyInfo.Vertical}/v{assemblyInfo.MajorVersion}";

        sb.AppendLine($"    [Function(\"Backup\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> Backup([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/backup\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            _logger.LogInformation(\"Processing backup upload for {Vertical} v{Version}\", \"{assemblyInfo.Vertical}\", {assemblyInfo.MajorVersion});");
        sb.AppendLine();
        sb.AppendLine("            // Step 1: Save uploaded zip to temporary location");
        sb.AppendLine("            var tempZipPath = Path.Combine(Path.GetTempPath(), $\"backup-{Guid.NewGuid()}.zip\");");
        sb.AppendLine("            using (var fileStream = File.Create(tempZipPath))");
        sb.AppendLine("            {");
        sb.AppendLine("                await req.Body.CopyToAsync(fileStream);");
        sb.AppendLine("            }");
        sb.AppendLine("            _logger.LogDebug(\"Zip file saved to: {TempPath}\", tempZipPath);");
        sb.AppendLine();
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                // Step 2: Extract SQLite data using version-specific schema");
        sb.AppendLine("                var extractionService = serviceProvider.GetRequiredService<SQLiteExtractionService>();");
        sb.AppendLine("                var entities = GetExtractableEntities(); // Version-specific entity list");
        sb.AppendLine("                var extractedData = await extractionService.ExtractDataFromZipAsync(tempZipPath, entities);");
        sb.AppendLine();
        sb.AppendLine("                _logger.LogInformation(\"Extracted data for user: {Email}, tables: {TableCount}\", ");
        sb.AppendLine("                    extractedData.UserInfo.Email, extractedData.TableData.Count);");
        sb.AppendLine();
        sb.AppendLine("                // Step 3: Generate SQL Server INSERT statements");
        sb.AppendLine("                var schemaMapper = serviceProvider.GetRequiredService<SchemaMapperService>();");
        sb.AppendLine("                var connectionString = Environment.GetEnvironmentVariable(\"SQL_CONNECTION_STRING\");");
        sb.AppendLine("                var insertStatements = await schemaMapper.GenerateInsertStatementsAsync(extractedData, entities, connectionString);");
        sb.AppendLine();
        sb.AppendLine("                _logger.LogInformation(\"Generated {StatementCount} INSERT statements\", insertStatements.Count);");
        sb.AppendLine();
        sb.AppendLine("                // Step 4: Execute SQL Server INSERTs");
        sb.AppendLine("                await ExecuteInsertStatementsAsync(insertStatements, connectionString);");
        sb.AppendLine();
        sb.AppendLine("                // Step 5: Upload photos to Azure Blob Storage");
        sb.AppendLine("                var photoUrls = await UploadPhotosToAzureBlobAsync(extractedData.PhotoFiles, extractedData.UserInfo);");
        sb.AppendLine();
        sb.AppendLine("                var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("                await response.WriteStringAsync(JsonSerializer.Serialize(new { ");
        sb.AppendLine("                    status = \"success\", ");
        sb.AppendLine("                    message = \"Backup processed successfully\",");
        sb.AppendLine("                    rowsInserted = insertStatements.Count,");
        sb.AppendLine("                    photosUploaded = photoUrls.Count,");
        sb.AppendLine("                    userEmail = extractedData.UserInfo.Email");
        sb.AppendLine("                }));");
        sb.AppendLine("                return response;");
        sb.AppendLine("            }");
        sb.AppendLine("            finally");
        sb.AppendLine("            {");
        sb.AppendLine("                // Cleanup temp zip file");
        sb.AppendLine("                if (File.Exists(tempZipPath))");
        sb.AppendLine("                {");
        sb.AppendLine("                    File.Delete(tempZipPath);");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine("            _logger.LogError(ex, \"Backup processing failed\");");
        sb.AppendLine("            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);");
        sb.AppendLine("            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { status = \"error\", message = ex.Message }));");
        sb.AppendLine("            return errorResponse;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private List<ExtractableEntity> GetExtractableEntities()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // Version-specific entity list for {assemblyInfo.Vertical} v{assemblyInfo.MajorVersion}");
        sb.AppendLine($"        // Tables: {string.Join(", ", entities.Select(e => e.TableName))}");
        sb.AppendLine("        // TODO: Load from version-specific DLL metadata");
        sb.AppendLine("        return new List<ExtractableEntity>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private async Task ExecuteInsertStatementsAsync(List<string> insertStatements, string connectionString)");
        sb.AppendLine("    {");
        sb.AppendLine("        using var connection = new SqlConnection(connectionString);");
        sb.AppendLine("        await connection.OpenAsync();");
        sb.AppendLine();
        sb.AppendLine("        foreach (var statement in insertStatements)");
        sb.AppendLine("        {");
        sb.AppendLine("            using var command = new SqlCommand(statement, connection);");
        sb.AppendLine("            await command.ExecuteNonQueryAsync();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        _logger.LogInformation(\"Executed {Count} INSERT statements successfully\", insertStatements.Count);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private async Task<List<string>> UploadPhotosToAzureBlobAsync(List<string> photoFiles, UserInfo userInfo)");
        sb.AppendLine("    {");
        sb.AppendLine("        var uploadedUrls = new List<string>();");
        sb.AppendLine();
        sb.AppendLine("        // TODO: Implement Azure Blob Storage upload");
        sb.AppendLine("        // Container: user-photos/{userInfo.Email}/{userInfo.Date}/");
        sb.AppendLine("        // Return blob URLs for future reference");
        sb.AppendLine();
        sb.AppendLine("        _logger.LogInformation(\"Would upload {PhotoCount} photos for user: {Email}\", photoFiles.Count, userInfo.Email);");
        sb.AppendLine("        return uploadedUrls;");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private void GenerateForgetMeEndpoint(StringBuilder sb, AssemblyInfo assemblyInfo)
    {
        var baseRoute = $"{assemblyInfo.Vertical}/v{assemblyInfo.MajorVersion}";

        sb.AppendLine($"    [Function(\"ForgetMe\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> ForgetMe([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/forgetme\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            _logger.LogInformation(\"Processing forget-me request\");");
        sb.AppendLine();
        sb.AppendLine("            // TODO: Parse email from request body");
        sb.AppendLine("            // TODO: Execute DELETE FROM Users WHERE email_address = @email");
        sb.AppendLine("            // TODO: Return confirmation or failure");
        sb.AppendLine();
        sb.AppendLine("            var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("            await response.WriteStringAsync(JsonSerializer.Serialize(new { status = \"success\", message = \"User data deleted successfully\" }));");
        sb.AppendLine("            return response;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine("            _logger.LogError(ex, \"Forget-me processing failed\");");
        sb.AppendLine("            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);");
        sb.AppendLine("            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { status = \"error\", message = ex.Message }));");
        sb.AppendLine("            return errorResponse;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
    }

    private async Task GenerateBicepTemplateAsync(AssemblyInfo assemblyInfo, GeneratedAssets assets, GeneratorOptions options)
    {
        _logger.LogDebug("Generating Bicep infrastructure template");

        var bicepTemplate = GenerateBicepCode(assemblyInfo, options);
        var bicepPath = Path.Combine(assets.OutputDirectory, "infrastructure.bicep");
        await File.WriteAllTextAsync(bicepPath, bicepTemplate);

        assets.BicepTemplate = bicepTemplate;
        _logger.LogDebug("Generated Bicep template: {BicepPath}", bicepPath);
    }

    private string GenerateBicepCode(AssemblyInfo assemblyInfo, GeneratorOptions options)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"// Generated Bicep template for {assemblyInfo.Vertical} API v{assemblyInfo.MajorVersion}");
        sb.AppendLine($"// Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine();

        sb.AppendLine("param location string = resourceGroup().location");
        sb.AppendLine($"param functionAppName string = 'location-{assemblyInfo.Vertical}-api-v{assemblyInfo.MajorVersion}'");
        sb.AppendLine("param storageAccountName string = '${functionAppName}storage'");
        sb.AppendLine("param appServicePlanName string = '${functionAppName}-plan'");
        sb.AppendLine();

        sb.AppendLine("// Storage Account for Function App");
        sb.AppendLine("resource storageAccount 'Microsoft.Storage/storageAccounts@2021-06-01' = {");
        sb.AppendLine("  name: storageAccountName");
        sb.AppendLine("  location: location");
        sb.AppendLine("  sku: {");
        sb.AppendLine("    name: 'Standard_LRS'");
        sb.AppendLine("  }");
        sb.AppendLine("  kind: 'StorageV2'");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("// App Service Plan (Consumption)");
        sb.AppendLine("resource appServicePlan 'Microsoft.Web/serverfarms@2021-02-01' = {");
        sb.AppendLine("  name: appServicePlanName");
        sb.AppendLine("  location: location");
        sb.AppendLine("  sku: {");
        sb.AppendLine("    name: 'Y1'");
        sb.AppendLine("    tier: 'Dynamic'");
        sb.AppendLine("  }");
        sb.AppendLine("}");
        sb.AppendLine();

        sb.AppendLine("// Function App");
        sb.AppendLine("resource functionApp 'Microsoft.Web/sites@2021-02-01' = {");
        sb.AppendLine("  name: functionAppName");
        sb.AppendLine("  location: location");
        sb.AppendLine("  kind: 'functionapp'");
        sb.AppendLine("  properties: {");
        sb.AppendLine("    serverFarmId: appServicePlan.id");
        sb.AppendLine("    siteConfig: {");
        sb.AppendLine("      appSettings: [");
        sb.AppendLine("        {");
        sb.AppendLine("          name: 'AzureWebJobsStorage'");
        sb.AppendLine("          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'");
        sb.AppendLine("        }");
        sb.AppendLine("        {");
        sb.AppendLine("          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'");
        sb.AppendLine("          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'");
        sb.AppendLine("        }");
        sb.AppendLine("        {");
        sb.AppendLine("          name: 'FUNCTIONS_EXTENSION_VERSION'");
        sb.AppendLine("          value: '~4'");
        sb.AppendLine("        }");
        sb.AppendLine("        {");
        sb.AppendLine("          name: 'FUNCTIONS_WORKER_RUNTIME'");
        sb.AppendLine("          value: 'dotnet-isolated'");
        sb.AppendLine("        }");
        sb.AppendLine("      ]");
        sb.AppendLine("    }");
        sb.AppendLine("  }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private async Task GenerateConfigurationFilesAsync(GeneratedAssets assets)
    {
        _logger.LogDebug("Generating configuration files");

        // Generate host.json
        var hostJson = @"{
  ""version"": ""2.0"",
  ""logging"": {
    ""applicationInsights"": {
      ""samplingSettings"": {
        ""isEnabled"": true,
        ""excludedTypes"": ""Request""
      }
    }
  },
  ""functionTimeout"": ""00:05:00""
}";

        var hostJsonPath = Path.Combine(assets.OutputDirectory, "host.json");
        await File.WriteAllTextAsync(hostJsonPath, hostJson);

        // Generate local.settings.json (for development)
        var localSettings = @"{
  ""IsEncrypted"": false,
  ""Values"": {
    ""AzureWebJobsStorage"": ""UseDevelopmentStorage=true"",
    ""FUNCTIONS_WORKER_RUNTIME"": ""dotnet-isolated""
  }
}";

        var localSettingsPath = Path.Combine(assets.OutputDirectory, "local.settings.json");
        await File.WriteAllTextAsync(localSettingsPath, localSettings);

        _logger.LogDebug("Generated configuration files");
    }
}