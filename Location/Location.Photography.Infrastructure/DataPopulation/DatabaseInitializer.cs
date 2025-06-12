// Location.Photography.Infrastructure/DataPopulation/DatabaseInitializer.cs
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Services;
using Location.Core.Domain.Entities;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Enums;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure
{
    public class DatabaseInitializer
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DatabaseInitializer> _logger;
        private readonly IAlertService _alertService;

        public DatabaseInitializer(
            IUnitOfWork unitOfWork,
            ILogger<DatabaseInitializer> logger,
            IAlertService alertService)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
        }

        /// <summary>
        /// Initializes database and populates with static data (tip types, sample locations, base settings)
        /// This is called during app startup and does not include user-specific settings
        /// </summary>
        public async Task InitializeDatabaseWithStaticDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Starting database initialization with static data");

                // Initialize database structure
                var databaseContext = (_unitOfWork as Location.Core.Infrastructure.UnitOfWork.UnitOfWork)?.GetDatabaseContext();
                if (databaseContext != null)
                {
                    await databaseContext.InitializeDatabaseAsync().ConfigureAwait(false);
                }
                else
                {
                    _logger.LogWarning("DatabaseContext not available through UnitOfWork");
                }

                // Parallelize independent database operations to improve performance
                var initializationTasks = new List<Task>
                {
                    CreateTipTypesAsync(cancellationToken),
                    CreateSampleLocationsAsync(cancellationToken),
                    CreateBaseSettingsAsync(cancellationToken),
                    CreateCameraSensorProfilesAsync(cancellationToken)
                };

                await Task.WhenAll(initializationTasks).ConfigureAwait(false);

                _logger.LogInformation("Database initialization with static data completed successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Database initialization was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database initialization with static data");
                throw;
            }
        }

        /// <summary>
        /// Creates user-specific settings based on onboarding form input
        /// This is called when the user completes the onboarding process
        /// </summary>
        public async Task CreateUserSettingsAsync(
            string hemisphere,
            string tempFormat,
            string dateFormat,
            string timeFormat,
            string windDirection,
            string email,
            string guid,
            CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("Creating user-specific settings");

                var userSettings = new List<(string Key, string Value, string Description)>
                {
                    // User-specific settings from onboarding form
                    (MagicStrings.Hemisphere, hemisphere, "User's hemisphere (north/south)"),
                    (MagicStrings.WindDirection, windDirection, "Wind direction setting (towardsWind/withWind)"),
                    (MagicStrings.TimeFormat, timeFormat, "Time format (12h/24h)"),
                    (MagicStrings.DateFormat, dateFormat, "Date format (US/International)"),
                    (MagicStrings.TemperatureType, tempFormat, "Temperature format (F/C)"),
                    (MagicStrings.Email, email, "User's email address"),
                    (MagicStrings.UniqueID, guid, "Unique identifier for the installation")
                };

                // Process user settings
                var settingTasks = userSettings.Select(async settingData =>
                {
                    var (key, value, description) = settingData;
                    var setting = new Setting(key, value, description);
                    var result = await _unitOfWork.Settings.CreateAsync(setting).ConfigureAwait(false);

                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning("Failed to create user setting {Key}: {Error}", key, result.ErrorMessage);
                    }
                });

                await Task.WhenAll(settingTasks).ConfigureAwait(false);

                _logger.LogInformation("Created {Count} user-specific settings", userSettings.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user-specific settings");
                throw;
            }
        }

        /// <summary>
        /// Legacy method for backward compatibility - now calls both static and user data initialization
        /// </summary>
        public async Task InitializeDatabaseAsync(
            CancellationToken ctx,
            string hemisphere = "north",
            string tempFormat = "F",
            string dateFormat = "MMM/dd/yyyy",
            string timeFormat = "hh:mm tt",
            string windDirection = "towardsWind",
            string email = "",
            string guid = ""
            )
        {
            try
            {
                // Initialize with static data first
                await InitializeDatabaseWithStaticDataAsync(ctx).ConfigureAwait(false);

                // Then create user settings
                await CreateUserSettingsAsync(hemisphere, tempFormat, dateFormat, timeFormat, windDirection, email, guid, ctx).ConfigureAwait(false);

                _logger.LogInformation("Complete database initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during complete database initialization");
                await _alertService.ShowErrorAlertAsync("Failed to initialize database: " + ex.Message, "Error").ConfigureAwait(false);
                throw;
            }
        }

        private async Task CreateTipTypesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var tipTypeNames = new[]
                {
                   "Landscape", "Silouette", "Building", "Person", "Baby", "Animals",
                   "Blury Water", "Night", "Blue Hour", "Golden Hour", "Sunset"
               };

                // Process tip types in batches to improve performance and reduce database contention
                const int batchSize = 3;
                for (int i = 0; i < tipTypeNames.Length; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = tipTypeNames.Skip(i).Take(batchSize);
                    var batchTasks = batch.Select(async name =>
                    {
                        var tipType = new TipType(name);
                        tipType.SetLocalization("en-US");

                        // Create tip type in repository
                        var typeResult = await _unitOfWork.TipTypes.CreateEntityAsync(tipType).ConfigureAwait(false);

                        if (!typeResult.IsSuccess || typeResult.Data == null)
                        {
                            _logger.LogWarning("Failed to create tip type: {Name}", name);
                            return;
                        }

                        // Create a sample tip for each type
                        var tip = new Tip(
                            typeResult.Data.Id,
                            $"How to Take Great {name} Photos",
                            "Text of the tip would appear here. Zombie ipsum reversus ab viral inferno, nam rick grimes malum cerebro. De carne lumbering animata corpora quaeritis. Summus brains sit​​, morbo vel maleficia? De apocalypsi gorger omero undead survivor dictum mauris."
                        );
                        tip.UpdatePhotographySettings("f/1", "1/125", "50");
                        tip.SetLocalization("en-US");

                        var tipResult = await _unitOfWork.Tips.CreateAsync(tip).ConfigureAwait(false);
                        if (!tipResult.IsSuccess)
                        {
                            _logger.LogWarning("Failed to create tip for type {Name}: {Error}", name, tipResult.ErrorMessage);
                        }
                    });

                    await Task.WhenAll(batchTasks).ConfigureAwait(false);
                }

                _logger.LogInformation("Created {Count} tip types with sample tips", tipTypeNames.Length);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tip types");
                throw;
            }
        }

        private async Task CreateSampleLocationsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var sampleLocations = new List<(string Title, string Description, double Latitude, double Longitude, string Photo)>
               {
                   (
                       "Soldiers and Sailors Monument",
                       "Located in the heart of downtown in Monument Circle, it was originally designed to honor Indiana's Civil War veterans. It now commemorates the valor of Hoosier veterans who served in all wars prior to WWI, including the Revolutionary War, the War of 1812, the Mexican War, the Civil War, the Frontier Wars and the Spanish-American War. One of the most popular parts of the monument is the observation deck with a 360-degree view of the city skyline from 275 feet up.",
                       39.7685, -86.1580,
                       "s_and_sm_new.jpg"
                   ),
                   (
                       "The Bean",
                       "What is The Bean?\r\nThe Bean is a work of public art in the heart of Chicago. The sculpture, which is officially titled Cloud Gate, is one of the world's largest permanent outdoor art installations. The monumental work was unveiled in 2004 and quickly became of the Chicago's most iconic sights.",
                       41.8827, -87.6233,
                       "chicagobean.jpg"
                   ),
                   (
                       "Golden Gate Bridge",
                       "The Golden Gate Bridge is a suspension bridge spanning the Golden Gate strait, the one-mile-wide (1.6 km) channel between San Francisco Bay and the Pacific Ocean. The strait is the entrance to San Francisco Bay from the Pacific Ocean. The bridge connects the city of San Francisco, California, to Marin County, carrying both U.S. Route 101 and California State Route 1 across the strait.",
                       37.8199, -122.4783,
                       "ggbridge.jpg"
                   ),
                   (
                       "Gateway Arch",
                       "The Gateway Arch is a 630-foot (192 m) monument in St. Louis, Missouri, that commemorates Thomas Jefferson and the westward expansion of the United States. The arch is the centerpiece of the Gateway Arch National Park and is the tallest arch in the world.",
                       38.6247, -90.1848,
                       "stlarch.jpg"
                   )
               };

                // Process locations in parallel to improve performance
                var locationTasks = sampleLocations.Select(async locationData =>
                {
                    var (title, description, latitude, longitude, photo) = locationData;

                    var location = new Core.Domain.Entities.Location(
                        title,
                        description,
                        new Core.Domain.ValueObjects.Coordinate(latitude, longitude),
                        new Core.Domain.ValueObjects.Address("", "")
                    );

                    if (!string.IsNullOrEmpty(photo))
                    {
                        location.AttachPhoto(photo);
                    }

                    var result = await _unitOfWork.Locations.CreateAsync(location).ConfigureAwait(false);
                    if (!result.IsSuccess)
                    {
                        _logger.LogWarning("Failed to create location {Title}: {Error}", title, result.ErrorMessage);
                    }
                });

                await Task.WhenAll(locationTasks).ConfigureAwait(false);

                _logger.LogInformation("Created {Count} sample locations", sampleLocations.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating sample locations");
                throw;
            }
        }

        /// <summary>
        /// Creates base application settings that are not user-specific
        /// </summary>
        private async Task CreateBaseSettingsAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var baseSettings = new List<(string Key, string Value, string Description)>
                {
                    // Application settings (not user-specific)
                    /*  (MagicStrings.FirstName, "", "User's first name"),
                      (MagicStrings.LastName, "", "User's last name"),
                      (MagicStrings.LastBulkWeatherUpdate, DateTime.Now.AddDays(-2).ToString(), "Timestamp of last bulk weather update"),
                      (MagicStrings.DefaultLanguage, "en-US", "Default language setting"),
                      (MagicStrings.CameraRefresh, "500", "Camera refresh rate in milliseconds"),
                      (MagicStrings.AppOpenCounter, "1", "Number of times the app has been opened"),
                      (MagicStrings.WeatherURL, "https://api.openweathermap.org/data/3.0/onecall", "Weather API URL"),
                      (MagicStrings.Weather_API_Key, "aa24f449cced50c0491032b2f955d610", "Weather API key"),
                      (MagicStrings.FreePremiumAdSupported, "false", "Whether the app is running in ad-supported mode"),
                      (MagicStrings.DeviceInfo, "", "Device information") */
                };

                // Add build-specific settings
#if DEBUG
                var debugSettings = new List<(string Key, string Value, string Description)>
                {
                    // Debug mode settings - features already viewed and premium subscription
                    (MagicStrings.SettingsViewed, MagicStrings.True_string, "Whether the settings page has been viewed"),
                    (MagicStrings.HomePageViewed, MagicStrings.True_string, "Whether the home page has been viewed"),
                    (MagicStrings.LocationListViewed, MagicStrings.True_string, "Whether the location list has been viewed"),
                    (MagicStrings.TipsViewed, MagicStrings.True_string, "Whether the tips page has been viewed"),
                    (MagicStrings.ExposureCalcViewed, MagicStrings.True_string, "Whether the exposure calculator has been viewed"),
                    (MagicStrings.LightMeterViewed, MagicStrings.True_string, "Whether the light meter has been viewed"),
                    (MagicStrings.SceneEvaluationViewed, MagicStrings.True_string, "Whether the scene evaluation has been viewed"),
                    (MagicStrings.AddLocationViewed, MagicStrings.True_string, "Whether the add location page has been viewed"),
                    (MagicStrings.WeatherDisplayViewed, MagicStrings.True_string, "Whether the weather display has been viewed"),
                    (MagicStrings.SunCalculatorViewed, MagicStrings.True_string, "Whether the sun calculator has been viewed"),
                    (MagicStrings.SunLocationViewed, MagicStrings.True_string, "Whether the SunLocation Page has been viewed."),
                    (MagicStrings.ExposureCalcAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last exposure calculator ad view"),
                    (MagicStrings.LightMeterAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last light meter ad view"),
                    (MagicStrings.SceneEvaluationAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last scene evaluation ad view"),
                    (MagicStrings.SunCalculatorViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last sun calculator ad view"),
                    (MagicStrings.SunLocationAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last sun location ad view"),
                    (MagicStrings.WeatherDisplayAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last weather display ad view"),
                    (MagicStrings.SubscriptionType, MagicStrings.Premium, "Subscription type (Free/Premium)"),
                    (MagicStrings.SubscriptionExpiration, DateTime.Now.AddYears(1).ToString("yyyy-MM-dd HH:mm:ss"), "Subscription expiration date"),
                    (MagicStrings.SubscriptionProductId, "premium_yearly_subscription", "Subscription product ID"),
                    (MagicStrings.SubscriptionPurchaseDate, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "Subscription purchase date"),
                    (MagicStrings.SubscriptionTransactionId, $"debug_transaction_{Guid.NewGuid():N}", "Subscription transaction ID"),
                    (MagicStrings.AdGivesHours, "24", "Hours of premium access granted per ad view"),
                    (MagicStrings.LastUploadTimeStamp, DateTime.Now.ToString(), "Last Time that data was backed up to cloud")
                };
                baseSettings.AddRange(debugSettings);
#else
                var releaseSettings = new List<(string Key, string Value, string Description)>
                {
                    // Release mode settings - features not viewed and expired subscription
                    (MagicStrings.SettingsViewed, MagicStrings.False_string, "Whether the settings page has been viewed"),
                    (MagicStrings.HomePageViewed, MagicStrings.False_string, "Whether the home page has been viewed"),
                    (MagicStrings.LocationListViewed, MagicStrings.False_string, "Whether the location list has been viewed"),
                    (MagicStrings.TipsViewed, MagicStrings.False_string, "Whether the tips page has been viewed"),
                    (MagicStrings.ExposureCalcViewed, MagicStrings.False_string, "Whether the exposure calculator has been viewed"),
                    (MagicStrings.LightMeterViewed, MagicStrings.False_string, "Whether the light meter has been viewed"),
                    (MagicStrings.SceneEvaluationViewed, MagicStrings.False_string, "Whether the scene evaluation has been viewed"),
                    (MagicStrings.AddLocationViewed, MagicStrings.False_string, "Whether the add location page has been viewed"),
                    (MagicStrings.WeatherDisplayViewed, MagicStrings.False_string, "Whether the weather display has been viewed"),
                    (MagicStrings.SunCalculatorViewed, MagicStrings.False_string, "Whether the sun calculator has been viewed"),
                    (MagicStrings.SunLocationViewed, MagicStrings.False_string, "Whether the SunLocation Page has been viewed."),
                    (MagicStrings.ExposureCalcAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last exposure calculator ad view"),
                    (MagicStrings.LightMeterAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last light meter ad view"),
                    (MagicStrings.SceneEvaluationAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last scene evaluation ad view"),
                    (MagicStrings.SunCalculatorViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last sun calculator ad view"),
                    (MagicStrings.SunLocationAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last sun location ad view"),
                    (MagicStrings.WeatherDisplayAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last weather display ad view"),
                    (MagicStrings.SubscriptionType, MagicStrings.Free, "Subscription type (Free/Premium)"),
                    (MagicStrings.SubscriptionExpiration, DateTime.Now.AddDays(-3).ToString("yyyy-MM-dd HH:mm:ss"), "Subscription expiration date"),
                    ("SubscriptionProductId", "", "Subscription product ID"),
                    ("SubscriptionPurchaseDate", "", "Subscription purchase date"),
                    ("SubscriptionTransactionId", "", "Subscription transaction ID"),
                    (MagicStrings.AdGivesHours, "12", "Hours of premium access granted per ad view"),
                    (MagicStrings.LastUploadTimeStamp, DateTime.Now.AddDays(-1).ToString(), "Last Time that data was backed up to cloud")
                };
                baseSettings.AddRange(releaseSettings);
#endif

                // Process base settings in batches to improve performance and reduce database contention
                const int batchSize = 10;
                for (int i = 0; i < baseSettings.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = baseSettings.Skip(i).Take(batchSize);
                    var settingTasks = batch.Select(async settingData =>
                    {
                        var (key, value, description) = settingData;
                        var setting = new Setting(key, value, description);
                        var result = await _unitOfWork.Settings.CreateAsync(setting).ConfigureAwait(false);

                        if (!result.IsSuccess)
                        {
                            _logger.LogWarning("Failed to create base setting {Key}: {Error}", key, result.ErrorMessage);
                        }
                    });

                    await Task.WhenAll(settingTasks).ConfigureAwait(false);
                }

                _logger.LogInformation("Created {Count} base settings", baseSettings.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating base settings");
                throw;
            }
        }
        private async Task CreateCameraSensorProfilesAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var cameraProfiles = new List<(string Name, string Brand, string SensorType, double SensorWidth, double SensorHeight)>
       {
           // 2010 Cameras
           ("Canon EOS 550D", "Canon", "Crop", 22.3, 14.9),
           ("Nikon D3100", "Nikon", "Crop", 23.1, 15.4),
           ("Canon EOS 7D", "Canon", "Crop", 22.3, 14.9),
           ("Nikon D7000", "Nikon", "Crop", 23.6, 15.6),
           ("Sony Alpha A500", "Sony", "Crop", 23.5, 15.6),
           ("Pentax K-x", "Pentax", "Crop", 23.6, 15.8),
           ("Canon EOS 5D Mark II", "Canon", "Full Frame", 36.0, 24.0),
           ("Nikon D3s", "Nikon", "Full Frame", 36.0, 23.9),
           ("Sony Alpha A850", "Sony", "Full Frame", 35.9, 24.0),
           ("Pentax K-7", "Pentax", "Crop", 23.4, 15.6),
           ("Canon EOS 1000D", "Canon", "Crop", 22.2, 14.8),
           ("Nikon D90", "Nikon", "Crop", 23.6, 15.8),
           ("Sony Alpha A230", "Sony", "Crop", 23.5, 15.7),
           ("Pentax K20D", "Pentax", "Crop", 23.4, 15.6),
           ("Canon EOS 50D", "Canon", "Crop", 22.3, 14.9),

           // 2011 Cameras
           ("Canon EOS 600D", "Canon", "Crop", 22.3, 14.9),
           ("Nikon D5100", "Nikon", "Crop", 23.6, 15.6),
           ("Sony Alpha A35", "Sony", "Crop", 23.5, 15.6),
           ("Pentax K-5", "Pentax", "Crop", 23.7, 15.7),
           ("Canon EOS 1100D", "Canon", "Crop", 22.2, 14.7),
           ("Nikon D7000", "Nikon", "Crop", 23.6, 15.6),
           ("Sony Alpha A55", "Sony", "Crop", 23.5, 15.6),
           ("Pentax K-r", "Pentax", "Crop", 23.6, 15.8),
           ("Canon EOS 5D Mark II", "Canon", "Full Frame", 36.0, 24.0),
           ("Nikon D3s", "Nikon", "Full Frame", 36.0, 23.9),
           ("Sony Alpha A900", "Sony", "Full Frame", 35.9, 24.0),
           ("Pentax 645D", "Pentax", "Medium Format", 44.0, 33.0),
           ("Canon EOS 60D", "Canon", "Crop", 22.3, 14.9),
           ("Nikon D3100", "Nikon", "Crop", 23.1, 15.4),
           ("Sony Alpha A290", "Sony", "Crop", 23.5, 15.7),
           // 2012 Cameras - Launch year priority for de-duplication
("Canon EOS 650D", "Canon", "Crop", 22.3, 14.9),
("Nikon D5200", "Nikon", "Crop", 23.5, 15.6),
("Canon EOS 6D", "Canon", "Full Frame", 35.8, 23.9),
("Nikon D600", "Nikon", "Full Frame", 35.9, 24.0),
("Nikon D800", "Nikon", "Full Frame", 35.9, 24.0),
("Canon EOS 5D Mark III", "Canon", "Full Frame", 36.0, 24.0),
("Nikon D3200", "Nikon", "Crop", 23.2, 15.4),
("Canon EOS 60D", "Canon", "Crop", 22.3, 14.9),
("Pentax K-30", "Pentax", "Crop", 23.7, 15.7),
("Sony Alpha A57", "Sony", "Crop", 23.5, 15.6),
("Canon EOS 1100D", "Canon", "Crop", 22.2, 14.7),
("Sony Alpha A65", "Sony", "Crop", 23.5, 15.6),
("Pentax K-01", "Pentax", "Crop", 23.7, 15.7),
("Canon EOS 1D X", "Canon", "Full Frame", 36.0, 24.0),
("Sony Alpha A99", "Sony", "Full Frame", 35.9, 24.0),

// 2013 Cameras
("Canon EOS 700D", "Canon", "Crop", 22.3, 14.9),
("Nikon D7100", "Nikon", "Crop", 23.5, 15.6),
("Canon EOS 100D", "Canon", "Crop", 22.3, 14.9),
("Nikon D5300", "Nikon", "Crop", 23.5, 15.6),
("Sony Alpha A58", "Sony", "Crop", 23.5, 15.6),
("Pentax K-3", "Pentax", "Crop", 23.5, 15.6),
("Nikon D610", "Nikon", "Full Frame", 35.9, 24.0),
("Canon EOS 70D", "Canon", "Crop", 22.3, 14.9),
("Nikon D800E", "Nikon", "Full Frame", 35.9, 24.0),
("Sony Alpha A77 II", "Sony", "Crop", 23.5, 15.6),
("Canon EOS 1200D", "Canon", "Crop", 22.2, 14.8),
("Pentax K-50", "Pentax", "Crop", 23.7, 15.7),
("Nikon D3300", "Nikon", "Crop", 23.5, 15.6),
("Sony Alpha A7", "Sony", "Full Frame", 35.8, 23.9),
("Sony Alpha A7R", "Sony", "Full Frame", 35.9, 24.0),

// 2014 Cameras
("Canon EOS 750D", "Canon", "Crop", 22.3, 14.9),
("Nikon D750", "Nikon", "Full Frame", 35.9, 24.0),
("Canon EOS 760D", "Canon", "Crop", 22.3, 14.9),
("Nikon D810", "Nikon", "Full Frame", 35.9, 24.0),
("Canon EOS 7D Mark II", "Canon", "Crop", 22.4, 15.0),
("Sony Alpha A7S", "Sony", "Full Frame", 35.6, 23.8),
("Sony Alpha A7 II", "Sony", "Full Frame", 35.9, 24.0),
("Canon EOS 5DS", "Canon", "Full Frame", 36.0, 24.0),
("Nikon D4S", "Nikon", "Full Frame", 36.0, 23.9),
("Nikon D5500", "Nikon", "Crop", 23.5, 15.6),
("Sony Alpha A3000", "Sony", "Crop", 23.2, 15.4),
("Pentax K-S1", "Pentax", "Crop", 23.5, 15.6),
("Fujifilm X-T1", "Fujifilm", "Crop", 23.6, 15.6),
("Panasonic Lumix DMC-GH4", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Olympus OM-D E-M1", "Olympus", "Micro Four Thirds", 17.3, 13.0),

// 2015 Cameras
("Canon EOS 5DS R", "Canon", "Full Frame", 36.0, 24.0),
("Nikon D7200", "Nikon", "Crop", 23.5, 15.6),
("Pentax K-3 II", "Pentax", "Crop", 23.5, 15.6),
("Sony Alpha A68", "Sony", "Crop", 23.5, 15.6),
("Canon EOS 1300D", "Canon", "Crop", 22.3, 14.9),
("Pentax K-S2", "Pentax", "Crop", 23.5, 15.6),
("Nikon D5", "Nikon", "Full Frame", 35.9, 23.9),
("Sony Alpha A7R II", "Sony", "Full Frame", 35.9, 24.0),
("Sony Alpha A7S II", "Sony", "Full Frame", 35.6, 23.8),
("Fujifilm X-T10", "Fujifilm", "Crop", 23.6, 15.6),
("Panasonic Lumix DMC-G7", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Olympus OM-D E-M5 Mark II", "Olympus", "Micro Four Thirds", 17.3, 13.0),
("Canon EOS M3", "Canon", "Crop", 22.3, 14.9),
("Samsung NX1", "Samsung", "Crop", 23.5, 15.7),
("Leica Q", "Leica", "Full Frame", 36.0, 24.0),

// 2016 Cameras
("Canon EOS 80D", "Canon", "Crop", 22.3, 14.9),
("Canon EOS 5D Mark IV", "Canon", "Full Frame", 36.0, 24.0),
("Nikon D500", "Nikon", "Crop", 23.5, 15.6),
("Pentax K-1", "Pentax", "Full Frame", 35.9, 24.0),
("Canon EOS 1D X Mark II", "Canon", "Full Frame", 36.0, 24.0),
("Nikon D3400", "Nikon", "Crop", 23.5, 15.6),
("Pentax K-70", "Pentax", "Crop", 23.5, 15.6),
("Sony Alpha A6300", "Sony", "Crop", 23.5, 15.6),
("Fujifilm X-Pro2", "Fujifilm", "Crop", 23.6, 15.6),
("Fujifilm X-T2", "Fujifilm", "Crop", 23.6, 15.6),
("Panasonic Lumix DMC-GX85", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Olympus OM-D E-M1 Mark II", "Olympus", "Micro Four Thirds", 17.3, 13.0),
("Canon EOS M5", "Canon", "Crop", 22.3, 14.9),
("Sony Alpha A99 II", "Sony", "Full Frame", 35.9, 24.0),
("Hasselblad X1D", "Hasselblad", "Medium Format", 43.8, 32.9),

// 2017 Cameras
("Nikon D850", "Nikon", "Full Frame", 35.9, 23.9),
("Canon EOS 77D", "Canon", "Crop", 22.3, 14.9),
("Canon EOS 800D", "Canon", "Crop", 22.3, 14.9),
("Nikon D7500", "Nikon", "Crop", 23.5, 15.6),
("Canon EOS 6D Mark II", "Canon", "Full Frame", 35.9, 24.0),
("Pentax KP", "Pentax", "Crop", 23.5, 15.6),
("Nikon D5600", "Nikon", "Crop", 23.5, 15.6),
("Canon EOS 200D", "Canon", "Crop", 22.3, 14.9),
("Sony Alpha A9", "Sony", "Full Frame", 35.6, 23.8),
("Sony Alpha A7R III", "Sony", "Full Frame", 35.9, 24.0),
("Sony Alpha A6500", "Sony", "Crop", 23.5, 15.6),
("Fujifilm X-T20", "Fujifilm", "Crop", 23.6, 15.6),
("Panasonic Lumix DC-GH5", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Canon EOS M6", "Canon", "Crop", 22.3, 14.9),
("Leica SL", "Leica", "Full Frame", 36.0, 24.0),

// 2018 Cameras
("Canon EOS 2000D", "Canon", "Crop", 22.3, 14.9),
("Canon EOS 4000D", "Canon", "Crop", 22.2, 14.8),
("Nikon D3500", "Nikon", "Crop", 23.5, 15.6),
("Canon EOS 1500D", "Canon", "Crop", 22.3, 14.9),
("Pentax K-1 Mark II", "Pentax", "Full Frame", 35.9, 24.0),
("Sony Alpha A7 III", "Sony", "Full Frame", 35.9, 24.0),
("Fujifilm X-T3", "Fujifilm", "Crop", 23.5, 15.6),
("Fujifilm X-H1", "Fujifilm", "Crop", 23.5, 15.6),
("Sony Alpha A6400", "Sony", "Crop", 23.5, 15.6),
("Canon EOS R", "Canon", "Full Frame", 36.0, 24.0),
("Nikon Z7", "Nikon", "Full Frame", 35.9, 24.0),
("Nikon Z6", "Nikon", "Full Frame", 35.9, 24.0),
("Panasonic Lumix DC-S1R", "Panasonic", "Full Frame", 36.0, 24.0),
("Panasonic Lumix DC-S1", "Panasonic", "Full Frame", 36.0, 24.0),
("Canon EOS M50", "Canon", "Crop", 22.3, 14.9),

// 2019 Cameras
("Canon EOS 90D", "Canon", "Crop", 22.3, 14.8),
("Canon EOS 250D", "Canon", "Crop", 22.3, 14.9),
("Nikon D780", "Nikon", "Full Frame", 35.9, 24.0),
("Sony Alpha A7R IV", "Sony", "Full Frame", 35.9, 24.0),
("Sony Alpha A6600", "Sony", "Crop", 23.5, 15.6),
("Sony Alpha A6100", "Sony", "Crop", 23.5, 15.6),
("Fujifilm X-T30", "Fujifilm", "Crop", 23.5, 15.6),
("Canon EOS RP", "Canon", "Full Frame", 35.9, 24.0),
("Nikon Z50", "Nikon", "Crop", 23.5, 15.6),
("Panasonic Lumix DC-G90", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Olympus OM-D E-M1X", "Olympus", "Micro Four Thirds", 17.3, 13.0),
("Sigma fp", "Sigma", "Full Frame", 35.9, 23.9),
("Canon EOS M6 Mark II", "Canon", "Crop", 22.3, 14.9),
("Fujifilm X-Pro3", "Fujifilm", "Crop", 23.5, 15.6),
("Leica SL2", "Leica", "Full Frame", 36.0, 24.0),

// 2020 Cameras
("Canon EOS R5", "Canon", "Full Frame", 36.0, 24.0),
("Canon EOS R6", "Canon", "Full Frame", 35.9, 24.0),
("Sony Alpha A7S III", "Sony", "Full Frame", 35.6, 23.8),
("Fujifilm X-T4", "Fujifilm", "Crop", 23.5, 15.6),
("Fujifilm X-S10", "Fujifilm", "Crop", 23.5, 15.6),
("Nikon Z5", "Nikon", "Full Frame", 35.9, 24.0),
("Panasonic Lumix DC-S5", "Panasonic", "Full Frame", 36.0, 24.0),
("Sony Alpha A7C", "Sony", "Full Frame", 35.9, 24.0),
("Canon EOS M50 Mark II", "Canon", "Crop", 22.3, 14.9),
("Panasonic Lumix DC-GH5S", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Olympus OM-D E-M5 Mark III", "Olympus", "Micro Four Thirds", 17.3, 13.0),
("Fujifilm X100V", "Fujifilm", "Crop", 23.5, 15.6),
("Leica Q2", "Leica", "Full Frame", 36.0, 24.0),
("Sigma fp L", "Sigma", "Full Frame", 35.9, 23.9),
("Pentax K-3 Mark III", "Pentax", "Crop", 23.5, 15.6),

// 2021 Cameras
("Sony Alpha A7 IV", "Sony", "Full Frame", 35.9, 23.9),
("Sony Alpha A1", "Sony", "Full Frame", 35.9, 24.0),
("Canon EOS R3", "Canon", "Full Frame", 36.0, 24.0),
("Nikon Z9", "Nikon", "Full Frame", 35.9, 24.0),
("Fujifilm X-E4", "Fujifilm", "Crop", 23.5, 15.6),
("Panasonic Lumix DC-GH6", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Canon EOS R7", "Canon", "Crop", 22.3, 14.9),
("Canon EOS R10", "Canon", "Crop", 22.3, 14.9),
("Olympus OM-D E-M1 Mark III", "Olympus", "Micro Four Thirds", 17.3, 13.0),
("Fujifilm X-T30 II", "Fujifilm", "Crop", 23.5, 15.6),
("Sony Alpha FX3", "Sony", "Full Frame", 35.6, 23.8),
("Leica M11", "Leica", "Full Frame", 36.0, 24.0),
("Hasselblad X1D II 50C", "Hasselblad", "Medium Format", 43.8, 32.9),
("Phase One XF IQ4", "Phase One", "Medium Format", 53.4, 40.0),
("Canon EOS R5 C", "Canon", "Full Frame", 36.0, 24.0),

// 2022 Cameras
("Sony Alpha A7R V", "Sony", "Full Frame", 35.9, 24.0),
("Fujifilm X-H2S", "Fujifilm", "Crop", 23.5, 15.6),
("Fujifilm X-H2", "Fujifilm", "Crop", 23.5, 15.6),
("Canon EOS R6 Mark II", "Canon", "Full Frame", 35.9, 24.0),
("Nikon Z30", "Nikon", "Crop", 23.5, 15.6),
("OM System OM-1", "OM System", "Micro Four Thirds", 17.3, 13.0),
("Panasonic Lumix DC-GH6", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Sony Alpha A7 IV", "Sony", "Full Frame", 35.9, 23.9),
("Canon EOS R7", "Canon", "Crop", 22.3, 14.9),
("Fujifilm X-T5", "Fujifilm", "Crop", 23.5, 15.6),
("Nikon Z6 II", "Nikon", "Full Frame", 35.9, 24.0),
("Nikon Z7 II", "Nikon", "Full Frame", 35.9, 24.0),
("Panasonic Lumix DC-S5 II", "Panasonic", "Full Frame", 36.0, 24.0),
("Leica M11 Monochrom", "Leica", "Full Frame", 36.0, 24.0),
("Pentax K-3 Mark III Monochrome", "Pentax", "Crop", 23.5, 15.6),

// 2023 Cameras
("Sony Alpha A7C II", "Sony", "Full Frame", 35.9, 24.0),
("Sony Alpha A7C R", "Sony", "Full Frame", 35.9, 24.0),
("Canon EOS R8", "Canon", "Full Frame", 35.9, 24.0),
("Canon EOS R50", "Canon", "Crop", 22.3, 14.9),
("Fujifilm X-S20", "Fujifilm", "Crop", 23.5, 15.6),
("Nikon Z8", "Nikon", "Full Frame", 35.9, 24.0),
("OM System OM-5", "OM System", "Micro Four Thirds", 17.3, 13.0),
("Panasonic Lumix DC-G9 II", "Panasonic", "Micro Four Thirds", 17.3, 13.0),
("Sony Alpha A6700", "Sony", "Crop", 23.5, 15.6),
("Canon EOS R100", "Canon", "Crop", 22.3, 14.9),
("Fujifilm X100VI", "Fujifilm", "Crop", 23.5, 15.6),
("Nikon Zf", "Nikon", "Full Frame", 35.9, 24.0),
("Leica Q3", "Leica", "Full Frame", 36.0, 24.0),
("Hasselblad X2D 100C", "Hasselblad", "Medium Format", 43.8, 32.9),
("Phase One XT", "Phase One", "Medium Format", 53.4, 40.0),

// 2024 Cameras
("Canon EOS R5 Mark II", "Canon", "Full Frame", 36.0, 24.0),
("Sony Alpha A9 III", "Sony", "Full Frame", 35.6, 23.8),
("Nikon Z6 III", "Nikon", "Full Frame", 35.9, 24.0),
("Fujifilm X-T50", "Fujifilm", "Crop", 23.5, 15.6),
("Canon EOS R1", "Canon", "Full Frame", 36.0, 24.0),
("Sony Alpha FX30", "Sony", "Crop", 23.5, 15.6),
("Panasonic Lumix DC-S9", "Panasonic", "Full Frame", 36.0, 24.0),
("OM System OM-1 Mark II", "OM System", "Micro Four Thirds", 17.3, 13.0),
("Fujifilm GFX100S II", "Fujifilm", "Medium Format", 43.8, 32.9),
("Canon EOS R6 Mark III", "Canon", "Full Frame", 35.9, 24.0),
("Nikon Z5 II", "Nikon", "Full Frame", 35.9, 24.0),
("Sony Alpha A7 V", "Sony", "Full Frame", 35.9, 24.0),
("Leica SL3", "Leica", "Full Frame", 36.0, 24.0),
("Sigma fp II", "Sigma", "Full Frame", 35.9, 23.9),
("Pentax KF", "Pentax", "Crop", 23.5, 15.6)
       };

                // Process camera profiles in batches to improve performance and reduce database contention
                const int batchSize = 10;
                for (int i = 0; i < cameraProfiles.Count; i += batchSize)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    var batch = cameraProfiles.Skip(i).Take(batchSize);
                    var cameraTasks = batch.Select(async cameraData =>
                    {
                        var (name, brand, sensorType, sensorWidth, sensorHeight) = cameraData;
                        var mountType = DetermineMountType(brand, name);

                        var cameraBody = new CameraBody(name, sensorType, sensorWidth, sensorHeight, mountType, false);

                        var databaseContext = (_unitOfWork as Location.Core.Infrastructure.UnitOfWork.UnitOfWork)?.GetDatabaseContext();
                        if (databaseContext != null)
                        {
                            await databaseContext.InsertAsync(cameraBody).ConfigureAwait(false);
                        }
                        else
                        {
                            _logger.LogWarning("Failed to create camera profile {Name}: DatabaseContext not available", name);
                        }
                    });

                    await Task.WhenAll(cameraTasks).ConfigureAwait(false);
                }

                _logger.LogInformation("Created {Count} camera sensor profiles", cameraProfiles.Count);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating camera sensor profiles");
                throw;
            }
        }

        private MountType DetermineMountType(string brand, string cameraName)
        {
            var brandLower = brand?.ToLowerInvariant() ?? "";
            var cameraNameLower = cameraName.ToLowerInvariant();

            return brandLower switch
            {
                "canon" when cameraNameLower.Contains("eos r") => MountType.CanonRF,
                "canon" when cameraNameLower.Contains("eos m") => MountType.CanonEFM,
                "canon" => MountType.CanonEF,
                "nikon" when cameraNameLower.Contains(" z") => MountType.NikonZ,
                "nikon" => MountType.NikonF,
                "sony" when cameraNameLower.Contains("fx") || cameraNameLower.Contains("a7") => MountType.SonyFE,
                "sony" => MountType.SonyE,
                "pentax" => MountType.PentaxK,
                _ => MountType.Other
            };
        }
    }
}