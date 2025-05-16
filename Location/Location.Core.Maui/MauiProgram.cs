using CommunityToolkit.Maui;
using Location.Core.Application;
using Location.Core.Application.Alerts;
using Location.Core.Application.Services;
using Location.Core.Infrastructure;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using MediatR;

namespace Location.Core.Maui
{
    public static class LocationCoreExtensions
    {
        /// <summary>
        /// Adds the Location Core services and configurations to the MAUI application
        /// </summary>
        public static MauiAppBuilder UseLocationCore(this MauiAppBuilder builder)
        {
            builder
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

            // Register the core application layer
            builder.Services.AddApplication();

            // Register the infrastructure layer
            builder.Services.AddInfrastructure();

            // Register MAUI services
            builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
            builder.Services.AddSingleton<IMediaService, MediaService>();

            builder.Services.AddTransient<INotificationHandler<AlertEvent>, AlertEventHandler>();
            builder.Services.AddTransient<LocationViewModel>();
            builder.Services.AddTransient<WeatherViewModel>();

            // Register Pages
            builder.Services.AddTransient<Views.AddLocation>();
            builder.Services.AddTransient<Views.EditLocation>();
            builder.Services.AddTransient<Views.WeatherDisplay>();

            // Register ViewModels 
            builder.Services.AddTransient<LocationViewModel>();

            return builder;
        }

        /// <summary>
        /// Registers view navigation services for the Location Core
        /// </summary>
        public static MauiAppBuilder ConfigureLocationCoreNavigation(this MauiAppBuilder builder)
        {
            // Add navigation-specific registrations here
            // This method can be called separately if needed

            return builder;
        }

        /// <summary>
        /// Optional method to add platform-specific implementations
        /// </summary>
        public static MauiAppBuilder ConfigureLocationCorePlatformServices(this MauiAppBuilder builder)
        {
#if ANDROID
            // Android-specific registrations
#elif IOS
            // iOS-specific registrations
#elif WINDOWS
            // Windows-specific registrations
#elif MACCATALYST
            // MacCatalyst-specific registrations
#endif

            return builder;
        }
    }
}