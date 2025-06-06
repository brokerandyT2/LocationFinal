// Location.Core.Infrastructure/DependencyInjection.cs
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Services;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Events;
using Location.Core.Infrastructure.External;
using Location.Core.Infrastructure.Services;
using Location.Photography.Application.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Location.Core.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Database
            services.AddSingleton<IDatabaseContext, DatabaseContext>();
            services.AddScoped<IUnitOfWork, Location.Core.Infrastructure.UnitOfWork.UnitOfWork>();

            // Persistence-layer repositories (implementing Common.Interfaces.Persistence interfaces)
            services.AddScoped<LocationRepository>();
            services.AddScoped<WeatherRepository>();
            services.AddScoped<TipRepository>();
            services.AddScoped<TipTypeRepository>();
            services.AddScoped<SettingRepository>();

            // IMPORTANT: Don't register AlertingService as IAlertService anymore
            // We'll register the concrete type, but not as the interface implementation
            services.AddScoped<AlertingService>();

            // If an IAlertService isn't registered yet, use DirectAlertingService as a fallback
            // This ensures infrastructure components have a non-circular alerting mechanism
            services.AddScoped<DirectAlertingService>();
            services.TryAddScoped<IAlertService, DirectAlertingService>();
           
            services.AddScoped<ITimezoneService, TimezoneService>();
            // Register persistence interfaces
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository>(sp =>
                sp.GetRequiredService<LocationRepository>());
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository>(sp =>
                sp.GetRequiredService<WeatherRepository>());
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.ITipRepository>(sp =>
                sp.GetRequiredService<TipRepository>());
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository>(sp =>
                sp.GetRequiredService<TipTypeRepository>());
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository>(sp =>
                sp.GetRequiredService<SettingRepository>());

            // Register application interfaces with adapters
            services.AddScoped<Location.Core.Application.Common.Interfaces.ILocationRepository, LocationRepositoryAdapter>();
            services.AddScoped<Location.Core.Application.Common.Interfaces.IWeatherRepository, WeatherRepositoryAdapter>();
            services.AddScoped<Location.Core.Application.Common.Interfaces.ITipRepository, TipRepositoryAdapter>();
            services.AddScoped<Location.Core.Application.Common.Interfaces.ITipTypeRepository, TipTypeRepositoryAdapter>();
            services.AddScoped<Location.Core.Application.Common.Interfaces.ISettingRepository, SettingRepositoryAdapter>();

            // Services
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<ILoggingService, LoggingService>();

            // Exception mapping service
            services.AddScoped<IInfrastructureExceptionMappingService, InfrastructureExceptionMappingService>();

            // Event Bus
            services.AddSingleton<IEventBus, InMemoryEventBus>();

            // HTTP Client for Weather API
            services.AddHttpClient<WeatherService>(client =>
            {
                client.BaseAddress = new Uri("https://api.openweathermap.org");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });

            // Initialize database on startup
            services.AddHostedService<DatabaseInitializationService>();

            return services;
        }

        // Background service to initialize database
        internal class DatabaseInitializationService : BackgroundService
        {
            private readonly IServiceProvider _serviceProvider;
            private readonly ILogger<DatabaseInitializationService> _logger;

            public DatabaseInitializationService(
                IServiceProvider serviceProvider,
                ILogger<DatabaseInitializationService> logger)
            {
                _serviceProvider = serviceProvider;
                _logger = logger;
            }

            protected override async Task ExecuteAsync(CancellationToken stoppingToken)
            {
                try
                {
                    _logger.LogInformation("Starting database initialization...");

                    // Wait a short time to ensure all services are registered
                    await Task.Delay(1000, stoppingToken);

                    using var scope = _serviceProvider.CreateScope();
                    var databaseContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();

                    _logger.LogInformation("Initializing database...");
                    await databaseContext.InitializeDatabaseAsync();
                    _logger.LogInformation("Database initialized successfully");

                    // Initial data seeding could go here if needed
                    // For example, creating default settings
                    await SeedInitialDataAsync(scope.ServiceProvider, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize database");
                    throw; // Let the host handle critical startup failures
                }
            }

            private async Task SeedInitialDataAsync(IServiceProvider serviceProvider, CancellationToken stoppingToken)
            {
                try
                {
                    var settingRepository = serviceProvider.GetRequiredService<Location.Core.Application.Common.Interfaces.ISettingRepository>();

                    // Check if WeatherApiKey setting exists, create if not
                    var apiKeyResult = await settingRepository.GetByKeyAsync("WeatherApiKey", stoppingToken);
                    if (!apiKeyResult.IsSuccess || apiKeyResult.Data == null)
                    {
                        // Create a placeholder setting that the user will need to update
                        var setting = new Domain.Entities.Setting("WeatherApiKey", "YOUR_API_KEY_HERE",
                            "API key for OpenWeatherMap service - obtain one at https://openweathermap.org");
                        await settingRepository.CreateAsync(setting, stoppingToken);
                        _logger.LogInformation("Created default WeatherApiKey setting");
                    }

                    // Additional seeding can be added here
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during initial data seeding");
                    // Non-critical error, don't throw
                }
            }
        }
    }
}