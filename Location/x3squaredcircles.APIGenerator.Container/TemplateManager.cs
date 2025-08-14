using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Manages template repository fetching, caching, and validation
    /// </summary>
    public class TemplateManager : IDisposable
    {
        private readonly Configuration _config;
        private readonly Logger _logger;
        private readonly KeyVaultManager _keyVaultManager;
        private readonly HttpClient _httpClient;
        private readonly string _cacheDirectory;
        private readonly Dictionary<string, TemplateCache> _templateCache;
        private bool _disposed;

        public TemplateManager(Configuration config, Logger logger, KeyVaultManager keyVaultManager)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _keyVaultManager = keyVaultManager ?? throw new ArgumentNullException(nameof(keyVaultManager));

            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            _cacheDirectory = Path.Combine(Path.GetTempPath(), "api-generator-templates");
            _templateCache = new Dictionary<string, TemplateCache>();

            Directory.CreateDirectory(_cacheDirectory);
        }

        /// <summary>
        /// Fetch and validate templates from the configured repository
        /// </summary>
        /// <returns>Path to the local template directory</returns>
        public async Task<string> FetchTemplatesAsync()
        {
            _logger.LogStartPhase("Template Fetch");

            try
            {
                using var operation = _logger.TimeOperation("Template Fetch");

                var cacheKey = GetCacheKey();
                var cachedTemplate = GetCachedTemplate(cacheKey);

                if (cachedTemplate != null && !IsTemplateExpired(cachedTemplate))
                {
                    _logger.Info($"Using cached templates: {cachedTemplate.LocalPath}");

                    if (_config.TemplateValidateStructure)
                    {
                        await ValidateTemplateStructureAsync(cachedTemplate.LocalPath);
                    }

                    _logger.LogEndPhase("Template Fetch", true);
                    return cachedTemplate.LocalPath;
                }

                // Fetch fresh templates
                var localPath = await FetchTemplatesFromRepositoryAsync();

                // Validate template structure if configured
                if (_config.TemplateValidateStructure)
                {
                    await ValidateTemplateStructureAsync(localPath);
                }

                // Cache the template
                _templateCache[cacheKey] = new TemplateCache
                {
                    LocalPath = localPath,
                    FetchTime = DateTime.UtcNow,
                    Repository = _config.TemplateRepo,
                    Branch = _config.TemplateBranch,
                    Path = _config.TemplatePath
                };

                _logger.LogEndPhase("Template Fetch", true);
                return localPath;
            }
            catch (Exception ex)
            {
                _logger.Error("Template fetch failed", ex);
                _logger.LogEndPhase("Template Fetch", false);
                throw new TemplateException($"Failed to fetch templates: {ex.Message}", 6);
            }
        }

        /// <summary>
        /// Get available template types from the fetched templates
        /// </summary>
        /// <param name="templatePath">Local path to templates</param>
        /// <returns>List of available template names</returns>
        public async Task<List<string>> GetAvailableTemplatesAsync(string templatePath)
        {
            try
            {
                var templates = new List<string>();
                var searchPath = string.IsNullOrWhiteSpace(_config.TemplatePath)
                    ? templatePath
                    : Path.Combine(templatePath, _config.TemplatePath);

                if (!Directory.Exists(searchPath))
                {
                    _logger.Warn($"Template path does not exist: {searchPath}");
                    return templates;
                }

                // Look for template directories
                var directories = Directory.GetDirectories(searchPath);
                foreach (var dir in directories)
                {
                    var templateName = Path.GetFileName(dir);
                    if (await IsValidTemplateDirectoryAsync(dir))
                    {
                        templates.Add(templateName);
                        _logger.Debug($"Found valid template: {templateName}");
                    }
                    else
                    {
                        _logger.Debug($"Invalid template directory: {templateName}");
                    }
                }

                _logger.Info($"Found {templates.Count} valid templates: {string.Join(", ", templates)}");
                return templates.OrderBy(t => t).ToList();
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to enumerate templates", ex);
                throw new TemplateException($"Failed to enumerate templates: {ex.Message}", 6);
            }
        }

        /// <summary>
        /// Get the full path to a specific template
        /// </summary>
        /// <param name="basePath">Base template path</param>
        /// <param name="templateName">Name of the template</param>
        /// <returns>Full path to the template</returns>
        public string GetTemplatePath(string basePath, string templateName)
        {
            var templatePath = string.IsNullOrWhiteSpace(_config.TemplatePath)
                ? Path.Combine(basePath, templateName)
                : Path.Combine(basePath, _config.TemplatePath, templateName);

            if (!Directory.Exists(templatePath))
            {
                throw new TemplateException($"Template not found: {templateName} at {templatePath}", 6);
            }

            return templatePath;
        }

        /// <summary>
        /// Copy template files to a destination directory
        /// </summary>
        /// <param name="templatePath">Source template path</param>
        /// <param name="destinationPath">Destination directory</param>
        /// <param name="tokenReplacements">Token replacements for template processing</param>
        public async Task CopyTemplateAsync(string templatePath, string destinationPath, Dictionary<string, string> tokenReplacements = null)
        {
            try
            {
                _logger.Info($"Copying template from {templatePath} to {destinationPath}");

                Directory.CreateDirectory(destinationPath);
                await CopyDirectoryRecursiveAsync(templatePath, destinationPath, tokenReplacements ?? new Dictionary<string, string>());

                _logger.Info($"Template copied successfully to {destinationPath}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to copy template", ex);
                throw new TemplateException($"Failed to copy template: {ex.Message}", 6);
            }
        }

        private async Task<string> FetchTemplatesFromRepositoryAsync()
        {
            _logger.Info($"Fetching templates from repository: {_config.TemplateRepo}");

            var patToken = await _keyVaultManager.GetTemplatePatTokenAsync();
            var localPath = Path.Combine(_cacheDirectory, $"templates-{Guid.NewGuid():N}");

            // Determine repository type and fetch accordingly
            if (_config.TemplateRepo.Contains("github.com"))
            {
                await FetchFromGitHubAsync(localPath, patToken);
            }
            else if (_config.TemplateRepo.Contains("dev.azure.com") || _config.TemplateRepo.Contains("visualstudio.com"))
            {
                await FetchFromAzureDevOpsAsync(localPath, patToken);
            }
            else
            {
                // Generic Git repository
                await FetchFromGenericGitAsync(localPath, patToken);
            }

            _logger.Info($"Templates fetched to: {localPath}");
            return localPath;
        }

        private async Task FetchFromGitHubAsync(string localPath, string patToken)
        {
            var repoPath = ExtractRepoPath(_config.TemplateRepo);
            var downloadUrl = $"https://api.github.com/repos/{repoPath}/zipball/{_config.TemplateBranch}";

            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "API-Generator/1.0");

            if (!string.IsNullOrWhiteSpace(patToken))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"token {patToken}");
            }

            _logger.Debug($"Downloading from GitHub: {downloadUrl}");
            var response = await _httpClient.GetAsync(downloadUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new TemplateException($"GitHub API error: {response.StatusCode} - {response.ReasonPhrase}", 6);
            }

            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            await ExtractZipToDirectoryAsync(zipBytes, localPath);
        }

        private async Task FetchFromAzureDevOpsAsync(string localPath, string patToken)
        {
            // Extract organization and project from Azure DevOps URL
            var uri = new Uri(_config.TemplateRepo);
            var pathSegments = uri.AbsolutePath.Trim('/').Split('/');

            if (pathSegments.Length < 3)
            {
                throw new TemplateException("Invalid Azure DevOps repository URL format", 6);
            }

            var organization = pathSegments[0];
            var project = pathSegments[1];
            var repository = pathSegments[3]; // Skip "_git"

            var downloadUrl = $"https://dev.azure.com/{organization}/{project}/_apis/git/repositories/{repository}/items" +
                             $"?path=/&versionDescriptor.version={_config.TemplateBranch}&versionDescriptor.versionType=branch" +
                             "&$format=zip&api-version=6.0&recursionLevel=full";

            _httpClient.DefaultRequestHeaders.Clear();

            if (!string.IsNullOrWhiteSpace(patToken))
            {
                var authValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{patToken}"));
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authValue}");
            }

            _logger.Debug($"Downloading from Azure DevOps: {downloadUrl}");
            var response = await _httpClient.GetAsync(downloadUrl);

            if (!response.IsSuccessStatusCode)
            {
                throw new TemplateException($"Azure DevOps API error: {response.StatusCode} - {response.ReasonPhrase}", 6);
            }

            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            await ExtractZipToDirectoryAsync(zipBytes, localPath);
        }

        private async Task FetchFromGenericGitAsync(string localPath, string patToken)
        {
            // For generic Git repositories, use git command-line tool
            var gitUrl = _config.TemplateRepo;

            if (!string.IsNullOrWhiteSpace(patToken))
            {
                // Insert PAT into URL for authentication
                var uri = new Uri(gitUrl);
                gitUrl = $"{uri.Scheme}://{patToken}@{uri.Host}{uri.PathAndQuery}";
            }

            var gitArgs = $"clone --branch {_config.TemplateBranch} --depth 1 {gitUrl} \"{localPath}\"";

            _logger.Debug($"Executing git clone: git {gitArgs.Replace(patToken ?? "", "[TOKEN]")}");

            var processResult = await ExecuteProcessAsync("git", gitArgs);
            if (processResult.ExitCode != 0)
            {
                throw new TemplateException($"Git clone failed: {processResult.Error}", 6);
            }

            // Remove .git directory to clean up
            var gitDir = Path.Combine(localPath, ".git");
            if (Directory.Exists(gitDir))
            {
                Directory.Delete(gitDir, true);
            }
        }

        private async Task ValidateTemplateStructureAsync(string templatePath)
        {
            _logger.Debug($"Validating template structure: {templatePath}");

            var searchPath = string.IsNullOrWhiteSpace(_config.TemplatePath)
                ? templatePath
                : Path.Combine(templatePath, _config.TemplatePath);

            if (!Directory.Exists(searchPath))
            {
                throw new TemplateException($"Template path does not exist: {searchPath}", 6);
            }

            var templates = Directory.GetDirectories(searchPath);
            if (templates.Length == 0)
            {
                throw new TemplateException($"No template directories found in: {searchPath}", 6);
            }

            var validTemplates = 0;
            foreach (var template in templates)
            {
                var templateName = Path.GetFileName(template);
                if (await IsValidTemplateDirectoryAsync(template))
                {
                    validTemplates++;
                    _logger.LogTemplateValidation(templateName, true);
                }
                else
                {
                    _logger.LogTemplateValidation(templateName, false, "Missing required template files");
                }
            }

            if (validTemplates == 0)
            {
                throw new TemplateException("No valid templates found in repository", 6);
            }

            _logger.Info($"Template structure validation passed: {validTemplates} valid templates");
        }

        private async Task<bool> IsValidTemplateDirectoryAsync(string templateDir)
        {
            // A valid template directory should contain at least one of these files
            var requiredFiles = new[]
            {
                "template.json",      // Template metadata
                "main.bicep",         // Azure Bicep template
                "main.tf",            // Terraform template
                "template.yaml",      // Generic YAML template
                "template.yml",       // Generic YAML template
                "cloudformation.yaml", // AWS CloudFormation
                "cloudformation.json", // AWS CloudFormation
                "function.json",      // Azure Functions
                "requirements.txt",   // Python requirements
                "package.json",       // Node.js package
                "pom.xml",            // Java Maven
                "build.gradle",       // Java Gradle
                "*.csproj",           // C# project
                "go.mod"              // Go module
            };

            foreach (var pattern in requiredFiles)
            {
                if (pattern.Contains("*"))
                {
                    var files = Directory.GetFiles(templateDir, pattern, SearchOption.TopDirectoryOnly);
                    if (files.Length > 0)
                        return true;
                }
                else
                {
                    var filePath = Path.Combine(templateDir, pattern);
                    if (File.Exists(filePath))
                        return true;
                }
            }

            return false;
        }

        private async Task CopyDirectoryRecursiveAsync(string sourceDir, string destDir, Dictionary<string, string> tokenReplacements)
        {
            Directory.CreateDirectory(destDir);

            // Copy files
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var fileName = Path.GetFileName(file);
                var destFile = Path.Combine(destDir, fileName);

                if (IsTextFile(file))
                {
                    // Process text files for token replacement
                    var content = await File.ReadAllTextAsync(file);
                    content = ProcessTokenReplacements(content, tokenReplacements);
                    await File.WriteAllTextAsync(destFile, content);
                }
                else
                {
                    // Copy binary files as-is
                    File.Copy(file, destFile, true);
                }

                _logger.LogFileGeneration(destFile, new FileInfo(destFile).Length);
            }

            // Copy subdirectories
            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var dirName = Path.GetFileName(subDir);
                var destSubDir = Path.Combine(destDir, dirName);
                await CopyDirectoryRecursiveAsync(subDir, destSubDir, tokenReplacements);
            }
        }

        private string ProcessTokenReplacements(string content, Dictionary<string, string> tokenReplacements)
        {
            foreach (var replacement in tokenReplacements)
            {
                content = content.Replace($"{{{replacement.Key}}}", replacement.Value);
            }
            return content;
        }

        private bool IsTextFile(string filePath)
        {
            var textExtensions = new[]
            {
                ".txt", ".json", ".yaml", ".yml", ".xml", ".cs", ".java", ".py", ".js", ".ts",
                ".bicep", ".tf", ".md", ".sql", ".ps1", ".sh", ".bat", ".cmd", ".config",
                ".template", ".tmpl", ".mustache", ".handlebars"
            };

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return textExtensions.Contains(extension);
        }

        private string GetCacheKey()
        {
            return $"{_config.TemplateRepo}|{_config.TemplateBranch}|{_config.TemplatePath}".GetHashCode().ToString("X");
        }

        private TemplateCache GetCachedTemplate(string cacheKey)
        {
            return _templateCache.TryGetValue(cacheKey, out var cached) ? cached : null;
        }

        private bool IsTemplateExpired(TemplateCache cache)
        {
            var age = DateTime.UtcNow - cache.FetchTime;
            return age.TotalSeconds > _config.TemplateCacheTtl;
        }

        private string ExtractRepoPath(string repoUrl)
        {
            var uri = new Uri(repoUrl);
            var path = uri.AbsolutePath.Trim('/');

            if (path.EndsWith(".git"))
            {
                path = path.Substring(0, path.Length - 4);
            }

            return path;
        }

        private async Task ExtractZipToDirectoryAsync(byte[] zipBytes, string extractPath)
        {
            var tempZipPath = Path.GetTempFileName();
            try
            {
                await File.WriteAllBytesAsync(tempZipPath, zipBytes);
                System.IO.Compression.ZipFile.ExtractToDirectory(tempZipPath, extractPath);

                // GitHub/Azure DevOps zips often have a root directory - flatten if needed
                var subDirs = Directory.GetDirectories(extractPath);
                if (subDirs.Length == 1)
                {
                    var singleSubDir = subDirs[0];
                    var tempMoveDir = extractPath + "_temp";
                    Directory.Move(singleSubDir, tempMoveDir);
                    Directory.Delete(extractPath, true);
                    Directory.Move(tempMoveDir, extractPath);
                }
            }
            finally
            {
                if (File.Exists(tempZipPath))
                {
                    File.Delete(tempZipPath);
                }
            }
        }

        private async Task<ProcessResult> ExecuteProcessAsync(string fileName, string arguments)
        {
            using var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            process.OutputDataReceived += (_, e) => { if (e.Data != null) outputBuilder.AppendLine(e.Data); };
            process.ErrorDataReceived += (_, e) => { if (e.Data != null) errorBuilder.AppendLine(e.Data); };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString()
            };
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // Clean up cache directory
                    if (Directory.Exists(_cacheDirectory))
                    {
                        Directory.Delete(_cacheDirectory, true);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.Debug($"Error cleaning up template cache: {ex.Message}");
                }

                _httpClient?.Dispose();
                _disposed = true;
            }
        }

        private class TemplateCache
        {
            public string LocalPath { get; set; }
            public DateTime FetchTime { get; set; }
            public string Repository { get; set; }
            public string Branch { get; set; }
            public string Path { get; set; }
        }

        private class ProcessResult
        {
            public int ExitCode { get; set; }
            public string Output { get; set; }
            public string Error { get; set; }
        }
    }

    /// <summary>
    /// Exception thrown for template-related errors
    /// </summary>
    public class TemplateException : Exception
    {
        public int ExitCode { get; }

        public TemplateException(string message, int exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        public TemplateException(string message, int exitCode, Exception innerException) : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }
}