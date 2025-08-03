// Enhanced FunctionGeneratorService with Extended ASCII support
// File: x3squaredCircles.API.Generator/Services/FunctionGeneratorService.cs

using x3squaredcirecles.API.Generator.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Globalization;

namespace x3squaredcirecles.API.Generator.APIGenerator.Services;

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
            _logger.LogInformation("Generating API assets for {Vertical} v{Version} with extended ASCII support",
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

            // Generate Azure Functions controllers with extended ASCII support
            await GenerateControllersWithExtendedASCIIAsync(assemblyInfo, entities, generatedAssets);

            // Generate Bicep infrastructure template
            await GenerateBicepTemplateAsync(assemblyInfo, generatedAssets, options);

            // Generate host.json and other configuration files
            await GenerateConfigurationFilesAsync(generatedAssets);

            _logger.LogInformation("Generated {ControllerCount} controllers and infrastructure for {FunctionApp} with extended ASCII support",
                generatedAssets.Controllers.Count, functionAppName);

            return generatedAssets;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate API assets");
            throw;
        }
    }

    private async Task GenerateControllersWithExtendedASCIIAsync(AssemblyInfo assemblyInfo, List<ExtractableEntity> entities, GeneratedAssets assets)
    {
        _logger.LogDebug("Generating Azure Functions controllers with extended ASCII support");

        // Generate main API controller with extended ASCII handling
        var controllerCode = GenerateMainControllerCodeWithExtendedASCII(assemblyInfo, entities);
        var controllerPath = Path.Combine(assets.OutputDirectory, "LocationAPIController.cs");

        // Write with UTF-8 encoding to preserve extended ASCII characters
        await File.WriteAllTextAsync(controllerPath, controllerCode, Encoding.UTF8);
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

        _logger.LogDebug("Generated controller with {EndpointCount} endpoints and extended ASCII support", assets.GeneratedEndpoints.Count);
    }

    private string GenerateMainControllerCodeWithExtendedASCII(AssemblyInfo assemblyInfo, List<ExtractableEntity> entities)
    {
        var sb = new StringBuilder();

        // Generate controller header with extended ASCII support
        sb.AppendLine("using Microsoft.Azure.Functions.Worker;");
        sb.AppendLine("using Microsoft.Azure.Functions.Worker.Http;");
        sb.AppendLine("using Microsoft.Extensions.Logging;");
        sb.AppendLine("using Microsoft.Data.SqlClient;");
        sb.AppendLine("using System.Net;");
        sb.AppendLine("using System.Text.Json;");
        sb.AppendLine("using System.Text;");
        sb.AppendLine("using System.Globalization;");
        sb.AppendLine("using System.Data;");
        sb.AppendLine();

        sb.AppendLine($"namespace Location.{assemblyInfo.Source}.API.V{assemblyInfo.MajorVersion};");
        sb.AppendLine();

        sb.AppendLine("/// <summary>");
        sb.AppendLine($"/// Generated API controller for {assemblyInfo.Vertical} v{assemblyInfo.MajorVersion} with extended ASCII support");
        sb.AppendLine($"/// Schema-perfect extraction for entities: {string.Join(", ", entities.Select(e => e.TableName))}");
        sb.AppendLine($"/// Supports extended ASCII characters: café, résumé, naïve, €, £, ¥, ©, ®, ™");
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

        // Generate authentication endpoints with extended ASCII support
        GenerateAuthEndpointsWithExtendedASCII(sb, assemblyInfo);

        // Generate backup endpoint with extended ASCII handling
        GenerateBackupEndpointWithExtendedASCII(sb, assemblyInfo, entities);

        // Generate forget-me endpoint
        GenerateForgetMeEndpointWithExtendedASCII(sb, assemblyInfo);

        sb.AppendLine("}");

        return sb.ToString();
    }

    private void GenerateAuthEndpointsWithExtendedASCII(StringBuilder sb, AssemblyInfo assemblyInfo)
    {
        var baseRoute = $"{assemblyInfo.Vertical}/v{assemblyInfo.MajorVersion}";

        // Request QR endpoint with extended ASCII email support
        sb.AppendLine($"    [Function(\"RequestQR\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> RequestQR([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/auth/request-qr\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine($"            _logger.LogInformation(\"Processing QR request for {{Vertical}} with extended ASCII support\", \"{assemblyInfo.Vertical}\");");
        sb.AppendLine();
        sb.AppendLine("            var requestBody = await JsonSerializer.DeserializeAsync<RegistrationRequest>(req.Body);");
        sb.AppendLine($"            var appType = \"{assemblyInfo.Vertical}\";");
        sb.AppendLine();
        sb.AppendLine("            // Normalize extended ASCII characters in email address");
        sb.AppendLine("            var normalizedEmail = NormalizeExtendedASCII(requestBody.Email);");
        sb.AppendLine("            _logger.LogDebug(\"Email normalization: {Original} -> {Normalized}\", requestBody.Email, normalizedEmail);");
        sb.AppendLine();
        sb.AppendLine("            // Step 1: Upsert AppType");
        sb.AppendLine("            var appTypeId = await UpsertAppTypeAsync(appType);");
        sb.AppendLine();
        sb.AppendLine("            // Step 2: Register/Update User with extended ASCII email support");
        sb.AppendLine("            await UpsertUserRegistrationAsync(normalizedEmail, requestBody.AppGuid, appTypeId);");
        sb.AppendLine();
        sb.AppendLine("            _logger.LogInformation(\"QR request processed for user: {Email}, appType: {AppType}\", normalizedEmail, appType);");
        sb.AppendLine();
        sb.AppendLine("            var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("            await response.WriteStringAsync(JsonSerializer.Serialize(new { ");
        sb.AppendLine("                status = \"success\", ");
        sb.AppendLine("                message = \"QR request processed\",");
        sb.AppendLine("                appTypeId = appTypeId,");
        sb.AppendLine("                normalizedEmail = normalizedEmail");
        sb.AppendLine("            }));");
        sb.AppendLine("            return response;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine("            _logger.LogError(ex, \"QR request processing failed\");");
        sb.AppendLine("            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);");
        sb.AppendLine("            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { status = \"error\", message = ex.Message }));");
        sb.AppendLine("            return errorResponse;");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Generate other auth endpoints with similar extended ASCII support
        GenerateOtherAuthEndpoints(sb, baseRoute, assemblyInfo);

        // Add helper methods with extended ASCII support
        GenerateExtendedASCIIHelperMethods(sb);
    }

    private void GenerateOtherAuthEndpoints(StringBuilder sb, string baseRoute, AssemblyInfo assemblyInfo)
    {
        var endpoints = new[]
        {
            ("VerifyEmail", "verify-email", "Email verification"),
            ("GenerateQR", "generate-qr", "QR generation"),
            ("ScanQR", "scan-qr", "QR scan processing"),
            ("ManualRestore", "manual-restore", "Manual restore processing"),
            ("SendRecoveryQR", "send-recovery-qr", "Recovery QR sending")
        };

        foreach (var (functionName, route, description) in endpoints)
        {
            sb.AppendLine($"    [Function(\"{functionName}\")]");
            sb.AppendLine($"    public async Task<HttpResponseData> {functionName}([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/auth/{route}\")] HttpRequestData req)");
            sb.AppendLine("    {");
            sb.AppendLine("        try");
            sb.AppendLine("        {");
            sb.AppendLine($"            _logger.LogInformation(\"{description} for {{Vertical}} with extended ASCII support\", \"{assemblyInfo.Vertical}\");");
            sb.AppendLine();
            sb.AppendLine("            var requestBody = await JsonSerializer.DeserializeAsync<RegistrationRequest>(req.Body);");
            sb.AppendLine($"            var appType = \"{assemblyInfo.Vertical}\";");
            sb.AppendLine();
            sb.AppendLine("            // Normalize extended ASCII characters in email");
            sb.AppendLine("            var normalizedEmail = NormalizeExtendedASCII(requestBody.Email);");
            sb.AppendLine();
            sb.AppendLine("            // Step 1: Upsert AppType");
            sb.AppendLine("            var appTypeId = await UpsertAppTypeAsync(appType);");
            sb.AppendLine();
            sb.AppendLine("            // Step 2: Register/Update User");
            sb.AppendLine("            await UpsertUserRegistrationAsync(normalizedEmail, requestBody.AppGuid, appTypeId);");
            sb.AppendLine();
            sb.AppendLine("            var response = req.CreateResponse(HttpStatusCode.OK);");
            sb.AppendLine("            await response.WriteStringAsync(JsonSerializer.Serialize(new { ");
            sb.AppendLine("                status = \"success\", ");
            sb.AppendLine($"                message = \"{description} completed\",");
            sb.AppendLine("                appTypeId = appTypeId,");
            sb.AppendLine("                normalizedEmail = normalizedEmail");
            sb.AppendLine("            }));");
            sb.AppendLine("            return response;");
            sb.AppendLine("        }");
            sb.AppendLine("        catch (Exception ex)");
            sb.AppendLine("        {");
            sb.AppendLine($"            _logger.LogError(ex, \"{description} failed\");");
            sb.AppendLine("            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);");
            sb.AppendLine("            await errorResponse.WriteStringAsync(JsonSerializer.Serialize(new { status = \"error\", message = ex.Message }));");
            sb.AppendLine("            return errorResponse;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }
    }

    private void GenerateExtendedASCIIHelperMethods(StringBuilder sb)
    {
        sb.AppendLine("    // Extended ASCII helper methods");
        sb.AppendLine("    private string NormalizeExtendedASCII(string input)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (string.IsNullOrEmpty(input))");
        sb.AppendLine("            return input;");
        sb.AppendLine();
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine("            // Normalize to consistent Unicode form (Canonical Composition)");
        sb.AppendLine("            var normalized = input.Normalize(NormalizationForm.FormC);");
        sb.AppendLine("            ");
        sb.AppendLine("            // Log if extended ASCII characters are present");
        sb.AppendLine("            if (normalized.Any(c => c > 127))");
        sb.AppendLine("            {");
        sb.AppendLine("                _logger.LogDebug(\"Extended ASCII characters detected in: {Input}\", ");
        sb.AppendLine("                    normalized.Length > 50 ? normalized.Substring(0, 50) + \"...\" : normalized);");
        sb.AppendLine("            }");
        sb.AppendLine("            ");
        sb.AppendLine("            return normalized;");
        sb.AppendLine("        }");
        sb.AppendLine("        catch (Exception ex)");
        sb.AppendLine("        {");
        sb.AppendLine("            _logger.LogWarning(ex, \"Failed to normalize extended ASCII for: {Input}\", input);");
        sb.AppendLine("            return input; // Return original if normalization fails");
        sb.AppendLine("        }");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private string EscapeSqlStringWithExtendedASCII(string input)");
        sb.AppendLine("    {");
        sb.AppendLine("        if (string.IsNullOrEmpty(input))");
        sb.AppendLine("            return \"\";");
        sb.AppendLine();
        sb.AppendLine("        // Escape single quotes and preserve extended ASCII characters");
        sb.AppendLine("        return input.Replace(\"'\", \"''\");");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Registration helper methods with extended ASCII support
        GenerateRegistrationHelperMethodsWithExtendedASCII(sb);
    }

    private void GenerateRegistrationHelperMethodsWithExtendedASCII(StringBuilder sb)
    {
        sb.AppendLine("    // Registration helper methods with extended ASCII support");
        sb.AppendLine("    private async Task<int> UpsertAppTypeAsync(string appTypeName)");
        sb.AppendLine("    {");
        sb.AppendLine("        var connectionString = Environment.GetEnvironmentVariable(\"SQL_CONNECTION_STRING\");");
        sb.AppendLine("        using var connection = new SqlConnection(connectionString);");
        sb.AppendLine("        await connection.OpenAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Use parameterized query to handle extended ASCII safely");
        sb.AppendLine("        var query = @\"");
        sb.AppendLine("            IF NOT EXISTS (SELECT 1 FROM [Core].[AppTypes] WHERE TypeName = @AppTypeName)");
        sb.AppendLine("            BEGIN");
        sb.AppendLine("                INSERT INTO [Core].[AppTypes] (TypeName, DisplayName, IsActive, DateCreated)");
        sb.AppendLine("                VALUES (@AppTypeName, @AppTypeName, 1, GETUTCDATE())");
        sb.AppendLine("            END");
        sb.AppendLine("            ");
        sb.AppendLine("            SELECT Id FROM [Core].[AppTypes] WHERE TypeName = @AppTypeName\";");
        sb.AppendLine();
        sb.AppendLine("        using var command = new SqlCommand(query, connection);");
        sb.AppendLine("        command.Parameters.Add(\"@AppTypeName\", SqlDbType.NVarChar, 255).Value = appTypeName;");
        sb.AppendLine();
        sb.AppendLine("        var result = await command.ExecuteScalarAsync();");
        sb.AppendLine("        var appTypeId = Convert.ToInt32(result);");
        sb.AppendLine();
        sb.AppendLine("        _logger.LogDebug(\"Upserted AppType with extended ASCII support: {AppTypeName} -> ID: {AppTypeId}\", appTypeName, appTypeId);");
        sb.AppendLine("        return appTypeId;");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private async Task UpsertUserRegistrationAsync(string email, string appGuid, int appTypeId)");
        sb.AppendLine("    {");
        sb.AppendLine("        var connectionString = Environment.GetEnvironmentVariable(\"SQL_CONNECTION_STRING\");");
        sb.AppendLine("        using var connection = new SqlConnection(connectionString);");
        sb.AppendLine("        await connection.OpenAsync();");
        sb.AppendLine();
        sb.AppendLine("        // Use parameterized query with NVARCHAR to handle extended ASCII in emails");
        sb.AppendLine("        var query = @\"");
        sb.AppendLine("            IF NOT EXISTS (SELECT 1 FROM [Core].[UserRegistrations] ");
        sb.AppendLine("                          WHERE UserEmail = @Email AND UserAppGuid = @AppGuid)");
        sb.AppendLine("            BEGIN");
        sb.AppendLine("                INSERT INTO [Core].[UserRegistrations] (UserEmail, UserAppGuid, AppTypeId, DateCreated)");
        sb.AppendLine("                VALUES (@Email, @AppGuid, @AppTypeId, GETUTCDATE())");
        sb.AppendLine("            END");
        sb.AppendLine("            ELSE");
        sb.AppendLine("            BEGIN");
        sb.AppendLine("                UPDATE [Core].[UserRegistrations] ");
        sb.AppendLine("                SET AppTypeId = @AppTypeId, DateCreated = GETUTCDATE()");
        sb.AppendLine("                WHERE UserEmail = @Email AND UserAppGuid = @AppGuid");
        sb.AppendLine("            END\";");
        sb.AppendLine();
        sb.AppendLine("        using var command = new SqlCommand(query, connection);");
        sb.AppendLine("        // Use NVARCHAR parameters to preserve extended ASCII characters");
        sb.AppendLine("        command.Parameters.Add(\"@Email\", SqlDbType.NVarChar, 255).Value = email;");
        sb.AppendLine("        command.Parameters.Add(\"@AppGuid\", SqlDbType.NVarChar, 255).Value = appGuid;");
        sb.AppendLine("        command.Parameters.Add(\"@AppTypeId\", SqlDbType.Int).Value = appTypeId;");
        sb.AppendLine();
        sb.AppendLine("        await command.ExecuteNonQueryAsync();");
        sb.AppendLine();
        sb.AppendLine("        _logger.LogDebug(\"Upserted UserRegistration with extended ASCII support: {Email}, AppTypeId: {AppTypeId}\", email, appTypeId);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    // Registration request model with extended ASCII support");
        sb.AppendLine("    public class RegistrationRequest");
        sb.AppendLine("    {");
        sb.AppendLine("        public string Email { get; set; } = string.Empty;");
        sb.AppendLine("        public string AppGuid { get; set; } = string.Empty;");
        sb.AppendLine("    }");
    }

    private void GenerateBackupEndpointWithExtendedASCII(StringBuilder sb, AssemblyInfo assemblyInfo, List<ExtractableEntity> entities)
    {
        var baseRoute = $"{assemblyInfo.Vertical}/v{assemblyInfo.MajorVersion}";

        sb.AppendLine($"    [Function(\"Backup\")]");
        sb.AppendLine($"    public async Task<HttpResponseData> Backup([HttpTrigger(AuthorizationLevel.Function, \"post\", Route = \"{baseRoute}/backup\")] HttpRequestData req)");
        sb.AppendLine("    {");
        sb.AppendLine("        try");
        sb.AppendLine("        {");
        sb.AppendLine($"            _logger.LogInformation(\"Processing backup upload for {{Vertical}} v{{Version}} with extended ASCII support\", \"{assemblyInfo.Vertical}\", {assemblyInfo.MajorVersion});");
        sb.AppendLine();
        sb.AppendLine("            // Step 1: Save uploaded zip to temporary location with UTF-8 support");
        sb.AppendLine("            var tempZipPath = Path.Combine(Path.GetTempPath(), $\"backup-{Guid.NewGuid()}.zip\");");
        sb.AppendLine("            using (var fileStream = File.Create(tempZipPath))");
        sb.AppendLine("            {");
        sb.AppendLine("                await req.Body.CopyToAsync(fileStream);");
        sb.AppendLine("            }");
        sb.AppendLine("            _logger.LogDebug(\"Zip file saved with extended ASCII filename support: {TempPath}\", tempZipPath);");
        sb.AppendLine();
        sb.AppendLine("            try");
        sb.AppendLine("            {");
        sb.AppendLine("                // Step 2: Extract SQLite data using version-specific schema with extended ASCII support");
        sb.AppendLine("                var extractionService = serviceProvider.GetRequiredService<SQLiteExtractionService>();");
        sb.AppendLine("                var entities = GetExtractableEntities(); // Version-specific entity list");
        sb.AppendLine("                var extractedData = await extractionService.ExtractDataFromZipAsync(tempZipPath, entities);");
        sb.AppendLine();
        sb.AppendLine("                _logger.LogInformation(\"Extracted data for user: {Email}, tables: {TableCount}, extended ASCII fields: {ExtendedASCIICount}\", ");
        sb.AppendLine("                    extractedData.UserInfo.Email, extractedData.TableData.Count, ");
        sb.AppendLine("                    extractedData.TableData.Values.SelectMany(rows => rows).Count(row => ");
        sb.AppendLine("                        row.Values.OfType<string>().Any(val => val?.Any(c => c > 127) == true)));");
        sb.AppendLine();
        sb.AppendLine("                // Step 3: Generate SQL Server INSERT statements with extended ASCII support");
        sb.AppendLine("                var schemaMapper = serviceProvider.GetRequiredService<SchemaMapperService>();");
        sb.AppendLine("                var connectionString = Environment.GetEnvironmentVariable(\"SQL_CONNECTION_STRING\");");
        sb.AppendLine("                var insertStatements = await schemaMapper.GenerateInsertStatementsAsync(extractedData, entities, connectionString);");
        sb.AppendLine();
        sb.AppendLine("                _logger.LogInformation(\"Generated {StatementCount} INSERT statements with extended ASCII preservation\", insertStatements.Count);");
        sb.AppendLine();
        sb.AppendLine("                // Step 4: Execute SQL Server INSERTs with extended ASCII support");
        sb.AppendLine("                await ExecuteInsertStatementsWithExtendedASCIIAsync(insertStatements, connectionString);");
        sb.AppendLine();
        sb.AppendLine("                // Step 5: Upload photos to Azure Blob Storage (with extended ASCII filename support)");
        sb.AppendLine("                var photoUrls = await UploadPhotosToAzureBlobWithExtendedASCIIAsync(extractedData.PhotoFiles, extractedData.UserInfo);");
        sb.AppendLine();
        sb.AppendLine("                var response = req.CreateResponse(HttpStatusCode.OK);");
        sb.AppendLine("                await response.WriteStringAsync(JsonSerializer.Serialize(new { ");
        sb.AppendLine("                    status = \"success\", ");
        sb.AppendLine("                    message = \"Backup processed successfully with extended ASCII support\",");
        sb.AppendLine("                    rowsInserted = insertStatements.Count,");
        sb.AppendLine("                    photosUploaded = photoUrls.Count,");
        sb.AppendLine("                    userEmail = extractedData.UserInfo.Email,");
        sb.AppendLine("                    extendedASCIISupported = true");
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

        // Generate helper methods for backup processing
        GenerateBackupHelperMethodsWithExtendedASCII(sb, entities);
    }

    private void GenerateBackupHelperMethodsWithExtendedASCII(StringBuilder sb, List<ExtractableEntity> entities)
    {
        sb.AppendLine("    private List<ExtractableEntity> GetExtractableEntities()");
        sb.AppendLine("    {");
        sb.AppendLine($"        // Version-specific entity list with extended ASCII support");
        sb.AppendLine($"        // Tables: {string.Join(", ", entities.Select(e => e.TableName))}");
        sb.AppendLine("        // TODO: Load from version-specific DLL metadata");
        sb.AppendLine("        return new List<ExtractableEntity>();");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private async Task ExecuteInsertStatementsWithExtendedASCIIAsync(List<string> insertStatements, string connectionString)");
        sb.AppendLine("    {");
        sb.AppendLine("        using var connection = new SqlConnection(connectionString);");
        sb.AppendLine("        await connection.OpenAsync();");
        sb.AppendLine();
        sb.AppendLine("        foreach (var statement in insertStatements)");
        sb.AppendLine("        {");
        sb.AppendLine("            using var command = new SqlCommand(statement, connection);");
        sb.AppendLine("            // Ensure proper extended ASCII handling in SQL execution");
        sb.AppendLine("            command.CommandTimeout = 300;");
        sb.AppendLine("            await command.ExecuteNonQueryAsync();");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        _logger.LogInformation(\"Executed {Count} INSERT statements with extended ASCII support\", insertStatements.Count);");
        sb.AppendLine("    }");
        sb.AppendLine();
        sb.AppendLine("    private async Task<List<string>> UploadPhotosToAzureBlobWithExtendedASCIIAsync(List<string> photoFiles, UserInfo userInfo)");
        sb.AppendLine("    {");
        sb.AppendLine("        var uploadedUrls = new List<string>();");
        sb.AppendLine();
        sb.AppendLine("        // TODO: Implement Azure Blob Storage upload with extended ASCII filename support");
        sb.AppendLine("        // Container: user-photos/{userInfo.Email}/{userInfo.Date}/");
        sb.AppendLine("        // Handle extended ASCII characters in photo filenames");
        sb.AppendLine("        // Use URL encoding for blob names if needed");
        sb.AppendLine();
        sb.AppendLine("        foreach (var photoFile in photoFiles)");
        sb.AppendLine("        {");
        sb.AppendLine("            var fileName = Path.GetFileName(photoFile);");
        sb.AppendLine("            var normalizedFileName = NormalizeExtendedASCII(fileName);");
        sb.AppendLine("            ");
        sb.AppendLine("            _logger.LogDebug(\"Processing photo with extended ASCII filename: {Original} -> {Normalized}\", fileName, normalizedFileName);");
        sb.AppendLine("            ");
        sb.AppendLine("            // TODO: Upload with normalized filename");
        sb.AppendLine("            // uploadedUrls.Add(blobUrl);");
        sb.AppendLine("        }");
        sb.AppendLine();
        sb.AppendLine("        _logger.LogInformation(\"Would uploa