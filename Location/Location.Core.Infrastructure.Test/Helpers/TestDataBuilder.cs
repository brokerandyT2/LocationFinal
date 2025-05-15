using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.External.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;

namespace Location.Core.Infrastructure.Tests.Helpers
{
    /// <summary>
    /// Test data builder for infrastructure tests
    /// </summary>
    public static class TestDataBuilder
    {
        // Domain entities
        public static Domain.Entities.Location CreateValidLocation(
            string title = "Test Location",
            string description = "Test Description",
            double latitude = 47.6062,
            double longitude = -122.3321,
            string city = "Seattle",
            string state = "WA")
        {
            var coordinate = new Coordinate(latitude, longitude);
            var address = new Address(city, state);
            return new Domain.Entities.Location(title, description, coordinate, address);
        }

        public static Weather CreateValidWeather(
            int locationId = 1,
            double latitude = 47.6062,
            double longitude = -122.3321,
            string timezone = "America/Los_Angeles",
            int timezoneOffset = -7)
        {
            var coordinate = new Coordinate(latitude, longitude);
            return new Weather(locationId, coordinate, timezone, timezoneOffset);
        }

        public static WeatherForecast CreateValidWeatherForecast(
            int weatherId = 1,
            DateTime? date = null,
            double temperature = 20,
            double minTemperature = 15,
            double maxTemperature = 25)
        {
            var forecastDate = date ?? DateTime.Today;
            return new WeatherForecast(
                weatherId,
                forecastDate,
                forecastDate.AddHours(6),
                forecastDate.AddHours(18),
                Temperature.FromCelsius(temperature),
                Temperature.FromCelsius(minTemperature),
                Temperature.FromCelsius(maxTemperature),
                "Clear sky",
                "01d",
                new WindInfo(10, 180),
                65,
                1013,
                10,
                5.0
            );
        }

        public static Tip CreateValidTip(
            int tipTypeId = 1,
            string title = "Test Tip",
            string content = "Test Content")
        {
            return new Tip(tipTypeId, title, content);
        }

        public static TipType CreateValidTipType(
            string name = "Test Category")
        {
            return new TipType(name);
        }

        public static Setting CreateValidSetting(
            string key = "test_key",
            string value = "test_value",
            string description = "Test setting")
        {
            return new Setting(key, value, description);
        }

        // Infrastructure entities
        public static LocationEntity CreateLocationEntity(
            int id = 1,
            string title = "Test Location",
            string description = "Test Description",
            double latitude = 47.6062,
            double longitude = -122.3321,
            string city = "Seattle",
            string state = "WA",
            bool isDeleted = false)
        {
            return new LocationEntity
            {
                Id = id,
                Title = title,
                Description = description,
                Latitude = latitude,
                Longitude = longitude,
                City = city,
                State = state,
                IsDeleted = isDeleted,
                Timestamp = DateTime.UtcNow
            };
        }

        public static WeatherEntity CreateWeatherEntity(
            int id = 1,
            int locationId = 1,
            double latitude = 47.6062,
            double longitude = -122.3321,
            string timezone = "America/Los_Angeles",
            int timezoneOffset = -7)
        {
            return new WeatherEntity
            {
                Id = id,
                LocationId = locationId,
                Latitude = latitude,
                Longitude = longitude,
                Timezone = timezone,
                TimezoneOffset = timezoneOffset,
                LastUpdate = DateTime.UtcNow
            };
        }

        public static WeatherForecastEntity CreateWeatherForecastEntity(
            int id = 1,
            int weatherId = 1,
            DateTime? date = null,
            double temperature = 20,
            double minTemperature = 15,
            double maxTemperature = 25)
        {
            var forecastDate = date ?? DateTime.Today;
            return new WeatherForecastEntity
            {
                Id = id,
                WeatherId = weatherId,
                Date = forecastDate,
                Sunrise = forecastDate.AddHours(6),
                Sunset = forecastDate.AddHours(18),
                Temperature = temperature,
                MinTemperature = minTemperature,
                MaxTemperature = maxTemperature,
                Description = "Clear sky",
                Icon = "01d",
                WindSpeed = 10,
                WindDirection = 180,
                Humidity = 65,
                Pressure = 1013,
                Clouds = 10,
                UvIndex = 5.0,
                MoonPhase = 0.5
            };
        }

        public static TipEntity CreateTipEntity(
            int id = 1,
            int tipTypeId = 1,
            string title = "Test Tip",
            string content = "Test Content")
        {
            return new TipEntity
            {
                Id = id,
                TipTypeId = tipTypeId,
                Title = title,
                Content = content,
                Fstop = "f/2.8",
                ShutterSpeed = "1/500",
                Iso = "ISO 100",
                I8n = "en-US"
            };
        }

        public static TipTypeEntity CreateTipTypeEntity(
            int id = 1,
            string name = "Test Category")
        {
            return new TipTypeEntity
            {
                Id = id,
                Name = name,
                I8n = "en-US"
            };
        }

        public static SettingEntity CreateSettingEntity(
            int id = 1,
            string key = "test_key",
            string value = "test_value",
            string description = "Test setting")
        {
            return new SettingEntity
            {
                Id = id,
                Key = key,
                Value = value,
                Description = description,
                Timestamp = DateTime.UtcNow
            };
        }

        // External models
        public static OpenWeatherResponse CreateOpenWeatherResponse()
        {
            return new OpenWeatherResponse
            {
                Lat = 47.6062,
                Lon = -122.3321,
                Timezone = "America/Los_Angeles",
                TimezoneOffset = -7,
                Current = new CurrentWeather
                {
                    Dt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    Sunrise = DateTimeOffset.UtcNow.AddHours(-6).ToUnixTimeSeconds(),
                    Sunset = DateTimeOffset.UtcNow.AddHours(6).ToUnixTimeSeconds(),
                    Temp = 20,
                    FeelsLike = 18,
                    Pressure = 1013,
                    Humidity = 65,
                    Uvi = 5.0,
                    Clouds = 10,
                    WindSpeed = 10,
                    WindDeg = 180,
                    Weather = new List<WeatherDescription>
                    {
                        new WeatherDescription
                        {
                            Id = 800,
                            Main = "Clear",
                            Description = "clear sky",
                            Icon = "01d"
                        }
                    }
                },
                Daily = new List<DailyForecast>()
            };
        }

        // Mock builders
        public static IServiceProvider CreateMockServiceProvider(Action<ServiceCollection>? configure = null)
        {
            var services = new ServiceCollection();

            // Add default mocks
            services.AddSingleton(Mock.Of<ILogger<object>>());
            services.AddSingleton(Mock.Of<ILoggerFactory>());

            // Allow custom configuration
            configure?.Invoke(services);

            return services.BuildServiceProvider();
        }

        // Mock configuration
        public static Mock<ILogger<T>> CreateMockLogger<T>() where T : class
        {
            var logger = new Mock<ILogger<T>>();
            return logger;
        }
    }
}