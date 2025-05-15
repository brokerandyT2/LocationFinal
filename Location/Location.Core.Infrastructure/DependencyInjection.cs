using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Events;
using Location.Core.Infrastructure.External;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly.Extensions.Http;
using Polly;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

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

            // The IUnitOfWork uses persistence interfaces, so we need to register the adapters with the same interface
            // This creates a problem because we can't have two registrations for the same interface
            // The solution is to modify the WeatherService to not expect Result types from GetActiveAsync

            // Fix: Cast explicitly to the interface type for Weather repository
            services.AddScoped<Location.Core.Application.Common.Interfaces.IWeatherRepository>(sp =>
            {
                var persistenceRepository = sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository>();
                return new WeatherRepositoryAdapter(persistenceRepository) as Location.Core.Application.Common.Interfaces.IWeatherRepository;
            });

            services.AddScoped<Location.Core.Application.Common.Interfaces.ITipRepository>(sp =>
                new TipRepositoryAdapter(sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ITipRepository>()));
            services.AddScoped<Location.Core.Application.Common.Interfaces.ITipTypeRepository>(sp =>
                new TipTypeRepositoryAdapter(sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository>()));
            services.AddScoped<Location.Core.Application.Common.Interfaces.ISettingRepository>(sp =>
                new SettingRepositoryAdapter(sp.GetRequiredService<Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository>()));

            // Services
            services.AddScoped<IWeatherService, WeatherService>();
            services.AddScoped<ILoggingService, LoggingService>();

            // Event Bus
            services.AddSingleton<IEventBus, InMemoryEventBus>();

            // HTTP Client for Weather API without Polly retry policy
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