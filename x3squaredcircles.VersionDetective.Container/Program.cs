using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using x3squaredcircles.VersionDetective.Container.Services;
using x3squaredcircles.VersionDetective.Container.Models;

namespace x3squaredcircles.VersionDetective.Container
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
                        // Register services
                        services.AddSingleton<IConfigurationService, ConfigurationService>();
                        services.AddSingleton<ILicenseClientService, LicenseClientService>();
                        services.AddSingleton<IGitAnalysisService, GitAnalysisService>();
                        services.AddSingleton<ILanguageAnalysisService, LanguageAnalysisService>();
                        services.AddSingleton<IVersionCalculationService, VersionCalculationService>();
                        services.AddSingleton<ITagTemplateService, TagTemplateService>();
                        services.AddSingleton<IOutputService, OutputService>();
                        services.AddSingleton<IKeyVaultService, KeyVaultService>();
                        services.AddSingleton<IVersionDetectiveOrchestrator, VersionDetectiveOrchestrator>();

                        // Add HTTP client for license server communication
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
                logger.LogInformation("🔍 Version Detective Container v1.0.0 starting...");

                // Get orchestrator and run
                var orchestrator = host.Services.GetRequiredService<IVersionDetectiveOrchestrator>();
                var exitCode = await orchestrator.RunAsync();

                logger.LogInformation("Version Detective Container completed with exit code: {ExitCode}", exitCode);
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