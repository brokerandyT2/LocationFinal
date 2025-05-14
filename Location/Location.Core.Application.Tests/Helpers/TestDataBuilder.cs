using System;
using System.Collections.Generic;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Application.Tests.Helpers
{
    public class TestDataBuilder
    {
        private static int _idCounter = 1;

        public Domain.Entities.Location BuildLocation(
            int? id = null,
            string title = "Test Location",
            string description = "Test Description",
            double latitude = 40.7128,
            double longitude = -74.0060,
            string city = "New York",
            string state = "NY",
            string? photoPath = null)
        {
            var coordinate = new Coordinate(latitude, longitude);
            var address = new Address(city, state);

            var location = new Domain.Entities.Location(title, description, coordinate, address);

            // Set ID using reflection for testing
            if (id.HasValue)
            {
                typeof(Domain.Entities.Location)
                    .GetProperty("Id")!
                    .SetValue(location, id.Value);
            }
            else
            {
                typeof(Domain.Entities.Location)
                    .GetProperty("Id")!
                    .SetValue(location, _idCounter++);
            }

            if (!string.IsNullOrEmpty(photoPath))
            {
                location.AttachPhoto(photoPath);
            }

            return location;
        }

        public Domain.Entities.Weather BuildWeather(
            int? id = null,
            int locationId = 1,
            double latitude = 40.7128,
            double longitude = -74.0060,
            string timezone = "America/New_York",
            int timezoneOffset = -18000)
        {
            var coordinate = new Coordinate(latitude, longitude);
            var weather = new Domain.Entities.Weather(locationId, coordinate, timezone, timezoneOffset);

            // Set ID using reflection for testing
            if (id.HasValue)
            {
                typeof(Domain.Entities.Weather)
                    .GetProperty("Id")!
                    .SetValue(weather, id.Value);
            }
            else
            {
                typeof(Domain.Entities.Weather)
                    .GetProperty("Id")!
                    .SetValue(weather, _idCounter++);
            }

            return weather;
        }

        public WeatherForecast BuildWeatherForecast(
            int weatherId = 1,
            DateTime? date = null,
            double temperature = 20.0,
            double minTemperature = 15.0,
            double maxTemperature = 25.0,
            string description = "Clear",
            string icon = "01d")
        {
            var forecastDate = date ?? DateTime.Today;
            var temp = Temperature.FromCelsius(temperature);
            var minTemp = Temperature.FromCelsius(minTemperature);
            var maxTemp = Temperature.FromCelsius(maxTemperature);
            var wind = new WindInfo(5.0, 180.0);

            return new WeatherForecast(
                weatherId,
                forecastDate,
                forecastDate.Date.AddHours(6), // sunrise
                forecastDate.Date.AddHours(18), // sunset
                temp,
                minTemp,
                maxTemp,
                description,
                icon,
                wind,
                60, // humidity
                1013, // pressure
                20, // clouds
                5.0 // uvIndex
            );
        }

        public Domain.Entities.Tip BuildTip(
            int? id = null,
            int tipTypeId = 1,
            string title = "Test Tip",
            string content = "Test Content")
        {
            var tip = new Domain.Entities.Tip(tipTypeId, title, content);

            if (id.HasValue)
            {
                typeof(Domain.Entities.Tip)
                    .GetProperty("Id")!
                    .SetValue(tip, id.Value);
            }
            else
            {
                typeof(Domain.Entities.Tip)
                    .GetProperty("Id")!
                    .SetValue(tip, _idCounter++);
            }

            return tip;
        }

        public Domain.Entities.TipType BuildTipType(
            int? id = null,
            string name = "Photography")
        {
            var tipType = new Domain.Entities.TipType(name);

            if (id.HasValue)
            {
                typeof(Domain.Entities.TipType)
                    .GetProperty("Id")!
                    .SetValue(tipType, id.Value);
            }
            else
            {
                typeof(Domain.Entities.TipType)
                    .GetProperty("Id")!
                    .SetValue(tipType, _idCounter++);
            }

            return tipType;
        }

        public Domain.Entities.Setting BuildSetting(
            int? id = null,
            string key = "TestKey",
            string value = "TestValue",
            string description = "Test Description")
        {
            var setting = new Domain.Entities.Setting(key, value, description);

            if (id.HasValue)
            {
                typeof(Domain.Entities.Setting)
                    .GetProperty("Id")!
                    .SetValue(setting, id.Value);
            }
            else
            {
                typeof(Domain.Entities.Setting)
                    .GetProperty("Id")!
                    .SetValue(setting, _idCounter++);
            }

            return setting;
        }
    }
}