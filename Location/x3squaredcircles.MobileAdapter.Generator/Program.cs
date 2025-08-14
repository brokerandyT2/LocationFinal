using System;
using System.Threading.Tasks;
using x3squaredcircles.MobileAdapter.Generator.Configuration;
using x3squaredcircles.MobileAdapter.Generator.Core;
using x3squaredcircles.MobileAdapter.Generator.Licensing;
using x3squaredcircles.MobileAdapter.Generator.Logging;

namespace x3squaredcircles.MobileAdapter.Generator
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var logger = new FileLogger();
            var configurationValidator = new ConfigurationValidator(logger);
            var licenseManager = new LicenseManager(logger);
            var adapterGenerator = new AdapterGeneratorEngine(logger);

            try
            {
                logger.LogInfo("Mobile Adapter Generator starting...");

                // Load and validate configuration
                var config = EnvironmentConfigurationLoader.LoadConfiguration();
                var validationResult = configurationValidator.Validate(config);

                if (!validationResult.IsValid)
                {
                    foreach (var error in validationResult.Errors)
                    {
                        logger.LogError(error);
                    }
                    return 1;
                }

                // Check license
                var licenseResult = await licenseManager.ValidateLicenseAsync(config);
                if (!licenseResult.IsValid)
                {
                    logger.LogError($"License validation failed: {licenseResult.ErrorMessage}");
                    return licenseResult.IsExpired ? 2 : 2;
                }

                if (licenseResult.IsNoOpMode)
                {
                    logger.LogWarning("Running in NOOP mode - license expired. Analysis only, no file generation.");
                    config.Mode = OperationMode.Analyze;
                }

                // Execute generation
                var result = await adapterGenerator.GenerateAdaptersAsync(config);

                if (!result.Success)
                {
                    logger.LogError($"Generation failed: {result.ErrorMessage}");
                    return result.ExitCode;
                }

                logger.LogInfo($"Generation completed successfully. Generated {result.GeneratedFiles.Count} files.");
                return 0;
            }
            catch (Exception ex)
            {
                logger.LogError($"Unhandled exception: {ex}");
                return 1;
            }
        }
    }
}