using Microsoft.Extensions.Logging;

namespace Location.Photography.Tools.AdapterGenerator.Services;

public class TemplateProcessor
{
    private readonly ILogger<TemplateProcessor> _logger;
    private readonly AndroidAdapterGenerator _androidGenerator;
    private readonly IOSAdapterGenerator _iosGenerator;

    public TemplateProcessor(
        ILogger<TemplateProcessor> logger,
        AndroidAdapterGenerator androidGenerator,
        IOSAdapterGenerator iosGenerator)
    {
        _logger = logger;
        _androidGenerator = androidGenerator;
        _iosGenerator = iosGenerator;
    }

    public async Task GenerateAdaptersAsync(List<ViewModelMetadata> viewModels, GeneratorOptions options)
    {
        try
        {
            if (options.Platform.ToLower() == "android")
            {
                await _androidGenerator.GenerateAdaptersAsync(viewModels, options.OutputPath);
            }
            else if (options.Platform.ToLower() == "ios")
            {
                await _iosGenerator.GenerateAdaptersAsync(viewModels, options.OutputPath);
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
}