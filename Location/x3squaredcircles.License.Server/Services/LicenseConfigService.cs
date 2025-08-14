using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using x3squaredcircles.License.Server.Data;
using x3squaredcircles.License.Server.Models;

namespace x3squaredcircles.License.Server.Services
{
    public interface ILicenseConfigService
    {
        Task InitializeLicenseConfigAsync();
        Task<LicenseConfig> GetLicenseConfigAsync();
        Task<List<string>> GetLicensedToolsAsync();
        Task<bool> IsToolLicensedAsync(string toolName);
    }

    public class LicenseConfigService : ILicenseConfigService
    {
        private readonly LicenseDbContext _context;
        private readonly ILogger<LicenseConfigService> _logger;
        private LicenseConfig? _cachedConfig;

        public LicenseConfigService(LicenseDbContext context, ILogger<LicenseConfigService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task InitializeLicenseConfigAsync()
        {
            try
            {
                _logger.LogInformation("Initializing license configuration from embedded config");

                // Check if configuration already exists
                var existingConfig = await _context.LicenseConfigs.FirstOrDefaultAsync();
                if (existingConfig != null)
                {
                    _logger.LogInformation("License configuration already exists, using existing config");
                    _cachedConfig = existingConfig;
                    return;
                }

                // Load embedded configuration
                var embeddedConfig = GetEmbeddedLicenseConfig();

                // Create new license configuration in database
                var licenseConfig = new LicenseConfig
                {
                    MaxConcurrent = embeddedConfig.MaxConcurrent,
                    ToolsLicensed = JsonSerializer.Serialize(embeddedConfig.ToolsLicensed),
                    BurstMultiplier = embeddedConfig.BurstMultiplier,
                    BurstAllowancePerMonth = embeddedConfig.BurstAllowancePerMonth
                };

                _context.LicenseConfigs.Add(licenseConfig);
                await _context.SaveChangesAsync();

                _cachedConfig = licenseConfig;

                _logger.LogInformation("License configuration initialized successfully");
                _logger.LogInformation("Max Concurrent: {MaxConcurrent}", licenseConfig.MaxConcurrent);
                _logger.LogInformation("Licensed Tools: {ToolCount}", embeddedConfig.ToolsLicensed.Count);
                _logger.LogInformation("Burst Multiplier: {BurstMultiplier}", licenseConfig.BurstMultiplier);
                _logger.LogInformation("Burst Allowance: {BurstAllowance}/month", licenseConfig.BurstAllowancePerMonth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize license configuration");
                throw;
            }
        }

        public async Task<LicenseConfig> GetLicenseConfigAsync()
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            var config = await _context.LicenseConfigs.FirstOrDefaultAsync();
            if (config == null)
            {
                throw new InvalidOperationException("License configuration not found. Call InitializeLicenseConfigAsync first.");
            }

            _cachedConfig = config;
            return config;
        }

        public async Task<List<string>> GetLicensedToolsAsync()
        {
            try
            {
                var config = await GetLicenseConfigAsync();
                var tools = JsonSerializer.Deserialize<List<string>>(config.ToolsLicensed);
                return tools ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get licensed tools");
                return new List<string>();
            }
        }

        public async Task<bool> IsToolLicensedAsync(string toolName)
        {
            try
            {
                var licensedTools = await GetLicensedToolsAsync();
                return licensedTools.Contains(toolName, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check if tool is licensed: {ToolName}", toolName);
                return false;
            }
        }

        private EmbeddedLicenseConfig GetEmbeddedLicenseConfig()
        {
            // Read embedded JSON configuration file
            var configPath = Path.Combine(AppContext.BaseDirectory, "license-config.json");

            if (File.Exists(configPath))
            {
                try
                {
                    _logger.LogInformation("Loading embedded license configuration from: {ConfigPath}", configPath);
                    var jsonContent = File.ReadAllText(configPath);
                    var config = JsonSerializer.Deserialize<EmbeddedLicenseConfig>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (config != null)
                    {
                        _logger.LogInformation("Embedded license config loaded successfully: MaxConcurrent={MaxConcurrent}, Tools={ToolCount}",
                            config.MaxConcurrent, config.ToolsLicensed.Count);
                        return config;
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Failed to parse embedded license configuration JSON file");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to read embedded license configuration file");
                }
            }
            else
            {
                _logger.LogWarning("Embedded license configuration file not found: {ConfigPath}", configPath);
            }

            // Fallback to environment variables if JSON file not found or invalid
            _logger.LogInformation("Falling back to environment variable configuration");
            return GetConfigFromEnvironmentVariables();
        }

        private EmbeddedLicenseConfig GetConfigFromEnvironmentVariables()
        {
            var maxConcurrentStr = Environment.GetEnvironmentVariable("LC_MAX_CONCURRENT");
            var toolsLicensedStr = Environment.GetEnvironmentVariable("LC_TOOLS_LICENSED");
            var burstMultiplierStr = Environment.GetEnvironmentVariable("LC_BURST_MULTIPLIER");
            var burstAllowanceStr = Environment.GetEnvironmentVariable("LC_BURST_ALLOWANCE");

            var config = new EmbeddedLicenseConfig
            {
                MaxConcurrent = int.TryParse(maxConcurrentStr, out var maxConcurrent) ? maxConcurrent : 5,
                BurstMultiplier = int.TryParse(burstMultiplierStr, out var burstMultiplier) ? burstMultiplier : 2,
                BurstAllowancePerMonth = int.TryParse(burstAllowanceStr, out var burstAllowance) ? burstAllowance : 2
            };

            // Parse licensed tools from environment variable or use defaults
            if (!string.IsNullOrEmpty(toolsLicensedStr))
            {
                try
                {
                    var tools = JsonSerializer.Deserialize<List<string>>(toolsLicensedStr);
                    if (tools != null && tools.Any())
                    {
                        config.ToolsLicensed = tools;
                    }
                    else
                    {
                        config.ToolsLicensed = GetDefaultLicensedTools();
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogWarning(ex, "Failed to parse LC_TOOLS_LICENSED environment variable, using defaults");
                    config.ToolsLicensed = GetDefaultLicensedTools();
                }
            }
            else
            {
                config.ToolsLicensed = GetDefaultLicensedTools();
            }

            _logger.LogDebug("Loaded config from environment variables: MaxConcurrent={MaxConcurrent}, Tools={ToolCount}",
                config.MaxConcurrent, config.ToolsLicensed.Count);

            return config;
        }

        private List<string> GetDefaultLicensedTools()
        {
            // Default set of licensed tools as per architecture document
            return new List<string>
            {
                "version-detective",
                "version-calculator",
                "sql-schema-generator",
                "api-generator",
                "release-notes-generator"
            };
        }
    }
}