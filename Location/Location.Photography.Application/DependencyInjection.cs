// Location.Photography.Application/DependencyInjection.cs
using FluentValidation;
using Location.Core.Application.Common.Behaviors;
using Location.Photography.Application.Services;
using MediatR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Location.Photography.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyApplication(this IServiceCollection services)
        {
            // Register MediatR with optimized pipeline behaviors
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
            });

            // Register FluentValidation validators with assembly scanning
            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), ServiceLifetime.Singleton);

            // Register application services with optimized lifetimes
            services.AddScoped<ITimezoneService, TimezoneService>();
            services.AddScoped<IImageAnalysisService, ImageAnalysisService>();

            // Add performance monitoring service
            services.AddHostedService<ApplicationPerformanceMonitoringService>();

            return services;
        }
    }

    /// <summary>
    /// Performance behavior to monitor and log slow-running requests
    /// </summary>
    public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

        public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var response = await next().ConfigureAwait(false);
                stopwatch.Stop();

                var requestName = typeof(TRequest).Name;
                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Log performance warnings for slow requests
                if (elapsedMs > 5000) // > 5 seconds
                {
                    _logger.LogWarning("Slow Request: {RequestName} took {ElapsedMs}ms", requestName, elapsedMs);
                }
                else if (elapsedMs > 1000) // > 1 second
                {
                    _logger.LogInformation("Request: {RequestName} took {ElapsedMs}ms", requestName, elapsedMs);
                }

                return response;
            }
            catch (Exception)
            {
                stopwatch.Stop();
                throw;
            }
        }
    }

    /// <summary>
    /// Caching behavior for frequently accessed read-only data
    /// </summary>
    public class CachingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<CachingBehavior<TRequest, TResponse>> _logger;
        private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, (object response, DateTime expiry)> _cache = new();
        private static readonly TimeSpan _defaultCacheTime = TimeSpan.FromMinutes(5);

        public CachingBehavior(ILogger<CachingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger;
        }

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;

            // Only cache read-only queries, not commands
            if (!requestName.EndsWith("Query"))
            {
                return await next().ConfigureAwait(false);
            }

            var cacheKey = $"{requestName}_{request.GetHashCode()}";

            // Check cache first
            if (_cache.TryGetValue(cacheKey, out var cached) && DateTime.UtcNow < cached.expiry)
            {
                _logger.LogDebug("Cache hit for {RequestName}", requestName);
                return (TResponse)cached.response;
            }

            // Execute request and cache result
            var response = await next().ConfigureAwait(false);

            // Cache successful responses
            if (response != null)
            {
                _cache[cacheKey] = (response, DateTime.UtcNow.Add(_defaultCacheTime));
                _logger.LogDebug("Cached response for {RequestName}", requestName);
            }

            return response;
        }

        /// <summary>
        /// Cleanup expired cache entries
        /// </summary>
        public static void CleanupExpiredCache()
        {
            var expiredKeys = _cache
                .Where(kvp => DateTime.UtcNow >= kvp.Value.expiry)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.TryRemove(key, out _);
            }
        }
    }

    /// <summary>
    /// Background service to monitor application performance and maintain caches
    /// </summary>
    public class ApplicationPerformanceMonitoringService : BackgroundService
    {
        private readonly ILogger<ApplicationPerformanceMonitoringService> _logger;
        private readonly TimeSpan _monitoringInterval = TimeSpan.FromMinutes(10);

        public ApplicationPerformanceMonitoringService(ILogger<ApplicationPerformanceMonitoringService> logger)
        {
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Application performance monitoring service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(_monitoringInterval, stoppingToken).ConfigureAwait(false);

                    if (stoppingToken.IsCancellationRequested)
                        break;

                    await PerformMaintenanceTasksAsync(stoppingToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during performance monitoring");
                }
            }

            _logger.LogInformation("Application performance monitoring service stopped");
        }

        private async Task PerformMaintenanceTasksAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Cleanup expired caches using reflection to access the static method
                await Task.Run(() =>
                {
                    var cachingBehaviorType = typeof(CachingBehavior<,>);
                    var cleanupMethod = cachingBehaviorType.GetMethod("CleanupExpiredCache",
                        BindingFlags.Public | BindingFlags.Static);

                    if (cleanupMethod != null)
                    {
                        // Create a concrete type to call the static method
                        var concreteType = cachingBehaviorType.MakeGenericType(typeof(IRequest<object>), typeof(object));
                        var concreteMethod = concreteType.GetMethod("CleanupExpiredCache",
                            BindingFlags.Public | BindingFlags.Static);
                        concreteMethod?.Invoke(null, null);
                    }
                }, cancellationToken).ConfigureAwait(false);

                // Monitor memory usage
                var memoryBefore = GC.GetTotalMemory(false);

                // Force garbage collection if memory usage is high
                if (memoryBefore > 100 * 1024 * 1024) // > 100MB
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    var memoryAfter = GC.GetTotalMemory(false);
                    var memoryFreed = memoryBefore - memoryAfter;

                    if (memoryFreed > 10 * 1024 * 1024) // > 10MB freed
                    {
                        _logger.LogInformation("Garbage collection freed {MemoryFreedMB:F1} MB",
                            memoryFreed / (1024.0 * 1024.0));
                    }
                }

                _logger.LogDebug("Performance maintenance completed");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error during performance maintenance");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Application performance monitoring service is stopping");
            await base.StopAsync(cancellationToken).ConfigureAwait(false);
        }
    }
}