using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using x3squaredcircles.DesignToken.Generator.Services;
using x3squaredcircles.DesignToken.Generator.Models;

namespace x3squaredcircles.DesignToken.Generator
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            try
            {
                // Create host builder
                var host = Host.CreateDefaultBuilder(args)
                    .ConfigureServices((context, services) =>
                    {
                        // Register core services
                        services.AddSingleton<IConfigurationService, ConfigurationService>();
                        services.AddSingleton<ILicenseClientService, LicenseClientService>();
                        services.AddSingleton<IKeyVaultService, KeyVaultService>();
                        services.AddSingleton<IGitOperationsService, GitOperationsService>();

                        // Register design platform connectors
                        services.AddSingleton<IFigmaConnectorService, FigmaConnectorService>();
                        services.AddSingleton<ISketchConnectorService, SketchConnectorService>();
                        services.AddSingleton<IAdobeXdConnectorService, AdobeXdConnectorService>();
                        services.AddSingleton<IZeplinConnectorService, ZeplinConnectorService>();
                        services.AddSingleton<IAbstractConnectorService, AbstractConnectorService>();
                        services.AddSingleton<IPenpotConnectorService, PenpotConnectorService>();
                        services.AddSingleton<IDesignPlatformFactory, DesignPlatformFactory>();

                        // Register token processing services
                        services.AddSingleton<ITokenExtractionService, TokenExtractionService>();
                        services.AddSingleton<ITokenNormalizationService, TokenNormalizationService>();

                        // Register platform generators
                        services.AddSingleton<IAndroidGeneratorService, AndroidGeneratorService>();
                        services.AddSingleton<IIosGeneratorService, IosGeneratorService>();
                        services.AddSingleton<IWebGeneratorService, WebGeneratorService>();
                        services.AddSingleton<IPlatformGeneratorFactory, PlatformGeneratorFactory>();

                        // Register file management services
                        services.AddSingleton<ICustomSectionService, CustomSectionService>();
                        services.AddSingleton<IFileOutputService, FileOutputService>();
                        services.AddSingleton<ITagTemplateService, TagTemplateService>();

                        // Register main orchestrator
                        services.AddSingleton<IDesignTokenOrchestrator, DesignTokenOrchestrator>();

                        // Add HTTP client for external API communication
                        services.AddHttpClient();
                    })
                    .ConfigureLogging((context, logging) =>
                    {
                        logging.ClearProviders();
                        logging.AddConsole();

                        // Set log level based on VERBOSE environment variable
                        var verbose = Environment.GetEnvironmentVariable("VERBOSE");
                        var logLevel = Environment.GetEnvironmentVariable("LOG_LEVEL");

                        if (bool.TryParse(verbose, out var isVerbose) && isVerbose)
                        {
                            logging.SetMinimumLevel(LogLevel.Debug);
                        }
                        else if (Enum.TryParse<LogLevel>(logLevel, true, out var level))
                        {
                            logging.SetMinimumLevel(level);
                        }
                        else
                        {
                            logging.SetMinimumLevel(LogLevel.Information);
                        }
                    })
                    .Build();

                var logger = host.Services.GetRequiredService<ILogger<Program>>();
                logger.LogInformation("🎨 Design Token Generator v1.0.0 starting...");

                // Get orchestrator and run
                var orchestrator = host.Services.GetRequiredService<IDesignTokenOrchestrator>();
                var exitCode = await orchestrator.RunAsync();

                logger.LogInformation("Design Token Generator completed with exit code: {ExitCode}", exitCode);
                return exitCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1; // Invalid configuration
            }
        }
    }
}