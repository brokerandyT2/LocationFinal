using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using x3squaredcircles.VersionDetective.Container.Models;

namespace x3squaredcircles.VersionDetective.Container.Services
{
    public interface IOutputService
    {
        Task GenerateOutputsAsync(
            VersionDetectiveConfiguration config,
            VersionCalculationResult versionResult,
            TagTemplateResult tagResult,
            GitAnalysisResult gitAnalysis,
            LicenseSession? licenseSession);
    }

    public class OutputService : IOutputService
    {
        private readonly ILogger<OutputService> _logger;
        private readonly string _outputDirectory = "/src"; // Container mount point

        public OutputService(ILogger<OutputService> logger)
        {
            _logger = logger;
        }

        public async Task GenerateOutputsAsync(
            VersionDetectiveConfiguration config,
            VersionCalculationResult versionResult,
            TagTemplateResult tagResult,
            GitAnalysisResult gitAnalysis,
            LicenseSession? licenseSession)
        {
            try
            {
                _logger.LogInformation("Generating output files to: {OutputDirectory}", _outputDirectory);

                // 1. Generate pipeline-tools.log entry
                await GeneratePipelineToolsLogAsync(config, versionResult, licenseSession);

                // 2. Generate version-metadata.json
                await GenerateVersionMetadataAsync(config, versionResult, tagResult, gitAnalysis, licenseSession);

                // 3. Generate tag-patterns.json
                await GenerateTagPatternsAsync(tagResult);

                _logger.LogInformation("✓ All output files generated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate output files");
                throw new VersionDetectiveException(VersionDetectiveExitCode.InvalidConfiguration,
                    $"Output generation failed: {ex.Message}", ex);
            }
        }

        private async Task GeneratePipelineToolsLogAsync(
            VersionDetectiveConfiguration config,
            VersionCalculationResult versionResult,
            LicenseSession? licenseSession)
        {
            try
            {
                var logEntry = new PipelineToolLogEntry
                {
                    ToolName = config.License.ToolName,
                    Version = versionResult.NewSemanticVersion,
                    BurstMode = licenseSession?.BurstMode ?? false,
                    Timestamp = DateTime.UtcNow
                };

                var logLine = FormatPipelineToolLogEntry(logEntry);
                var logFilePath = Path.Combine(_outputDirectory, "pipeline-tools.log");

                // Append to log file (create if doesn't exist)
                await File.AppendAllTextAsync(logFilePath, logLine + Environment.NewLine);

                _logger.LogInformation("✓ Updated pipeline-tools.log");
                _logger.LogDebug("Log entry: {LogEntry}", logLine);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate pipeline-tools.log");
                throw;
            }
        }

        private async Task GenerateVersionMetadataAsync(
            VersionDetectiveConfiguration config,
            VersionCalculationResult versionResult,
            TagTemplateResult tagResult,
            GitAnalysisResult gitAnalysis,
            LicenseSession? licenseSession)
        {
            try
            {
                var metadata = new VersionMetadata
                {
                    ToolName = config.License.ToolName,
                    ToolVersion = "1.0.0", // Could be made configurable
                    ExecutionTime = DateTime.UtcNow,
                    Language = config.Language.GetSelectedLanguage(),
                    Repository = config.RepoUrl,
                    Branch = config.Branch,
                    CurrentCommit = gitAnalysis.CurrentCommit,
                    BaselineCommit = gitAnalysis.BaselineCommit,
                    VersionCalculation = versionResult,
                    TagTemplates = tagResult,
                    LicenseUsed = licenseSession != null,
                    BurstModeUsed = licenseSession?.BurstMode ?? false,
                    Mode = config.Analysis.Mode
                };

                var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = true
                });

                var metadataFilePath = Path.Combine(_outputDirectory, "version-metadata.json");
                await File.WriteAllTextAsync(metadataFilePath, json);

                _logger.LogInformation("✓ Generated version-metadata.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate version-metadata.json");
                throw;
            }
        }

        private async Task GenerateTagPatternsAsync(TagTemplateResult tagResult)
        {
            try
            {
                var tagPatterns = new
                {
                    semantic_tag = tagResult.SemanticTag,
                    marketing_tag = tagResult.MarketingTag,
                    generated_at = DateTime.UtcNow,
                    token_values = tagResult.TokenValues
                };

                var json = JsonSerializer.Serialize(tagPatterns, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    WriteIndented = true
                });

                var tagPatternsFilePath = Path.Combine(_outputDirectory, "tag-patterns.json");
                await File.WriteAllTextAsync(tagPatternsFilePath, json);

                _logger.LogInformation("✓ Generated tag-patterns.json");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate tag-patterns.json");
                throw;
            }
        }

        private string FormatPipelineToolLogEntry(PipelineToolLogEntry entry)
        {
            var burstIndicator = entry.BurstMode ? " (BURST)" : "";
            return $"{entry.ToolName}={entry.Version}{burstIndicator}";
        }
    }
}