using Microsoft.Extensions.Logging;
using System.Text;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class TemplateProcessor
{
    private readonly ILogger<TemplateProcessor> _logger;
    private readonly TypeTranslator _typeTranslator;

    public TemplateProcessor(ILogger<TemplateProcessor> logger, TypeTranslator typeTranslator)
    {
        _logger = logger;
        _typeTranslator = typeTranslator;
    }

    public async Task GenerateAdaptersAsync(List<ViewModelMetadata> viewModels, GeneratorOptions options)
    {
        try
        {
            if (options.Platform.ToLower() == "android")
            {
                await GenerateAndroidAdaptersAsync(viewModels, options.OutputPath);
            }
            else if (options.Platform.ToLower() == "ios")
            {
                await GenerateIOSAdaptersAsync(viewModels, options.OutputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate adapters");
            throw;
        }
    }

    private async Task GenerateAndroidAdaptersAsync(List<ViewModelMetadata> viewModels, string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        foreach (var viewModel in viewModels)
        {
            var fileName = $"{viewModel.AdapterName}.kt";
            var filePath = Path.Combine(outputPath, fileName);

            _logger.LogDebug("Generating adapter: {FileName} for {ViewModelName}", fileName, viewModel.Name);

            var content = GenerateAndroidAdapter(viewModel);

            await File.WriteAllTextAsync(filePath, content);
            _logger.LogInformation("Generated adapter: {FileName}", fileName);
        }
    }

    private string GenerateAndroidAdapter(ViewModelMetadata viewModel)
    {
        var sb = new StringBuilder();

        // Header comment
        sb.AppendLine("/**");
        sb.AppendLine(" * This is a truly stupid adapter that just bridges stuff.");
        sb.AppendLine(" * It should never be smart. Ever.");
        sb.AppendLine(" * (Now even more stupid because Akavache handles all the caching!)");
        sb.AppendLine(" *");
        sb.AppendLine($" * Generated from: {viewModel.FullName}");
        sb.AppendLine($" * Source: {viewModel.Source}");
        sb.AppendLine($" * Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine(" */");
        sb.AppendLine();

        // Package and imports
        sb.AppendLine("package com.3xSquaredCircles.photography.NoOpViewModelAdapters");
        sb.AppendLine();
        sb.AppendLine("import androidx.lifecycle.ViewModel");
        sb.AppendLine("import kotlinx.coroutines.flow.MutableStateFlow");
        sb.AppendLine("import kotlinx.coroutines.flow.StateFlow");
        sb.AppendLine("import kotlinx.coroutines.flow.asStateFlow");
        sb.AppendLine("import kotlinx.coroutines.Dispatchers");
        sb.AppendLine("import kotlinx.coroutines.withContext");
        sb.AppendLine("import javax.inject.Inject");
        sb.AppendLine("import kotlin.Result");
        sb.AppendLine();

        // Class declaration
        sb.AppendLine($"class {viewModel.AdapterName} @Inject constructor(");
        sb.AppendLine($"    private val dotnetViewModel: {viewModel.FullName}");
        sb.AppendLine(") : ViewModel() {");
        sb.AppendLine();

        // Generate simple properties (non-collections)
        var simpleProperties = viewModel.Properties.Where(p => !p.IsObservableCollection).ToList();
        if (simpleProperties.Any())
        {
            foreach (var prop in simpleProperties)
            {
                sb.AppendLine($"    // Direct property access - let .NET handle threading");
                sb.AppendLine($"    val {prop.CamelCaseName}: {_typeTranslator.GetKotlinType(prop.Type)} get() = dotnetViewModel.{prop.Name}");
                sb.AppendLine();
            }
        }

        // Generate observable collections
        var collections = viewModel.Properties.Where(p => p.IsObservableCollection).ToList();
        if (collections.Any())
        {
            foreach (var collection in collections)
            {
                var elementType = _typeTranslator.GetKotlinType(collection.ElementType);
                sb.AppendLine($"    // ObservableCollection<{collection.ElementType?.Name ?? "Unknown"}> → StateFlow<List<{elementType}>>");
                sb.AppendLine($"    private val _{collection.CamelCaseName} = MutableStateFlow<List<{elementType}>>(emptyList())");
                sb.AppendLine($"    val {collection.CamelCaseName}: StateFlow<List<{elementType}>> = _{collection.CamelCaseName}.asStateFlow()");
                sb.AppendLine();
            }
        }

        // Generate commands
        if (viewModel.Commands.Any())
        {
            foreach (var command in viewModel.Commands)
            {
                if (command.IsAsync)
                {
                    sb.AppendLine($"    // Async command: {command.Name} - let .NET handle threading");
                    sb.AppendLine($"    suspend fun {command.MethodName}({GetParameterSignature(command)}): Result<Unit> = withContext(Dispatchers.IO) {{");
                    sb.AppendLine($"        return try {{");

                    if (command.HasParameter)
                    {
                        sb.AppendLine($"            dotnetViewModel.{command.Name}.executeAsync({GetParameterName(command)})");
                    }
                    else
                    {
                        sb.AppendLine($"            dotnetViewModel.{command.Name}.executeAsync()");
                    }

                    sb.AppendLine($"            Result.success(Unit)");
                    sb.AppendLine($"        }} catch (e: Exception) {{");
                    sb.AppendLine($"            Result.failure(e)");
                    sb.AppendLine($"        }}");
                    sb.AppendLine($"    }}");
                }
                else
                {
                    sb.AppendLine($"    // Sync command: {command.Name}");
                    sb.AppendLine($"    fun {command.MethodName}({GetParameterSignature(command)}) {{");

                    if (command.HasParameter)
                    {
                        sb.AppendLine($"        dotnetViewModel.{command.Name}.execute({GetParameterName(command)})");
                    }
                    else
                    {
                        sb.AppendLine($"        dotnetViewModel.{command.Name}.execute()");
                    }

                    sb.AppendLine($"    }}");
                }
                sb.AppendLine();
            }
        }

        // Generate init block for observable collections
        if (collections.Any())
        {
            sb.AppendLine("    init {");
            foreach (var collection in collections)
            {
                sb.AppendLine($"        // Subscribe to ObservableCollection changes for {collection.Name}");
                sb.AppendLine($"        dotnetViewModel.{collection.Name}.collectionChanged += {{ _, _ ->");
                sb.AppendLine($"            _{collection.CamelCaseName}.value = dotnetViewModel.{collection.Name}.toList()");
                sb.AppendLine($"        }}");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        // Generate onCleared
        sb.AppendLine("    override fun onCleared() {");
        sb.AppendLine("        super.onCleared()");
        sb.AppendLine("        dotnetViewModel.dispose() // Always safe - all ViewModels implement IDisposable");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    private string GetParameterSignature(CommandMetadata command)
    {
        if (!command.HasParameter || command.ParameterType == null)
            return "";

        var paramName = ToCamelCase(command.ParameterType.Name);
        var paramType = _typeTranslator.GetKotlinType(command.ParameterType);
        return $"{paramName}: {paramType}";
    }

    private string GetParameterName(CommandMetadata command)
    {
        if (!command.HasParameter || command.ParameterType == null)
            return "";

        return ToCamelCase(command.ParameterType.Name);
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToLower(input[0]) + input[1..];
    }

    private async Task GenerateIOSAdaptersAsync(List<ViewModelMetadata> viewModels, string outputPath)
    {
        // TODO: Implement iOS generation using StringBuilder approach
        _logger.LogWarning("iOS template generation not yet implemented");
    }
}

public class TemplateContext
{
    public List<ViewModelMetadata> ViewModels { get; set; } = new();
    public string Platform { get; set; } = string.Empty;
    public string PackageName { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public TypeTranslator TypeTranslator { get; set; } = null!;
}

public class TypeTranslator
{
    public string GetKotlinType(Type? dotnetType)
    {
        if (dotnetType == null) return "Any";

        return dotnetType.Name switch
        {
            "String" => "String",
            "Int32" => "Int",
            "Int64" => "Long",
            "Boolean" => "Boolean",
            "Double" => "Double",
            "Single" => "Float",
            "DateTime" => "LocalDateTime",
            "ObservableCollection`1" => $"StateFlow<List<{GetElementType(dotnetType)}>>",
            _ => dotnetType.Name
        };
    }

    private string GetElementType(Type collectionType)
    {
        if (collectionType.IsGenericType)
        {
            var elementType = collectionType.GetGenericArguments().FirstOrDefault();
            return elementType?.Name ?? "Any";
        }
        return "Any";
    }
}