using Microsoft.Extensions.Logging;
using RazorEngine;
using RazorEngine.Templating;
using System.Reflection;

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
            var context = new TemplateContext
            {
                ViewModels = viewModels,
                Platform = options.Platform.ToLower(),
                PackageName = "com.3xSquaredCircles.photography.NoOpViewModelAdapters",
                GeneratedAt = DateTime.UtcNow,
                TypeTranslator = _typeTranslator
            };

            if (options.Platform.ToLower() == "android")
            {
                await GenerateAndroidAdaptersAsync(context, options.OutputPath);
            }
            else if (options.Platform.ToLower() == "ios")
            {
                await GenerateIOSAdaptersAsync(context, options.OutputPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate adapters");
            throw;
        }
    }

    private async Task GenerateAndroidAdaptersAsync(TemplateContext context, string outputPath)
    {
        var template = LoadEmbeddedTemplate("AndroidAdapter.razor");

        foreach (var viewModel in context.ViewModels)
        {
            var fileName = $"{viewModel.AdapterName}.kt";
            var filePath = Path.Combine(outputPath, fileName);

            var content = Engine.Razor.RunCompile(template, viewModel.Name, typeof(ViewModelMetadata), viewModel);

            Directory.CreateDirectory(outputPath);
            await File.WriteAllTextAsync(filePath, content);

            _logger.LogInformation("Generated adapter: {FileName}", fileName);
        }
    }

    private async Task GenerateIOSAdaptersAsync(TemplateContext context, string outputPath)
    {
        // TODO: Implement iOS template generation
        _logger.LogWarning("iOS template generation not yet implemented");
    }

    private string LoadEmbeddedTemplate(string templateName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = $"Location.Photography.Tools.AdapterGenerator.Templates.{templateName}";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new InvalidOperationException($"Template not found: {templateName}");
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
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
    public string GetKotlinType(Type dotnetType)
    {
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