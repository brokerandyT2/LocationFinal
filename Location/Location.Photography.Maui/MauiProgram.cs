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
using Location.Photography.ViewModels.Premium;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

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

            builder.Services.AddSingleton<MauiAlertService>();
            builder.Services.AddSingleton<IAlertService>(sp => sp.GetRequiredService<MauiAlertService>());
            // Register the core application layer
            builder.Services.AddApplication();

            // Register the infrastructure layer
            builder.Services.AddInfrastructure();

            // Register Photography application and infrastructure
            builder.Services.AddPhotographyApplication();
            builder.Services.AddPhotographyInfrastructure();

            // Register the database initializer for use during onboarding
            //builder.Services.AddDatabaseInitializer();

            // Register MAUI services
            builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
            builder.Services.AddSingleton<IMediaService, MediaService>();
            builder.Services.AddTransient<INotificationHandler<AlertEvent>, AlertEventHandler>();
        
           // builder.Services.AddTransient<INotificationHandler<AlertEvent>, AlertEventHandler>();

            // Register Core ViewModels
            builder.Services.AddTransient<ViewModels.LocationViewModel>();
            builder.Services.AddTransient<WeatherViewModel>();

            // Register Photography ViewModels
            builder.Services.AddTransient<ExposureCalculatorViewModel>();
            var coreAssembly = System.Reflection.Assembly.Load("Location.Core.Application");
            var photographyAssembly = System.Reflection.Assembly.Load("Location.Photography.Application");

            builder.Services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(coreAssembly);
                cfg.RegisterServicesFromAssembly(photographyAssembly);
            });
            // Register Core Pages
            builder.Services.AddTransient<Location.Core.Maui.Views.AddLocation>();
            builder.Services.AddTransient<Location.Core.Maui.Views.EditLocation>();
            builder.Services.AddTransient<Location.Core.Maui.Views.WeatherDisplay>();

            builder.Services.AddSingleton<Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository, Location.Core.Infrastructure.Data.Repositories.TipTypeRepository>();
            builder.Services.AddSingleton<Location.Core.Application.Common.Interfaces.Persistence.ITipRepository, Location.Core.Infrastructure.Data.Repositories.TipRepository>();
            builder.Services.AddSingleton<IAlertService, Location.Core.Infrastructure.Services.AlertingService>(); // Adjust class name if different
            builder.Services.AddSingleton<INavigationService, NavigationService>(); // Adjust class name if different
            // Register Photography Pages including UserOnboarding
            builder.Services.AddTransient<UserOnboarding>();
            builder.Services.AddTransient<Location.Photography.Maui.Views.Premium.ExposureCalculator>();
            builder.Services.AddTransient<Views.Settings>();
            builder.Services.AddTransient<Views.Professional.SunCalculator>();
            builder.Services.AddTransient<Views.Premium.SunLocation>();
            builder.Services.AddTransient<SunCalculatorViewModel>();
            builder.Services.AddTransient<Location.Core.Maui.Views.LocationsPage>();
            builder.Services.AddTransient<Core.ViewModels.LocationsViewModel>();
            builder.Services.AddTransient<Location.Photography.Maui.Views.Professional.SceneEvaluation>();
            builder.Services.AddTransient<Location.Photography.ViewModels.SceneEvaluationViewModel>();
            builder.Services.AddTransient<DatabaseInitializer>();
            builder.Services.AddTransient<Location.Core.Maui.Views.TipsPage>();
            builder.Services.AddTransient<Core.ViewModels.TipsViewModel>();


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