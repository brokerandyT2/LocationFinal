using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Events;
using Location.Core.Infrastructure.External;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using System;
namespace Location.Core.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services)
        {
            // Database
            services.AddSingleton<IDatabaseContext, DatabaseContext>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            // Repositories - registering as Persistence interfaces
            services.AddScoped<LocationRepository>();
            services.AddScoped<WeatherRepository>();
            services.AddScoped<TipRepository>();
            services.AddScoped<TipTypeRepository>();
            services.AddScoped<SettingRepository>();

            // Application interfaces - using adapters
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<LocationRepository>();
                return persistenceRepo;
            });
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<WeatherRepository>();
                return persistenceRepo;
            });
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.ITipRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<TipRepository>();
                return persistenceRepo;
            });
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<TipTypeRepository>();
                return persistenceRepo;
            });
            services.AddScoped<Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<SettingRepository>();
                return persistenceRepo;
            });

            // Application interfaces - using adapters
            services.AddScoped<Location.Core.Application.Common.Interfaces.ILocationRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository>();
                return new LocationRepositoryAdapter(persistenceRepo);
            });
            services.AddScoped<Location.Core.Application.Common.Interfaces.IWeatherRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository>();
                return new WeatherRepositoryAdapter(persistenceRepo);
            });
            services.AddScoped<Location.Core.Application.Common.Interfaces.ITipRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ITipRepository>();
                return new TipRepositoryAdapter(persistenceRepo);
            });
            services.AddScoped<Location.Core.Application.Common.Interfaces.ITipTypeRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository>();
                return new TipTypeRepositoryAdapter(persistenceRepo);
            });
            services.AddScoped<Location.Core.Application.Common.Interfaces.ISettingRepository>(sp =>
            {
                var persistenceRepo = sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository>();
                return new SettingRepositoryAdapter(persistenceRepo);
            });

            // Services
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<ILoggingService, LoggingService>();

            // Event Bus
            services.AddSingleton<IEventBus, InMemoryEventBus>();

            // HTTP Client for Weather API
            services.AddHttpClient<WeatherService>(client =>
            {
                client.BaseAddress = new Uri("https://api.openweathermap.org");
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
            // .AddPolicyHandler(GetRetryPolicy()); // Removed due to missing extension method

            // Initialize database on startup
            services.AddHostedService<DatabaseInitializationService>();

            return services;
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    3,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        if (context.Values.TryGetValue("logger", out var loggerObj) && loggerObj is ILogger logger)
                        {
                            logger.LogWarning("Retry {RetryCount} after {Timespan} seconds",
                                retryCount, timespan.TotalSeconds);
                        }
                    });
        }
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
                using var scope = _serviceProvider.CreateScope();
                var databaseContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();

                _logger.LogInformation("Initializing database...");
                await databaseContext.InitializeDatabaseAsync();
                _logger.LogInformation("Database initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize database");
                throw; // Let the host handle critical startup failures
            }
        }
    }
}