using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using x3squaredcircles.SQLSync.Generator.Services;
using x3squaredcircles.SQLSync.Generator.Models;

namespace x3squaredcircles.SQLSync.Generator
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

                        // Register language analysis services
                        services.AddSingleton<ICSharpAnalyzerService, CSharpAnalyzerService>();
                        services.AddSingleton<IJavaAnalyzerService, JavaAnalyzerService>();
                        services.AddSingleton<IPythonAnalyzerService, PythonAnalyzerService>();
                        services.AddSingleton<IJavaScriptAnalyzerService, JavaScriptAnalyzerService>();
                        services.AddSingleton<ITypeScriptAnalyzerService, TypeScriptAnalyzerService>();
                        services.AddSingleton<IGoAnalyzerService, GoAnalyzerService>();
                        services.AddSingleton<ILanguageAnalyzerFactory, LanguageAnalyzerFactory>();

                        // Register database provider services
                        services.AddSingleton<ISqlServerProviderService, SqlServerProviderService>();
                        services.AddSingleton<IPostgreSqlProviderService, PostgreSqlProviderService>();
                        services.AddSingleton<IMySqlProviderService, MySqlProviderService>();
                        services.AddSingleton<IOracleProviderService, OracleProviderService>();
                        services.AddSingleton<ISqliteProviderService, SqliteProviderService>();
                        services.AddSingleton<IDatabaseProviderFactory, DatabaseProviderFactory>();

                        // Register schema processing services
                        services.AddSingleton<IEntityDiscoveryService, EntityDiscoveryService>();
                        services.AddSingleton<ISchemaAnalysisService, SchemaAnalysisService>();
                        services.AddSingleton<ISchemaValidationService, SchemaValidationService>();
                        services.AddSingleton<IRiskAssessmentService, RiskAssessmentService>();

                        // Register deployment services
                        services.AddSingleton<IDeploymentPlanService, DeploymentPlanService>();
                        services.AddSingleton<ISqlGenerationService, SqlGenerationService>();
                        services.AddSingleton<IBackupService, BackupService>();
                        services.AddSingleton<IDeploymentExecutionService, DeploymentExecutionService>();

                        // Register file management services
                        services.AddSingleton<IFileOutputService, FileOutputService>();
                        services.AddSingleton<ITagTemplateService, TagTemplateService>();
                        services.AddSingleton<ICustomScriptService, CustomScriptService>();

                        // Register main orchestrator
                        services.AddSingleton<ISqlSchemaOrchestrator, SqlSchemaOrchestrator>();

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
                logger.LogInformation("🗄️ SQL Schema Generator v1.0.0 starting...");

                // Get orchestrator and run
                var orchestrator = host.Services.GetRequiredService<ISqlSchemaOrchestrator>();
                var exitCode = await orchestrator.RunAsync();

                logger.LogInformation("SQL Schema Generator completed with exit code: {ExitCode}", exitCode);
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