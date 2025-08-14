using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Generates API code from discovered entities using templates
    /// </summary>
    public class CodeGenerator
    {
        private readonly Configuration _config;
        private readonly Logger _logger;
        private readonly TagProcessor _tagProcessor;

        public CodeGenerator(Configuration config, Logger logger, TagProcessor tagProcessor)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _tagProcessor = tagProcessor ?? throw new ArgumentNullException(nameof(tagProcessor));
        }

        /// <summary>
        /// Generate API project from entities and templates
        /// </summary>
        public async Task<GeneratedProject> GenerateProjectAsync(List<DiscoveredEntity> entities, string templatePath, string version)
        {
            _logger.LogStartPhase("Code Generation");

            try
            {
                using var operation = _logger.TimeOperation("Code Generation");

                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "generated-api");
                Directory.CreateDirectory(outputPath);

                var project = new GeneratedProject
                {
                    OutputPath = outputPath,
                    Language = _config.SelectedLanguage,
                    Cloud = _config.SelectedCloud,
                    Version = version,
                    TemplatePath = templatePath,
                    GeneratedAt = DateTime.UtcNow,
                    Entities = entities
                };

                // Generate token replacements for template processing
                var tokenReplacements = CreateTokenReplacements(entities, version, templatePath);
                project.TokenReplacements = tokenReplacements;

                // Copy and process template files
                await ProcessTemplateFilesAsync(project, templatePath, tokenReplacements);

                // Generate entity-specific code
                await GenerateEntityCodeAsync(project, tokenReplacements);

                // Write project metadata
                await WriteProjectMetadataAsync(project);

                _logger.Info($"Generated {project.GeneratedFiles.Count} files in {project.OutputPath}");
                _logger.LogEndPhase("Code Generation", true);

                return project;
            }
            catch (Exception ex)
            {
                _logger.Error("Code generation failed", ex);
                _logger.LogEndPhase("Code Generation", false);
                throw new CodeGenerationException($"Code generation failed: {ex.Message}", 7);
            }
        }

        private Dictionary<string, string> CreateTokenReplacements(List<DiscoveredEntity> entities, string version, string templatePath)
        {
            var tokens = new Dictionary<string, string>
            {
                ["project-name"] = SanitizeIdentifier($"API-{_config.SelectedCloud}-{ExtractTemplateName(templatePath)}"),
                ["namespace"] = SanitizeNamespace($"Generated.API.{_config.SelectedCloud.Capitalize()}"),
                ["version"] = version,
                ["language"] = _config.SelectedLanguage,
                ["cloud"] = _config.SelectedCloud,
                ["template-name"] = ExtractTemplateName(templatePath),
                ["repo-url"] = _config.RepoUrl,
                ["branch"] = _config.Branch,
                ["repo-name"] = ExtractRepoName(_config.RepoUrl),
                ["entity-count"] = entities.Count.ToString(),
                ["entity-names"] = string.Join(", ", entities.Select(e => e.Name)),
                ["primary-entity"] = entities.FirstOrDefault()?.Name ?? "Entity",
                ["generated-date"] = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                ["generated-datetime"] = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"),
                ["generator-version"] = GetGeneratorVersion(),
                ["track-attribute"] = _config.TrackAttribute,
                ["azure-subscription"] = _config.AzureSubscription,
                ["azure-resource-group"] = _config.AzureResourceGroup,
                ["azure-region"] = _config.AzureRegion,
                ["aws-region"] = _config.AwsRegion,
                ["aws-account-id"] = _config.AwsAccountId,
                ["gcp-project-id"] = _config.GcpProjectId,
                ["gcp-region"] = _config.GcpRegion,
                ["oci-compartment-id"] = _config.OciCompartmentId,
                ["oci-region"] = _config.OciRegion
            };

            // Add deployment tag
            var tag = _tagProcessor.ProcessTagTemplate(version, templatePath);
            tokens["deployment-tag"] = tag;
            tokens["tag-safe"] = SanitizeIdentifier(tag);

            return tokens;
        }

        private async Task ProcessTemplateFilesAsync(GeneratedProject project, string templatePath, Dictionary<string, string> tokenReplacements)
        {
            _logger.Debug("Processing template files");

            var templateFiles = Directory.GetFiles(templatePath, "*", SearchOption.AllDirectories);

            foreach (var templateFile in templateFiles)
            {
                var relativePath = Path.GetRelativePath(templatePath, templateFile);
                var outputFile = Path.Combine(project.OutputPath, relativePath);

                Directory.CreateDirectory(Path.GetDirectoryName(outputFile));

                if (IsTextFile(templateFile))
                {
                    var content = await File.ReadAllTextAsync(templateFile);
                    content = ProcessTokenReplacements(content, tokenReplacements);
                    await File.WriteAllTextAsync(outputFile, content);
                }
                else
                {
                    File.Copy(templateFile, outputFile, true);
                }

                _logger.LogFileGeneration(outputFile, new FileInfo(outputFile).Length);

                project.GeneratedFiles.Add(new GeneratedFile
                {
                    RelativePath = relativePath,
                    FullPath = outputFile,
                    Type = "template",
                    SizeBytes = new FileInfo(outputFile).Length
                });
            }
        }

        private async Task GenerateEntityCodeAsync(GeneratedProject project, Dictionary<string, string> tokenReplacements)
        {
            _logger.Debug($"Generating entity code for {project.Entities.Count} entities");

            var generator = CreateLanguageGenerator();

            foreach (var entity in project.Entities)
            {
                var entityTokens = new Dictionary<string, string>(tokenReplacements)
                {
                    ["entity-name"] = entity.Name,
                    ["entity-full-name"] = entity.FullName,
                    ["entity-namespace"] = entity.Namespace ?? "",
                    ["entity-properties"] = generator.GenerateProperties(entity.Properties),
                    ["entity-properties-count"] = entity.Properties.Count.ToString(),
                    ["entity-source-file"] = entity.SourceFile
                };

                var entityFiles = generator.GenerateEntityFiles(entity, entityTokens);

                foreach (var file in entityFiles)
                {
                    var fullPath = Path.Combine(project.OutputPath, file.RelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(fullPath));

                    await File.WriteAllTextAsync(fullPath, file.Content);
                    _logger.LogFileGeneration(fullPath, file.Content.Length);

                    project.GeneratedFiles.Add(new GeneratedFile
                    {
                        RelativePath = file.RelativePath,
                        FullPath = fullPath,
                        Type = file.Type,
                        Entity = entity.Name,
                        SizeBytes = file.Content.Length
                    });
                }
            }
        }

        private async Task WriteProjectMetadataAsync(GeneratedProject project)
        {
            var metadataPath = Path.Combine(project.OutputPath, "api-metadata.json");

            var metadata = new
            {
                project.Language,
                project.Cloud,
                project.Version,
                project.TemplatePath,
                project.GeneratedAt,
                EntityCount = project.Entities.Count,
                Entities = project.Entities.Select(e => new
                {
                    e.Name,
                    e.FullName,
                    e.Namespace,
                    e.Language,
                    PropertyCount = e.Properties.Count
                }),
                GeneratedFileCount = project.GeneratedFiles.Count,
                GeneratedFiles = project.GeneratedFiles.Select(f => new
                {
                    f.RelativePath,
                    f.Type,
                    f.Entity,
                    f.SizeBytes
                }),
                TotalSizeBytes = project.GeneratedFiles.Sum(f => f.SizeBytes),
                Configuration = new
                {
                    TrackAttribute = _config.TrackAttribute,
                    RepoUrl = _config.RepoUrl,
                    Branch = _config.Branch,
                    Mode = _config.Mode
                }
            };

            var json = JsonSerializer.Serialize(metadata, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await File.WriteAllTextAsync(metadataPath, json);
            _logger.LogFileGeneration(metadataPath, json.Length);

            project.GeneratedFiles.Add(new GeneratedFile
            {
                RelativePath = "api-metadata.json",
                FullPath = metadataPath,
                Type = "metadata",
                SizeBytes = json.Length
            });
        }

        private ILanguageGenerator CreateLanguageGenerator()
        {
            return _config.SelectedLanguage switch
            {
                "csharp" => new CSharpGenerator(_config, _logger),
                "java" => new JavaGenerator(_config, _logger),
                "python" => new PythonGenerator(_config, _logger),
                "javascript" => new JavaScriptGenerator(_config, _logger),
                "typescript" => new TypeScriptGenerator(_config, _logger),
                "go" => new GoGenerator(_config, _logger),
                _ => throw new CodeGenerationException($"Unsupported language: {_config.SelectedLanguage}", 7)
            };
        }

        private bool IsTextFile(string filePath)
        {
            var textExtensions = new[]
            {
                ".txt", ".json", ".yaml", ".yml", ".xml", ".cs", ".java", ".py", ".js", ".ts",
                ".bicep", ".tf", ".md", ".sql", ".ps1", ".sh", ".bat", ".cmd", ".config",
                ".template", ".tmpl", ".mustache", ".handlebars", ".toml", ".ini", ".properties"
            };

            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            return textExtensions.Contains(extension);
        }

        private string ProcessTokenReplacements(string content, Dictionary<string, string> tokenReplacements)
        {
            foreach (var replacement in tokenReplacements)
            {
                content = content.Replace($"{{{replacement.Key}}}", replacement.Value);
            }
            return content;
        }

        private string ExtractTemplateName(string templatePath)
        {
            return Path.GetFileName(templatePath?.TrimEnd(Path.DirectorySeparatorChar)) ?? "default";
        }

        private string ExtractRepoName(string repoUrl)
        {
            if (string.IsNullOrWhiteSpace(repoUrl)) return "unknown";

            try
            {
                var uri = new Uri(repoUrl);
                var name = Path.GetFileNameWithoutExtension(uri.LocalPath.Split('/').LastOrDefault() ?? "unknown");
                return SanitizeIdentifier(name);
            }
            catch
            {
                return "unknown";
            }
        }

        private string SanitizeIdentifier(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "Generated";

            var sanitized = System.Text.RegularExpressions.Regex.Replace(input, @"[^a-zA-Z0-9_]", "");
            return char.IsDigit(sanitized[0]) ? $"_{sanitized}" : sanitized;
        }

        private string SanitizeNamespace(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "Generated.API";

            return string.Join(".", input.Split('.').Select(SanitizeIdentifier));
        }

        private string GetGeneratorVersion()
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            return assembly.GetName().Version?.ToString() ?? "1.0.0";
        }
    }

    // Supporting interfaces and classes
    public interface ILanguageGenerator
    {
        string GenerateProperties(List<EntityProperty> properties);
        List<GeneratedCodeFile> GenerateEntityFiles(DiscoveredEntity entity, Dictionary<string, string> tokens);
    }

    public class GeneratedProject
    {
        public string OutputPath { get; set; }
        public string Language { get; set; }
        public string Cloud { get; set; }
        public string Version { get; set; }
        public string TemplatePath { get; set; }
        public DateTime GeneratedAt { get; set; }
        public List<DiscoveredEntity> Entities { get; set; } = new List<DiscoveredEntity>();
        public List<GeneratedFile> GeneratedFiles { get; set; } = new List<GeneratedFile>();
        public Dictionary<string, string> TokenReplacements { get; set; } = new Dictionary<string, string>();
    }

    public class GeneratedFile
    {
        public string RelativePath { get; set; }
        public string FullPath { get; set; }
        public string Type { get; set; }
        public string Entity { get; set; }
        public long SizeBytes { get; set; }
    }

    public class GeneratedCodeFile
    {
        public string RelativePath { get; set; }
        public string Content { get; set; }
        public string Type { get; set; }
    }

    public static class StringExtensions
    {
        public static string Capitalize(this string input)
        {
            return string.IsNullOrWhiteSpace(input) ? input : char.ToUpperInvariant(input[0]) + input.Substring(1);
        }
    }

    public class CodeGenerationException : Exception
    {
        public int ExitCode { get; }

        public CodeGenerationException(string message, int exitCode) : base(message)
        {
            ExitCode = exitCode;
        }

        public CodeGenerationException(string message, int exitCode, Exception innerException) : base(message, innerException)
        {
            ExitCode = exitCode;
        }
    }

    // Language-specific generators (simplified)
    public class CSharpGenerator : ILanguageGenerator
    {
        private readonly Configuration _config;
        private readonly Logger _logger;

        public CSharpGenerator(Configuration config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GenerateProperties(List<EntityProperty> properties)
        {
            var sb = new StringBuilder();
            foreach (var prop in properties)
            {
                var nullable = prop.IsNullable ? "?" : "";
                sb.AppendLine($"        public {prop.Type}{nullable} {prop.Name} {{ get; set; }}");
            }
            return sb.ToString().TrimEnd();
        }

        public List<GeneratedCodeFile> GenerateEntityFiles(DiscoveredEntity entity, Dictionary<string, string> tokens)
        {
            var files = new List<GeneratedCodeFile>();

            // Generate model
            var modelContent = $@"using System;
using System.ComponentModel.DataAnnotations;

namespace {tokens["namespace"]}.Models
{{
    public class {entity.Name}
    {{
{GenerateProperties(entity.Properties)}
    }}
}}";

            files.Add(new GeneratedCodeFile
            {
                RelativePath = $"Models/{entity.Name}.cs",
                Content = modelContent,
                Type = "model"
            });

            return files;
        }
    }

    public class JavaGenerator : ILanguageGenerator
    {
        private readonly Configuration _config;
        private readonly Logger _logger;

        public JavaGenerator(Configuration config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GenerateProperties(List<EntityProperty> properties)
        {
            var sb = new StringBuilder();
            foreach (var prop in properties)
            {
                var javaType = ConvertToJavaType(prop.Type);
                sb.AppendLine($"    private {javaType} {ToCamelCase(prop.Name)};");
                sb.AppendLine($"    public {javaType} get{prop.Name}() {{ return {ToCamelCase(prop.Name)}; }}");
                sb.AppendLine($"    public void set{prop.Name}({javaType} {ToCamelCase(prop.Name)}) {{ this.{ToCamelCase(prop.Name)} = {ToCamelCase(prop.Name)}; }}");
            }
            return sb.ToString().TrimEnd();
        }

        public List<GeneratedCodeFile> GenerateEntityFiles(DiscoveredEntity entity, Dictionary<string, string> tokens)
        {
            var files = new List<GeneratedCodeFile>();

            var modelContent = $@"package {tokens["namespace"].ToLowerInvariant()}.models;

public class {entity.Name} {{
{GenerateProperties(entity.Properties)}
}}";

            files.Add(new GeneratedCodeFile
            {
                RelativePath = $"src/main/java/models/{entity.Name}.java",
                Content = modelContent,
                Type = "model"
            });

            return files;
        }

        private string ConvertToJavaType(string csharpType)
        {
            return csharpType switch
            {
                "string" => "String",
                "int" => "Integer",
                "long" => "Long",
                "bool" => "Boolean",
                "double" => "Double",
                "float" => "Float",
                _ => "Object"
            };
        }

        private string ToCamelCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }
    }

    public class PythonGenerator : ILanguageGenerator
    {
        private readonly Configuration _config;
        private readonly Logger _logger;

        public PythonGenerator(Configuration config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GenerateProperties(List<EntityProperty> properties)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    def __init__(self):");
            foreach (var prop in properties)
            {
                sb.AppendLine($"        self.{ToSnakeCase(prop.Name)} = None");
            }
            return sb.ToString().TrimEnd();
        }

        public List<GeneratedCodeFile> GenerateEntityFiles(DiscoveredEntity entity, Dictionary<string, string> tokens)
        {
            var files = new List<GeneratedCodeFile>();

            var modelContent = $@"from dataclasses import dataclass

@dataclass
class {entity.Name}:
{GenerateProperties(entity.Properties)}
";

            files.Add(new GeneratedCodeFile
            {
                RelativePath = $"models/{entity.Name.ToLowerInvariant()}.py",
                Content = modelContent,
                Type = "model"
            });

            return files;
        }

        private string ToSnakeCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return System.Text.RegularExpressions.Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
        }
    }

    public class JavaScriptGenerator : ILanguageGenerator
    {
        private readonly Configuration _config;
        private readonly Logger _logger;

        public JavaScriptGenerator(Configuration config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GenerateProperties(List<EntityProperty> properties)
        {
            var sb = new StringBuilder();
            sb.AppendLine("    constructor() {");
            foreach (var prop in properties)
            {
                sb.AppendLine($"        this.{ToCamelCase(prop.Name)} = null;");
            }
            sb.AppendLine("    }");
            return sb.ToString().TrimEnd();
        }

        public List<GeneratedCodeFile> GenerateEntityFiles(DiscoveredEntity entity, Dictionary<string, string> tokens)
        {
            var files = new List<GeneratedCodeFile>();

            var modelContent = $@"class {entity.Name} {{
{GenerateProperties(entity.Properties)}
}}

module.exports = {entity.Name};";

            files.Add(new GeneratedCodeFile
            {
                RelativePath = $"models/{entity.Name}.js",
                Content = modelContent,
                Type = "model"
            });

            return files;
        }

        private string ToCamelCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }
    }

    public class TypeScriptGenerator : ILanguageGenerator
    {
        private readonly Configuration _config;
        private readonly Logger _logger;

        public TypeScriptGenerator(Configuration config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GenerateProperties(List<EntityProperty> properties)
        {
            var sb = new StringBuilder();
            foreach (var prop in properties)
            {
                var tsType = ConvertToTypeScriptType(prop.Type);
                var optional = prop.IsNullable ? "?" : "";
                sb.AppendLine($"    {ToCamelCase(prop.Name)}{optional}: {tsType};");
            }
            return sb.ToString().TrimEnd();
        }

        public List<GeneratedCodeFile> GenerateEntityFiles(DiscoveredEntity entity, Dictionary<string, string> tokens)
        {
            var files = new List<GeneratedCodeFile>();

            var modelContent = $@"export interface {entity.Name} {{
{GenerateProperties(entity.Properties)}
}}";

            files.Add(new GeneratedCodeFile
            {
                RelativePath = $"models/{entity.Name}.ts",
                Content = modelContent,
                Type = "model"
            });

            return files;
        }

        private string ConvertToTypeScriptType(string csharpType)
        {
            return csharpType switch
            {
                "string" => "string",
                "int" or "long" or "double" or "float" => "number",
                "bool" => "boolean",
                _ => "any"
            };
        }

        private string ToCamelCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return char.ToLowerInvariant(input[0]) + input.Substring(1);
        }
    }

    public class GoGenerator : ILanguageGenerator
    {
        private readonly Configuration _config;
        private readonly Logger _logger;

        public GoGenerator(Configuration config, Logger logger)
        {
            _config = config;
            _logger = logger;
        }

        public string GenerateProperties(List<EntityProperty> properties)
        {
            var sb = new StringBuilder();
            foreach (var prop in properties)
            {
                var goType = ConvertToGoType(prop.Type);
                var pointer = prop.IsNullable ? "*" : "";
                sb.AppendLine($"    {prop.Name} {pointer}{goType} `json:\"{ToSnakeCase(prop.Name)}\"`");
            }
            return sb.ToString().TrimEnd();
        }

        public List<GeneratedCodeFile> GenerateEntityFiles(DiscoveredEntity entity, Dictionary<string, string> tokens)
        {
            var files = new List<GeneratedCodeFile>();

            var modelContent = $@"package models

type {entity.Name} struct {{
{GenerateProperties(entity.Properties)}
}}";

            files.Add(new GeneratedCodeFile
            {
                RelativePath = $"models/{entity.Name.ToLowerInvariant()}.go",
                Content = modelContent,
                Type = "model"
            });

            return files;
        }

        private string ConvertToGoType(string csharpType)
        {
            return csharpType switch
            {
                "string" => "string",
                "int" => "int",
                "long" => "int64",
                "bool" => "bool",
                "double" => "float64",
                "float" => "float32",
                _ => "interface{}"
            };
        }

        private string ToSnakeCase(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return input;
            return System.Text.RegularExpressions.Regex.Replace(input, "([a-z])([A-Z])", "$1_$2").ToLowerInvariant();
        }
    }
}