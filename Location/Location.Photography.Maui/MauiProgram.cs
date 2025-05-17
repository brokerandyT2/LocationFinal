// Location.Photography.Maui/MauiProgram.cs
using CommunityToolkit.Maui;
using Location.Core.Application;
using Location.Core.Application.Alerts;
using Location.Core.Application.Services;
using Location.Core.Infrastructure;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using Location.Photography.Application;
using Location.Photography.Infrastructure;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Premium;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
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

            // Register Photography application and infrastructure
            builder.Services.AddPhotographyApplication();
            builder.Services.AddPhotographyInfrastructure();

            // Register MAUI services
            builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
            builder.Services.AddSingleton<IMediaService, MediaService>();
            builder.Services.AddTransient<INotificationHandler<AlertEvent>, AlertEventHandler>();

            // Register Core ViewModels
            builder.Services.AddTransient<ViewModels.LocationViewModel>();
            builder.Services.AddTransient<WeatherViewModel>();

            // Register Photography ViewModels
            builder.Services.AddTransient<ExposureCalculatorViewModel>();

            // Register Core Pages
            builder.Services.AddTransient<Location.Core.Maui.Views.AddLocation>();
            builder.Services.AddTransient<Location.Core.Maui.Views.EditLocation>();
            builder.Services.AddTransient<Location.Core.Maui.Views.WeatherDisplay>();

            // Register Photography Pages
            builder.Services.AddTransient<Location.Photography.Maui.Views.Premium.ExposureCalculator>();
            builder.Services.AddTransient<Views.Premium.SunCalculator>();
            builder.Services.AddTransient<Views.Premium.SunLocation>();
            builder.Services.AddTransient<SunCalculatorViewModel>();
            builder.Services.AddTransient<Views.Premium.SunCalculator>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}