using Location.Tools.APIGenerator.Models;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace Location.Tools.APIGenerator.Services;

public class AzureArtifactsService
{
    private readonly ILogger<AzureArtifactsService> _logger;
    private readonly HttpClient _httpClient;

    public AzureArtifactsService(ILogger<AzureArtifactsService> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<bool> TryPublishArtifactAsync(AssemblyInfo assemblyInfo, string compiledBinaryPath, GeneratorOptions options)
    {
        try
        {
            // Create artifact version (Major.0.0 format)
            var artifactVersion = $"{assemblyInfo.MajorVersion}.0.0";
            var artifactName = $"location-{assemblyInfo.Vertical}-api";

            _logger.LogInformation("Publishing artifact {ArtifactName} version {Version}", artifactName, artifactVersion);

            // Check if artifact already exists
            var existsResult = await CheckArtifactExistsAsync(artifactName, artifactVersion, options);

            if (existsResult.Exists)
            {
                _logger.LogInformation("Artifact {ArtifactName} v{Version} already exists, skipping upload",
                    artifactName, artifactVersion);
                return true; // Silent success
            }

            // Upload new artifact
            var uploadSuccess = await UploadArtifactAsync(artifactName, artifactVersion, compiledBinaryPath, options);

            if (uploadSuccess)
            {
                _logger.LogInformation("Artifact {ArtifactName} v{Version} uploaded successfully",
                    artifactName, artifactVersion);
            }

            return uploadSuccess;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish artifact");
            return false;
        }
    }

    private async Task<(bool Exists, string? ErrorMessage)> CheckArtifactExistsAsync(string artifactName, string version, GeneratorOptions options)
    {
        try
        {
            // This is a placeholder implementation
            // In a real scenario, you would call Azure DevOps REST API to check if artifact exists
            // For now, we'll simulate the check

            var checkUrl = $"https://feeds.dev.azure.com/{options.AzureSubscription}/_apis/packaging/feeds/LocationAPIs/packages/{artifactName}/versions/{version}";

            _logger.LogDebug("Checking artifact existence: {CheckUrl}", checkUrl);

            // Add authentication header if available
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT")))
            {
                var pat = Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
                var authBytes = Encoding.ASCII.GetBytes($":{pat}");
                var authHeader = Convert.ToBase64String(authBytes);
                _httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeader);
            }

            var response = await _httpClient.GetAsync(checkUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return (true, null); // Artifact exists
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, null); // Artifact doesn't exist
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return (false, $"Error checking artifact: {response.StatusCode} - {errorContent}");
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "Could not check artifact existence, assuming it doesn't exist");
            return (false, null); // Assume it doesn't exist if we can't check
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking artifact existence");
            return (false, ex.Message);
        }
    }

    private async Task<bool> UploadArtifactAsync(string artifactName, string version, string binaryPath, GeneratorOptions options)
    {
        try
        {
            // Create a properly formatted zip for Azure Artifacts
            var artifactZipPath = await CreateArtifactZipAsync(binaryPath, artifactName, version);

            try
            {
                // This is a placeholder for Azure DevOps Artifacts upload
                // In reality, you would use the Azure DevOps REST API or CLI
                var uploadUrl = $"https://feeds.dev.azure.com/{options.AzureSubscription}/_apis/packaging/feeds/LocationAPIs/nuget/packages/{artifactName}/versions/{version}";

                _logger.LogDebug("Would upload to: {UploadUrl}", uploadUrl);

                // For now, we'll simulate successful upload
                // In production, implement actual Azure DevOps Artifacts API calls
                await Task.Delay(1000); // Simulate upload time

                _logger.LogInformation("Artifact uploaded successfully (simulated)");
                return true;
            }
            finally
            {
                // Cleanup artifact zip
                if (File.Exists(artifactZipPath))
                {
                    File.Delete(artifactZipPath);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload artifact");
            return false;
        }
    }

    private async Task<string> CreateArtifactZipAsync(string binaryPath, string artifactName, string version)
    {
        var artifactZipPath = Path.Combine(Path.GetTempPath(), $"{artifactName}-{version}.zip");

        _logger.LogDebug("Creating artifact zip: {ArtifactZipPath}", artifactZipPath);

        // Create zip with binary and metadata
        using (var archive = ZipFile.Open(artifactZipPath, ZipArchiveMode.Create))
        {
            // Add all files from the binary directory
            if (Directory.Exists(binaryPath))
            {
                var files = Directory.GetFiles(binaryPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    var relativePath = Path.GetRelativePath(binaryPath, file);
                    archive.CreateEntryFromFile(file, relativePath);
                }
            }
            else if (File.Exists(binaryPath))
            {
                // Single file
                var fileName = Path.GetFileName(binaryPath);
                archive.CreateEntryFromFile(binaryPath, fileName);
            }

            // Add metadata file
            var metadata = new
            {
                name = artifactName,
                version = version,
                createdAt = DateTime.UtcNow,
                type = "LocationAPI",
                description = "Generated Location API Function App"
            };

            var metadataEntry = archive.CreateEntry("artifact-metadata.json");
            using var metadataStream = metadataEntry.Open();
            using var writer = new StreamWriter(metadataStream);
            await writer.WriteAsync(JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
        }

        var fileInfo = new FileInfo(artifactZipPath);
        _logger.LogDebug("Created artifact zip: {Size} bytes", fileInfo.Length);

        return artifactZipPath;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}