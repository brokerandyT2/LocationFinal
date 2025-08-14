using System;
using System.Collections.Generic;

namespace x3squaredcircles.MobileAdapter.Generator.Configuration
{
    public class GeneratorConfiguration
    {
        // Language Selection
        public bool LanguageCSharp { get; set; }
        public bool LanguageJava { get; set; }
        public bool LanguageKotlin { get; set; }
        public bool LanguageJavaScript { get; set; }
        public bool LanguageTypeScript { get; set; }
        public bool LanguagePython { get; set; }

        // Platform Selection
        public bool PlatformAndroid { get; set; }
        public bool PlatformIOS { get; set; }

        // Core Configuration
        public string RepoUrl { get; set; }
        public string Branch { get; set; }

        // Authentication
        public string PatToken { get; set; }
        public string PatSecretName { get; set; }

        // Licensing
        public string LicenseServer { get; set; }
        public string ToolName { get; set; } = "mobile-adapter-generator";
        public int LicenseTimeout { get; set; } = 300;
        public int LicenseRetryInterval { get; set; } = 30;

        // Key Vault
        public VaultConfiguration Vault { get; set; } = new VaultConfiguration();

        // Discovery
        public string TrackAttribute { get; set; }
        public string TrackPattern { get; set; }
        public string TrackNamespace { get; set; }
        public string TrackFilePattern { get; set; }

        // Assembly and Source Discovery
        public AssemblyConfiguration Assembly { get; set; } = new AssemblyConfiguration();
        public SourceConfiguration Source { get; set; } = new SourceConfiguration();

        // Output
        public OutputConfiguration Output { get; set; } = new OutputConfiguration();

        // Code Generation
        public CodeGenerationConfiguration CodeGeneration { get; set; } = new CodeGenerationConfiguration();

        // Type Mapping
        public TypeMappingConfiguration TypeMapping { get; set; } = new TypeMappingConfiguration();

        // Operation
        public OperationMode Mode { get; set; } = OperationMode.Generate;
        public bool DryRun { get; set; }
        public bool ValidateOnly { get; set; }
        public bool OverwriteExisting { get; set; } = true;
        public bool PreserveCustomCode { get; set; } = true;
        public bool GenerateTests { get; set; }
        public bool IncludeDocumentation { get; set; } = true;

        // Tag Template
        public string TagTemplate { get; set; } = "{branch}/{repo}/adapters/{version}";

        // Logging
        public bool Verbose { get; set; }
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        // Derived Properties
        public SourceLanguage GetSelectedLanguage()
        {
            if (LanguageCSharp) return SourceLanguage.CSharp;
            if (LanguageJava) return SourceLanguage.Java;
            if (LanguageKotlin) return SourceLanguage.Kotlin;
            if (LanguageJavaScript) return SourceLanguage.JavaScript;
            if (LanguageTypeScript) return SourceLanguage.TypeScript;
            if (LanguagePython) return SourceLanguage.Python;
            return SourceLanguage.None;
        }

        public TargetPlatform GetSelectedPlatform()
        {
            if (PlatformAndroid) return TargetPlatform.Android;
            if (PlatformIOS) return TargetPlatform.iOS;
            return TargetPlatform.None;
        }
    }

    public class VaultConfiguration
    {
        public VaultType Type { get; set; }
        public string Url { get; set; }

        // Azure
        public string AzureClientId { get; set; }
        public string AzureClientSecret { get; set; }
        public string AzureTenantId { get; set; }

        // AWS
        public string AwsRegion { get; set; }
        public string AwsAccessKeyId { get; set; }
        public string AwsSecretAccessKey { get; set; }

        // HashiCorp
        public string HashiCorpToken { get; set; }
    }

    public class AssemblyConfiguration
    {
        public string CoreAssemblyPath { get; set; }
        public string TargetAssemblyPath { get; set; }
        public string SearchFolders { get; set; }
        public string AssemblyPattern { get; set; }
    }

    public class SourceConfiguration
    {
        public string SourcePaths { get; set; }
        public string ClassPath { get; set; }
        public string PackagePattern { get; set; }
        public string NodeModulesPath { get; set; }
        public string TypeScriptConfig { get; set; }
        public string PythonPaths { get; set; }
        public string VirtualEnvPath { get; set; }
        public string RequirementsFile { get; set; }
    }

    public class OutputConfiguration
    {
        public string OutputDir { get; set; } = "Generated-Adapters";
        public string AndroidOutputDir { get; set; } = "android/kotlin/";
        public string IosOutputDir { get; set; } = "ios/swift/";
        public string AndroidPackageName { get; set; }
        public string IosModuleName { get; set; }
        public bool GenerateManifest { get; set; } = true;
    }

    public class CodeGenerationConfiguration
    {
        public AndroidGenerationOptions Android { get; set; } = new AndroidGenerationOptions();
        public IosGenerationOptions Ios { get; set; } = new IosGenerationOptions();
    }

    public class AndroidGenerationOptions
    {
        public bool UseCoroutines { get; set; } = true;
        public bool UseStateFlow { get; set; } = true;
        public int TargetApi { get; set; } = 34;
        public string KotlinVersion { get; set; } = "1.9";
    }

    public class IosGenerationOptions
    {
        public bool UseCombine { get; set; } = true;
        public bool UseAsyncAwait { get; set; } = true;
        public string TargetVersion { get; set; } = "15.0";
        public string SwiftVersion { get; set; } = "5.9";
    }

    public class TypeMappingConfiguration
    {
        public string CustomTypeMappings { get; set; }
        public bool PreserveNullableTypes { get; set; } = true;
        public bool UsePlatformCollections { get; set; } = true;
    }

    public enum SourceLanguage
    {
        None,
        CSharp,
        Java,
        Kotlin,
        JavaScript,
        TypeScript,
        Python
    }

    public enum TargetPlatform
    {
        None,
        Android,
        iOS
    }

    public enum OperationMode
    {
        Analyze,
        Generate,
        Validate
    }

    public enum VaultType
    {
        None,
        Azure,
        Aws,
        HashiCorp
    }

    public enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }
}