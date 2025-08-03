using Microsoft.Extensions.Logging;

namespace x3squaredcirecles.Adapter.Generator.Services;

public class TemplateProcessor
{
    private readonly ILogger<TemplateProcessor> _logger;
    private readonly AndroidAdapterGenerator _androidGenerator;
    private readonly IOSAdapterGenerator _iosGenerator;
    private readonly ViewModelAnalyzer _viewModelAnalyzer;

    public TemplateProcessor(
        ILogger<TemplateProcessor> logger,
        AndroidAdapterGenerator androidGenerator,
        IOSAdapterGenerator iosGenerator,
        ViewModelAnalyzer viewModelAnalyzer)
    {
        _logger = logger;
        _androidGenerator = androidGenerator;
        _iosGenerator = iosGenerator;
        _viewModelAnalyzer = viewModelAnalyzer;
    }

    public async Task GenerateAdaptersAsync(List<ViewModelMetadata> viewModels, GeneratorOptions options)
    {
        try
        {
            // NEW: Filter ViewModels based on platform availability before generation
            var filteredViewModels = FilterViewModelsForPlatform(viewModels, options.Platform);

            if (filteredViewModels.Count < viewModels.Count)
            {
                var excludedCount = viewModels.Count - filteredViewModels.Count;
                _logger.LogInformation("Excluded {ExcludedCount} ViewModels from {Platform} generation due to platform availability constraints",
                    excludedCount, options.Platform);
            }

            if (options.Platform.ToLower() == "android")
            {
                await _androidGenerator.GenerateAdaptersAsync(filteredViewModels, options.OutputPath);
            }
            else if (options.Platform.ToLower() == "ios")
            {
                await _iosGenerator.GenerateAdaptersAsync(filteredViewModels, options.OutputPath);
            }
            else
            {
                _logger.LogError("Unsupported platform: {Platform}", options.Platform);
                throw new ArgumentException($"Unsupported platform: {options.Platform}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate {Platform} adapters", options.Platform);
            throw;
        }
    }

    // NEW: Filter ViewModels based on platform availability attributes
    private List<ViewModelMetadata> FilterViewModelsForPlatform(List<ViewModelMetadata> viewModels, string platform)
    {
        var filteredViewModels = new List<ViewModelMetadata>();

        foreach (var viewModel in viewModels)
        {
            // Check if ViewModel should be excluded for this platform
            if (ShouldExcludeViewModelForPlatform(viewModel, platform))
            {
                _logger.LogDebug("Excluding ViewModel {Name} from {Platform} generation due to platform availability",
                    viewModel.Name, platform);
                continue;
            }

            // Create a filtered copy of the ViewModel with platform-specific filtering
            var filteredViewModel = CreatePlatformFilteredViewModel(viewModel, platform);
            filteredViewModels.Add(filteredViewModel);
        }

        return filteredViewModels;
    }

    // NEW: Check if entire ViewModel should be excluded for platform
    private bool ShouldExcludeViewModelForPlatform(ViewModelMetadata viewModel, string platform)
    {
        if (viewModel.AvailableAttribute != null)
        {
            return platform.ToLower() switch
            {
                "android" => !viewModel.AvailableAttribute.Android,
                "ios" => !viewModel.AvailableAttribute.iOS,
                _ => false
            };
        }

        return false;
    }

    // NEW: Create a filtered copy of ViewModel for specific platform
    private ViewModelMetadata CreatePlatformFilteredViewModel(ViewModelMetadata originalViewModel, string platform)
    {
        // Create a copy of the ViewModel
        var filteredViewModel = new ViewModelMetadata
        {
            Name = originalViewModel.Name,
            AdapterName = originalViewModel.AdapterName,
            FullName = originalViewModel.FullName,
            Namespace = originalViewModel.Namespace,
            Source = originalViewModel.Source,
            IsDisposable = originalViewModel.IsDisposable,
            BaseClass = originalViewModel.BaseClass,
            Constructor = originalViewModel.Constructor,
            Events = originalViewModel.Events, // Events are typically platform-agnostic

            // Copy class-level attributes
            AvailableAttribute = originalViewModel.AvailableAttribute,
            ExcludeAttribute = originalViewModel.ExcludeAttribute,
            GenerateAsAttribute = originalViewModel.GenerateAsAttribute,

            // Filter properties for platform
            Properties = FilterPropertiesForPlatform(originalViewModel.Properties, platform),

            // Filter commands for platform
            Commands = FilterCommandsForPlatform(originalViewModel.Commands, platform),

            // Filter methods for platform
            Methods = FilterMethodsForPlatform(originalViewModel.Methods, platform)
        };

        // Log filtering results
        var originalPropertyCount = originalViewModel.Properties.Count;
        var filteredPropertyCount = filteredViewModel.Properties.Count;
        var originalCommandCount = originalViewModel.Commands.Count;
        var filteredCommandCount = filteredViewModel.Commands.Count;

        if (originalPropertyCount != filteredPropertyCount || originalCommandCount != filteredCommandCount)
        {
            _logger.LogDebug("Filtered ViewModel {Name} for {Platform}: Properties {OriginalProps}→{FilteredProps}, Commands {OriginalCmds}→{FilteredCmds}",
                originalViewModel.Name, platform,
                originalPropertyCount, filteredPropertyCount,
                originalCommandCount, filteredCommandCount);
        }

        return filteredViewModel;
    }

    // NEW: Filter properties based on platform availability
    private List<PropertyMetadata> FilterPropertiesForPlatform(List<PropertyMetadata> properties, string platform)
    {
        return properties.Where(prop => !ShouldExcludePropertyForPlatform(prop, platform)).ToList();
    }

    // NEW: Filter commands based on platform availability
    private List<CommandMetadata> FilterCommandsForPlatform(List<CommandMetadata> commands, string platform)
    {
        return commands.Where(cmd => !ShouldExcludeCommandForPlatform(cmd, platform)).ToList();
    }

    // NEW: Filter methods based on platform availability
    private List<MethodMetadata> FilterMethodsForPlatform(List<MethodMetadata> methods, string platform)
    {
        return methods.Where(method => !ShouldExcludeMethodForPlatform(method, platform)).ToList();
    }

    // NEW: Check if property should be excluded for platform
    private bool ShouldExcludePropertyForPlatform(PropertyMetadata property, string platform)
    {
        if (property.AvailableAttribute != null)
        {
            return platform.ToLower() switch
            {
                "android" => !property.AvailableAttribute.Android,
                "ios" => !property.AvailableAttribute.iOS,
                _ => false
            };
        }

        return false;
    }

    // NEW: Check if command should be excluded for platform
    private bool ShouldExcludeCommandForPlatform(CommandMetadata command, string platform)
    {
        if (command.AvailableAttribute != null)
        {
            return platform.ToLower() switch
            {
                "android" => !command.AvailableAttribute.Android,
                "ios" => !command.AvailableAttribute.iOS,
                _ => false
            };
        }

        return false;
    }

    // NEW: Check if method should be excluded for platform
    private bool ShouldExcludeMethodForPlatform(MethodMetadata method, string platform)
    {
        if (method.AvailableAttribute != null)
        {
            return platform.ToLower() switch
            {
                "android" => !method.AvailableAttribute.Android,
                "ios" => !method.AvailableAttribute.iOS,
                _ => false
            };
        }

        return false;
    }
}