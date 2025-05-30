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
using Microsoft.Extensions.Logging;
using System.Reflection;
using ISubscriptionRepository = Location.Photography.Application.Common.Interfaces.ISubscriptionRepository;

namespace Location.Photography.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyInfrastructure(this IServiceCollection services)
        {
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

            return services;
        }
    }
}