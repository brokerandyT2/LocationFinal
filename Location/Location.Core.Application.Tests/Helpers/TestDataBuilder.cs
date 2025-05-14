using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.Commands.SaveLocation;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using System;
using System.Collections.Generic;

namespace Location.Core.Application.Tests.Helpers
{
    /// <summary>
    /// Test data builder for creating application layer objects in tests
    /// </summary>
    public static class TestDataBuilder
    {
        public static Domain.Entities.Weather CreateValidWeather(
    int locationId = 1,
    double latitude = 47.6062,
    double longitude = -122.3321,
    string timezone = "America/Los_Angeles",
    int timezoneOffset = -7)
        {
            var coordinate = new Coordinate(latitude, longitude);
            return new Domain.Entities.Weather(locationId, coordinate, timezone, timezoneOffset);
        }

        public static List<Domain.Entities.WeatherForecast> CreateValidWeatherForecasts(
            int count,
            int weatherId = 1,
            DateTime? startDate = null)
        {
            var forecasts = new List<Domain.Entities.WeatherForecast>();
            var baseDate = startDate ?? DateTime.Today;

            for (int i = 0; i < count; i++)
            {
                var date = baseDate.AddDays(i);
                var forecast = new Domain.Entities.WeatherForecast(
                    weatherId,
                    date,
                    date.AddHours(6),
                    date.AddHours(18),
                    Temperature.FromCelsius(20),
                    Temperature.FromCelsius(15),
                    Temperature.FromCelsius(25),
                    "Clear sky",
                    "01d",
                    new WindInfo(10, 180),
                    65,
                    1013,
                    10,
                    5.0
                );
                forecasts.Add(forecast);
            }
            return forecasts;
        }

        public static Domain.Entities.WeatherForecast CreateValidWeatherForecast(
            int weatherId = 1,
            DateTime? date = null,
            double temperature = 20,
            double minTemperature = 15,
            double maxTemperature = 25,
            string description = "Clear sky",
            string icon = "01d",
            double windSpeed = 10,
            double windDirection = 180,
            int humidity = 65,
            int pressure = 1013,
            int clouds = 10,
            double uvIndex = 5.0)
        {
            var forecastDate = date ?? DateTime.Today;
            return new Domain.Entities.WeatherForecast(
                weatherId,
                forecastDate,
                forecastDate.AddHours(6),
                forecastDate.AddHours(18),
                Temperature.FromCelsius(temperature),
                Temperature.FromCelsius(minTemperature),
                Temperature.FromCelsius(maxTemperature),
                description,
                icon,
                new WindInfo(windSpeed, windDirection),
                humidity,
                pressure,
                clouds,
                uvIndex
            );
        }
        // Domain entities (reuse from domain tests)
        public static Location.Core.Domain.Entities.Location CreateValidLocation(
            string title = "Test Location",
            string description = "Test Description",
            double latitude = 47.6062,
            double longitude = -122.3321,
            string city = "Seattle",
            string state = "WA")
        {
            var coordinate = new Coordinate(latitude, longitude);
            var address = new Address(city, state);
            return new Location.Core.Domain.Entities.Location(title, description, coordinate, address);
        }

        // Command builders
        public static SaveLocationCommand CreateValidSaveLocationCommand(
            int? id = null,
            string title = "Test Location",
            string description = "Test Description",
            double latitude = 47.6062,
            double longitude = -122.3321,
            string city = "Seattle",
            string state = "WA",
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

        // DTO builders
        public static LocationDto CreateValidLocationDto(
            int id = 1,
            string title = "Test Location",
            string description = "Test Description",
            double latitude = 47.6062,
            double longitude = -122.3321,
            string city = "Seattle",
            string state = "WA",
            string? photoPath = null,
            bool isDeleted = false)
        {
            return new LocationDto
            {
                Id = id,
                Title = title,
                Description = description,
                Latitude = latitude,
                Longitude = longitude,
                City = city,
                State = state,
                PhotoPath = photoPath,
                Timestamp = DateTime.UtcNow,
                IsDeleted = isDeleted
            };
        }

        public static WeatherDto CreateValidWeatherDto(
            int id = 1,
            int locationId = 1,
            double latitude = 47.6062,
            double longitude = -122.3321,
            double temperature = 20.0,
            string description = "Clear sky",
            string icon = "01d")
        {
            return new WeatherDto
            {
                Id = id,
                LocationId = locationId,
                Latitude = latitude,
                Longitude = longitude,
                Temperature = temperature,
                Description = description,
                Icon = icon,
                Timezone = "America/Los_Angeles",
                TimezoneOffset = -7,
                LastUpdate = DateTime.UtcNow,
                WindSpeed = 10,
                WindDirection = 180,
                Humidity = 65,
                Pressure = 1013,
                Clouds = 10,
                UvIndex = 5.0,
                Sunrise = DateTime.Today.AddHours(6),
                Sunset = DateTime.Today.AddHours(18)
            };
        }

        public static WeatherForecastDto CreateValidWeatherForecastDto()
        {
            return new WeatherForecastDto
            {
                WeatherId = 1,
                LastUpdate = DateTime.UtcNow,
                Timezone = "America/Los_Angeles",
                TimezoneOffset = -7,
                DailyForecasts = CreateValidDailyForecasts(7)
            };
        }

        public static List<DailyForecastDto> CreateValidDailyForecasts(int count)
        {
            var forecasts = new List<DailyForecastDto>();
            for (int i = 0; i < count; i++)
            {
                var date = DateTime.Today.AddDays(i);
                forecasts.Add(new DailyForecastDto
                {
                    Date = date,
                    Sunrise = date.AddHours(6),
                    Sunset = date.AddHours(18),
                    Temperature = 20,
                    MinTemperature = 15,
                    MaxTemperature = 25,
                    Description = "Clear sky",
                    Icon = "01d",
                    WindSpeed = 10,
                    WindDirection = 180,
                    Humidity = 65,
                    Pressure = 1013,
                    Clouds = 10,
                    UvIndex = 5.0,
                    MoonPhase = 0.5
                });
            }
            return forecasts;
        }

        // Error builders
        public static Error CreateValidationError(string propertyName = "TestProperty", string message = "Validation failed")
        {
            return Error.Validation(propertyName, message);
        }

        public static Error CreateNotFoundError(string message = "Entity not found")
        {
            return Error.NotFound(message);
        }

        public static Error CreateDatabaseError(string message = "Database operation failed")
        {
            return Error.Database(message);
        }

        // Result builders
        public static Result<T> CreateSuccessResult<T>(T data)
        {
            return Result<T>.Success(data);
        }

        public static Result<T> CreateFailureResult<T>(string errorMessage)
        {
            return Result<T>.Failure(errorMessage);
        }

        public static Result<T> CreateFailureResult<T>(Error error)
        {
            return Result<T>.Failure(error);
        }

        public static Result<T> CreateFailureResult<T>(IEnumerable<Error> errors)
        {
            return Result<T>.Failure(errors);
        }
    }
}