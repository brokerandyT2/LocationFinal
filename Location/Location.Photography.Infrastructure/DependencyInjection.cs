// Location.Photography.Infrastructure/DependencyInjection.cs
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure.Repositories;
using Location.Photography.Infrastructure.Services;
using Location.Photography.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyInfrastructure(this IServiceCollection services)
        {
            // Register database initializer
            //services.AddDatabaseInitializer();

            services.AddScoped<ISunCalculatorService, SunCalculatorService>();
            services.AddScoped<ISunService, SunService>();

            // Register exposure calculation services
            services.AddScoped<IExposureTriangleService, ExposureTriangleService>();
            services.AddScoped<IExposureCalculatorService, ExposureCalculatorService>();

            // Register subscription services
            services.AddScoped<ISubscriptionService, SubscriptionService>();
            services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
            services.AddScoped<ISubscriptionStatusService, SubscriptionStatusService>();
            services.AddScoped<ISubscriptionFeatureGuard, SubscriptionFeatureGuard>();

            // Register other photography services
            services.AddScoped<ISceneEvaluationService, SceneEvaluationService>();
            // Register ViewModels as Transient (new instance each time)
            services.AddTransient<SunCalculationsViewModel>();
            services.AddTransient<SunCalculatorViewModel>();
            services.AddTransient<SunLocationViewModel>();
            services.AddTransient<ExposureCalculatorViewModel>();
            services.AddTransient<SceneEvaluationViewModel>();
            services.AddTransient<SubscriptionSignUpViewModel>();
            services.AddTransient<SettingsViewModel>();
            services.AddTransient<SettingViewModel>();

            // Register subscription-aware base ViewModels
            services.AddTransient<SubscriptionAwareViewModelBase>();
            return services;
        }
    }
}