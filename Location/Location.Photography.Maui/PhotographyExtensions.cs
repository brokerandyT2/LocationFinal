using Location.Photography.Application;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure;
using Location.Photography.Infrastructure.Services;
using Location.Photography.ViewModels;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Maui
{
    public static class PhotographyExtensions
    {
        /// <summary>
        /// Adds the Photography module services and configurations to the MAUI application
        /// </summary>
        public static MauiAppBuilder UsePhotography(this MauiAppBuilder builder)
        {
            // Add photography-specific services
            builder.Services.AddPhotographyApplication();
            builder.Services.AddPhotographyInfrastructure();

            // Register domain services
            builder.Services.AddSingleton<ISunCalculatorService, SunCalculatorService>();

            // Register viewmodels
            builder.Services.AddTransient<SunLocationViewModel>();
            builder.Services.AddTransient<SunCalculationsViewModel>();

            // Register pages - assuming you have these view classes
            builder.Services.AddTransient<Views.Premium.SunCalculator>();
            builder.Services.AddTransient<Views.Premium.SunLocation>();

            return builder;
        }

        /// <summary>
        /// Registers view navigation services for the Photography module
        /// </summary>
        public static MauiAppBuilder ConfigurePhotographyNavigation(this MauiAppBuilder builder)
        {
            // Add navigation-specific registrations here
            return builder;
        }
    }
}