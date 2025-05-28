// Location.Photography.Maui/MauiProgram.cs
using CommunityToolkit.Maui;
using Location.Core.Application;
using Location.Core.Application.Alerts;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Infrastructure;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Maui.Services;
using Location.Core.ViewModels;
using Location.Photography.Application;
using Location.Photography.Infrastructure;
using Location.Photography.Maui.Views;
using Location.Photography.ViewModels;
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
                })
             .ConfigureMauiHandlers(handlers =>
              {
#if ANDROID
                  handlers.AddHandler<Location.Photography.Maui.Controls.ColorTemperatureDial,
         SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
                  handlers.AddHandler<Location.Photography.Maui.Controls.TintDial,
                      SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
#else
            handlers.AddHandler<Location.Photography.Maui.Controls.ColorTemperatureDial, 
                SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
            handlers.AddHandler<Location.Photography.Maui.Controls.TintDial, 
                SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
#endif
              });
            // Core services
            builder.Services.AddSingleton<MauiAlertService>();
            builder.Services.AddSingleton<IAlertService>(sp => sp.GetRequiredService<MauiAlertService>());
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure();

            // Photography application and infrastructure
            builder.Services.AddPhotographyApplication();
            builder.Services.AddPhotographyInfrastructure();

            // MAUI platform services
            builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
            builder.Services.AddSingleton<IMediaService, MediaService>();
#if ANDROID
            Microsoft.Extensions.DependencyInjection.ServiceCollectionServiceExtensions.AddSingleton<Platforms.Android.ILightSensorService, Platforms.Android.LightSensorService>(builder.Services);
#endif
            // MediatR configuration
            var coreAssembly = System.Reflection.Assembly.Load("Location.Core.Application");
            var photographyAssembly = System.Reflection.Assembly.Load("Location.Photography.Application");
            builder.Services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(coreAssembly);
                cfg.RegisterServicesFromAssembly(photographyAssembly);
            });

            // Alert event handler
            builder.Services.AddTransient<INotificationHandler<AlertEvent>, AlertEventHandler>();

            // Core repositories
            builder.Services.AddSingleton<ITipTypeRepository, TipTypeRepository>();
            builder.Services.AddSingleton<ITipRepository, TipRepository>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            //builder.Services.AddSingleton<IServiceScopeFactory>();
            //builder.Services.AddTransient<IServiceProvider, ServiceProvider>();
            //builder.Services.AddSingleton(IServiceProviderFactory,)
            // Core ViewModels
            builder.Services.AddTransient<Core.ViewModels.LocationViewModel>();
            builder.Services.AddTransient<WeatherViewModel>();
            builder.Services.AddTransient<Core.ViewModels.LocationsViewModel>();
            builder.Services.AddTransient<Core.ViewModels.TipsViewModel>();

            // Photography ViewModels
            builder.Services.AddTransient<ExposureCalculatorViewModel>();
            builder.Services.AddTransient<SunCalculatorViewModel>();
            builder.Services.AddTransient<SunLocationViewModel>();
            builder.Services.AddTransient<SceneEvaluationViewModel>();
            builder.Services.AddTransient<SubscriptionSignUpViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<LightMeterViewModel>();

            // Core Pages
            builder.Services.AddSingleton<Location.Core.Maui.Views.AddLocation>();
            builder.Services.AddSingleton<Location.Core.Maui.Views.EditLocation>();
            builder.Services.AddSingleton<Location.Core.Maui.Views.LocationsPage>();
            builder.Services.AddSingleton<Location.Core.Maui.Views.TipsPage>();
            builder.Services.AddSingleton<Location.Core.Maui.Views.WeatherDisplay>();

            // Photography Pages
            builder.Services.AddTransient<UserOnboarding>();
            builder.Services.AddTransient<Views.Premium.ExposureCalculator>();
            builder.Services.AddTransient<Views.Premium.SunLocation>();
            builder.Services.AddTransient<Views.Professional.SunCalculator>();
            builder.Services.AddTransient<Views.Professional.SceneEvaluation>();
            builder.Services.AddTransient<Views.Settings>();
            builder.Services.AddSingleton<MainPage>();
            builder.Services.AddTransient<SubscriptionSignUpPage>();
            builder.Services.AddSingleton<Views.Premium.LightMeter>();
            builder.Services.AddTransient<Views.Premium.DummyPage>();
            
            // Database initializer
            builder.Services.AddTransient<DatabaseInitializer>();
                   
                  
    
                // Logging configuration
                builder.Logging.AddConsole();
                builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));
    
                // Initialize the database
                builder.Services.AddSingleton<IDatabaseContext, DatabaseContext>();
                builder.Services.AddSingleton<DatabaseInitializer>();
            builder.Services.AddTransient(sp => sp.GetRequiredService<DatabaseInitializer>());
    
                // Ensure the database is initialized at startup
                builder.Services.AddSingleton(sp => DatabaseSetup.EnsureDatabaseInitialized(sp));
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }

    public static class DatabaseSetup
    {
        public static async Task EnsureDatabaseInitialized(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
            await dbContext.InitializeDatabaseAsync();
        }
    }
}