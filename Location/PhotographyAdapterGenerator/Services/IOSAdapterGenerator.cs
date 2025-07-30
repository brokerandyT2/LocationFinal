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

        // Header comment
        sb.AppendLine("/**");
        sb.AppendLine(" * This is a truly stupid adapter that just bridges stuff.");
        sb.AppendLine(" * It should never be smart. Ever.");
        sb.AppendLine(" * Universal @Published pattern with true two-way binding!");
        sb.AppendLine(" *");
        sb.AppendLine($" * Generated from: {viewModel.FullName}");
        sb.AppendLine($" * Source: {viewModel.Source}");
        sb.AppendLine($" * Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine(" */");
        sb.AppendLine();

        sb.AppendLine("import Foundation");
        sb.AppendLine("import Combine");
        sb.AppendLine();

        sb.AppendLine($"class {viewModel.AdapterName}: ObservableObject {{");
        sb.AppendLine($"    private let dotnetViewModel: {viewModel.FullName}");
        sb.AppendLine("    private var cancellables = Set<AnyCancellable>()");
        sb.AppendLine();

        // Generate @Published properties for simple props
        var simpleProperties = viewModel.Properties.Where(p => !p.IsObservableCollection).ToList();
        if (simpleProperties.Any())
        {
            sb.AppendLine("    // Universal @Published pattern - ALL properties get reactive binding");
            foreach (var prop in simpleProperties)
            {
                var swiftType = _typeTranslator.GetSwiftType(prop.Type);
                // Initialize with default value, will be set in init
                sb.AppendLine($"    @Published var {prop.CamelCaseName}: {swiftType}");
            }
            sb.AppendLine();
        }

        // Generate @Published arrays for collections
        var collections = viewModel.Properties.Where(p => p.IsObservableCollection).ToList();
        if (collections.Any())
        {
            sb.AppendLine("    // ObservableCollection → @Published [T] with CollectionChanged + PropertyChanged");
            foreach (var collection in collections)
            {
                var elementType = _typeTranslator.GetSwiftType(collection.ElementType);
                sb.AppendLine($"    @Published var {collection.CamelCaseName}: [{elementType}] = []");
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
                    sb.AppendLine($"    func {command.MethodName}({GetSwiftParameterSignature(command)}) async -> Result<Void, Error> {{");
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
                    sb.AppendLine("    }");
                }
                else
                {
                    sb.AppendLine($"    func {command.MethodName}({GetSwiftParameterSignature(command)}) {{");
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
                sb.AppendLine();
            }
        }

        // Constructor with initialization
        sb.AppendLine($"    init(dotnetViewModel: {viewModel.FullName}) {{");
        sb.AppendLine("        self.dotnetViewModel = dotnetViewModel");
        sb.AppendLine();

        // Initialize @Published properties with current .NET values - need to handle default values
        foreach (var prop in simpleProperties)
        {
            var swiftType = _typeTranslator.GetSwiftType(prop.Type);
            var defaultValue = GetSwiftDefaultValue(swiftType);
            sb.AppendLine($"        self.{prop.CamelCaseName} = {defaultValue}");
        }
        foreach (var collection in collections)
        {
            sb.AppendLine($"        self.{collection.CamelCaseName} = []");
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
            sb.AppendLine($"        self.{prop.CamelCaseName} = dotnetViewModel.{prop.Name}");
        }
        foreach (var collection in collections)
        {
            sb.AppendLine($"        self.{collection.CamelCaseName} = Array(dotnetViewModel.{collection.Name})");
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
            sb.AppendLine($"                case \"{prop.Name}\":");
            sb.AppendLine($"                    self.{prop.CamelCaseName} = self.dotnetViewModel.{prop.Name}");
        }
        foreach (var collection in collections)
        {
            sb.AppendLine($"                case \"{collection.Name}\":");
            sb.AppendLine($"                    self.{collection.CamelCaseName} = Array(self.dotnetViewModel.{collection.Name})");
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
                sb.AppendLine($"        dotnetViewModel.{collection.Name}.CollectionChanged.subscribe {{ [weak self] args in");
                sb.AppendLine("            DispatchQueue.main.async {");
                sb.AppendLine($"                self?.{collection.CamelCaseName} = Array(self?.dotnetViewModel.{collection.Name} ?? [])");
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
                sb.AppendLine($"        ${prop.CamelCaseName}");
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
            _ => "nil" // For optional types or custom objects
        };
    }

    private string ToCamelCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToLower(input[0]) + input[1..];
    }
}