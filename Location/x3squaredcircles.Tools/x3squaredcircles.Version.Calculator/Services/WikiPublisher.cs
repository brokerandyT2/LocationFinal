using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace x3squaredcircles.Version.Calculator.Services;

public class WikiPublisher
{
    private readonly ILogger<WikiPublisher> _logger;
    private readonly HttpClient _httpClient;

    public WikiPublisher(ILogger<WikiPublisher> logger)
    {
        _logger = logger;
        _httpClient = new HttpClient();
    }

    public async Task<bool> PublishReleaseNotesAsync(
        string wikiUrl,
        VersionImpact versionImpact,
        Solution solution,
        string releaseNotesContent)
    {
        try
        {
            _logger.LogInformation("Publishing release notes to wiki: {WikiUrl}", wikiUrl);

            var wikiType = DetectWikiType(wikiUrl);
            var directoryPath = BuildDirectoryPath(solution);
            var pageTitle = BuildPageTitle(versionImpact);

            _logger.LogInformation("Wiki directory: {DirectoryPath}", directoryPath);
            _logger.LogInformation("Page title: {PageTitle}", pageTitle);

            return wikiType switch
            {
                WikiType.AzureDevOps => await PublishToAzureDevOpsWikiAsync(wikiUrl, directoryPath, pageTitle, releaseNotesContent),
                WikiType.GitHub => await PublishToGitHubWikiAsync(wikiUrl, directoryPath, pageTitle, releaseNotesContent),
                WikiType.Confluence => await PublishToConfluenceAsync(wikiUrl, directoryPath, pageTitle, releaseNotesContent),
                _ => await PublishGenericWikiAsync(wikiUrl, directoryPath, pageTitle, releaseNotesContent)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish release notes to wiki");
            return false;
        }
    }

    public async Task<bool> PublishFromFileAsync(string wikiUrl, string releaseNotesFilePath)
    {
        try
        {
            if (!File.Exists(releaseNotesFilePath))
            {
                _logger.LogError("Release notes file not found: {FilePath}", releaseNotesFilePath);
                return false;
            }

            var content = await File.ReadAllTextAsync(releaseNotesFilePath);
            var versionInfo = ExtractVersionInfoFromFile(content, releaseNotesFilePath);

            if (versionInfo == null)
            {
                _logger.LogError("Could not extract version information from release notes file");
                return false;
            }

            var directoryPath = BuildDirectoryPathFromFileName(releaseNotesFilePath);
            var pageTitle = BuildPageTitleFromVersionInfo(versionInfo);

            var wikiType = DetectWikiType(wikiUrl);
            return wikiType switch
            {
                WikiType.AzureDevOps => await PublishToAzureDevOpsWikiAsync(wikiUrl, directoryPath, pageTitle, content),
                WikiType.GitHub => await PublishToGitHubWikiAsync(wikiUrl, directoryPath, pageTitle, content),
                WikiType.Confluence => await PublishToConfluenceAsync(wikiUrl, directoryPath, pageTitle, content),
                _ => await PublishGenericWikiAsync(wikiUrl, directoryPath, pageTitle, content)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish release notes from file to wiki");
            return false;
        }
    }

    private WikiType DetectWikiType(string wikiUrl)
    {
        var url = wikiUrl.ToLowerInvariant();

        if (url.Contains("dev.azure.com") || url.Contains("visualstudio.com"))
            return WikiType.AzureDevOps;

        if (url.Contains("github.com"))
            return WikiType.GitHub;

        if (url.Contains("atlassian.net") || url.Contains("confluence"))
            return WikiType.Confluence;

        return WikiType.Generic;
    }

    private string BuildDirectoryPath(Solution solution)
    {
        var vertical = ExtractVerticalName(solution);
        var now = DateTime.UtcNow;
        return $"/Releases/{vertical}/{now.Year}/{now.Month:D2}";
    }

    private string BuildDirectoryPathFromFileName(string fileName)
    {
        // Try to extract vertical from filename like "RELEASE_NOTES_v5.0.0.md"
        var vertical = "Unknown";
        var now = DateTime.UtcNow;

        // For now, use current date - could be enhanced to parse from git or other sources
        return $"/Releases/{vertical}/{now.Year}/{now.Month:D2}";
    }

    private string BuildPageTitle(VersionImpact versionImpact)
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return $"Release \"{versionImpact.SemanticVersion}\" / External: \"{versionImpact.MarketingVersion}\" - {date}";
    }

    private string BuildPageTitleFromVersionInfo(VersionInfo versionInfo)
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        return $"Release \"{versionInfo.SemanticVersion}\" / External: \"{versionInfo.MarketingVersion}\" - {date}";
    }

    private string ExtractVerticalName(Solution solution)
    {
        if (solution.Type == "core") return "Core";

        // Extract from solution name: Location.Photography -> Photography
        if (solution.Name.StartsWith("Location.", StringComparison.OrdinalIgnoreCase))
        {
            var parts = solution.Name.Split('.');
            if (parts.Length >= 2)
            {
                return ToTitleCase(parts[1]);
            }
        }

        return "Unknown";
    }

    private string ToTitleCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input.Substring(1).ToLowerInvariant();
    }

    private async Task<bool> PublishToAzureDevOpsWikiAsync(string wikiUrl, string directoryPath, string pageTitle, string content)
    {
        try
        {
            _logger.LogInformation("Publishing to Azure DevOps Wiki");

            // Azure DevOps Wiki API integration
            var pat = GetPersonalAccessToken();
            if (string.IsNullOrEmpty(pat))
            {
                _logger.LogError("Azure DevOps Personal Access Token not found. Set environment variable AZURE_DEVOPS_PAT");
                return false;
            }

            SetupHttpClientAuth(pat, AuthType.BasicPAT);

            // Extract organization, project, and wiki from URL
            var wikiInfo = ParseAzureDevOpsWikiUrl(wikiUrl);
            if (wikiInfo == null)
            {
                _logger.LogError("Invalid Azure DevOps Wiki URL format");
                return false;
            }

            // Create directory structure
            await EnsureWikiDirectoryExistsAsync(wikiInfo, directoryPath);

            // Create or update the page
            var pageId = SanitizePageId(pageTitle);
            var apiUrl = $"https://dev.azure.com/{wikiInfo.Organization}/{wikiInfo.Project}/_apis/wiki/wikis/{wikiInfo.WikiId}/pages?path={directoryPath}/{pageId}&api-version=7.0";

            var wikiPage = new
            {
                content = content
            };

            var json = JsonSerializer.Serialize(wikiPage);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PutAsync(apiUrl, httpContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully published to Azure DevOps Wiki");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to publish to Azure DevOps Wiki: {StatusCode} - {Error}", response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception publishing to Azure DevOps Wiki");
            return false;
        }
    }

    private async Task<bool> PublishToGitHubWikiAsync(string wikiUrl, string directoryPath, string pageTitle, string content)
    {
        try
        {
            _logger.LogInformation("Publishing to GitHub Wiki");

            // GitHub Wiki is actually a git repository, so we need to clone, commit, and push
            var token = GetGitHubToken();
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogError("GitHub token not found. Set environment variable GITHUB_TOKEN");
                return false;
            }

            // GitHub Wiki pages are markdown files in a git repository
            // For now, log the approach - actual implementation would require git operations
            _logger.LogInformation("GitHub Wiki publishing requires git clone/commit/push operations");
            _logger.LogInformation("Directory: {DirectoryPath}", directoryPath);
            _logger.LogInformation("Page: {PageTitle}", pageTitle);

            // TODO: Implement git-based GitHub Wiki publishing
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception publishing to GitHub Wiki");
            return false;
        }
    }

    private async Task<bool> PublishToConfluenceAsync(string wikiUrl, string directoryPath, string pageTitle, string content)
    {
        try
        {
            _logger.LogInformation("Publishing to Confluence");

            var apiToken = GetConfluenceToken();
            var username = GetConfluenceUsername();

            if (string.IsNullOrEmpty(apiToken) || string.IsNullOrEmpty(username))
            {
                _logger.LogError("Confluence credentials not found. Set CONFLUENCE_USERNAME and CONFLUENCE_TOKEN");
                return false;
            }

            SetupHttpClientAuth($"{username}:{apiToken}", AuthType.BasicAuth);

            // Convert markdown to Confluence markup
            var confluenceContent = ConvertMarkdownToConfluence(content);

            // Confluence API implementation
            var spaceKey = ExtractConfluenceSpaceKey(wikiUrl);
            var apiUrl = $"{GetConfluenceBaseUrl(wikiUrl)}/rest/api/content";

            var confluencePage = new
            {
                type = "page",
                title = pageTitle,
                space = new { key = spaceKey },
                body = new
                {
                    storage = new
                    {
                        value = confluenceContent,
                        representation = "storage"
                    }
                }
            };

            var json = JsonSerializer.Serialize(confluencePage);
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(apiUrl, httpContent);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Successfully published to Confluence");
                return true;
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to publish to Confluence: {StatusCode} - {Error}", response.StatusCode, error);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception publishing to Confluence");
            return false;
        }
    }

    private async Task<bool> PublishGenericWikiAsync(string wikiUrl, string directoryPath, string pageTitle, string content)
    {
        _logger.LogInformation("Generic wiki publishing - saving to file system");

        // Fallback: save to structured file system
        var basePath = "wiki-export";
        var fullPath = Path.Combine(basePath, directoryPath.TrimStart('/'));
        var fileName = $"{SanitizeFileName(pageTitle)}.md";
        var filePath = Path.Combine(fullPath, fileName);

        Directory.CreateDirectory(fullPath);
        await File.WriteAllTextAsync(filePath, content);

        _logger.LogInformation("Release notes saved to: {FilePath}", filePath);
        return true;
    }

    private void SetupHttpClientAuth(string credentials, AuthType authType)
    {
        switch (authType)
        {
            case AuthType.BasicPAT:
                var encodedPat = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{credentials}"));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedPat);
                break;
            case AuthType.BasicAuth:
                var encodedAuth = Convert.ToBase64String(Encoding.ASCII.GetBytes(credentials));
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", encodedAuth);
                break;
            case AuthType.Bearer:
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", credentials);
                break;
        }
    }

    private async Task EnsureWikiDirectoryExistsAsync(AzureDevOpsWikiInfo wikiInfo, string directoryPath)
    {
        // TODO: Implement directory creation logic for Azure DevOps Wiki
        _logger.LogDebug("Ensuring wiki directory exists: {DirectoryPath}", directoryPath);
    }

    private string SanitizePageId(string pageTitle)
    {
        // Remove or replace characters that aren't valid in wiki page IDs
        return pageTitle
            .Replace("\"", "")
            .Replace("/", "-")
            .Replace(":", "-")
            .Replace(" ", "-")
            .Replace("--", "-");
    }

    private string SanitizeFileName(string fileName)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
    }

    private string ConvertMarkdownToConfluence(string markdown)
    {
        // Basic markdown to Confluence conversion
        // This could be enhanced with a proper markdown parser
        return markdown
            .Replace("# ", "<h1>").Replace("\n", "</h1>\n")
            .Replace("## ", "<h2>").Replace("\n", "</h2>\n")
            .Replace("### ", "<h3>").Replace("\n", "</h3>\n")
            .Replace("**", "<strong>", StringComparison.OrdinalIgnoreCase)
            .Replace("**", "</strong>", StringComparison.OrdinalIgnoreCase)
            .Replace("- ", "<li>").Replace("\n", "</li>\n");
    }

    private VersionInfo? ExtractVersionInfoFromFile(string content, string fileName)
    {
        try
        {
            // Try to extract versions from file content or filename
            // RELEASE_NOTES_v5.0.0.md pattern
            var semanticMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"v(\d+\.\d+\.\d+)");
            if (semanticMatch.Success)
            {
                return new VersionInfo
                {
                    SemanticVersion = semanticMatch.Groups[1].Value,
                    MarketingVersion = semanticMatch.Groups[1].Value // Default to same if not found
                };
            }

            // Try to extract from content
            var contentMatch = System.Text.RegularExpressions.Regex.Match(content, @"Version (\d+\.\d+\.\d+)");
            if (contentMatch.Success)
            {
                return new VersionInfo
                {
                    SemanticVersion = contentMatch.Groups[1].Value,
                    MarketingVersion = contentMatch.Groups[1].Value
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to extract version info from file");
        }

        return null;
    }

    private AzureDevOpsWikiInfo? ParseAzureDevOpsWikiUrl(string wikiUrl)
    {
        // Parse URL like: https://dev.azure.com/organization/project/_wiki/wikis/wiki-name
        var match = System.Text.RegularExpressions.Regex.Match(wikiUrl,
            @"dev\.azure\.com/([^/]+)/([^/]+)/_wiki/wikis/([^/]+)");

        if (match.Success)
        {
            return new AzureDevOpsWikiInfo
            {
                Organization = match.Groups[1].Value,
                Project = match.Groups[2].Value,
                WikiId = match.Groups[3].Value
            };
        }

        return null;
    }

    private string? GetPersonalAccessToken() => Environment.GetEnvironmentVariable("AZURE_DEVOPS_PAT");
    private string? GetGitHubToken() => Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    private string? GetConfluenceToken() => Environment.GetEnvironmentVariable("CONFLUENCE_TOKEN");
    private string? GetConfluenceUsername() => Environment.GetEnvironmentVariable("CONFLUENCE_USERNAME");

    private string ExtractConfluenceSpaceKey(string wikiUrl)
    {
        // Extract space key from Confluence URL
        return "DEV"; // Default space key
    }

    private string GetConfluenceBaseUrl(string wikiUrl)
    {
        var uri = new Uri(wikiUrl);
        return $"{uri.Scheme}://{uri.Host}";
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}

public enum WikiType
{
    AzureDevOps,
    GitHub,
    Confluence,
    Generic
}

public enum AuthType
{
    BasicPAT,
    BasicAuth,
    Bearer
}

public record AzureDevOpsWikiInfo
{
    public string Organization { get; init; } = "";
    public string Project { get; init; } = "";
    public string WikiId { get; init; } = "";
}

public record VersionInfo
{
    public string SemanticVersion { get; init; } = "";
    public string MarketingVersion { get; init; } = "";
}