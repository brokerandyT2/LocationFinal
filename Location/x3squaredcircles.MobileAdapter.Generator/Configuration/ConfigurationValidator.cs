using System.Collections.Generic;
using System.Linq;
using x3squaredcircles.MobileAdapter.Generator.Logging;

namespace x3squaredcircles.MobileAdapter.Generator.Configuration
{
    public class ConfigurationValidator
    {
        private readonly ILogger _logger;

        public ConfigurationValidator(ILogger logger)
        {
            _logger = logger;
        }

        public ValidationResult Validate(GeneratorConfiguration config)
        {
            var errors = new List<string>();

            // Validate language selection
            ValidateLanguageSelection(config, errors);

            // Validate platform selection
            ValidatePlatformSelection(config, errors);

            // Validate core configuration
            ValidateCoreConfiguration(config, errors);

            // Validate licensing
            ValidateLicensingConfiguration(config, errors);

            // Validate discovery methods
            ValidateDiscoveryConfiguration(config, errors);

            // Validate language-specific configurations
            ValidateLanguageSpecificConfiguration(config, errors);

            // Validate output configuration
            ValidateOutputConfiguration(config, errors);

            // Validate vault configuration if specified
            ValidateVaultConfiguration(config, errors);

            return new ValidationResult
            {
                IsValid = !errors.Any(),
                Errors = errors
            };
        }

        private void ValidateLanguageSelection(GeneratorConfiguration config, List<string> errors)
        {
            var languageFlags = new[]
            {
                config.LanguageCSharp,
                config.LanguageJava,
                config.LanguageKotlin,
                config.LanguageJavaScript,
                config.LanguageTypeScript,
                config.LanguagePython
            };

            var selectedCount = languageFlags.Count(flag => flag);

            if (selectedCount == 0)
            {
                errors.Add("No language specified. Set exactly one: LANGUAGE_CSHARP, LANGUAGE_JAVA, LANGUAGE_KOTLIN, LANGUAGE_JAVASCRIPT, LANGUAGE_TYPESCRIPT, or LANGUAGE_PYTHON");
            }
            else if (selectedCount > 1)
            {
                errors.Add("Multiple languages specified. Only one language flag may be set per execution.");
            }
        }

        private void ValidatePlatformSelection(GeneratorConfiguration config, List<string> errors)
        {
            var platformFlags = new[] { config.PlatformAndroid, config.PlatformIOS };
            var selectedCount = platformFlags.Count(flag => flag);

            if (selectedCount == 0)
            {
                errors.Add("No platform specified. Set exactly one: PLATFORM_ANDROID or PLATFORM_IOS");
            }
            else if (selectedCount > 1)
            {
                errors.Add("Multiple platforms specified. Only one platform flag may be set per execution.");
            }
        }

        private void ValidateCoreConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.RepoUrl))
            {
                errors.Add("REPO_URL is required");
            }

            if (string.IsNullOrWhiteSpace(config.Branch))
            {
                errors.Add("BRANCH is required");
            }
        }

        private void ValidateLicensingConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.LicenseServer))
            {
                errors.Add("LICENSE_SERVER is required");
            }

            if (config.LicenseTimeout <= 0)
            {
                errors.Add("LICENSE_TIMEOUT must be greater than 0");
            }

            if (config.LicenseRetryInterval <= 0)
            {
                errors.Add("LICENSE_RETRY_INTERVAL must be greater than 0");
            }
        }

        private void ValidateDiscoveryConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            var discoveryMethods = new[]
            {
                !string.IsNullOrWhiteSpace(config.TrackAttribute),
                !string.IsNullOrWhiteSpace(config.TrackPattern),
                !string.IsNullOrWhiteSpace(config.TrackNamespace),
                !string.IsNullOrWhiteSpace(config.TrackFilePattern)
            };

            var selectedCount = discoveryMethods.Count(method => method);

            if (selectedCount == 0)
            {
                errors.Add("No discovery method specified. Set one of: TRACK_ATTRIBUTE, TRACK_PATTERN, TRACK_NAMESPACE, or TRACK_FILE_PATTERN");
            }
            else if (selectedCount > 1)
            {
                errors.Add("Multiple discovery methods specified. Choose exactly one discovery method per execution.");
            }
        }

        private void ValidateLanguageSpecificConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            var selectedLanguage = config.GetSelectedLanguage();

            switch (selectedLanguage)
            {
                case SourceLanguage.CSharp:
                    ValidateCSharpConfiguration(config, errors);
                    break;
                case SourceLanguage.Java:
                case SourceLanguage.Kotlin:
                    ValidateJavaKotlinConfiguration(config, errors);
                    break;
                case SourceLanguage.JavaScript:
                case SourceLanguage.TypeScript:
                    ValidateJavaScriptTypeScriptConfiguration(config, errors);
                    break;
                case SourceLanguage.Python:
                    ValidatePythonConfiguration(config, errors);
                    break;
            }
        }

        private void ValidateCSharpConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Assembly.CoreAssemblyPath) &&
                string.IsNullOrWhiteSpace(config.Assembly.TargetAssemblyPath))
            {
                errors.Add("For C# analysis, either CORE_ASSEMBLY_PATH or TARGET_ASSEMBLY_PATH must be specified");
            }
        }

        private void ValidateJavaKotlinConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Source.SourcePaths))
            {
                errors.Add("For Java/Kotlin analysis, SOURCE_PATHS must be specified");
            }
        }

        private void ValidateJavaScriptTypeScriptConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Source.SourcePaths))
            {
                errors.Add("For JavaScript/TypeScript analysis, SOURCE_PATHS must be specified");
            }
        }

        private void ValidatePythonConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Source.PythonPaths))
            {
                errors.Add("For Python analysis, PYTHON_PATHS must be specified");
            }
        }

        private void ValidateOutputConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Output.OutputDir))
            {
                errors.Add("OUTPUT_DIR cannot be empty");
            }

            var selectedPlatform = config.GetSelectedPlatform();

            if (selectedPlatform == TargetPlatform.Android)
            {
                if (string.IsNullOrWhiteSpace(config.Output.AndroidOutputDir))
                {
                    errors.Add("ANDROID_OUTPUT_DIR cannot be empty when targeting Android");
                }
            }
            else if (selectedPlatform == TargetPlatform.iOS)
            {
                if (string.IsNullOrWhiteSpace(config.Output.IosOutputDir))
                {
                    errors.Add("IOS_OUTPUT_DIR cannot be empty when targeting iOS");
                }
            }
        }

        private void ValidateVaultConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (config.Vault.Type == VaultType.None)
                return;

            if (string.IsNullOrWhiteSpace(config.Vault.Url))
            {
                errors.Add("VAULT_URL is required when vault type is specified");
            }

            switch (config.Vault.Type)
            {
                case VaultType.Azure:
                    ValidateAzureVaultConfiguration(config, errors);
                    break;
                case VaultType.Aws:
                    ValidateAwsVaultConfiguration(config, errors);
                    break;
                case VaultType.HashiCorp:
                    ValidateHashiCorpVaultConfiguration(config, errors);
                    break;
            }
        }

        private void ValidateAzureVaultConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Vault.AzureClientId))
            {
                errors.Add("AZURE_CLIENT_ID is required for Azure Key Vault");
            }

            if (string.IsNullOrWhiteSpace(config.Vault.AzureClientSecret))
            {
                errors.Add("AZURE_CLIENT_SECRET is required for Azure Key Vault");
            }

            if (string.IsNullOrWhiteSpace(config.Vault.AzureTenantId))
            {
                errors.Add("AZURE_TENANT_ID is required for Azure Key Vault");
            }
        }

        private void ValidateAwsVaultConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Vault.AwsRegion))
            {
                errors.Add("AWS_REGION is required for AWS Secrets Manager");
            }

            if (string.IsNullOrWhiteSpace(config.Vault.AwsAccessKeyId))
            {
                errors.Add("AWS_ACCESS_KEY_ID is required for AWS Secrets Manager");
            }

            if (string.IsNullOrWhiteSpace(config.Vault.AwsSecretAccessKey))
            {
                errors.Add("AWS_SECRET_ACCESS_KEY is required for AWS Secrets Manager");
            }
        }

        private void ValidateHashiCorpVaultConfiguration(GeneratorConfiguration config, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(config.Vault.HashiCorpToken))
            {
                errors.Add("VAULT_TOKEN is required for HashiCorp Vault");
            }
        }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
    }
}