using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using x3squaredcircles.MobileAdapter.Generator.Configuration;
using x3squaredcircles.MobileAdapter.Generator.Discovery;
using x3squaredcircles.MobileAdapter.Generator.Generation;
using x3squaredcircles.MobileAdapter.Generator.Logging;
using x3squaredcircles.MobileAdapter.Generator.TypeMapping;

namespace x3squaredcircles.MobileAdapter.Generator.Core
{
    public class AdapterGeneratorEngine
    {
        private readonly ILogger _logger;

        public AdapterGeneratorEngine(ILogger logger)
        {
            _logger = logger;
        }

        public async Task<GenerationResult> GenerateAdaptersAsync(GeneratorConfiguration config)
        {
            try
            {
                _logger.LogInfo("Starting adapter generation process...");

                // Apply configuration settings to logger
                _logger.SetLogLevel(config.LogLevel);
                _logger.SetVerbose(config.Verbose);

                if (config.Verbose)
                {
                    LogConfigurationSummary(config);
                }

                // Phase 1: Class Discovery
                _logger.LogInfo("Phase 1: Discovering classes for adapter generation...");
                var discoveryEngine = CreateDiscoveryEngine(config);
                var discoveredClasses = await discoveryEngine.DiscoverClassesAsync(config);

                if (discoveredClasses.Count == 0)
                {
                    _logger.LogError("No classes found for adapter generation");
                    return new GenerationResult
                    {
                        Success = false,
                        ErrorMessage = "No classes found for adapter generation",
                        ExitCode = 6
                    };
                }

                _logger.LogInfo($"Discovered {discoveredClasses.Count} classes for adapter generation");

                // Write analysis results
                await WriteAnalysisResultsAsync(discoveredClasses);

                // Phase 2: Type Mapping Analysis
                _logger.LogInfo("Phase 2: Analyzing type mappings...");
                var typeMappingEngine = new TypeMappingEngine(_logger);
                var typeMappings = await typeMappingEngine.AnalyzeTypeMappingsAsync(discoveredClasses, config);

                // Write type mapping results
                await WriteTypeMappingResultsAsync(typeMappings);

                // Phase 3: Code Generation (skip if analyze-only mode)
                if (config.Mode == OperationMode.Analyze || config.DryRun)
                {
                    _logger.LogInfo("Analysis completed. Skipping code generation (analyze-only mode).");
                    return new GenerationResult
                    {
                        Success = true,
                        DiscoveredClasses = discoveredClasses,
                        TypeMappings = typeMappings,
                        GeneratedFiles = new List<string>()
                    };
                }

                _logger.LogInfo("Phase 3: Generating adapter code...");
                var codeGenerator = CreateCodeGenerator(config);
                var generatedFiles = await codeGenerator.GenerateAdaptersAsync(discoveredClasses, typeMappings, config);

                if (generatedFiles.Count == 0)
                {
                    _logger.LogError("Code generation failed - no files generated");
                    return new GenerationResult
                    {
                        Success = false,
                        ErrorMessage = "Code generation failed - no files generated",
                        ExitCode = 8
                    };
                }

                // Phase 4: Generate Reports and Manifests
                _logger.LogInfo("Phase 4: Generating reports and manifests...");
                await GenerateReportsAsync(config, discoveredClasses, typeMappings, generatedFiles);

                // Phase 5: Generate Tag Patterns
                await GenerateTagPatternsAsync(config);

                _logger.LogInfo($"Adapter generation completed successfully. Generated {generatedFiles.Count} files.");

                return new GenerationResult
                {
                    Success = true,
                    DiscoveredClasses = discoveredClasses,
                    TypeMappings = typeMappings,
                    GeneratedFiles = generatedFiles
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Adapter generation failed", ex);
                return new GenerationResult
                {
                    Success = false,
                    ErrorMessage = ex.Message,
                    ExitCode = 1
                };
            }
        }

        private void LogConfigurationSummary(GeneratorConfiguration config)
        {
            _logger.LogDebug("=== Configuration Summary ===");
            _logger.LogDebug($"Language: {config.GetSelectedLanguage()}");
            _logger.LogDebug($"Platform: {config.GetSelectedPlatform()}");
            _logger.LogDebug($"Repository: {MaskSensitiveValue(config.RepoUrl)}");
            _logger.LogDebug($"Branch: {config.Branch}");
            _logger.LogDebug($"Mode: {config.Mode}");
            _logger.LogDebug($"Output Directory: {config.Output.OutputDir}");
            _logger.LogDebug($"Tag Template: {config.TagTemplate}");

            if (!string.IsNullOrWhiteSpace(config.TrackAttribute))
                _logger.LogDebug($"Discovery Method: Attribute ({config.TrackAttribute})");
            else if (!string.IsNullOrWhiteSpace(config.TrackPattern))
                _logger.LogDebug($"Discovery Method: Pattern ({config.TrackPattern})");
            else if (!string.IsNullOrWhiteSpace(config.TrackNamespace))
                _logger.LogDebug($"Discovery Method: Namespace ({config.TrackNamespace})");
            else if (!string.IsNullOrWhiteSpace(config.TrackFilePattern))
                _logger.LogDebug($"Discovery Method: File Pattern ({config.TrackFilePattern})");

            _logger.LogDebug("==============================");
        }

        private string MaskSensitiveValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "[not set]";

            if (value.Length <= 8)
                return "***";

            return value.Substring(0, 4) + "***" + value.Substring(value.Length - 4);
        }

        private IClassDiscoveryEngine CreateDiscoveryEngine(GeneratorConfiguration config)
        {
            var selectedLanguage = config.GetSelectedLanguage();

            return selectedLanguage switch
            {
                SourceLanguage.CSharp => new CSharpDiscoveryEngine(_logger),
                SourceLanguage.Java => new JavaDiscoveryEngine(_logger),
                SourceLanguage.Kotlin => new KotlinDiscoveryEngine(_logger),
                SourceLanguage.JavaScript => new JavaScriptDiscoveryEngine(_logger),
                SourceLanguage.TypeScript => new TypeScriptDiscoveryEngine(_logger),
                SourceLanguage.Python => new PythonDiscoveryEngine(_logger),
                _ => throw new NotSupportedException($"Language {selectedLanguage} is not supported")
            };
        }

        private ICodeGenerator CreateCodeGenerator(GeneratorConfiguration config)
        {
            var selectedPlatform = config.GetSelectedPlatform();

            return selectedPlatform switch
            {
                TargetPlatform.Android => new AndroidCodeGenerator(_logger),
                TargetPlatform.iOS => new IosCodeGenerator(_logger),
                _ => throw new NotSupportedException($"Platform {selectedPlatform} is not supported")
            };
        }

        private async Task WriteAnalysisResultsAsync(List<DiscoveredClass> discoveredClasses)
        {
            try
            {
                var analysisResult = new
                {
                    DiscoveredAt = DateTime.UtcNow,
                    ClassCount = discoveredClasses.Count,
                    Classes = discoveredClasses
                };

                var json = JsonSerializer.Serialize(analysisResult, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync("adapter-analysis.json", json);
                _logger.LogDebug("Analysis results written to adapter-analysis.json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write analysis results: {ex.Message}");
            }
        }

        private async Task WriteTypeMappingResultsAsync(Dictionary<string, TypeMappingInfo> typeMappings)
        {
            try
            {
                var typeMappingResult = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    MappingCount = typeMappings.Count,
                    Mappings = typeMappings
                };

                var json = JsonSerializer.Serialize(typeMappingResult, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync("type-mappings.json", json);
                _logger.LogDebug("Type mapping results written to type-mappings.json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write type mapping results: {ex.Message}");
            }
        }

        private async Task GenerateReportsAsync(GeneratorConfiguration config, List<DiscoveredClass> discoveredClasses,
            Dictionary<string, TypeMappingInfo> typeMappings, List<string> generatedFiles)
        {
            try
            {
                var generationReport = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    Configuration = new
                    {
                        Language = config.GetSelectedLanguage().ToString(),
                        Platform = config.GetSelectedPlatform().ToString(),
                        Mode = config.Mode.ToString(),
                        Repository = config.RepoUrl,
                        Branch = config.Branch
                    },
                    Summary = new
                    {
                        DiscoveredClasses = discoveredClasses.Count,
                        TypeMappings = typeMappings.Count,
                        GeneratedFiles = generatedFiles.Count
                    },
                    GeneratedFiles = generatedFiles
                };

                var json = JsonSerializer.Serialize(generationReport, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync("generation-report.json", json);
                _logger.LogDebug("Generation report written to generation-report.json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write generation report: {ex.Message}");
            }
        }

        private async Task GenerateTagPatternsAsync(GeneratorConfiguration config)
        {
            try
            {
                var tagResolver = new TagTemplateResolver();
                var resolvedTag = tagResolver.ResolveTemplate(config.TagTemplate, config);

                var tagPatterns = new
                {
                    GeneratedAt = DateTime.UtcNow,
                    Template = config.TagTemplate,
                    ResolvedTag = resolvedTag,
                    Platform = config.GetSelectedPlatform().ToString(),
                    Language = config.GetSelectedLanguage().ToString()
                };

                var json = JsonSerializer.Serialize(tagPatterns, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                await File.WriteAllTextAsync("tag-patterns.json", json);
                _logger.LogDebug("Tag patterns written to tag-patterns.json");
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Failed to write tag patterns: {ex.Message}");
            }
        }
    }

    public class GenerationResult
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public int ExitCode { get; set; }
        public List<DiscoveredClass> DiscoveredClasses { get; set; } = new List<DiscoveredClass>();
        public Dictionary<string, TypeMappingInfo> TypeMappings { get; set; } = new Dictionary<string, TypeMappingInfo>();
        public List<string> GeneratedFiles { get; set; } = new List<string>();
    }
}