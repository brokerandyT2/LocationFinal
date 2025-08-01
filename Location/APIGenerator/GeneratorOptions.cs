using CommandLine;

namespace Location.Tools.APIGenerator;

public class GeneratorOptions
{
    [Option("auto-discover", Required = false, Default = true, HelpText = "Auto-discover entities with [ExportToSQL] attribute (recommended)")]
    public bool AutoDiscover { get; set; }

    /// <summary>
    /// DISCOURAGED: Manual table extractor override
    /// WARNING: This bypasses [ExportToSQL] attribute validation
    /// Use --auto-discover for production deployments
    /// </summary>
    [Option("extractors", Required = false, HelpText = "Manual table list (bypasses [ExportToSQL] - use with caution)")]
    public string? ManualExtractors { get; set; }

    /// <summary>
    /// DANGEROUS: Forces export of any table regardless of attributes
    /// This should ONLY be used for debugging/development
    /// </summary>
    [Option("ignore-export-attribute", Required = false, HelpText = "Ignore [ExportToSQL] validation (DEVELOPMENT ONLY)")]
    public bool IgnoreExportAttribute { get; set; }

    // SQL Server connection (reused from SQLGenerator pattern)
    [Option("server", Required = true, HelpText = "SQL Server name")]
    public string Server { get; set; } = string.Empty;

    [Option("database", Required = true, HelpText = "Database name")]
    public string Database { get; set; } = string.Empty;

    [Option("keyvault-url", Required = true, HelpText = "Azure Key Vault URL")]
    public string KeyVaultUrl { get; set; } = string.Empty;

    [Option("username-secret", Required = true, HelpText = "Key Vault secret for SQL username")]
    public string UsernameSecret { get; set; } = string.Empty;

    [Option("password-secret", Required = true, HelpText = "Key Vault secret for SQL password")]
    public string PasswordSecret { get; set; } = string.Empty;

    // Azure deployment
    [Option("azure-subscription", Required = true, HelpText = "Azure subscription ID")]
    public string AzureSubscription { get; set; } = string.Empty;

    [Option("resource-group", Required = true, HelpText = "Resource group for deployment")]
    public string ResourceGroup { get; set; } = string.Empty;

    // Control options
    [Option("prod", Required = false, Default = false, HelpText = "Production deployment mode")]
    public bool IsProduction { get; set; }

    [Option("noop", Required = false, Default = false, HelpText = "Generate assets without deploying")]
    public bool NoOp { get; set; }

    [Option("verbose", Required = false, Default = false, HelpText = "Enable verbose logging")]
    public bool Verbose { get; set; }

    // Assembly overrides
    [Option("infrastructure-assembly", Required = false, HelpText = "Custom path to Infrastructure.dll")]
    public string? InfrastructureAssemblyPath { get; set; }
}