using Microsoft.Extensions.Logging;
using Location.Photography.Application;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure;
using Location.Photography.Infrastructure.Services;
using Location.Photography.ViewModels;
using CommunityToolkit.Maui;
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

            // Add photography-specific services
            AddPhotographyServices(builder.Services);

#if DEBUG
            builder.Services.AddLogging(configure =>
            {
                configure.AddConsole();
                configure.SetMinimumLevel(LogLevel.Debug);
            });
#endif

            return builder.Build();
        }

        private static void AddPhotographyServices(IServiceCollection services)
        {
            // Register domain services
            services.AddSingleton<ISunCalculatorService, SunCalculatorService>();

            // Register viewmodels
            services.AddTransient<SunLocationViewModel>();
            services.AddTransient<SunCalculationsViewModel>();
        }
    }
}