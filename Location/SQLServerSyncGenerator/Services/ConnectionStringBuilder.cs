using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace SQLServerSyncGenerator.Services;

public class ConnectionStringBuilder
{
    private readonly ILogger<ConnectionStringBuilder> _logger;

    public ConnectionStringBuilder(ILogger<ConnectionStringBuilder> logger)
    {
        _logger = logger;
    }

    public async Task<string> BuildConnectionStringAsync(GeneratorOptions options)
    {
        try
        {
            _logger.LogDebug("Retrieving SQL credentials from Azure Key Vault: {KeyVaultUrl}", options.KeyVaultUrl);

            // Create Key Vault client using DefaultAzureCredential
            // This supports managed identity, service principal, developer credentials, etc.
            var keyVaultClient = new SecretClient(new Uri(options.KeyVaultUrl), new DefaultAzureCredential());

            // Retrieve username and password secrets
            _logger.LogDebug("Retrieving username secret: {UsernameSecret}", options.UsernameSecret);
            var usernameResponse = await keyVaultClient.GetSecretAsync(options.UsernameSecret);

            _logger.LogDebug("Retrieving password secret: {PasswordSecret}", options.PasswordSecret);
            var passwordResponse = await keyVaultClient.GetSecretAsync(options.PasswordSecret);

            _logger.LogDebug("Successfully retrieved credentials from Key Vault");

            // Build SQL connection string
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = options.Server,
                InitialCatalog = options.Database,
                UserID = usernameResponse.Value.Value,
                Password = passwordResponse.Value.Value,
                Encrypt = true,                    // Always encrypt for Azure SQL
                TrustServerCertificate = false,    // Validate certificate
                ConnectTimeout = 30,               // 30 second connection timeout
                CommandTimeout = 300               // 5 minute command timeout for DDL operations
            };

            _logger.LogInformation("Connection string built successfully for {Server}/{Database}",
                options.Server, options.Database);

            return builder.ConnectionString;
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401)
        {
            _logger.LogError("Azure Key Vault authentication failed. Ensure the application has proper access to Key Vault.");
            throw new InvalidOperationException("Key Vault authentication failed", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            _logger.LogError("Access denied to Azure Key Vault. Check Key Vault access policies and permissions.");
            throw new InvalidOperationException("Key Vault access denied", ex);
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogError("Key Vault secret not found. Verify secret names: {UsernameSecret}, {PasswordSecret}",
                options.UsernameSecret, options.PasswordSecret);
            throw new InvalidOperationException("Key Vault secret not found", ex);
        }
        catch (UriFormatException ex)
        {
            _logger.LogError("Invalid Key Vault URL format: {KeyVaultUrl}", options.KeyVaultUrl);
            throw new InvalidOperationException("Invalid Key Vault URL format", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build connection string from Key Vault");
            throw;
        }
    }

    /// <summary>
    /// Validates that the connection string works by attempting a simple connection test
    /// </summary>
    public async Task<bool> ValidateConnectionAsync(string connectionString)
    {
        try
        {
            _logger.LogDebug("Validating SQL connection...");

            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();

            // Simple validation query
            using var command = new SqlCommand("SELECT @@VERSION", connection);
            var version = await command.ExecuteScalarAsync();

            _logger.LogDebug("Connection validation successful. SQL Server version: {Version}",
                version?.ToString()?.Substring(0, Math.Min(50, version.ToString()?.Length ?? 0)));

            return true;
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "SQL connection validation failed: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection validation failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// Creates an admin connection string for database-level operations (backup, restore, etc.)
    /// Uses the master database instead of the target database
    /// </summary>
    public async Task<string> BuildAdminConnectionStringAsync(GeneratorOptions options)
    {
        try
        {
            _logger.LogDebug("Building admin connection string for database operations");

            // Get the regular connection string first
            var regularConnectionString = await BuildConnectionStringAsync(options);
            var builder = new SqlConnectionStringBuilder(regularConnectionString);

            // Change to master database for admin operations
            builder.InitialCatalog = "master";
            builder.CommandTimeout = 600; // 10 minutes for backup/restore operations

            _logger.LogDebug("Admin connection string built for master database");

            return builder.ConnectionString;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build admin connection string");
            throw;
        }
    }
}