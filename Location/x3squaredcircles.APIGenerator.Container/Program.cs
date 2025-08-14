using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Main entry point for the API Generator Container tool
    /// </summary>
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Configuration? config = null;
            Logger? logger = null;
            LicenseManager? licenseManager = null;
            KeyVaultManager? keyVaultManager = null;
            TemplateManager? templateManager = null;

            try
            {
                // Parse and validate configuration
                config = Configuration.Parse();
                config.Validate();

                // Initialize logger
                logger = new Logger(config);
                logger.Info("API Generator Container starting...");
                logger.LogConfiguration(config);

                // Initialize managers
                keyVaultManager = new KeyVaultManager(config, logger);
                licenseManager = new LicenseManager(config, logger);
                templateManager = new TemplateManager(config, logger, keyVaultManager);

                // Acquire license
                var licenseAcquired = await licenseManager.AcquireLicenseAsync();

                if (licenseManager.IsInNoOpMode)
                {
                    logger.Info("Running in NOOP mode - analysis only, no changes will be applied");
                    return await RunAnalysisOnlyAsync(config, logger, templateManager);
                }

                if (!licenseAcquired)
                {
                    logger.Error("Failed to acquire license");
                    return 2; // License unavailable
                }

                // Execute main workflow
                return await ExecuteMainWorkflowAsync(config, logger, licenseManager, keyVaultManager, templateManager);
            }
            catch (LicenseException ex)
            {
                logger?.Error($"License error: {ex.Message}");
                return ex.ExitCode;
            }
            catch (KeyVaultException ex)
            {
                logger?.Error($"Key vault error: {ex.Message}");
                return ex.ExitCode;
            }
            catch (TemplateException ex)
            {
                logger?.Error($"Template error: {ex.Message}");
                return ex.ExitCode;
            }
            catch (EntityDiscoveryException ex)
            {
                logger?.Error($"Entity discovery error: {ex.Message}");
                return ex.ExitCode;
            }
            catch (CodeGenerationException ex)
            {
                logger?.Error($"Code generation error: {ex.Message}");
                return ex.ExitCode;
            }
            catch (CloudDeploymentException ex)
            {
                logger?.Error($"Cloud deployment error: {ex.Message}");
                return ex.ExitCode;
            }
            catch (ArgumentException ex)
            {
                logger?.Error($"Configuration error: {ex.Message}");
                return 1; // Invalid configuration
            }
            catch (Exception ex)
            {
                logger?.Error($"Unexpected error: {ex.Message}", ex);
                return 99; // Unexpected error
            }
            finally
            {
                await CleanupResourcesAsync(logger, licenseManager, keyVaultManager, templateManager);
            }
        }

        private static async Task<int> RunAnalysisOnlyAsync(Configuration config, Logger logger, TemplateManager templateManager)
        {
            logger.LogStartPhase("Analysis Only Mode");

            try
            {
                // Fetch and validate templates
                var templatePath = await templateManager.FetchTemplatesAsync();
                var availableTemplates = await templateManager.GetAvailableTemplatesAsync(templatePath);
                logger.Info($"Found {availableTemplates.Count} available templates");

                // Discover entities
                var entityDiscovery = new EntityDiscovery(config, logger);
                var entities = await entityDiscovery.DiscoverEntitiesAsync();

                // Generate tag patterns for reference
                var tagProcessor = new TagProcessor(config, logger);
                var version = GenerateVersion();
                tagProcessor.GenerateTagPatterns(version, templatePath);

                // Write analysis results
                await WriteAnalysisResultsAsync(config, logger, entities, availableTemplates, version);

                logger.LogEndPhase("Analysis Only Mode", true);
                logger.Info("Analysis completed successfully - no changes applied due to NOOP mode");

                return 0; // Success
            }
            catch (Exception ex)
            {
                logger.Error("Analysis failed", ex);
                logger.LogEndPhase("Analysis Only Mode", false);
                return 99;
            }
        }

        private static async Task<int> ExecuteMainWorkflowAsync(Configuration config, Logger logger,
            LicenseManager licenseManager, KeyVaultManager keyVaultManager, TemplateManager templateManager)
        {
            logger.LogStartPhase("Main Workflow");

            try
            {
                // Step 1: Fetch and validate templates
                logger.Info("Fetching templates...");
                var templatePath = await templateManager.FetchTemplatesAsync();
                var availableTemplates = await templateManager.GetAvailableTemplatesAsync(templatePath);

                if (availableTemplates.Count == 0)
                {
                    throw new TemplateException("No valid templates found", 6);
                }

                // Step 2: Discover entities
                logger.Info("Discovering entities...");
                var entityDiscovery = new EntityDiscovery(config, logger);
                var entities = await entityDiscovery.DiscoverEntitiesAsync();

                if (entities.Count == 0 && !config.IgnoreExportAttribute)
                {
                    throw new EntityDiscoveryException($"No entities found with attribute: {config.TrackAttribute}", 5);
                }

                // Step 3: Generate API version
                var version = GenerateVersion();
                logger.Info($"Generated API version: {version}");

                // Step 4: Select and prepare template
                var selectedTemplate = SelectTemplate(availableTemplates, config.TemplatePath);
                var selectedTemplatePath = templateManager.GetTemplatePath(templatePath, selectedTemplate);
                logger.Info($"Using template: {selectedTemplate}");

                // Step 5: Generate code
                if (!config.SkipBuild)
                {
                    var tagProcessor = new TagProcessor(config, logger);
                    var codeGenerator = new CodeGenerator(config, logger, tagProcessor);

                    logger.Info("Generating API code...");
                    var generatedProject = await codeGenerator.GenerateProjectAsync(entities, selectedTemplatePath, version);

                    logger.Info($"Generated project with {generatedProject.GeneratedFiles.Count} files");

                    // Step 6: Deploy to cloud (unless skipped)
                    if (!config.SkipDeployment && !config.ValidateOnly)
                    {
                        var cloudDeployer = new CloudDeployer(config, logger, keyVaultManager);
                        var deploymentTag = tagProcessor.ProcessTagTemplate(version, selectedTemplatePath);

                        logger.Info($"Deploying to {config.SelectedCloud} with tag: {deploymentTag}");
                        var deploymentResult = await cloudDeployer.DeployAsync(generatedProject.OutputPath, entities, deploymentTag);

                        if (deploymentResult.Success)
                        {
                            logger.Info($"Deployment successful: {deploymentResult.ServiceUrl}");
                            await WriteDeploymentInfoAsync(deploymentResult);
                        }
                        else
                        {
                            throw new CloudDeploymentException($"Deployment failed: {deploymentResult.ErrorMessage}", 8);
                        }
                    }
                    else if (config.ValidateOnly)
                    {
                        logger.Info("Validation mode - skipping deployment");
                    }
                    else
                    {
                        logger.Info("Deployment skipped per configuration");
                    }
                }
                else
                {
                    logger.Info("Build skipped per configuration");
                }

                logger.LogEndPhase("Main Workflow", true);
                logger.Info("API Generator completed successfully");

                return 0; // Success
            }
            catch (Exception ex)
            {
                logger.Error("Main workflow failed", ex);
                logger.LogEndPhase("Main Workflow", false);
                throw;
            }
        }

        private static string GenerateVersion()
        {
            // Simple versioning scheme: MAJOR.MINOR.PATCH based on current date and time
            var now = DateTime.UtcNow;
            var major = now.Year - 2020; // Start from year 2020 as baseline
            var minor = now.Month;
            var patch = now.Day;

            return $"{major}.{minor}.{patch}";
        }

        private static string SelectTemplate(System.Collections.Generic.List<string> availableTemplates, string configuredTemplatePath)
        {
            // If specific template path is configured, try to use it
            if (!string.IsNullOrWhiteSpace(configuredTemplatePath))
            {
                var configuredTemplate = Path.GetFileName(configuredTemplatePath.TrimEnd(Path.DirectorySeparatorChar));
                if (availableTemplates.Contains(configuredTemplate))
                {
                    return configuredTemplate;
                }
            }

            // Default selection logic
            var preferredTemplates = new[] { "minimal", "standard", "default", "basic" };

            foreach (var preferred in preferredTemplates)
            {
                var match = availableTemplates.FirstOrDefault(t =>
                    t.Equals(preferred, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                {
                    return match;
                }
            }

            // Fall back to first available template
            return availableTemplates.First();
        }

        private static async Task WriteAnalysisResultsAsync(Configuration config, Logger logger,
            System.Collections.Generic.List<DiscoveredEntity> entities,
            System.Collections.Generic.List<string> availableTemplates,
            string version)
        {
            var analysisPath = Path.Combine(Directory.GetCurrentDirectory(), "analysis-results.json");

            var analysisResults = new
            {
                AnalysisMode = "NOOP",
                GeneratedAt = DateTime.UtcNow,
                Version = version,
                Configuration = new
                {
                    Language = config.SelectedLanguage,
                    Cloud = config.SelectedCloud,
                    TrackAttribute = config.TrackAttribute,
                    RepoUrl = config.RepoUrl,
                    Branch = config.Branch
                },
                Templates = new
                {
                    AvailableCount = availableTemplates.Count,
                    Available = availableTemplates
                },
                Entities = new
                {
                    DiscoveredCount = entities.Count,
                    Discovered = entities.Select(e => new
                    {
                        e.Name,
                        e.FullName,
                        e.Namespace,
                        e.Language,
                        PropertyCount = e.Properties.Count,
                        e.SourceFile
                    })
                }
            };

            var json = JsonSerializer.Serialize(analysisResults, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(analysisPath, json);
            logger.LogFileGeneration(analysisPath, json.Length);
            logger.Info($"Analysis results written to: {analysisPath}");
        }

        private static async Task WriteDeploymentInfoAsync(DeploymentResult deploymentResult)
        {
            var deploymentPath = Path.Combine(Directory.GetCurrentDirectory(), "deployment-info.json");

            var deploymentInfo = new
            {
                deploymentResult.Cloud,
                deploymentResult.DeploymentTag,
                deploymentResult.ServiceName,
                deploymentResult.ServiceUrl,
                deploymentResult.ResourceId,
                deploymentResult.Success,
                deploymentResult.StartTime,
                deploymentResult.EndTime,
                DurationSeconds = deploymentResult.Duration.TotalSeconds,
                deploymentResult.Metadata
            };

            var json = JsonSerializer.Serialize(deploymentInfo, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(deploymentPath, json);
        }

        private static async Task CleanupResourcesAsync(Logger? logger,
            LicenseManager? licenseManager,
            KeyVaultManager? keyVaultManager,
            TemplateManager? templateManager)
        {
            try
            {
                if (licenseManager != null)
                {
                    await licenseManager.ReleaseLicenseAsync();
                    licenseManager.Dispose();
                }

                keyVaultManager?.Dispose();
                templateManager?.Dispose();

                logger?.Info("API Generator Container completed");
            }
            catch (Exception ex)
            {
                logger?.Debug($"Error during cleanup: {ex.Message}");
            }
        }
    }
}