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

        // Header comment - enhanced with attribute info
        sb.AppendLine("/**");
        sb.AppendLine(" * This is a truly stupid adapter that just bridges stuff.");
        sb.AppendLine(" * It should never be smart. Ever.");
        sb.AppendLine(" * Universal StateFlow pattern with true two-way binding!");
        sb.AppendLine(" *");
        sb.AppendLine($" * Generated from: {viewModel.FullName}");
        sb.AppendLine($" * Source: {viewModel.Source}");
        sb.AppendLine($" * Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");

        // NEW: Add attribute information to header
        if (viewModel.Properties.Any(p => p.MapToAttribute != null || p.DateTypeAttribute != null))
        {
            sb.AppendLine(" * Uses custom type mappings and DateTime semantics");
        }

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

        // NEW: Add conditional imports based on attributes
        AddConditionalAndroidImports(sb, viewModel);

        sb.AppendLine();

        // Class declaration
        sb.AppendLine($"class {viewModel.AdapterName} @Inject constructor(");
        sb.AppendLine($"    private val dotnetViewModel: {viewModel.FullName}");
        sb.AppendLine(") : ViewModel() {");
        sb.AppendLine();

        // Generate StateFlow properties for ALL properties (universal pattern)
        // NEW: Filter properties based on platform availability
        var simpleProperties = viewModel.Properties
            .Where(p => !p.IsObservableCollection &&
                       !_typeTranslator.ShouldExcludePropertyForPlatform(p, "android"))
            .ToList();

        if (simpleProperties.Any())
        {
            sb.AppendLine("    // Universal StateFlow pattern - ALL properties get reactive StateFlow");
            foreach (var prop in simpleProperties)
            {
                // NEW: Use attribute-aware type mapping
                var kotlinType = _typeTranslator.GetKotlinType(prop);
                var propertyName = _typeTranslator.GetKotlinPropertyName(prop);

                // NEW: Add warning comment for custom implementation needed
                if (prop.WarnCustomImplementationNeededAttribute != null)
                {
                    sb.AppendLine($"    // WARNING: {prop.WarnCustomImplementationNeededAttribute.Message ?? "Custom implementation needed"}");
                }

                sb.AppendLine($"    private val _{propertyName} = MutableStateFlow(dotnetViewModel.{prop.Name})");
                sb.AppendLine($"    val {propertyName}: StateFlow<{kotlinType}> = _{propertyName}.asStateFlow()");
            }
            sb.AppendLine();
        }

        // Generate observable collections with StateFlow pattern
        var collections = viewModel.Properties
            .Where(p => p.IsObservableCollection &&
                       !_typeTranslator.ShouldExcludePropertyForPlatform(p, "android"))
            .ToList();

        if (collections.Any())
        {
            sb.AppendLine("    // ObservableCollection → StateFlow<List<T>> with CollectionChanged + PropertyChanged");
            foreach (var collection in collections)
            {
                var elementType = _typeTranslator.GetKotlinType(collection.ElementType);
                var collectionName = _typeTranslator.GetKotlinPropertyName(collection);

                // NEW: Handle collection behavior attributes
                if (collection.CollectionBehaviorAttribute?.SupportsBatching == true)
                {
                    sb.AppendLine($"    // Collection supports batching (batch size: {collection.CollectionBehaviorAttribute.BatchSize})");
                }

                sb.AppendLine($"    private val _{collectionName} = MutableStateFlow<List<{elementType}>>(dotnetViewModel.{collection.Name}.toList())");
                sb.AppendLine($"    val {collectionName}: StateFlow<List<{elementType}>> = _{collectionName}.asStateFlow()");
            }
            sb.AppendLine();
        }

        // Generate commands
        // NEW: Filter commands based on platform availability
        var availableCommands = viewModel.Commands
            .Where(c => !_typeTranslator.ShouldExcludeCommandForPlatform(c, "android"))
            .ToList();

        if (availableCommands.Any())
        {
            sb.AppendLine("    // Commands - let .NET handle all the business logic");
            foreach (var command in availableCommands)
            {
                GenerateAndroidCommand(sb, command);
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
            var propertyName = _typeTranslator.GetKotlinPropertyName(prop);
            // NEW: Use attribute-aware conversion
            var conversion = _typeTranslator.GetKotlinDateTimeConversion(prop, $"dotnetViewModel.{prop.Name}");
            sb.AppendLine($"                \"{prop.Name}\" -> _{propertyName}.value = {conversion}");
        }

        // Add PropertyChanged cases for collections (whole collection replacement)
        foreach (var collection in collections)
        {
            var collectionName = _typeTranslator.GetKotlinPropertyName(collection);
            sb.AppendLine($"                \"{collection.Name}\" -> _{collectionName}.value = dotnetViewModel.{collection.Name}.toList()");
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
                var collectionName = _typeTranslator.GetKotlinPropertyName(collection);
                sb.AppendLine($"        dotnetViewModel.{collection.Name}.CollectionChanged += {{ sender, args ->");
                sb.AppendLine($"            _{collectionName}.value = dotnetViewModel.{collection.Name}.toList()");
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
                var propertyName = _typeTranslator.GetKotlinPropertyName(prop);

                // NEW: Handle validation behavior
                if (prop.ValidationBehaviorAttribute?.ValidateOnSet == true)
                {
                    sb.AppendLine($"        // Property has validation: {prop.ValidationBehaviorAttribute.ValidatorMethod ?? "default validation"}");
                }

                sb.AppendLine($"        {propertyName}.onEach {{ newValue ->");
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

    // NEW: Generate command with attribute support
    private void GenerateAndroidCommand(StringBuilder sb, CommandMetadata command)
    {
        var commandName = _typeTranslator.GetKotlinCommandName(command);

        // NEW: Add warning comment for custom implementation needed
        if (command.WarnCustomImplementationNeededAttribute != null)
        {
            sb.AppendLine($"    // WARNING: {command.WarnCustomImplementationNeededAttribute.Message ?? "Custom implementation needed"}");
        }

        // NEW: Handle threading behavior
        var requiresMainThread = command.ThreadingBehaviorAttribute?.RequiresMainThread == true ||
                                command.CommandBehaviorAttribute?.RequiresMainThread == true;
        var requiresBackgroundThread = command.ThreadingBehaviorAttribute?.RequiresBackgroundThread == true;

        if (command.IsAsync)
        {
            var dispatcher = requiresMainThread ? "Dispatchers.Main" :
                           requiresBackgroundThread ? "Dispatchers.IO" :
                           "Dispatchers.IO"; // Default for async

            sb.AppendLine($"    suspend fun {commandName}({GetParameterSignature(command)}): Result<Unit> = withContext({dispatcher}) {{");
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
            if (requiresMainThread)
            {
                sb.AppendLine($"    fun {commandName}({GetParameterSignature(command)}) {{");
                sb.AppendLine($"        // Ensure main thread execution");
                sb.AppendLine($"        viewModelScope.launch(Dispatchers.Main) {{");

                if (command.HasParameter)
                {
                    sb.AppendLine($"            dotnetViewModel.{command.Name}.execute({GetParameterName(command)})");
                }
                else
                {
                    sb.AppendLine($"            dotnetViewModel.{command.Name}.execute()");
                }

                sb.AppendLine($"        }}");
                sb.AppendLine($"    }}");
            }
            else
            {
                sb.AppendLine($"    fun {commandName}({GetParameterSignature(command)}) {{");

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
        }

        // NEW: Expose CanExecute if requested
        if (command.CommandBehaviorAttribute?.ExposeCanExecute == true)
        {
            sb.AppendLine($"    val can{commandName.Substring(0, 1).ToUpper()}{commandName.Substring(1)}: Boolean get() = dotnetViewModel.{command.Name}.canExecute");
        }

        sb.AppendLine();
    }

    // NEW: Add conditional imports based on used attributes
    private void AddConditionalAndroidImports(StringBuilder sb, ViewModelMetadata viewModel)
    {
        var needsDateTime = viewModel.Properties.Any(p => p.DateTypeAttribute != null);
        var needsLatLng = viewModel.Properties.Any(p => p.MapToAttribute?.Android.ToString().Contains("LatLng") == true);
        var needsUUID = viewModel.Properties.Any(p => p.MapToAttribute?.Android.ToString().Contains("UUID") == true);
        var hasThreadingRequirements = viewModel.Commands.Any(c =>
            c.ThreadingBehaviorAttribute?.RequiresMainThread == true ||
            c.CommandBehaviorAttribute?.RequiresMainThread == true);

        if (needsDateTime)
        {
            sb.AppendLine("import java.time.*");
        }

        if (needsLatLng)
        {
            sb.AppendLine("import com.google.android.gms.maps.model.LatLng");
        }

        if (needsUUID)
        {
            sb.AppendLine("import java.util.UUID");
        }

        if (hasThreadingRequirements)
        {
            sb.AppendLine("import kotlinx.coroutines.launch");
        }
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