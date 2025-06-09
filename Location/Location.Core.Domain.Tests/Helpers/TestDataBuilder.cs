using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Domain.Tests.Helpers
{
    /// <summary>
    /// Test data builder for creating domain objects in tests
    /// </summary>
    public static class TestDataBuilder
    {
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
            return new WeatherForecast(
                weatherId,
                forecastDate,
                forecastDate.AddHours(6),
                forecastDate.AddHours(18),
                temperature,
                minTemperature,
                maxTemperature,
                description,
                icon,
                new WindInfo(windSpeed, windDirection),
                humidity,
                pressure,
                clouds,
                uvIndex
            );
        }

        public static List<WeatherForecast> CreateValidWeatherForecasts(
            int count,
            int weatherId = 1,
            DateTime? startDate = null)
        {
            var forecasts = new List<WeatherForecast>();
            var baseDate = startDate ?? DateTime.Today;

            for (int i = 0; i < count; i++)
            {
                var forecast = CreateValidWeatherForecast(
                    weatherId: weatherId,
                    date: baseDate.AddDays(i)
                );
                forecasts.Add(forecast);
            }

            return forecasts;
        }

        public static Tip CreateValidTip(
            int tipTypeId = 1,
            string title = "Test Tip",
            string content = "Test Content",
            string fstop = "f/2.8",
            string shutterSpeed = "1/500",
            string iso = "ISO 100")
        {
            var tip = new Tip(tipTypeId, title, content);
            tip.UpdatePhotographySettings(fstop, shutterSpeed, iso);
            return tip;
        }

        public static TipType CreateValidTipType(
            string name = "Test Category",
            string localization = "en-US")
        {
            var tipType = new TipType(name);
            tipType.SetLocalization(localization);
            return tipType;
        }

        public static Setting CreateValidSetting(
            string key = "test_key",
            string value = "test_value",
            string description = "Test setting")
        {
            return new Setting(key, value, description);
        }

        public static Coordinate CreateValidCoordinate(
            double latitude = 47.6062,
            double longitude = -122.3321)
        {
            return new Coordinate(latitude, longitude);
        }

        public static Address CreateValidAddress(
            string city = "Seattle",
            string state = "WA")
        {
            return new Address(city, state);
        }

        public static Temperature CreateTemperatureCelsius(double celsius = 20)
        {
            return Temperature.FromCelsius(celsius);
        }

        public static WindInfo CreateValidWindInfo(
            double speed = 10,
            double direction = 180,
            double? gust = null)
        {
            return new WindInfo(speed, direction, gust);
        }
    }
}