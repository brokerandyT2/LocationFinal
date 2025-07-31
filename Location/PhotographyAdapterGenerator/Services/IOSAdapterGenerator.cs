using Microsoft.Extensions.Logging;
using System.Text;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class IOSAdapterGenerator
{
    private readonly ILogger<IOSAdapterGenerator> _logger;
    private readonly TypeTranslator _typeTranslator;

    public IOSAdapterGenerator(ILogger<IOSAdapterGenerator> logger, TypeTranslator typeTranslator)
    {
        _logger = logger;
        _typeTranslator = typeTranslator;
    }

    public async Task GenerateAdaptersAsync(List<ViewModelMetadata> viewModels, string outputPath)
    {
        Directory.CreateDirectory(outputPath);

        foreach (var viewModel in viewModels)
        {
            var fileName = $"{viewModel.AdapterName}.swift";
            var filePath = Path.Combine(outputPath, fileName);

            _logger.LogDebug("Generating iOS adapter: {FileName} for {ViewModelName}", fileName, viewModel.Name);

            var content = GenerateIOSAdapter(viewModel);

            await File.WriteAllTextAsync(filePath, content);
            _logger.LogInformation("Generated iOS adapter: {FileName}", fileName);
        }
    }

    private string GenerateIOSAdapter(ViewModelMetadata viewModel)
    {
        var sb = new StringBuilder();

        // Header comment - enhanced with attribute info
        sb.AppendLine("/**");
        sb.AppendLine(" * This is a truly stupid adapter that just bridges stuff.");
        sb.AppendLine(" * It should never be smart. Ever.");
        sb.AppendLine(" * Universal @Published pattern with true two-way binding!");
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

        sb.AppendLine("import Foundation");
        sb.AppendLine("import Combine");

        // NEW: Add conditional imports based on attributes
        AddConditionalIOSImports(sb, viewModel);

        sb.AppendLine();

        sb.AppendLine($"class {viewModel.AdapterName}: ObservableObject {{");
        sb.AppendLine($"    private let dotnetViewModel: {viewModel.FullName}");
        sb.AppendLine("    private var cancellables = Set<AnyCancellable>()");
        sb.AppendLine();

        // Generate @Published properties for simple props
        // NEW: Filter properties based on platform availability
        var simpleProperties = viewModel.Properties
            .Where(p => !p.IsObservableCollection &&
                       !_typeTranslator.ShouldExcludePropertyForPlatform(p, "ios"))
            .ToList();

        if (simpleProperties.Any())
        {
            sb.AppendLine("    // Universal @Published pattern - ALL properties get reactive binding");
            foreach (var prop in simpleProperties)
            {
                // NEW: Use attribute-aware type mapping
                var swiftType = _typeTranslator.GetSwiftType(prop);
                var propertyName = _typeTranslator.GetSwiftPropertyName(prop);

                // NEW: Add warning comment for custom implementation needed
                if (prop.WarnCustomImplementationNeededAttribute != null)
                {
                    sb.AppendLine($"    // WARNING: {prop.WarnCustomImplementationNeededAttribute.Message ?? "Custom implementation needed"}");
                }

                // Initialize with default value, will be set in init
                sb.AppendLine($"    @Published var {propertyName}: {swiftType}");
            }
            sb.AppendLine();
        }

        // Generate @Published arrays for collections
        var collections = viewModel.Properties
            .Where(p => p.IsObservableCollection &&
                       !_typeTranslator.ShouldExcludePropertyForPlatform(p, "ios"))
            .ToList();

        if (collections.Any())
        {
            sb.AppendLine("    // ObservableCollection → @Published [T] with CollectionChanged + PropertyChanged");
            foreach (var collection in collections)
            {
                var elementType = _typeTranslator.GetSwiftType(collection.ElementType);
                var collectionName = _typeTranslator.GetSwiftPropertyName(collection);

                // NEW: Handle collection behavior attributes
                if (collection.CollectionBehaviorAttribute?.SupportsBatching == true)
                {
                    sb.AppendLine($"    // Collection supports batching (batch size: {collection.CollectionBehaviorAttribute.BatchSize})");
                }

                sb.AppendLine($"    @Published var {collectionName}: [{elementType}] = []");
            }
            sb.AppendLine();
        }

        // Generate commands
        // NEW: Filter commands based on platform availability
        var availableCommands = viewModel.Commands
            .Where(c => !_typeTranslator.ShouldExcludeCommandForPlatform(c, "ios"))
            .ToList();

        if (availableCommands.Any())
        {
            sb.AppendLine("    // Commands - let .NET handle all the business logic");
            foreach (var command in availableCommands)
            {
                GenerateIOSCommand(sb, command);
            }
        }

        // Constructor with initialization
        sb.AppendLine($"    init(dotnetViewModel: {viewModel.FullName}) {{");
        sb.AppendLine("        self.dotnetViewModel = dotnetViewModel");
        sb.AppendLine();

        // Initialize @Published properties with current .NET values - need to handle default values
        foreach (var prop in simpleProperties)
        {
            var swiftType = _typeTranslator.GetSwiftType(prop);
            var propertyName = _typeTranslator.GetSwiftPropertyName(prop);
            var defaultValue = GetSwiftDefaultValue(swiftType);
            sb.AppendLine($"        self.{propertyName} = {defaultValue}");
        }
        foreach (var collection in collections)
        {
            var collectionName = _typeTranslator.GetSwiftPropertyName(collection);
            sb.AppendLine($"        self.{collectionName} = []");
        }
        sb.AppendLine();

        sb.AppendLine("        setupBindings()");
        sb.AppendLine("        syncInitialValues()");
        sb.AppendLine("    }");
        sb.AppendLine();

        // Initial value sync method
        sb.AppendLine("    private func syncInitialValues() {");
        sb.AppendLine("        // Sync initial values from .NET ViewModel");
        foreach (var prop in simpleProperties)
        {
            var propertyName = _typeTranslator.GetSwiftPropertyName(prop);
            // NEW: Use attribute-aware conversion
            var conversion = _typeTranslator.GetSwiftDateTimeConversion(prop, $"dotnetViewModel.{prop.Name}");
            sb.AppendLine($"        self.{propertyName} = {conversion}");
        }
        foreach (var collection in collections)
        {
            var collectionName = _typeTranslator.GetSwiftPropertyName(collection);
            sb.AppendLine($"        self.{collectionName} = Array(dotnetViewModel.{collection.Name})");
        }
        sb.AppendLine("    }");
        sb.AppendLine();

        // Setup bindings method
        sb.AppendLine("    private func setupBindings() {");
        sb.AppendLine("        // Universal PropertyChanged listener - syncs ALL .NET changes");
        sb.AppendLine("        dotnetViewModel.PropertyChanged.subscribe { [weak self] args in");
        sb.AppendLine("            DispatchQueue.main.async {");
        sb.AppendLine("                guard let self = self else { return }");
        sb.AppendLine("                switch args.PropertyName {");

        foreach (var prop in simpleProperties)
        {
            var propertyName = _typeTranslator.GetSwiftPropertyName(prop);
            // NEW: Use attribute-aware conversion
            var conversion = _typeTranslator.GetSwiftDateTimeConversion(prop, $"self.dotnetViewModel.{prop.Name}");
            sb.AppendLine($"                case \"{prop.Name}\":");
            sb.AppendLine($"                    self.{propertyName} = {conversion}");
        }

        foreach (var collection in collections)
        {
            var collectionName = _typeTranslator.GetSwiftPropertyName(collection);
            sb.AppendLine($"                case \"{collection.Name}\":");
            sb.AppendLine($"                    self.{collectionName} = Array(self.dotnetViewModel.{collection.Name})");
        }

        sb.AppendLine("                default:");
        sb.AppendLine("                    break");
        sb.AppendLine("                }");
        sb.AppendLine("            }");
        sb.AppendLine("        }.store(in: &cancellables)");
        sb.AppendLine();

        // CollectionChanged listeners for ObservableCollections
        if (collections.Any())
        {
            sb.AppendLine("        // CollectionChanged listeners for progressive loading and granular updates");
            foreach (var collection in collections)
            {
                var collectionName = _typeTranslator.GetSwiftPropertyName(collection);
                sb.AppendLine($"        dotnetViewModel.{collection.Name}.CollectionChanged.subscribe {{ [weak self] args in");
                sb.AppendLine("            DispatchQueue.main.async {");
                sb.AppendLine($"                self?.{collectionName} = Array(self?.dotnetViewModel.{collection.Name} ?? [])");
                sb.AppendLine("            }");
                sb.AppendLine("        }.store(in: &cancellables)");
            }
            sb.AppendLine();
        }

        // Two-way binding for read-write properties
        var readWriteProperties = simpleProperties.Where(p => !p.IsReadOnly).ToList();
        if (readWriteProperties.Any())
        {
            sb.AppendLine("        // Two-way binding - sync Swift changes back to .NET (read-write properties only)");
            foreach (var prop in readWriteProperties)
            {
                var propertyName = _typeTranslator.GetSwiftPropertyName(prop);

                // NEW: Handle validation behavior
                if (prop.ValidationBehaviorAttribute?.ValidateOnSet == true)
                {
                    sb.AppendLine($"        // Property has validation: {prop.ValidationBehaviorAttribute.ValidatorMethod ?? "default validation"}");
                }

                sb.AppendLine($"        ${propertyName}");
                sb.AppendLine("            .dropFirst() // Skip initial value to avoid feedback loop");
                sb.AppendLine("            .sink { [weak self] newValue in");
                sb.AppendLine($"                self?.dotnetViewModel.{prop.Name} = newValue");
                sb.AppendLine("            }");
                sb.AppendLine("            .store(in: &cancellables)");
            }
        }

        sb.AppendLine("    }");
        sb.AppendLine();

        // Deinitializer
        sb.AppendLine("    deinit {");
        sb.AppendLine("        dotnetViewModel.dispose() // Always safe - all ViewModels implement IDisposable");
        sb.AppendLine("        cancellables.removeAll()");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        return sb.ToString();
    }

    // NEW: Generate command with attribute support
    private void GenerateIOSCommand(StringBuilder sb, CommandMetadata command)
    {
        var commandName = _typeTranslator.GetSwiftCommandName(command);

        // NEW: Add warning comment for custom implementation needed
        if (command.WarnCustomImplementationNeededAttribute != null)
        {
            sb.AppendLine($"    // WARNING: {command.WarnCustomImplementationNeededAttribute.Message ?? "Custom implementation needed"}");
        }

        // NEW: Handle threading behavior
        var requiresMainThread = command.ThreadingBehaviorAttribute?.RequiresMainThread == true ||
                                command.CommandBehaviorAttribute?.RequiresMainThread == true;

        if (command.IsAsync)
        {
            sb.AppendLine($"    func {commandName}({GetSwiftParameterSignature(command)}) async -> Result<Void, Error> {{");

            if (requiresMainThread)
            {
                sb.AppendLine("        return await MainActor.run {");
                sb.AppendLine("            do {");
                if (command.HasParameter)
                {
                    sb.AppendLine($"                try await dotnetViewModel.{command.Name}.executeAsync({GetSwiftParameterName(command)})");
                }
                else
                {
                    sb.AppendLine($"                try await dotnetViewModel.{command.Name}.executeAsync()");
                }
                sb.AppendLine("                return .success(())");
                sb.AppendLine("            } catch {");
                sb.AppendLine("                return .failure(error)");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            else
            {
                sb.AppendLine("        do {");
                if (command.HasParameter)
                {
                    sb.AppendLine($"            try await dotnetViewModel.{command.Name}.executeAsync({GetSwiftParameterName(command)})");
                }
                else
                {
                    sb.AppendLine($"            try await dotnetViewModel.{command.Name}.executeAsync()");
                }
                sb.AppendLine("            return .success(())");
                sb.AppendLine("        } catch {");
                sb.AppendLine("            return .failure(error)");
                sb.AppendLine("        }");
            }
            sb.AppendLine("    }");
        }
        else
        {
            if (requiresMainThread)
            {
                sb.AppendLine($"    func {commandName}({GetSwiftParameterSignature(command)}) {{");
                sb.AppendLine("        DispatchQueue.main.async { [weak self] in");

                if (command.HasParameter)
                {
                    sb.AppendLine($"            self?.dotnetViewModel.{command.Name}.execute({GetSwiftParameterName(command)})");
                }
                else
                {
                    sb.AppendLine($"            self?.dotnetViewModel.{command.Name}.execute()");
                }

                sb.AppendLine("        }");
                sb.AppendLine("    }");
            }
            else
            {
                sb.AppendLine($"    func {commandName}({GetSwiftParameterSignature(command)}) {{");
                if (command.HasParameter)
                {
                    sb.AppendLine($"        dotnetViewModel.{command.Name}.execute({GetSwiftParameterName(command)})");
                }
                else
                {
                    sb.AppendLine($"        dotnetViewModel.{command.Name}.execute()");
                }
                sb.AppendLine("    }");
            }
        }

        // NEW: Expose CanExecute if requested
        if (command.CommandBehaviorAttribute?.ExposeCanExecute == true)
        {
            var capitalizedName = commandName.Substring(0, 1).ToUpper() + commandName.Substring(1);
            sb.AppendLine($"    var can{capitalizedName}: Bool {{ return dotnetViewModel.{command.Name}.canExecute }}");
        }

        sb.AppendLine();
    }

    // NEW: Add conditional imports based on used attributes
    private void AddConditionalIOSImports(StringBuilder sb, ViewModelMetadata viewModel)
    {
        var needsLocation = viewModel.Properties.Any(p =>
            p.MapToAttribute?.iOS.ToString().Contains("CLLocation") == true);
        var needsAVFoundation = viewModel.Properties.Any(p =>
            p.MapToAttribute?.iOS.ToString().Contains("AVCapture") == true);
        var needsCoreGraphics = viewModel.Properties.Any(p =>
            p.MapToAttribute?.iOS.ToString().Contains("CG") == true);

        if (needsLocation)
        {
            sb.AppendLine("import CoreLocation");
        }

        if (needsAVFoundation)
        {
            sb.AppendLine("import AVFoundation");
        }

        if (needsCoreGraphics)
        {
            sb.AppendLine("import CoreGraphics");
        }
    }

    private string GetSwiftParameterSignature(CommandMetadata command)
    {
        if (!command.HasParameter || command.ParameterType == null)
            return "";

        var paramName = ToCamelCase(command.ParameterType.Name);
        var paramType = _typeTranslator.GetSwiftType(command.ParameterType);
        return $"{paramName}: {paramType}";
    }

    private string GetSwiftParameterName(CommandMetadata command)
    {
        if (!command.HasParameter || command.ParameterType == null)
            return "";

        return ToCamelCase(command.ParameterType.Name);
    }

    private string GetSwiftDefaultValue(string swiftType)
    {
        return swiftType switch
        {
            "String" => "\"\"",
            "Int32" => "0",
            "Int64" => "0",
            "Bool" => "false",
            "Double" => "0.0",
            "Float" => "0.0",
            "Date" => "Date()",
            "DateComponents" => "DateComponents()",
            "CLLocationCoordinate2D" => "CLLocationCoordinate2D()",
            "UUID" => "UUID()",
            _ => "nil" // For optional types or custom objects
        };
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToLower(input[0]) + input[1..];
    }
}