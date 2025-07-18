﻿// Location.Photography.Maui/MauiProgram.cs - Complete implementation with Modal Registration
using Camera.MAUI;
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
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Notifications;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure;
using Location.Photography.Infrastructure.Repositories;
using Location.Photography.Infrastructure.Services;
using Location.Photography.Maui.Views;
using Location.Photography.Maui.Views.Premium;
using Location.Photography.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;

#if ANDROID

using Location.Photography.Maui.Platforms.Android.Handlers;

#endif

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
                .UseMauiCameraView()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .ConfigureMauiHandlers(handlers =>
                {
#if ANDROID
                    // handlers.AddHandler<Entry, CustomEntryHandler>();
                    handlers.AddHandler<Entry, NoUnderlineEntryHandler>();
                    handlers.AddHandler<Controls.ColorTemperatureDial, SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
                    handlers.AddHandler<Controls.TintDial, SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
#else
                    handlers.AddHandler<Location.Photography.Maui.Controls.ColorTemperatureDial,
                        SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
                    handlers.AddHandler<Location.Photography.Maui.Controls.TintDial,
                        SkiaSharp.Views.Maui.Handlers.SKCanvasViewHandler>();
#endif
                });

            // ==================== CORE SERVICES ====================
            // Alert service (must be registered before other services that depend on it)
            builder.Services.AddSingleton<MauiAlertService>();
            builder.Services.AddSingleton<IAlertService>(sp => sp.GetRequiredService<MauiAlertService>());

            // Core application and infrastructure layers
            builder.Services.AddApplication();
            builder.Services.AddInfrastructure();

            // Photography application and infrastructure layers
            builder.Services.AddPhotographyApplication();
            builder.Services.AddPhotographyInfrastructure();

            // ==================== PLATFORM SERVICES ====================
            // MAUI platform-specific services
            builder.Services.AddSingleton<IGeolocationService, GeolocationService>();
            builder.Services.AddSingleton<IMediaService, MediaService>();
            builder.Services.AddSingleton<INavigationService, NavigationService>();
            builder.Services.AddSingleton<IErrorDisplayService, ErrorDisplayService>();
            builder.Services.AddSingleton<IImageAnalysisService, ImageAnalysisService>();
            builder.Services.AddTransient<ITimezoneService, TimezoneService>();
            builder.Services.AddSingleton<IExifService, ExifService>();
            builder.Services.AddSingleton<IFOVCalculationService, FOVCalculationService>();
            builder.Services.AddSingleton<IPhoneCameraProfileRepository, PhoneCameraProfileRepository>();
            builder.Services.AddSingleton<ICameraDataService, CameraDataService>();
            builder.Services.AddSingleton<ICameraSensorProfileService, CameraSensorProfileService>();

#if ANDROID
            // Android-specific services
            builder.Services.AddSingleton<Platforms.Android.ILightSensorService, Platforms.Android.LightSensorService>();
#endif

            // ==================== MEDIATR CONFIGURATION ====================
            // MediatR with both Core and Photography assemblies
            var coreAssembly = System.Reflection.Assembly.Load("Location.Core.Application");
            var photographyAssembly = System.Reflection.Assembly.Load("Location.Photography.Application");

            builder.Services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(coreAssembly);
                cfg.RegisterServicesFromAssembly(photographyAssembly);
            });

            // Alert event handler
            builder.Services.AddTransient<INotificationHandler<AlertEvent>, AlertEventHandler>();
            builder.Services.AddTransient<INotificationHandler<LensCreatedNotification>, FieldOfView>();
            builder.Services.AddTransient<INotificationHandler<CameraCreatedNotification>, FieldOfView>();
            // ==================== REPOSITORIES ====================
            // Core repositories
            builder.Services.AddSingleton<ITipTypeRepository, TipTypeRepository>();
            builder.Services.AddSingleton<ITipRepository, TipRepository>();
            builder.Services.AddSingleton<ISettingRepository, SettingRepository>();
            builder.Services.AddSingleton<ILocationRepository, LocationRepository>();
            builder.Services.AddSingleton<IUserCameraBodyRepository, UserCameraBodyRepository>();

            // Photography repositories
            builder.Services.AddSingleton<ICameraBodyRepository, CameraBodyRepository>();
            builder.Services.AddSingleton<ILensRepository, LensRepository>();
            builder.Services.AddSingleton<ILensCameraCompatibilityRepository, LensCameraCompatibilityRepository>();
            builder.Services.AddSingleton<StellariumMeteorShowerParser>();
            builder.Services.AddSingleton<MeteorShowerDataService>();
            builder.Services.AddSingleton<IMeteorShowerDataService>(sp => sp.GetRequiredService<MeteorShowerDataService>());
            // Register the Application service first
            builder.Services.AddSingleton<Location.Photography.Application.Services.IEquipmentRecommendationService, Location.Photography.Infrastructure.Services.EquipmentRecommendationService>();

            // Create an adapter that wraps the Application service for ViewModels
            builder.Services.AddSingleton<Location.Photography.ViewModels.Interfaces.IEquipmentRecommendationService>(sp =>
            {
                var applicationService = sp.GetRequiredService<Location.Photography.Application.Services.IEquipmentRecommendationService>();
                // For now, we'll create a simple wrapper - you may need to implement this adapter class
                // Return a wrapper that delegates to the application service
                return new EquipmentRecommendationServiceAdapter(applicationService);
            });

            builder.Services.AddSingleton<Location.Photography.Application.Common.Interfaces.IAstroHourlyPredictionMappingService, Location.Photography.Infrastructure.Services.AstroHourlyPredictionMappingService>();

            builder.Services.AddSingleton<IPredictiveLightService, PredictiveLightService>();

            // ==================== CORE VIEWMODELS ====================
            // Core ViewModels (transient for fresh instances)
            builder.Services.AddTransient<Core.ViewModels.LocationViewModel>();
            builder.Services.AddTransient<LocationsViewModel>();
            builder.Services.AddTransient<TipsViewModel>();
            builder.Services.AddTransient<WeatherViewModel>();

            // ==================== PHOTOGRAPHY VIEWMODELS ====================
            // Photography ViewModels
            builder.Services.AddTransient<ExposureCalculatorViewModel>();
            builder.Services.AddTransient<SunCalculatorViewModel>();
            builder.Services.AddTransient<SunLocationViewModel>();
            builder.Services.AddTransient<SceneEvaluationViewModel>();
            builder.Services.AddTransient<SubscriptionSignUpViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();
            builder.Services.AddTransient<LightMeterViewModel>();
            builder.Services.AddTransient<EnhancedSunCalculatorViewModel>();
            // Explicitly register AstroPhotographyCalculatorViewModel with factory
            builder.Services.AddTransient<AstroPhotographyCalculatorViewModel>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var errorDisplayService = sp.GetRequiredService<IErrorDisplayService>();
                var astroCalculationService = sp.GetRequiredService<IAstroCalculationService>();
                var cameraBodyRepository = sp.GetRequiredService<ICameraBodyRepository>();
                var lensRepository = sp.GetRequiredService<ILensRepository>();
                var userCameraBodyRepository = sp.GetRequiredService<IUserCameraBodyRepository>();
                var equipmentRecommendationService = sp.GetRequiredService<Location.Photography.ViewModels.Interfaces.IEquipmentRecommendationService>();
                var predictiveLightService = sp.GetRequiredService<IPredictiveLightService>();
                var exposureCalculatorService = sp.GetRequiredService<IExposureCalculatorService>();
                var mappingService = sp.GetRequiredService<Location.Photography.Application.Common.Interfaces.IAstroHourlyPredictionMappingService>();
                var sunCalculatorService = sp.GetRequiredService<Domain.Services.ISunCalculatorService>();
                var meteorShowerDataService = sp.GetRequiredService<IMeteorShowerDataService>();
                var cameraDataService = sp.GetRequiredService<ICameraDataService>();
                var logger = sp.GetRequiredService<ILogger<AstroPhotographyCalculatorViewModel>>();
                var compat = sp.GetRequiredService<ILensCameraCompatibilityRepository>();
                return new AstroPhotographyCalculatorViewModel(
                    mediator,
                    errorDisplayService,
                    astroCalculationService,
                    cameraBodyRepository,
                    lensRepository,
                    userCameraBodyRepository,
                    equipmentRecommendationService,
                    predictiveLightService,
                    exposureCalculatorService,
                    mappingService, meteorShowerDataService, cameraDataService, logger, compat);
            });

            // ==================== CORE PAGES ====================
            // Core Pages - using factory pattern for proper DI
            builder.Services.AddTransient<Location.Core.Maui.Views.AddLocation>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var mediaService = sp.GetRequiredService<IMediaService>();
                var geoService = sp.GetRequiredService<IGeolocationService>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                return new Location.Core.Maui.Views.AddLocation(mediator, mediaService, geoService, errorService);
            });

            builder.Services.AddScoped<IAstroCalculationService>(provider =>
                 new AstroCalculationService(
                     provider.GetRequiredService<ILogger<AstroCalculationService>>(),
                     provider.GetRequiredService<ISunCalculatorService>()));

            builder.Services.AddTransient<Location.Core.Maui.Views.LocationsPage>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var navService = sp.GetRequiredService<INavigationService>();
                var mediaService = sp.GetRequiredService<IMediaService>();
                var geoService = sp.GetRequiredService<IGeolocationService>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                var weatherService = sp.GetRequiredService<IWeatherService>();
                return new Location.Core.Maui.Views.LocationsPage(mediator, navService, mediaService, geoService, errorService, weatherService);
            });

            builder.Services.AddTransient<Location.Core.Maui.Views.TipsPage>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                var tipRepo = sp.GetRequiredService<ITipRepository>();
                var tipTypeRepo = sp.GetRequiredService<ITipTypeRepository>();
                return new Location.Core.Maui.Views.TipsPage(mediator, errorService, tipRepo, tipTypeRepo);
            });

            builder.Services.AddTransient<Location.Core.Maui.Views.EditLocation>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var mediaService = sp.GetRequiredService<IMediaService>();
                var geoService = sp.GetRequiredService<IGeolocationService>();
                var navService = sp.GetRequiredService<INavigationService>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                var weatherService = sp.GetRequiredService<IWeatherService>();
                return new Location.Core.Maui.Views.EditLocation(mediator, mediaService, geoService, navService, errorService, weatherService);
            });

            builder.Services.AddTransient<Location.Core.Maui.Views.WeatherDisplay>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                return new Location.Core.Maui.Views.WeatherDisplay(mediator, errorService, 0);
            });

            // ==================== PHOTOGRAPHY PAGES ====================
            // Premium Pages
            builder.Services.AddTransient<ExposureCalculator>(sp =>
            {
                var exposureService = sp.GetService<Application.Services.IExposureCalculatorService>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                var mediator = sp.GetRequiredService<IMediator>();
                return new ExposureCalculator(exposureService, alertService, errorService, mediator);
            });
            builder.Services.AddTransient<AstroLocation>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var locationRepo = sp.GetRequiredService<Core.Application.Common.Interfaces.ILocationRepository>();
                var sunCalcService = sp.GetService<Domain.Services.ISunCalculatorService>();
                var settingRepo = sp.GetRequiredService<ISettingRepository>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                var timezoneService = sp.GetRequiredService<ITimezoneService>();

                return new Views.Premium.AstroLocation(mediator, alertService, locationRepo, sunCalcService, settingRepo, errorService, timezoneService);
            });

            // NEW: AstroLocationViewModel Registration
            builder.Services.AddTransient<AstroLocationViewModel>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var sunCalculatorService = sp.GetRequiredService<Domain.Services.ISunCalculatorService>();
                var errorDisplayService = sp.GetRequiredService<IErrorDisplayService>();
                var timezoneService = sp.GetRequiredService<ITimezoneService>();
                var logger = sp.GetRequiredService<ILogger<AstroLocationViewModel>>();

                return new AstroLocationViewModel(mediator, sunCalculatorService, errorDisplayService, timezoneService, null, null, logger);
            });
            builder.Services.AddTransient<FieldOfView>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var logger = sp.GetRequiredService<ILogger<FieldOfView>>();
                var fovCalculationService = sp.GetRequiredService<IFOVCalculationService>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var cameraDataService = sp.GetRequiredService<ICameraDataService>();
                var cameraSensorProfileService = sp.GetRequiredService<ICameraSensorProfileService>();
                var userCameraBodyRepository = sp.GetRequiredService<IUserCameraBodyRepository>();
                var serviceProvider = sp;
                return new FieldOfView(mediator, logger, fovCalculationService, alertService, cameraDataService, cameraSensorProfileService, userCameraBodyRepository, serviceProvider);
            });

            // Camera Lens Management Page
            builder.Services.AddTransient<CameraLensManagement>(sp =>
            {
                var cameraDataService = sp.GetRequiredService<ICameraDataService>();
                var compatibilityRepository = sp.GetRequiredService<ILensCameraCompatibilityRepository>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var logger = sp.GetRequiredService<ILogger<CameraLensManagement>>();
                var mediator = sp.GetRequiredService<IMediator>();
                var serviceProvider = sp;
                return new CameraLensManagement(cameraDataService, compatibilityRepository, alertService, logger, mediator, serviceProvider);
            });

            // Modal Pages for Field of View feature
            builder.Services.AddTransient<AddCameraModal>(sp =>
            {
                var cameraDataService = sp.GetRequiredService<ICameraDataService>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var logger = sp.GetRequiredService<ILogger<AddCameraModal>>();
                return new AddCameraModal(cameraDataService, alertService, logger);
            });

            builder.Services.AddTransient<AddLensModal>(sp =>
            {
                var cameraDataService = sp.GetRequiredService<ICameraDataService>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var logger = sp.GetRequiredService<ILogger<AddLensModal>>();
                return new AddLensModal(cameraDataService, alertService, logger);
            });

            builder.Services.AddTransient<SunLocation>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var locationRepo = sp.GetRequiredService<Core.Application.Common.Interfaces.ILocationRepository>();
                var sunCalcService = sp.GetService<Domain.Services.ISunCalculatorService>();
                var settingRepo = sp.GetRequiredService<ISettingRepository>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                var timezoneService = sp.GetRequiredService<ITimezoneService>();
                var exposureCalculatorService = sp.GetRequiredService<IExposureCalculatorService>();
                return new Views.Premium.SunLocation(mediator, alertService, locationRepo, sunCalcService, settingRepo, errorService, timezoneService, exposureCalculatorService);
            });

            builder.Services.AddTransient<Views.Professional.LightMeter>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var settingRepo = sp.GetRequiredService<ISettingRepository>();
                var expService = sp.GetRequiredService<IExposureCalculatorService>();
                var serviceProvider = sp;
                var sceneEvalService = sp.GetRequiredService<ISceneEvaluationService>();
#if ANDROID
                var lightSensorService = sp.GetRequiredService<Platforms.Android.ILightSensorService>();
                return new Views.Professional.LightMeter(mediator, alertService, settingRepo, lightSensorService, expService, sceneEvalService);
#else
                return new Views.Professional.LightMeter();
#endif
            });

            // Professional Pages
            builder.Services.AddTransient<Views.Professional.SceneEvaluation>(sp =>
            {
                var imageAnalysisService = sp.GetRequiredService<IImageAnalysisService>();
                var errorService = sp.GetRequiredService<IErrorDisplayService>();
                return new Views.Professional.SceneEvaluation(imageAnalysisService, errorService);
            });

            builder.Services.AddTransient<Views.Professional.SunCalculator>(sp =>
            {
                var viewModel = sp.GetRequiredService<EnhancedSunCalculatorViewModel>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var mediator = sp.GetRequiredService<IMediator>();
                var expCalc = sp.GetRequiredService<IExposureCalculatorService>();
                return new Views.Professional.SunCalculator(viewModel, alertService, mediator, expCalc);
            });

            builder.Services.AddTransient<Views.Professional.AstroPhotographyCalculator>(sp =>
            {
                var viewModel = sp.GetRequiredService<AstroPhotographyCalculatorViewModel>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var mediator = sp.GetRequiredService<IMediator>();
                var logger = sp.GetRequiredService<ILogger<Views.Professional.AstroPhotographyCalculator>>();
                var serviceProvider = sp.GetService<IServiceProvider>();
                return new Views.Professional.AstroPhotographyCalculator(viewModel, logger, serviceProvider, alertService, mediator);
            });

            // Settings and Onboarding Pages
            builder.Services.AddTransient<Views.Settings>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var alertService = sp.GetRequiredService<IAlertService>();
                var settingRepo = sp.GetRequiredService<ISettingRepository>();
                var serviceProvider = sp.GetRequiredService<IServiceProvider>();
                return new Views.Settings(mediator, alertService, settingRepo, serviceProvider);
            });
            builder.Services.AddTransient<CameraEvaluation>(sp =>
            {
                var mediator = sp.GetRequiredService<IMediator>();
                var logger = sp.GetRequiredService<ILogger<CameraEvaluation>>();
                var serviceProvider = sp;
                return new CameraEvaluation(mediator, logger, serviceProvider);
            });
            builder.Services.AddTransient<UserOnboarding>(sp =>
            {
                var alertService = sp.GetRequiredService<IAlertService>();
                var dbInitializer = sp.GetRequiredService<DatabaseInitializer>();
                var logger = sp.GetRequiredService<ILogger<UserOnboarding>>();
                var serviceProvider = sp;
                return new UserOnboarding(alertService, dbInitializer, logger, serviceProvider);
            });

            // Subscription and Main Pages
            builder.Services.AddTransient<SubscriptionSignUpPage>(sp =>
            {
                var serviceProvider = sp;
                var alertService = sp.GetRequiredService<IAlertService>();
                var logger = sp.GetRequiredService<ILogger<SubscriptionSignUpPage>>();
                var viewModel = sp.GetRequiredService<SubscriptionSignUpViewModel>();
                return new SubscriptionSignUpPage(serviceProvider, alertService, logger, viewModel);
            });

            builder.Services.AddSingleton<MainPage>();


            // ==================== APP SHELL ====================
            // AppShell with all dependencies
            builder.Services.AddSingleton<AppShell>(sp =>
            {
                var serviceProvider = sp;
                var subscriptionService = sp.GetService<ISubscriptionStatusService>();
                var logger = sp.GetRequiredService<ILogger<AppShell>>();
                return new AppShell(serviceProvider, subscriptionService, logger);
            });

            // ==================== DATABASE SERVICES ====================
            // Database context and initializer
            builder.Services.AddSingleton<IDatabaseContext, DatabaseContext>();
            builder.Services.AddSingleton<DatabaseInitializer>();

            // ==================== LOGGING CONFIGURATION ====================
            // Logging setup
            builder.Logging.AddConsole();
            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

#if DEBUG
            builder.Logging.AddDebug();
            builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
           builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

            // ==================== BUILD AND INITIALIZE ====================
            var app = builder.Build();

            // Initialize database on startup
            Task.Run(async () =>
            {
                try
                {
                    using var scope = app.Services.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<IDatabaseContext>();
                    await dbContext.InitializeDatabaseAsync();
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetService<ILogger>();
                    logger?.LogError(ex, "Failed to initialize database on startup");
                }
            });

            return app;
        }
    }

    // Helper class for database setup
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