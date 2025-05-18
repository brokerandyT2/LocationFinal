// Location.Photography.Infrastructure/DataPopulation/DatabaseInitializer.cs
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
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
                var databaseContext = (_unitOfWork as Location.Core.Infrastructure.UnitOfWork.UnitOfWork)?.GetDatabaseContext();
                if (databaseContext != null)
                {
                    await databaseContext.InitializeDatabaseAsync();
                }
                else
                {
                    _logger.LogWarning("DatabaseContext not available through UnitOfWork");
                }


                _logger.LogInformation("Starting database population");

                await CreateTipTypesAsync();
                await CreateSampleLocationsAsync();
                await CreateSettingsAsync(hemisphere, tempFormat, dateFormat, timeFormat, windDirection, email);

                _logger.LogInformation("Database population completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database population");
                await _alertService.ShowErrorAlertAsync("Failed to initialize database: " + ex.Message, "Error");
                throw;
            }
        }

        private async Task CreateTipTypesAsync()
        {
            var tipTypeNames = new[]
            {
                "Landscape", "Silouette", "Building", "Person", "Baby", "Animals",
                "Blury Water", "Night", "Blue Hour", "Golden Hour", "Sunset"
            };

            foreach (var name in tipTypeNames)
            {
                var tipType = new TipType(name);
                tipType.SetLocalization("en-US");

                // Create tip type in repository
                var typeResult = await _unitOfWork.TipTypes.CreateEntityAsync(tipType);

                if (!typeResult.IsSuccess || typeResult.Data == null)
                {
                    _logger.LogWarning("Failed to create tip type: {Name}", name);
                    continue;
                }

                // Create a sample tip for each type
                var tip = new Tip(
                    typeResult.Data.Id,
                    $"How to Take Great {name} Photos",
                    "Text of the tip would appear here. Zombie ipsum reversus ab viral inferno, nam rick grimes malum cerebro. De carne lumbering animata corpora quaeritis. Summus brains sit​​, morbo vel maleficia? De apocalypsi gorger omero undead survivor dictum mauris."
                );
                tip.UpdatePhotographySettings("f/1", "1/125", "50");
                tip.SetLocalization("en-US");

                var tipResult = await _unitOfWork.Tips.CreateAsync(tip);
                if (!tipResult.IsSuccess)
                {
                    _logger.LogWarning("Failed to create tip for type {Name}: {Error}", name, tipResult.ErrorMessage);
                }
            }

            _logger.LogInformation("Created {Count} tip types with sample tips", tipTypeNames.Length);
        }

        private async Task CreateSampleLocationsAsync()
        {
            var sampleLocations = new List<(string Title, string Description, double Latitude, double Longitude, string Photo)>
            {
                (
                    "Soldiers and Sailors Monument",
                    "Located in the heart of downtown in Monument Circle, it was originally designed to honor Indiana's Civil War veterans. It now commemorates the valor of Hoosier veterans who served in all wars prior to WWI, including the Revolutionary War, the War of 1812, the Mexican War, the Civil War, the Frontier Wars and the Spanish-American War. One of the most popular parts of the monument is the observation deck with a 360-degree view of the city skyline from 275 feet up.",
                    39.7685, -86.1580,
                    "Resources/Images/s_and_sm_new.jpg"
                ),
                (
                    "The Bean",
                    "What is The Bean?\r\nThe Bean is a work of public art in the heart of Chicago. The sculpture, which is officially titled Cloud Gate, is one of the world's largest permanent outdoor art installations. The monumental work was unveiled in 2004 and quickly became of the Chicago's most iconic sights.",
                    41.8827, -87.6233,
                    "Resources/Images/chicagobean.jpg"
                ),
                (
                    "Golden Gate Bridge",
                    "The Golden Gate Bridge is a suspension bridge spanning the Golden Gate strait, the one-mile-wide (1.6 km) channel between San Francisco Bay and the Pacific Ocean. The strait is the entrance to San Francisco Bay from the Pacific Ocean. The bridge connects the city of San Francisco, California, to Marin County, carrying both U.S. Route 101 and California State Route 1 across the strait.",
                    37.8199, -122.4783,
                    "Resources/Images/ggbridge.jpg"
                ),
                (
                    "Gateway Arch",
                    "The Gateway Arch is a 630-foot (192 m) monument in St. Louis, Missouri, that commemorates Thomas Jefferson and the westward expansion of the United States. The arch is the centerpiece of the Gateway Arch National Park and is the tallest arch in the world.",
                    38.6247, -90.1848,
                    "Resources/Images/stlarch.jpg"
                )
            };

            foreach (var (title, description, latitude, longitude, photo) in sampleLocations)
            {
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

                var result = await _unitOfWork.Locations.CreateAsync(location);
                if (!result.IsSuccess)
                {
                    _logger.LogWarning("Failed to create location {Title}: {Error}", title, result.ErrorMessage);
                }
            }

            _logger.LogInformation("Created {Count} sample locations", sampleLocations.Count);
        }

        private async Task CreateSettingsAsync(
            string hemisphere,
            string tempFormat,
            string dateFormat,
            string timeFormat,
            string windDirection,
            string email)
        {
            var guid = Guid.NewGuid().ToString();

            var settings = new List<(string Key, string Value, string Description)>
            {
                // Core settings
                (MagicStrings.Hemisphere, hemisphere, "User's hemisphere (north/south)"),
                (MagicStrings.FirstName, "", "User's first name"),
                (MagicStrings.LastName, "", "User's last name"),
                (MagicStrings.UniqueID, guid, "Unique identifier for the installation"),
                (MagicStrings.LastBulkWeatherUpdate, DateTime.Now.AddDays(-2).ToString(), "Timestamp of last bulk weather update"),
                (MagicStrings.DefaultLanguage, "en-US", "Default language setting"),
                (MagicStrings.WindDirection, windDirection, "Wind direction setting (towardsWind/withWind)"),
                (MagicStrings.CameraRefresh, "2000", "Camera refresh rate in milliseconds"),
                (MagicStrings.AppOpenCounter, "1", "Number of times the app has been opened"),
                (MagicStrings.TimeFormat, timeFormat, "Time format (12h/24h)"),
                (MagicStrings.DateFormat, dateFormat, "Date format (US/International)"),
                (MagicStrings.WeatherURL, "https://api.openweathermap.org/data/3.0/onecall", "Weather API URL"),
                (MagicStrings.Weather_API_Key, "aa24f449cced50c0491032b2f955d610", "Weather API key"),
                (MagicStrings.FreePremiumAdSupported, "false", "Whether the app is running in ad-supported mode"),
                (MagicStrings.TemperatureType, tempFormat, "Temperature format (F/C)"),
                (MagicStrings.DeviceInfo, "", "Device information"),
                (MagicStrings.Email, email, "User's email address"),
            };

            // Add additional settings based on the build configuration
#if DEBUG
            // Debug mode settings - features already viewed and additional functionality
            var debugSettings = new List<(string Key, string Value, string Description)>
            {
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
                (MagicStrings.ExposureCalcAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last exposure calculator ad view"),
                (MagicStrings.LightMeterAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last light meter ad view"),
                (MagicStrings.SceneEvaluationAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last scene evaluation ad view"),
                (MagicStrings.SunCalculatorViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last sun calculator ad view"),
                (MagicStrings.SunLocationAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last sun location ad view"),
                (MagicStrings.WeatherDisplayAdViewed_TimeStamp, DateTime.Now.ToString(), "Timestamp of last weather display ad view"),
                (MagicStrings.SubscriptionType, MagicStrings.Premium, "Subscription type (Free/Premium)"),
                (MagicStrings.SubscriptionExpiration, DateTime.Now.AddDays(100).ToString(), "Subscription expiration date"),
                (MagicStrings.AdGivesHours, "24", "Hours of premium access granted per ad view")
            };
            settings.AddRange(debugSettings);
#else
            // Release mode settings - features not viewed and basic functionality
            var releaseSettings = new List<(string Key, string Value, string Description)>
            {
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
                (MagicStrings.ExposureCalcAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last exposure calculator ad view"),
                (MagicStrings.LightMeterAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last light meter ad view"),
                (MagicStrings.SceneEvaluationAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last scene evaluation ad view"),
                (MagicStrings.SunCalculatorViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last sun calculator ad view"),
                (MagicStrings.SunLocationAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last sun location ad view"),
                (MagicStrings.WeatherDisplayAdViewed_TimeStamp, DateTime.Now.AddDays(-1).ToString(), "Timestamp of last weather display ad view"),
                (MagicStrings.SubscriptionType, MagicStrings.Free, "Subscription type (Free/Premium)"),
                (MagicStrings.SubscriptionExpiration, DateTime.Now.AddDays(-1).ToString(), "Subscription expiration date"),
                (MagicStrings.AdGivesHours, "12", "Hours of premium access granted per ad view")
            };
            settings.AddRange(releaseSettings);
#endif

            foreach (var (key, value, description) in settings)
            {
                var setting = new Setting(key, value, description);
                var result = await _unitOfWork.Settings.CreateAsync(setting);
                if (!result.IsSuccess)
                {
                    //_logger.LogWarning("Failed to create setting {Key}: {Error}", key, result.ErrorMessage);
                }
            }

            _logger.LogInformation("Created {Count} settings", settings.Count);
        }
    }
}