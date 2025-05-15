using Location.Core.Application;
using Location.Core.Application.Services;
using Location.Core.Infrastructure;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Maui;
using Location.Core.Application.Alerts;
namespace Location.Core.Maui
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

            // Register MAUI services
            builder.Services.AddSingleton<IAlertService, AlertService>();
            builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
            builder.Services.AddSingleton<IMediaService, MediaService>();

            builder.Services.AddTransient<INotificationHandler<AlertEvent>, AlertEventHandler>();

            // Register ViewModels 
            builder.Services.AddTransient<LocationViewModel>();

            // Register Pages
            builder.Services.AddTransient<Views.AddLocation>();

            return builder.Build();
        }
    }
}