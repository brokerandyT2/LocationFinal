using System;

namespace x3squaredcircles.MobileAdapter.Generator.Configuration
{
    public static class EnvironmentConfigurationLoader
    {
        public static GeneratorConfiguration LoadConfiguration()
        {
            var config = new GeneratorConfiguration();

            // Language Selection
            config.LanguageCSharp = GetBoolEnvironmentVariable("LANGUAGE_CSHARP");
            config.LanguageJava = GetBoolEnvironmentVariable("LANGUAGE_JAVA");
            config.LanguageKotlin = GetBoolEnvironmentVariable("LANGUAGE_KOTLIN");
            config.LanguageJavaScript = GetBoolEnvironmentVariable("LANGUAGE_JAVASCRIPT");
            config.LanguageTypeScript = GetBoolEnvironmentVariable("LANGUAGE_TYPESCRIPT");
            config.LanguagePython = GetBoolEnvironmentVariable("LANGUAGE_PYTHON");

            // Platform Selection
            config.PlatformAndroid = GetBoolEnvironmentVariable("PLATFORM_ANDROID");
            config.PlatformIOS = GetBoolEnvironmentVariable("PLATFORM_IOS");

            // Core Configuration
            config.RepoUrl = Environment.GetEnvironmentVariable("REPO_URL");
            config.Branch = Environment.GetEnvironmentVariable("BRANCH");

            // Authentication
            config.PatToken = Environment.GetEnvironmentVariable("PAT_TOKEN");
            config.PatSecretName = Environment.GetEnvironmentVariable("PAT_SECRET_NAME");

            // Licensing
            config.LicenseServer = Environment.GetEnvironmentVariable("LICENSE_SERVER");
            config.ToolName = Environment.GetEnvironmentVariable("TOOL_NAME") ?? "mobile-adapter-generator";
            config.LicenseTimeout = GetIntEnvironmentVariable("LICENSE_TIMEOUT", 300);
            config.LicenseRetryInterval = GetIntEnvironmentVariable("LICENSE_RETRY_INTERVAL", 30);

            // Key Vault
            LoadVaultConfiguration(config);

            // Discovery
            config.TrackAttribute = Environment.GetEnvironmentVariable("TRACK_ATTRIBUTE");
            config.TrackPattern = Environment.GetEnvironmentVariable("TRACK_PATTERN");
            config.TrackNamespace = Environment.GetEnvironmentVariable("TRACK_NAMESPACE");
            config.TrackFilePattern = Environment.GetEnvironmentVariable("TRACK_FILE_PATTERN");

            // Assembly and Source Discovery
            LoadAssemblyConfiguration(config);
            LoadSourceConfiguration(config);

            // Output
            LoadOutputConfiguration(config);

            // Code Generation
            LoadCodeGenerationConfiguration(config);

            // Type Mapping
            LoadTypeMappingConfiguration(config);

            // Operation
            config.Mode = GetEnumEnvironmentVariable<OperationMode>("MODE", OperationMode.Generate);
            config.DryRun = GetBoolEnvironmentVariable("DRY_RUN");
            config.ValidateOnly = GetBoolEnvironmentVariable("VALIDATE_ONLY");
            config.OverwriteExisting = GetBoolEnvironmentVariable("OVERWRITE_EXISTING", true);
            config.PreserveCustomCode = GetBoolEnvironmentVariable("PRESERVE_CUSTOM_CODE", true);
            config.GenerateTests = GetBoolEnvironmentVariable("GENERATE_TESTS");
            config.IncludeDocumentation = GetBoolEnvironmentVariable("INCLUDE_DOCUMENTATION", true);

            // Tag Template
            config.TagTemplate = Environment.GetEnvironmentVariable("TAG_TEMPLATE") ?? "{branch}/{repo}/adapters/{version}";

            // Logging
            config.Verbose = GetBoolEnvironmentVariable("VERBOSE");
            config.LogLevel = GetEnumEnvironmentVariable<LogLevel>("LOG_LEVEL", LogLevel.Info);

            return config;
        }

        private static void LoadVaultConfiguration(GeneratorConfiguration config)
        {
            config.Vault.Type = GetEnumEnvironmentVariable<VaultType>("VAULT_TYPE", VaultType.None);
            config.Vault.Url = Environment.GetEnvironmentVariable("VAULT_URL");

            // Azure
            config.Vault.AzureClientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID");
            config.Vault.AzureClientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET");
            config.Vault.AzureTenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID");

            // AWS
            config.Vault.AwsRegion = Environment.GetEnvironmentVariable("AWS_REGION");
            config.Vault.AwsAccessKeyId = Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");
            config.Vault.AwsSecretAccessKey = Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY");

            // HashiCorp
            config.Vault.HashiCorpToken = Environment.GetEnvironmentVariable("VAULT_TOKEN");
        }

        private static void LoadAssemblyConfiguration(GeneratorConfiguration config)
        {
            config.Assembly.CoreAssemblyPath = Environment.GetEnvironmentVariable("CORE_ASSEMBLY_PATH");
            config.Assembly.TargetAssemblyPath = Environment.GetEnvironmentVariable("TARGET_ASSEMBLY_PATH");
            config.Assembly.SearchFolders = Environment.GetEnvironmentVariable("SEARCH_FOLDERS");
            config.Assembly.AssemblyPattern = Environment.GetEnvironmentVariable("ASSEMBLY_PATTERN");
        }

        private static void LoadSourceConfiguration(GeneratorConfiguration config)
        {
            config.Source.SourcePaths = Environment.GetEnvironmentVariable("SOURCE_PATHS");
            config.Source.ClassPath = Environment.GetEnvironmentVariable("CLASSPATH");
            config.Source.PackagePattern = Environment.GetEnvironmentVariable("PACKAGE_PATTERN");
            config.Source.NodeModulesPath = Environment.GetEnvironmentVariable("NODE_MODULES_PATH");
            config.Source.TypeScriptConfig = Environment.GetEnvironmentVariable("TYPESCRIPT_CONFIG");
            config.Source.PythonPaths = Environment.GetEnvironmentVariable("PYTHON_PATHS");
            config.Source.VirtualEnvPath = Environment.GetEnvironmentVariable("VIRTUAL_ENV_PATH");
            config.Source.RequirementsFile = Environment.GetEnvironmentVariable("REQUIREMENTS_FILE");
        }

        private static void LoadOutputConfiguration(GeneratorConfiguration config)
        {
            config.Output.OutputDir = Environment.GetEnvironmentVariable("OUTPUT_DIR") ?? "Generated-Adapters";
            config.Output.AndroidOutputDir = Environment.GetEnvironmentVariable("ANDROID_OUTPUT_DIR") ?? "android/kotlin/";
            config.Output.IosOutputDir = Environment.GetEnvironmentVariable("IOS_OUTPUT_DIR") ?? "ios/swift/";
            config.Output.AndroidPackageName = Environment.GetEnvironmentVariable("ANDROID_PACKAGE_NAME");
            config.Output.IosModuleName = Environment.GetEnvironmentVariable("IOS_MODULE_NAME");
            config.Output.GenerateManifest = GetBoolEnvironmentVariable("GENERATE_MANIFEST", true);
        }

        private static void LoadCodeGenerationConfiguration(GeneratorConfiguration config)
        {
            // Android
            config.CodeGeneration.Android.UseCoroutines = GetBoolEnvironmentVariable("ANDROID_USE_COROUTINES", true);
            config.CodeGeneration.Android.UseStateFlow = GetBoolEnvironmentVariable("ANDROID_USE_STATEFLOW", true);
            config.CodeGeneration.Android.TargetApi = GetIntEnvironmentVariable("ANDROID_TARGET_API", 34);
            config.CodeGeneration.Android.KotlinVersion = Environment.GetEnvironmentVariable("ANDROID_KOTLIN_VERSION") ?? "1.9";

            // iOS
            config.CodeGeneration.Ios.UseCombine = GetBoolEnvironmentVariable("IOS_USE_COMBINE", true);
            config.CodeGeneration.Ios.UseAsyncAwait = GetBoolEnvironmentVariable("IOS_USE_ASYNC_AWAIT", true);
            config.CodeGeneration.Ios.TargetVersion = Environment.GetEnvironmentVariable("IOS_TARGET_VERSION") ?? "15.0";
            config.CodeGeneration.Ios.SwiftVersion = Environment.GetEnvironmentVariable("IOS_SWIFT_VERSION") ?? "5.9";
        }

        private static void LoadTypeMappingConfiguration(GeneratorConfiguration config)
        {
            config.TypeMapping.CustomTypeMappings = Environment.GetEnvironmentVariable("CUSTOM_TYPE_MAPPINGS");
            config.TypeMapping.PreserveNullableTypes = GetBoolEnvironmentVariable("PRESERVE_NULLABLE_TYPES", true);
            config.TypeMapping.UsePlatformCollections = GetBoolEnvironmentVariable("USE_PLATFORM_COLLECTIONS", true);
        }

        private static bool GetBoolEnvironmentVariable(string name, bool defaultValue = false)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static int GetIntEnvironmentVariable(string name, int defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            if (int.TryParse(value, out var result))
                return result;

            return defaultValue;
        }

        private static T GetEnumEnvironmentVariable<T>(string name, T defaultValue) where T : struct, Enum
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            if (Enum.TryParse<T>(value, true, out var result))
                return result;

            return defaultValue;
        }
    }
}