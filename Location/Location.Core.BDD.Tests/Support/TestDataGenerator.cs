using Location.Core.BDD.Tests.Models;

namespace Location.Core.BDD.Tests.Support
{
    public static class TestDataGenerator
    {
        private static readonly Random _random = new();

        /// <summary>
        /// Generates a random location test model
        /// </summary>
        public static LocationTestModel GenerateLocation(int? id = null)
        {
            string[] cities = { "New York", "San Francisco", "Chicago", "Miami", "Seattle", "Denver", "Boston", "Austin" };
            string[] states = { "NY", "CA", "IL", "FL", "WA", "CO", "MA", "TX" };

            var randomCity = cities[_random.Next(cities.Length)];
            var randomState = states[_random.Next(states.Length)];

            return new LocationTestModel
            {
                Id = id ?? _random.Next(1, 1000),
                Title = $"Test Location {_random.Next(1, 100)}",
                Description = $"This is a test location generated for BDD testing",
                Latitude = Math.Round((_random.NextDouble() * 180) - 90, 6),
                Longitude = Math.Round((_random.NextDouble() * 360) - 180, 6),
                City = randomCity,
                State = randomState,
                PhotoPath = _random.Next(0, 2) == 0 ? null : $"/path/to/photo_{_random.Next(1, 100)}.jpg",
                Timestamp = DateTime.UtcNow.AddDays(-_random.Next(0, 30)),
                IsDeleted = _random.Next(0, 10) == 0 // 10% chance of being deleted
            };
        }

        /// <summary>
        /// Generates a list of random location test models
        /// </summary>
        public static List<LocationTestModel> GenerateLocations(int count)
        {
            var result = new List<LocationTestModel>();

            for (int i = 0; i < count; i++)
            {
                result.Add(GenerateLocation(i + 1));
            }

            return result;
        }

        /// <summary>
        /// Generates a random weather test model
        /// </summary>
        public static WeatherTestModel GenerateWeather(int? id = null, int? locationId = null)
        {
            string[] timezones = { "America/New_York", "America/Los_Angeles", "America/Chicago", "Europe/London", "Asia/Tokyo" };
            string[] descriptions = { "Clear sky", "Partly cloudy", "Cloudy", "Light rain", "Heavy rain", "Thunderstorm", "Snowy" };
            string[] icons = { "01d", "02d", "03d", "04d", "09d", "10d", "11d", "13d" };

            var randomTimezone = timezones[_random.Next(timezones.Length)];
            var randomDescription = descriptions[_random.Next(descriptions.Length)];
            var randomIcon = icons[_random.Next(icons.Length)];

            return new WeatherTestModel
            {
                Id = id ?? _random.Next(1, 1000),
                LocationId = locationId ?? _random.Next(1, 100),
                Latitude = Math.Round((_random.NextDouble() * 180) - 90, 6),
                Longitude = Math.Round((_random.NextDouble() * 360) - 180, 6),
                Timezone = randomTimezone,
                TimezoneOffset = _random.Next(-12, 13) * 3600,
                LastUpdate = DateTime.UtcNow.AddHours(-_random.Next(0, 48)),
                Temperature = Math.Round(_random.NextDouble() * 40 - 10, 1),
                Description = randomDescription,
                Icon = randomIcon,
                WindSpeed = Math.Round(_random.NextDouble() * 30, 1),
                WindDirection = _random.Next(0, 360),
                WindGust = _random.Next(0, 2) == 0 ? null : Math.Round(_random.NextDouble() * 40, 1),
                Humidity = _random.Next(0, 101),
                Pressure = _random.Next(980, 1050),
                Clouds = _random.Next(0, 101),
                UvIndex = Math.Round(_random.NextDouble() * 12, 1),
                Precipitation = _random.Next(0, 2) == 0 ? null : Math.Round(_random.NextDouble() * 50, 1),
                Sunrise = DateTime.Today.AddHours(5).AddMinutes(_random.Next(0, 60)),
                Sunset = DateTime.Today.AddHours(18).AddMinutes(_random.Next(0, 60)),
                MoonRise = _random.Next(0, 2) == 0 ? null : DateTime.Today.AddHours(19).AddMinutes(_random.Next(0, 60)),
                MoonSet = _random.Next(0, 2) == 0 ? null : DateTime.Today.AddHours(7).AddMinutes(_random.Next(0, 60)),
                MoonPhase = Math.Round(_random.NextDouble(), 2)
            };
        }

        /// <summary>
        /// Generates a random tip type test model
        /// </summary>
        public static TipTypeTestModel GenerateTipType(int? id = null)
        {
            string[] names = { "Landscape", "Portrait", "Night", "Wildlife", "Macro", "Street", "Architecture", "Sports" };
            string[] localizations = { "en-US", "en-GB", "es-ES", "fr-FR", "de-DE", "ja-JP", "zh-CN", "ru-RU" };

            var randomName = names[_random.Next(names.Length)];
            var randomI8n = localizations[_random.Next(localizations.Length)];

            return new TipTypeTestModel
            {
                Id = id ?? _random.Next(1, 1000),
                Name = randomName,
                I8n = randomI8n
            };
        }

        /// <summary>
        /// Generates a random tip test model
        /// </summary>
        public static TipTestModel GenerateTip(int? id = null, int? tipTypeId = null)
        {
            string[] titles = { "Rule of Thirds", "Golden Hour", "Leading Lines", "Framing", "Depth of Field", "Negative Space", "Symmetry", "Patterns" };
            string[] fstops = { "f/1.8", "f/2.8", "f/4", "f/5.6", "f/8", "f/11", "f/16", "f/22" };
            string[] shutterSpeeds = { "1/4000s", "1/2000s", "1/1000s", "1/500s", "1/250s", "1/125s", "1/60s", "1/30s", "1/15s", "1/8s", "1/4s", "1/2s", "1s", "2s" };
            string[] isos = { "100", "200", "400", "800", "1600", "3200", "6400" };
            string[] localizations = { "en-US", "en-GB", "es-ES", "fr-FR", "de-DE", "ja-JP", "zh-CN", "ru-RU" };

            var randomTitle = titles[_random.Next(titles.Length)];
            var randomFstop = fstops[_random.Next(fstops.Length)];
            var randomShutterSpeed = shutterSpeeds[_random.Next(shutterSpeeds.Length)];
            var randomIso = isos[_random.Next(isos.Length)];
            var randomI8n = localizations[_random.Next(localizations.Length)];

            return new TipTestModel
            {
                Id = id ?? _random.Next(1, 1000),
                TipTypeId = tipTypeId ?? _random.Next(1, 10),
                Title = randomTitle,
                Content = $"This is a test tip about {randomTitle}. It provides guidance on how to use this technique in photography.",
                Fstop = randomFstop,
                ShutterSpeed = randomShutterSpeed,
                Iso = randomIso,
                I8n = randomI8n
            };
        }

        /// <summary>
        /// Generates a random setting test model
        /// </summary>
        public static SettingTestModel GenerateSetting(int? id = null)
        {
            string[] keys = { "DarkModeEnabled", "MapZoomLevel", "CacheTimeoutMinutes", "AutoSync", "DefaultTemperatureUnit", "HistoryLimit", "NotificationEnabled" };
            string[] values = { "true", "false", "10", "30", "60", "celsius", "fahrenheit", "100", "1000" };

            var randomKey = keys[_random.Next(keys.Length)];
            var randomValue = values[_random.Next(values.Length)];

            return new SettingTestModel
            {
                Id = id ?? _random.Next(1, 1000),
                Key = randomKey,
                Value = randomValue,
                Description = $"Controls the {randomKey} setting for the application",
                Timestamp = DateTime.UtcNow.AddDays(-_random.Next(0, 30))
            };
        }
    }
}