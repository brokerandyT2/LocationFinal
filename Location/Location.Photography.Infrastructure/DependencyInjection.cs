// Location.Photography.Infrastructure/DependencyInjection.cs
using FluentValidation;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Infrastructure.UnitOfWork;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure.Repositories;
using Location.Photography.Infrastructure.Services;
using Location.Photography.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using ISubscriptionRepository = Location.Photography.Application.Common.Interfaces.ISubscriptionRepository;

namespace Location.Photography.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyInfrastructure(this IServiceCollection services)
        {
            // Core services with optimized registration
            services.AddScoped<ISunCalculatorService, SunCalculatorService>();
            services.AddScoped<ISunService, SunService>();
            services.AddScoped<IExposureTriangleService, ExposureTriangleService>();
            services.AddScoped<IExposureCalculatorService, ExposureCalculatorService>();
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<ISubscriptionStatusService, SubscriptionStatusService>();
            services.AddScoped<ISubscriptionFeatureGuard, SubscriptionFeatureGuardService>();
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());
            services.AddScoped<IUnitOfWork, UnitOfWork>();
            services.AddScoped<IPredictiveLightService, PredictiveLightService>();
            services.AddScoped<ITimezoneService, TimezoneService>();
            services.AddScoped<ISceneEvaluationService, SceneEvaluationService>();

            // NEW: Camera/Lens services
            services.AddScoped<ICameraBodyRepository, CameraBodyRepository>();
            services.AddScoped<ILensRepository, LensRepository>();
            services.AddScoped<ILensCameraCompatibilityRepository, LensCameraCompatibilityRepository>();
            services.AddScoped<ICameraDataService, CameraDataService>();
            services.AddScoped<IFOVCalculationService, FOVCalculationService>();
            services.AddScoped<IExifService, ExifService>();
            services.AddScoped<IImageAnalysisService, ImageAnalysisService>();

            services.AddScoped<IAstroCalculationService>(provider =>
    new AstroCalculationService(
        provider.GetRequiredService<ILogger<AstroCalculationService>>(),
        provider.GetRequiredService<ISunCalculatorService>()
    ));

            // ViewModels registered as transient for better memory management
            services.AddTransient<SunCalculationsViewModel>();
            services.AddTransient<SunCalculatorViewModel>();
            services.AddTransient<SunLocationViewModel>();
            services.AddTransient<ExposureCalculatorViewModel>();
            services.AddTransient<SceneEvaluationViewModel>();
            services.AddTransient<SubscriptionSignUpViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingViewModel>();
            services.AddTransient<EnhancedSunCalculatorViewModel>();
            services.AddTransient<SubscriptionAwareViewModelBase>();

            // Add background service for cache cleanup and maintenance
            services.AddHostedService<CacheMaintenanceService>();

            return services;
        }
    }

    /// <summary>
    /// Background service to handle periodic cache cleanup and maintenance tasks
    /// to prevent memory leaks and maintain optimal performance
    /// </summary>
    public class CacheMaintenanceService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CacheMaintenanceService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(30); // Run cleanup every 30 minutes

        public CacheMaintenanceService(
            IServiceProvider serviceProvider,
            ILogger<CacheMaintenanceService> logger)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Cache maintenance service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_cleanupInterval, stoppingToken).ConfigureAwait(false);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    await PerformCacheMaintenanceAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    // Expected when cancellation is requested
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cache maintenance");
                    // Continue running despite errors
                }
            }

            _logger.LogInformation("Cache maintenance service stopped");
        }

        private async Task PerformCacheMaintenanceAsync(CancellationToken cancellationToken)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();

                // Clean up sun calculator cache
                var sunCalculatorService = scope.ServiceProvider.GetService<ISunCalculatorService>();
                if (sunCalculatorService is SunCalculatorService concreteService)
                {
                    await Task.Run(() => concreteService.CleanupExpiredCache(), cancellationToken).ConfigureAwait(false);
                }

                // Clean up subscription repository cache
                var subscriptionRepository = scope.ServiceProvider.GetService<ISubscriptionRepository>();
                if (subscriptionRepository is SubscriptionRepository concreteRepo)
                {
                    await Task.Run(() => concreteRepo.CleanupExpiredCache(), cancellationToken).ConfigureAwait(false);
                }

                // Force garbage collection to free up memory from cleaned caches
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                _logger.LogDebug("Cache maintenance completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during cache maintenance operation");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Cache maintenance service is stopping");
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}