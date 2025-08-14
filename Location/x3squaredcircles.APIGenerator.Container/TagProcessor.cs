using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Processes tag templates and generates tag patterns for API versioning and deployment
    /// </summary>
    public class TagProcessor
    {
        private readonly Configuration _config;
        private readonly Logger _logger;
        private readonly Dictionary<string, string> _tokenValues;

        public TagProcessor(Configuration config, Logger logger)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tokenValues = new Dictionary<string, string>();

            InitializeTokenValues();
        }

        /// <summary>
        /// Process the tag template and generate the final tag
        /// </summary>
        /// <param name="version">API version to use in tag</param>
        /// <param name="templatePath">Template path for template-specific tokens</param>
        /// <returns>Processed tag string</returns>
        public string ProcessTagTemplate(string version, string templatePath = null)
        {
            try
            {
                _logger.Debug($"Processing tag template: {_config.TagTemplate}");

                var processedTag = _config.TagTemplate;
                var tokenValues = GetTokenValues(version, templatePath);

                foreach (var token in tokenValues)
                {
                    var tokenPattern = $"{{{token.Key}}}";
                    processedTag = processedTag.Replace(tokenPattern, token.Value);

                    _logger.Debug($"Token replacement: {tokenPattern} -> {token.Value}");
                }

                // Validate that all tokens were replaced
                var remainingTokens = ExtractTokens(processedTag);
                if (remainingTokens.Any())
                {
                    _logger.Warn($"Unresolved tokens in tag template: {string.Join(", ", remainingTokens)}");
                }

                _logger.Info($"Generated tag: {processedTag}");
                return processedTag;
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to process tag template", ex);
                throw new TagProcessingException($"Tag processing failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate tag patterns for multiple scenarios and write to file
        /// </summary>
        /// <param name="version">API version</param>
        /// <param name="templatePath">Template path</param>
        /// <param name="outputPath">Output file path for tag patterns</param>
        public void GenerateTagPatterns(string version, string templatePath, string outputPath = null)
        {
            try
            {
                outputPath ??= Path.Combine(Directory.GetCurrentDirectory(), "tag-patterns.json");

                var patterns = new TagPatterns
                {
                    Primary = ProcessTagTemplate(version, templatePath),
                    Variants = GenerateTagVariants(version, templatePath),
                    Metadata = new TagMetadata
                    {
                        Template = _config.TagTemplate,
                        Version = version,
                        TemplatePath = templatePath,
                        Cloud = _config.SelectedCloud,
                        Language = _config.SelectedLanguage,
                        GeneratedAt = DateTime.UtcNow,
                        TokensUsed = GetTokenValues(version, templatePath).Keys.ToList()
                    }
                };

                var json = JsonSerializer.Serialize(patterns, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                File.WriteAllText(outputPath, json);
                _logger.LogFileGeneration(outputPath, new FileInfo(outputPath).Length);

                _logger.Info($"Tag patterns generated: Primary={patterns.Primary}, Variants={patterns.Variants.Count}");
            }
            catch (Exception ex)
            {
                _logger.Error("Failed to generate tag patterns", ex);
                throw new TagProcessingException($"Tag pattern generation failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse version components from a version string
        /// </summary>
        /// <param name="version">Version string (e.g., "1.2.3")</param>
        /// <returns>Version components</returns>
        public VersionComponents ParseVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
            {
                return new VersionComponents { Major = "1", Minor = "0", Patch = "0" };
            }

            var versionPattern = @"^(\d+)(?:\.(\d+))?(?:\.(\d+))?";
            var match = Regex.Match(version, versionPattern);

            if (!match.Success)
            {
                _logger.Warn($"Invalid version format: {version}, using default 1.0.0");
                return new VersionComponents { Major = "1", Minor = "0", Patch = "0" };
            }

            return new VersionComponents
            {
                Major = match.Groups[1].Value,
                Minor = match.Groups[2].Success ? match.Groups[2].Value : "0",
                Patch = match.Groups[3].Success ? match.Groups[3].Value : "0"
            };
        }

        /// <summary>
        /// Extract template type from template path
        /// </summary>
        /// <param name="templatePath">Full template path</param>
        /// <returns>Template type name</returns>
        public string ExtractTemplateType(string templatePath)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
                return "default";

            var templateName = Path.GetFileName(templatePath.TrimEnd(Path.DirectorySeparatorChar));
            return string.IsNullOrWhiteSpace(templateName) ? "default" : templateName;
        }

        private void InitializeTokenValues()
        {
            // Initialize static token values
            _tokenValues["branch"] = SanitizeForTag(_config.Branch);
            _tokenValues["repo"] = ExtractRepositoryName(_config.RepoUrl);
            _tokenValues["date"] = DateTime.UtcNow.ToString("yyyy-MM-dd");
            _tokenValues["datetime"] = DateTime.UtcNow.ToString("yyyy-MM-dd-HHmmss");
            _tokenValues["cloud"] = _config.SelectedCloud;
            _tokenValues["user"] = SanitizeForTag(Environment.UserName ?? "system");

            // Git commit information
            _tokenValues["commit-hash"] = GetGitCommitHash(false);
            _tokenValues["commit-hash-full"] = GetGitCommitHash(true);

            // Build information
            _tokenValues["build-number"] = GetBuildNumber();
        }

        private Dictionary<string, string> GetTokenValues(string version, string templatePath)
        {
            var tokenValues = new Dictionary<string, string>(_tokenValues);

            // Version-specific tokens
            var versionComponents = ParseVersion(version);
            tokenValues["version"] = version ?? "1.0.0";
            tokenValues["major"] = versionComponents.Major;
            tokenValues["minor"] = versionComponents.Minor;
            tokenValues["patch"] = versionComponents.Patch;

            // Template-specific tokens
            tokenValues["template-path"] = ExtractTemplateType(templatePath);

            return tokenValues;
        }

        private List<string> GenerateTagVariants(string version, string templatePath)
        {
            var variants = new List<string>();
            var tokenValues = GetTokenValues(version, templatePath);

            // Common tag patterns
            var commonPatterns = new[]
            {
                "{cloud}/{version}",
                "{repo}/v{version}",
                "api-{version}-{date}",
                "{template-path}-{version}",
                "{cloud}-{template-path}-v{version}",
                "{branch}-{version}",
                "release-{major}.{minor}.{patch}"
            };

            foreach (var pattern in commonPatterns)
            {
                try
                {
                    var variant = pattern;
                    foreach (var token in tokenValues)
                    {
                        variant = variant.Replace($"{{{token.Key}}}", token.Value);
                    }

                    if (!variants.Contains(variant) && variant != ProcessTagTemplate(version, templatePath))
                    {
                        variants.Add(variant);
                    }
                }
                catch (Exception ex)
                {
                    _logger.Debug($"Failed to generate variant for pattern {pattern}: {ex.Message}");
                }
            }

            return variants.Take(10).ToList(); // Limit to 10 variants
        }

        private List<string> ExtractTokens(string template)
        {
            var tokenPattern = @"\{([^}]+)\}";
            var matches = Regex.Matches(template, tokenPattern);

            return matches.Cast<Match>()
                         .Select(m => m.Groups[1].Value)
                         .Distinct()
                         .ToList();
        }

        private string ExtractRepositoryName(string repoUrl)
        {
            if (string.IsNullOrWhiteSpace(repoUrl))
                return "unknown";

            try
            {
                var uri = new Uri(repoUrl);
                var pathSegments = uri.AbsolutePath.Trim('/').Split('/');
                var repoName = pathSegments.LastOrDefault();

                if (string.IsNullOrWhiteSpace(repoName))
                    return "unknown";

                // Remove .git extension if present
                if (repoName.EndsWith(".git", StringComparison.OrdinalIgnoreCase))
                {
                    repoName = repoName.Substring(0, repoName.Length - 4);
                }

                return SanitizeForTag(repoName);
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to extract repository name from {repoUrl}: {ex.Message}");
                return "unknown";
            }
        }

        private string GetGitCommitHash(bool full = false)
        {
            try
            {
                // Try environment variables first (CI/CD systems often provide these)
                var commitSha = Environment.GetEnvironmentVariable("GITHUB_SHA") ??
                               Environment.GetEnvironmentVariable("BUILD_SOURCEVERSION") ??
                               Environment.GetEnvironmentVariable("CI_COMMIT_SHA") ??
                               Environment.GetEnvironmentVariable("COMMIT_SHA");

                if (!string.IsNullOrWhiteSpace(commitSha))
                {
                    return full ? commitSha : commitSha.Substring(0, Math.Min(7, commitSha.Length));
                }

                // Fallback to git command if available
                var gitDir = FindGitDirectory();
                if (!string.IsNullOrWhiteSpace(gitDir))
                {
                    var headFile = Path.Combine(gitDir, "HEAD");
                    if (File.Exists(headFile))
                    {
                        var headContent = File.ReadAllText(headFile).Trim();
                        if (headContent.StartsWith("ref: "))
                        {
                            var refPath = headContent.Substring(5);
                            var refFile = Path.Combine(gitDir, refPath);
                            if (File.Exists(refFile))
                            {
                                var hash = File.ReadAllText(refFile).Trim();
                                return full ? hash : hash.Substring(0, Math.Min(7, hash.Length));
                            }
                        }
                        else if (headContent.Length >= 7)
                        {
                            return full ? headContent : headContent.Substring(0, 7);
                        }
                    }
                }

                return "unknown";
            }
            catch (Exception ex)
            {
                _logger.Debug($"Failed to get git commit hash: {ex.Message}");
                return "unknown";
            }
        }

        private string FindGitDirectory()
        {
            var currentDir = Directory.GetCurrentDirectory();

            while (!string.IsNullOrWhiteSpace(currentDir))
            {
                var gitDir = Path.Combine(currentDir, ".git");
                if (Directory.Exists(gitDir))
                {
                    return gitDir;
                }

                var parentDir = Directory.GetParent(currentDir);
                currentDir = parentDir?.FullName;
            }

            return null;
        }

        private string GetBuildNumber()
        {
            // Check various CI/CD environment variables
            var buildNumber = Environment.GetEnvironmentVariable("BUILD_BUILDNUMBER") ??
                             Environment.GetEnvironmentVariable("BUILD_NUMBER") ??
                             Environment.GetEnvironmentVariable("GITHUB_RUN_NUMBER") ??
                             Environment.GetEnvironmentVariable("CI_PIPELINE_ID") ??
                             Environment.GetEnvironmentVariable("CIRCLE_BUILD_NUM") ??
                             Environment.GetEnvironmentVariable("TRAVIS_BUILD_NUMBER");

            return !string.IsNullOrWhiteSpace(buildNumber) ? buildNumber : "local";
        }

        private string SanitizeForTag(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "unknown";

            // Replace invalid characters with hyphens and convert to lowercase
            var sanitized = Regex.Replace(input, @"[^a-zA-Z0-9\-_.]", "-")
                                .ToLowerInvariant()
                                .Trim('-', '_', '.');

            // Collapse multiple consecutive hyphens
            sanitized = Regex.Replace(sanitized, @"-+", "-");

            return string.IsNullOrWhiteSpace(sanitized) ? "unknown" : sanitized;
        }

        // Data classes for tag patterns output
        public class TagPatterns
        {
            public string Primary { get; set; }
            public List<string> Variants { get; set; } = new List<string>();
            public TagMetadata Metadata { get; set; }
        }

        public class TagMetadata
        {
            public string Template { get; set; }
            public string Version { get; set; }
            public string TemplatePath { get; set; }
            public string Cloud { get; set; }
            public string Language { get; set; }
            public DateTime GeneratedAt { get; set; }
            public List<string> TokensUsed { get; set; } = new List<string>();
        }

        public class VersionComponents
        {
            public string Major { get; set; }
            public string Minor { get; set; }
            public string Patch { get; set; }
        }
    }

    /// <summary>
    /// Exception thrown for tag processing errors
    /// </summary>
    public class TagProcessingException : Exception
    {
        public TagProcessingException(string message) : base(message) { }
        public TagProcessingException(string message, Exception innerException) : base(message, innerException) { }
    }
}