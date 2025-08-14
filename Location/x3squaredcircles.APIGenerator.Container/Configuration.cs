using System;
using System.Collections.Generic;
using System.Linq;

namespace x3squaredcircles.APIGenerator.Container
{
    /// <summary>
    /// Configuration class that parses and validates all environment variables for the API Generator
    /// </summary>
    public class Configuration
    {
        // Language Selection (Mutually Exclusive)
        public bool LanguageCSharp { get; set; }
        public bool LanguageJava { get; set; }
        public bool LanguagePython { get; set; }
        public bool LanguageJavaScript { get; set; }
        public bool LanguageTypeScript { get; set; }
        public bool LanguageGo { get; set; }

        // Cloud Provider Selection (Mutually Exclusive)
        public bool CloudAzure { get; set; }
        public bool CloudAws { get; set; }
        public bool CloudGcp { get; set; }
        public bool CloudOracle { get; set; }

        // Core Configuration (Required)
        public string TrackAttribute { get; set; } = string.Empty;
        public string RepoUrl { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;

        // Authentication
        public string PatToken { get; set; } = string.Empty;
        public string PatSecretName { get; set; } = string.Empty;

        // Licensing
        public string LicenseServer { get; set; } = string.Empty;
        public string ToolName { get; set; } = "api-generator";
        public int LicenseTimeout { get; set; } = 300; // 5 minutes default
        public int LicenseRetryInterval { get; set; } = 30; // 30 seconds default

        // Key Vault Integration
        public string VaultType { get; set; } = string.Empty;
        public string VaultUrl { get; set; } = string.Empty;

        // Azure Key Vault
        public string AzureClientId { get; set; } = string.Empty;
        public string AzureClientSecret { get; set; } = string.Empty;
        public string AzureTenantId { get; set; } = string.Empty;

        // AWS Secrets Manager
        public string AwsRegion { get; set; } = string.Empty;
        public string AwsAccessKeyId { get; set; } = string.Empty;
        public string AwsSecretAccessKey { get; set; } = string.Empty;

        // HashiCorp Vault
        public string VaultToken { get; set; } = string.Empty;

        // Template Configuration
        public string TemplateRepo { get; set; } = string.Empty;
        public string TemplateBranch { get; set; } = "main";
        public string TemplatePath { get; set; } = string.Empty;
        public string TemplatePat { get; set; } = string.Empty;
        public string TemplatePATVaultKey { get; set; } = string.Empty;
        public int TemplateCacheTtl { get; set; } = 300;
        public bool TemplateValidateStructure { get; set; } = true;

        // Tag Template Configuration
        public string TagTemplate { get; set; } = "{branch}/{repo}/api/{version}";

        // Cloud-Specific Configuration - Azure
        public string AzureSubscription { get; set; } = string.Empty;
        public string AzureResourceGroup { get; set; } = string.Empty;
        public string AzureRegion { get; set; } = string.Empty;

        // Cloud-Specific Configuration - AWS
        public string AwsAccountId { get; set; } = string.Empty;
        public string AwsApiGatewayType { get; set; } = string.Empty;

        // Cloud-Specific Configuration - GCP
        public string GcpProjectId { get; set; } = string.Empty;
        public string GcpRegion { get; set; } = string.Empty;

        // Cloud-Specific Configuration - Oracle
        public string OciCompartmentId { get; set; } = string.Empty;
        public string OciRegion { get; set; } = string.Empty;

        // Generation Configuration
        public string Mode { get; set; } = "deploy";
        public bool ValidateOnly { get; set; }
        public bool NoOp { get; set; }
        public bool SkipBuild { get; set; }
        public bool SkipDeployment { get; set; }

        // Entity Discovery
        public string EntityPaths { get; set; } = string.Empty;
        public string BuildOutputPath { get; set; } = string.Empty;
        public bool IgnoreExportAttribute { get; set; }

        // Logging and Output
        public bool Verbose { get; set; }
        public string LogLevel { get; set; } = "INFO";

        // Derived Properties
        public string SelectedLanguage => GetSelectedLanguage();
        public string SelectedCloud => GetSelectedCloud();

        /// <summary>
        /// Parse configuration from environment variables
        /// </summary>
        public static Configuration Parse()
        {
            var config = new Configuration();

            // Language Selection
            config.LanguageCSharp = ParseBool("LANGUAGE_CSHARP");
            config.LanguageJava = ParseBool("LANGUAGE_JAVA");
            config.LanguagePython = ParseBool("LANGUAGE_PYTHON");
            config.LanguageJavaScript = ParseBool("LANGUAGE_JAVASCRIPT");
            config.LanguageTypeScript = ParseBool("LANGUAGE_TYPESCRIPT");
            config.LanguageGo = ParseBool("LANGUAGE_GO");

            // Cloud Provider Selection
            config.CloudAzure = ParseBool("CLOUD_AZURE");
            config.CloudAws = ParseBool("CLOUD_AWS");
            config.CloudGcp = ParseBool("CLOUD_GCP");
            config.CloudOracle = ParseBool("CLOUD_ORACLE");

            // Core Configuration
            config.TrackAttribute = ParseString("TRACK_ATTRIBUTE");
            config.RepoUrl = ParseString("REPO_URL");
            config.Branch = ParseString("BRANCH");

            // Authentication
            config.PatToken = ParseString("PAT_TOKEN");
            config.PatSecretName = ParseString("PAT_SECRET_NAME");

            // Licensing
            config.LicenseServer = ParseString("LICENSE_SERVER");
            config.ToolName = ParseString("TOOL_NAME", "api-generator");
            config.LicenseTimeout = ParseInt("LICENSE_TIMEOUT", 300);
            config.LicenseRetryInterval = ParseInt("LICENSE_RETRY_INTERVAL", 30);

            // Key Vault Integration
            config.VaultType = ParseString("VAULT_TYPE");
            config.VaultUrl = ParseString("VAULT_URL");
            config.AzureClientId = ParseString("AZURE_CLIENT_ID");
            config.AzureClientSecret = ParseString("AZURE_CLIENT_SECRET");
            config.AzureTenantId = ParseString("AZURE_TENANT_ID");
            config.AwsRegion = ParseString("AWS_REGION");
            config.AwsAccessKeyId = ParseString("AWS_ACCESS_KEY_ID");
            config.AwsSecretAccessKey = ParseString("AWS_SECRET_ACCESS_KEY");
            config.VaultToken = ParseString("VAULT_TOKEN");

            // Template Configuration
            config.TemplateRepo = ParseString("TEMPLATE_REPO");
            config.TemplateBranch = ParseString("TEMPLATE_BRANCH", "main");
            config.TemplatePath = ParseString("TEMPLATE_PATH");
            config.TemplatePat = ParseString("TEMPLATE_PAT");
            config.TemplatePATVaultKey = ParseString("TEMPLATE_PAT_VAULT_KEY");
            config.TemplateCacheTtl = ParseInt("TEMPLATE_CACHE_TTL", 300);
            config.TemplateValidateStructure = ParseBool("TEMPLATE_VALIDATE_STRUCTURE", true);

            // Tag Template Configuration
            config.TagTemplate = ParseString("TAG_TEMPLATE", "{branch}/{repo}/api/{version}");

            // Cloud-Specific Configuration - Azure
            config.AzureSubscription = ParseString("AZURE_SUBSCRIPTION");
            config.AzureResourceGroup = ParseString("AZURE_RESOURCE_GROUP");
            config.AzureRegion = ParseString("AZURE_REGION");

            // Cloud-Specific Configuration - AWS
            config.AwsAccountId = ParseString("AWS_ACCOUNT_ID");
            config.AwsApiGatewayType = ParseString("AWS_API_GATEWAY_TYPE");

            // Cloud-Specific Configuration - GCP
            config.GcpProjectId = ParseString("GCP_PROJECT_ID");
            config.GcpRegion = ParseString("GCP_REGION");

            // Cloud-Specific Configuration - Oracle
            config.OciCompartmentId = ParseString("OCI_COMPARTMENT_ID");
            config.OciRegion = ParseString("OCI_REGION");

            // Generation Configuration
            config.Mode = ParseString("MODE", "deploy");
            config.ValidateOnly = ParseBool("VALIDATE_ONLY");
            config.NoOp = ParseBool("NO_OP");
            config.SkipBuild = ParseBool("SKIP_BUILD");
            config.SkipDeployment = ParseBool("SKIP_DEPLOYMENT");

            // Entity Discovery
            config.EntityPaths = ParseString("ENTITY_PATHS");
            config.BuildOutputPath = ParseString("BUILD_OUTPUT_PATH");
            config.IgnoreExportAttribute = ParseBool("IGNORE_EXPORT_ATTRIBUTE");

            // Logging and Output
            config.Verbose = ParseBool("VERBOSE");
            config.LogLevel = ParseString("LOG_LEVEL", "INFO");

            return config;
        }

        /// <summary>
        /// Validate the configuration and throw exceptions for invalid states
        /// </summary>
        public void Validate()
        {
            var errors = new List<string>();

            // Validate language selection (mutually exclusive)
            var languageCount = new[] { LanguageCSharp, LanguageJava, LanguagePython, LanguageJavaScript, LanguageTypeScript, LanguageGo }.Count(x => x);
            if (languageCount == 0)
            {
                errors.Add("No language specified. Set exactly one: LANGUAGE_CSHARP, LANGUAGE_JAVA, LANGUAGE_PYTHON, LANGUAGE_JAVASCRIPT, LANGUAGE_TYPESCRIPT, LANGUAGE_GO");
            }
            else if (languageCount > 1)
            {
                errors.Add("Multiple languages specified. Set exactly one language flag to true");
            }

            // Validate cloud provider selection (mutually exclusive)
            var cloudCount = new[] { CloudAzure, CloudAws, CloudGcp, CloudOracle }.Count(x => x);
            if (cloudCount == 0)
            {
                errors.Add("No cloud provider specified. Set exactly one: CLOUD_AZURE, CLOUD_AWS, CLOUD_GCP, CLOUD_ORACLE");
            }
            else if (cloudCount > 1)
            {
                errors.Add("Multiple cloud providers specified. Set exactly one cloud provider flag to true");
            }

            // Validate required core configuration
            if (string.IsNullOrWhiteSpace(TrackAttribute))
            {
                errors.Add("TRACK_ATTRIBUTE is required");
            }

            if (string.IsNullOrWhiteSpace(RepoUrl))
            {
                errors.Add("REPO_URL is required");
            }

            if (string.IsNullOrWhiteSpace(Branch))
            {
                errors.Add("BRANCH is required");
            }

            if (string.IsNullOrWhiteSpace(LicenseServer))
            {
                errors.Add("LICENSE_SERVER is required");
            }

            if (string.IsNullOrWhiteSpace(TemplateRepo))
            {
                errors.Add("TEMPLATE_REPO is required");
            }

            // Validate cloud-specific configuration
            ValidateCloudSpecificConfig(errors);

            // Validate key vault configuration if vault type is specified
            ValidateKeyVaultConfig(errors);

            if (errors.Any())
            {
                throw new ArgumentException($"Configuration validation failed:\n{string.Join("\n", errors)}");
            }
        }

        private void ValidateCloudSpecificConfig(List<string> errors)
        {
            if (CloudAzure)
            {
                if (string.IsNullOrWhiteSpace(AzureSubscription))
                    errors.Add("AZURE_SUBSCRIPTION is required when CLOUD_AZURE is true");
                if (string.IsNullOrWhiteSpace(AzureResourceGroup))
                    errors.Add("AZURE_RESOURCE_GROUP is required when CLOUD_AZURE is true");
                if (string.IsNullOrWhiteSpace(AzureRegion))
                    errors.Add("AZURE_REGION is required when CLOUD_AZURE is true");
            }

            if (CloudAws)
            {
                if (string.IsNullOrWhiteSpace(AwsRegion))
                    errors.Add("AWS_REGION is required when CLOUD_AWS is true");
            }

            if (CloudGcp)
            {
                if (string.IsNullOrWhiteSpace(GcpProjectId))
                    errors.Add("GCP_PROJECT_ID is required when CLOUD_GCP is true");
                if (string.IsNullOrWhiteSpace(GcpRegion))
                    errors.Add("GCP_REGION is required when CLOUD_GCP is true");
            }

            if (CloudOracle)
            {
                if (string.IsNullOrWhiteSpace(OciCompartmentId))
                    errors.Add("OCI_COMPARTMENT_ID is required when CLOUD_ORACLE is true");
                if (string.IsNullOrWhiteSpace(OciRegion))
                    errors.Add("OCI_REGION is required when CLOUD_ORACLE is true");
            }
        }

        private void ValidateKeyVaultConfig(List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(VaultType))
                return;

            if (string.IsNullOrWhiteSpace(VaultUrl))
            {
                errors.Add("VAULT_URL is required when VAULT_TYPE is specified");
            }

            switch (VaultType.ToLowerInvariant())
            {
                case "azure":
                    if (string.IsNullOrWhiteSpace(AzureClientId))
                        errors.Add("AZURE_CLIENT_ID is required for Azure Key Vault");
                    if (string.IsNullOrWhiteSpace(AzureClientSecret))
                        errors.Add("AZURE_CLIENT_SECRET is required for Azure Key Vault");
                    if (string.IsNullOrWhiteSpace(AzureTenantId))
                        errors.Add("AZURE_TENANT_ID is required for Azure Key Vault");
                    break;

                case "aws":
                    if (string.IsNullOrWhiteSpace(AwsRegion))
                        errors.Add("AWS_REGION is required for AWS Secrets Manager");
                    if (string.IsNullOrWhiteSpace(AwsAccessKeyId))
                        errors.Add("AWS_ACCESS_KEY_ID is required for AWS Secrets Manager");
                    if (string.IsNullOrWhiteSpace(AwsSecretAccessKey))
                        errors.Add("AWS_SECRET_ACCESS_KEY is required for AWS Secrets Manager");
                    break;

                case "hashicorp":
                    if (string.IsNullOrWhiteSpace(VaultToken))
                        errors.Add("VAULT_TOKEN is required for HashiCorp Vault");
                    break;

                default:
                    errors.Add($"Invalid VAULT_TYPE: {VaultType}. Supported values: azure, aws, hashicorp");
                    break;
            }
        }

        private string GetSelectedLanguage()
        {
            if (LanguageCSharp) return "csharp";
            if (LanguageJava) return "java";
            if (LanguagePython) return "python";
            if (LanguageJavaScript) return "javascript";
            if (LanguageTypeScript) return "typescript";
            if (LanguageGo) return "go";
            return string.Empty;
        }

        private string GetSelectedCloud()
        {
            if (CloudAzure) return "azure";
            if (CloudAws) return "aws";
            if (CloudGcp) return "gcp";
            if (CloudOracle) return "oracle";
            return string.Empty;
        }

        private static string ParseString(string envVar, string defaultValue = "")
        {
            return Environment.GetEnvironmentVariable(envVar) ?? defaultValue;
        }

        private static bool ParseBool(string envVar, bool defaultValue = false)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            return !string.IsNullOrWhiteSpace(value) &&
                   (value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1");
        }

        private static int ParseInt(string envVar, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(envVar);
            return int.TryParse(value, out var result) ? result : defaultValue;
        }

        /// <summary>
        /// Get a masked representation of the configuration for logging (sensitive values hidden)
        /// </summary>
        public string ToMaskedString()
        {
            return $@"API Generator Configuration:
Language: {SelectedLanguage}
Cloud: {SelectedCloud}
Track Attribute: {TrackAttribute}
Repo URL: {RepoUrl}
Branch: {Branch}
License Server: {LicenseServer}
Template Repo: {TemplateRepo}
Template Branch: {TemplateBranch}
Template Path: {TemplatePath}
Mode: {Mode}
Verbose: {Verbose}
Log Level: {LogLevel}
[Sensitive values masked]";
        }
    }
}