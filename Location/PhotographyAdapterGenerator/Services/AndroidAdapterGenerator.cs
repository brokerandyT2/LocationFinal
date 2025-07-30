using Microsoft.Extensions.Logging;
using System.Text;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class AndroidAdapterGenerator
{
    private readonly ILogger<AndroidAdapterGenerator> _logger;
    private readonly TypeTranslator _typeTranslator;

    public AndroidAdapterGenerator(ILogger<AndroidAdapterGenerator> logger, TypeTranslator typeTranslator)
    {
        _logger = logger;
        _typeTranslator = typeTranslator;
    }

    public async Task GenerateAdaptersAsync(List<ViewModelMetadata> viewModels, string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        foreach (var viewModel in viewModels)
        {
            var fileName = $"{viewModel.AdapterName}.kt";
            var filePath = Path.Combine(outputPath, fileName);

            _logger.LogDebug("Generating Android adapter: {FileName} for {ViewModelName}", fileName, viewModel.Name);

            var content = GenerateAndroidAdapter(viewModel);

            await File.WriteAllTextAsync(filePath, content);
            _logger.LogInformation("Generated Android adapter: {FileName}", fileName);
        }
    }

    private string GenerateAndroidAdapter(ViewModelMetadata viewModel)
    {
        var sb = new StringBuilder();

        // Header comment
        sb.AppendLine("/**");
        sb.AppendLine(" * This is a truly stupid adapter that just bridges stuff.");
        sb.AppendLine(" * It should never be smart. Ever.");
        sb.AppendLine(" * Universal StateFlow pattern with true two-way binding!");
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
        sb.AppendLine("import androidx.lifecycle.viewModelScope");
        sb.AppendLine("import kotlinx.coroutines.flow.MutableStateFlow");
        sb.AppendLine("import kotlinx.coroutines.flow.StateFlow");
        sb.AppendLine("import kotlinx.coroutines.flow.asStateFlow");
        sb.AppendLine("import kotlinx.coroutines.flow.onEach");
        sb.AppendLine("import kotlinx.coroutines.flow.launchIn");
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

        // Generate StateFlow properties for ALL properties (universal pattern)
        var simpleProperties = viewModel.Properties.Where(p => !p.IsObservableCollection).ToList();
        if (simpleProperties.Any())
        {
            sb.AppendLine("    // Universal StateFlow pattern - ALL properties get reactive StateFlow");
            foreach (var prop in simpleProperties)
            {
                var kotlinType = _typeTranslator.GetKotlinType(prop.Type);
                sb.AppendLine($"    private val _{prop.CamelCaseName} = MutableStateFlow(dotnetViewModel.{prop.Name})");
                sb.AppendLine($"    val {prop.CamelCaseName}: StateFlow<{kotlinType}> = _{prop.CamelCaseName}.asStateFlow()");
            }
            sb.AppendLine();
        }

        // Generate observable collections with StateFlow pattern
        var collections = viewModel.Properties.Where(p => p.IsObservableCollection).ToList();
        if (collections.Any())
        {
            sb.AppendLine("    // ObservableCollection → StateFlow<List<T>> with CollectionChanged + PropertyChanged");
            foreach (var collection in collections)
            {
                var elementType = _typeTranslator.GetKotlinType(collection.ElementType);
                sb.AppendLine($"    private val _{collection.CamelCaseName} = MutableStateFlow<List<{elementType}>>(dotnetViewModel.{collection.Name}.toList())");
                sb.AppendLine($"    val {collection.CamelCaseName}: StateFlow<List<{elementType}>> = _{collection.CamelCaseName}.asStateFlow()");
            }
            sb.AppendLine();
        }

        // Generate commands
        if (viewModel.Commands.Any())
        {
            sb.AppendLine("    // Commands - let .NET handle all the business logic");
            foreach (var command in viewModel.Commands)
            {
                if (command.IsAsync)
                {
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

        // Generate init block with universal PropertyChanged listener + two-way sync
        sb.AppendLine("    init {");
        sb.AppendLine("        // Universal PropertyChanged listener - syncs ALL .NET changes to StateFlow");
        sb.AppendLine("        dotnetViewModel.PropertyChanged += { sender, args ->");
        sb.AppendLine("            when (args.PropertyName) {");

        // Add PropertyChanged cases for simple properties
        foreach (var prop in simpleProperties)
        {
            sb.AppendLine($"                \"{prop.Name}\" -> _{prop.CamelCaseName}.value = dotnetViewModel.{prop.Name}");
        }

        // Add PropertyChanged cases for collections (whole collection replacement)
        foreach (var collection in collections)
        {
            sb.AppendLine($"                \"{collection.Name}\" -> _{collection.CamelCaseName}.value = dotnetViewModel.{collection.Name}.toList()");
        }

        sb.AppendLine("            }");
        sb.AppendLine("        }");
        sb.AppendLine();

        // Add CollectionChanged listeners for ObservableCollections
        if (collections.Any())
        {
            sb.AppendLine("        // CollectionChanged listeners for progressive loading and granular updates");
            foreach (var collection in collections)
            {
                sb.AppendLine($"        dotnetViewModel.{collection.Name}.CollectionChanged += {{ sender, args ->");
                sb.AppendLine($"            _{collection.CamelCaseName}.value = dotnetViewModel.{collection.Name}.toList()");
                sb.AppendLine($"        }}");
            }
            sb.AppendLine();
        }

        // Add two-way binding for read-write properties
        var readWriteProperties = simpleProperties.Where(p => !p.IsReadOnly).ToList();
        if (readWriteProperties.Any())
        {
            sb.AppendLine("        // Two-way binding - sync Kotlin changes back to .NET (read-write properties only)");
            foreach (var prop in readWriteProperties)
            {
                sb.AppendLine($"        {prop.CamelCaseName}.onEach {{ newValue ->");
                sb.AppendLine($"            dotnetViewModel.{prop.Name} = newValue");
                sb.AppendLine($"        }}.launchIn(viewModelScope)");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();

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
}