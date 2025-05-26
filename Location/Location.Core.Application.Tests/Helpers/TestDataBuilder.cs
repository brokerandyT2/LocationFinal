namespace Location.Core.Application.Tests.Utilities
{
    using Location.Core.Application.Commands.Locations;
    using Location.Core.Application.Locations.DTOs;
    using Location.Core.Application.Weather.DTOs;
    using Location.Core.Domain.Entities;
    using Location.Core.Domain.ValueObjects;
    using System;
    using System.Collections.Generic;

    public static class TestDataBuilder
    {
        public static SaveLocationCommand CreateValidSaveLocationCommand()
        {
            return new SaveLocationCommand
            {
                Title = "Test Location",
                Description = "Test Description",
                Latitude = 40.7128,
                Longitude = -74.0060,
                City = "New York",
                State = "NY",
                PhotoPath = null
            };
        }

        public static SaveLocationCommand CreateValidSaveLocationCommand(
            int? id = null,
            string title = "Test Location",
            string description = "Test Description",
            double latitude = 40.7128,
            double longitude = -74.0060,
            string city = "New York",
            string state = "NY",
            string? photoPath = null)
        {
            return new SaveLocationCommand
            {
                Id = id,
                Title = title,
                Description = description,
                Latitude = latitude,
                Longitude = longitude,
                City = city,
                State = state,
                PhotoPath = photoPath
            };
        }

        public static Domain.Entities.Location CreateValidLocation()
        {
            var coordinate = new Coordinate(40.7128, -74.0060);
            var address = new Address("New York", "NY");

            return new Domain.Entities.Location(
                "Test Location",
                "Test Description",
                coordinate,
                address);
        }

        public static Domain.Entities.Location CreateValidLocation(
            string title = "Test Location",
            string description = "Test Description",
            double latitude = 40.7128,
            double longitude = -74.0060,
            string city = "New York",
            string state = "NY")
        {
            var coordinate = new Coordinate(latitude, longitude);
            var address = new Address(city, state);

            return new Domain.Entities.Location(
                title,
                description,
                coordinate,
                address);
        }

        // Create valid Location entity with ID (for test purposes)
        // Note: This requires reflection or test helpers since ID is protected
        public static Domain.Entities.Location CreateValidLocation(int id)
        {
            var coordinate = new Coordinate(40.7128, -74.0060);
            var address = new Address("New York", "NY");

            var location = new Domain.Entities.Location(
                $"Test Location {id}",
                $"Test Description {id}",
                coordinate,
                address);

            // Use reflection to set the ID (only for testing)
            var idProperty = location.GetType().GetProperty("Id");
            idProperty?.SetValue(location, id);

            return location;
        }

        // Create list of valid Location entities
        public static List<Domain.Entities.Location> CreateValidLocationList(int count = 3)
        {
            var locations = new List<Domain.Entities.Location>();
            for (int i = 1; i <= count; i++)
            {
                locations.Add(CreateValidLocation(i));
            }
            return locations;
        }

        public static LocationDto CreateValidLocationDto()
        {
            return new LocationDto
            {
                Id = 1,
                Title = "Test Location",
                Description = "Test Description",
                Latitude = 40.7128,
                Longitude = -74.0060,
                City = "New York",
                State = "NY",
                PhotoPath = null,
                Timestamp = DateTime.UtcNow,
                IsDeleted = false
            };
        }

        public static Weather CreateValidWeather(int locationId = 1)
        {
            var coordinate = new Coordinate(40.7128, -74.0060);
            return new Weather(locationId, coordinate, "America/New_York", -18000);
        }

        public static WeatherForecast CreateValidWeatherForecast(int weatherId = 1)
        {
            var temperature = 20;
            var minTemp = 15;
            var maxTemp = 25;
            var wind = new WindInfo(10, 180, 15);

            return new WeatherForecast(
                weatherId,
                DateTime.Today,
                DateTime.Today.AddHours(6),
                DateTime.Today.AddHours(18),
                temperature,
                minTemp,
                maxTemp,
                "Clear sky",
                "01d",
                wind,
                65,
                1013,
                10,
                5.5);
        }

        public static List<DailyForecastDto> CreateValidDailyForecasts(int count = 7)
        {
            var forecasts = new List<DailyForecastDto>();

            for (int i = 0; i < count; i++)
            {
                forecasts.Add(new DailyForecastDto
                {
                    Date = DateTime.UtcNow.Date.AddDays(i),
                    Sunrise = DateTime.UtcNow.Date.AddDays(i).AddHours(6),
                    Sunset = DateTime.UtcNow.Date.AddDays(i).AddHours(18),
                    Temperature = 20 + i,
                    MinTemperature = 15 + i,
                    MaxTemperature = 25 + i,
                    Description = $"Day {i + 1} weather",
                    Icon = $"0{i + 1}d",
                    WindSpeed = 10 + i,
                    WindDirection = 180 + (i * 10),
                    WindGust = i % 2 == 0 ? (double?)(15 + i) : null,
                    Humidity = 65 + i,
                    Pressure = 1013 + i,
                    Clouds = 10 + (i * 5),
                    UvIndex = 5.5 + i,
                    Precipitation = i % 3 == 0 ? (double?)(0.1 * i) : null,
                    MoonRise = DateTime.UtcNow.Date.AddDays(i).AddHours(20),
                    MoonSet = DateTime.UtcNow.Date.AddDays(i).AddHours(7),
                    MoonPhase = 0.1 * i
                });
            }

            return forecasts;
        }

        public static WeatherForecastDto CreateValidWeatherForecastDto()
        {
            return new WeatherForecastDto
            {
                WeatherId = 1,
                LastUpdate = DateTime.UtcNow,
                Timezone = "America/New_York",
                TimezoneOffset = -18000,
                DailyForecasts = CreateValidDailyForecasts(7)
            };
        }

        public static Tip CreateValidTip(int tipTypeId = 1)
        {
            return new Tip(tipTypeId, "Photography Tip", "Use the rule of thirds");
        }

        public static TipType CreateValidTipType()
        {
            return new TipType("Landscape Photography");
        }

        public static Setting CreateValidSetting()
        {
            return new Setting("test_key", "test_value", "Test setting description");
        }

        // Additional helpers for command variations
        public static SaveLocationCommand CreateSaveLocationCommandWithId(int id)
        {
            var command = CreateValidSaveLocationCommand();
            command.Id = id;
            return command;
        }

        public static SaveLocationCommand CreateSaveLocationCommandWithPhoto()
        {
            var command = CreateValidSaveLocationCommand();
            command.PhotoPath = "/path/to/photo.jpg";
            return command;
        }

        public static SaveLocationCommand CreateInvalidSaveLocationCommand()
        {
            return new SaveLocationCommand
            {
                Title = "", // Invalid - empty title
                Description = new string('a', 501), // Invalid - too long
                Latitude = 91, // Invalid - out of range
                Longitude = 181, // Invalid - out of range
                City = "Invalid City",
                State = "XX"
            };
        }

        // Create valid WeatherDto
        public static WeatherDto CreateValidWeatherDto()
        {
            return new WeatherDto
            {
                Id = 1,
                LocationId = 1,
                Latitude = 40.7128,
                Longitude = -74.0060,
                Timezone = "America/New_York",
                TimezoneOffset = -18000,
                LastUpdate = DateTime.UtcNow,
                Temperature = 22.5,
                Description = "Partly cloudy",
                Icon = "02d",
                WindSpeed = 12.5,
                WindDirection = 180,
                WindGust = 15.0,
                Humidity = 65,
                Pressure = 1015,
                Clouds = 40,
                UvIndex = 5.2,
                Precipitation = 0.0,
                Sunrise = DateTime.UtcNow.Date.AddHours(6),
                Sunset = DateTime.UtcNow.Date.AddHours(18),
                MoonRise = DateTime.UtcNow.Date.AddHours(20),
                MoonSet = DateTime.UtcNow.Date.AddHours(8),
                MoonPhase = 0.45
            };
        }

        // Create WeatherDto with custom parameters
        public static WeatherDto CreateWeatherDto(double temperature, string description, int humidity)
        {
            var dto = CreateValidWeatherDto();
            dto.Temperature = temperature;
            dto.Description = description;
            dto.Humidity = humidity;
            return dto;
        }

        // Create list of WeatherDto for forecast
        public static List<WeatherDto> CreateWeatherForecast(int days = 5)
        {
            var forecast = new List<WeatherDto>();
            var baseTemp = 20.0;

            for (int i = 0; i < days; i++)
            {
                var dto = CreateValidWeatherDto();
                dto.Temperature = baseTemp + (i * 1.5);
                dto.Description = i % 2 == 0 ? "Sunny" : "Cloudy";
                dto.Humidity = 50 + (i * 5);
                dto.WindSpeed = 8.0 + i;
                dto.Pressure = 1010 + i;
                dto.Icon = i % 2 == 0 ? "01d" : "03d";
                dto.LastUpdate = DateTime.UtcNow.AddDays(i);
                forecast.Add(dto);
            }

            return forecast;
        }
    }
}