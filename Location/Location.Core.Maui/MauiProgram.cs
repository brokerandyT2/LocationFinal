using CommunityToolkit.Maui;
using Location.Core.Application;
using Location.Core.Application.Services;
using Location.Core.Infrastructure;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using Location.Photography.ViewModels;
using Microsoft.Extensions.Logging;
using LocationViewModel = Location.Core.ViewModels.LocationViewModel;

namespace Location.Core.Maui
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture =System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
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

            // Register MAUI services
            builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
            builder.Services.AddSingleton<IMediaService, MediaService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddTransient<SceneEvaluationViewModel>();
            // Register ViewModels
            builder.Services.AddTransient<LocationViewModel>();
            builder.Services.AddTransient<WeatherViewModel>();

            // Register Pages
            builder.Services.AddTransient<Views.AddLocation>();
            builder.Services.AddTransient<Views.EditLocation>();
            builder.Services.AddTransient<Views.WeatherDisplay>();
            builder.Services.AddTransient<Views.LocationsPage>();
            builder.Services.AddTransient<Views.TipsPage>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}