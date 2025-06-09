using FluentAssertions;
using Location.Core.Domain.Entities;
using Location.Core.Domain.Events;
using Location.Core.Domain.ValueObjects;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Entities
{
    [TestFixture]
    public class WeatherTests
    {
        private Coordinate _validCoordinate;

        [SetUp]
        public void Setup()
        {
            _validCoordinate = new Coordinate(47.6062, -122.3321);
        }

        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange & Act
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);

            // Assert
            weather.LocationId.Should().Be(1);
            weather.Coordinate.Should().Be(_validCoordinate);
            weather.Timezone.Should().Be("America/Los_Angeles");
            weather.TimezoneOffset.Should().Be(-7);
            weather.LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            weather.Forecasts.Should().BeEmpty();
        }

        [Test]
        public void Constructor_WithNullCoordinate_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Weather(1, null, "America/Los_Angeles", -7);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("value");
        }

        [Test]
        public void UpdateForecasts_WithValidForecasts_ShouldUpdateCollection()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecasts = CreateSampleForecasts(5);
            weather.ClearDomainEvents();

            // Act
            weather.UpdateForecasts(forecasts);

            // Assert
            weather.Forecasts.Count.Should().Be(5);
            weather.LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
            weather.DomainEvents.Should().ContainSingle();
            weather.DomainEvents.Should().ContainItemsAssignableTo<WeatherUpdatedEvent>();
            var domainEvent = weather.DomainEvents.First() as WeatherUpdatedEvent;
            domainEvent?.LocationId.Should().Be(1);
        }

        [Test]
        public void UpdateForecasts_WithMoreThan7Days_ShouldLimitTo7()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecasts = CreateSampleForecasts(10);

            // Act
            weather.UpdateForecasts(forecasts);

            // Assert
            weather.Forecasts.Count.Should().Be(7);
        }

        [Test]
        public void UpdateForecasts_WithNull_ShouldThrowException()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);

            // Act
            Action act = () => weather.UpdateForecasts(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("forecasts");
        }

        [Test]
        public void UpdateForecasts_ShouldReplaceExistingForecasts()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var initialForecasts = CreateSampleForecasts(3);
            var newForecasts = CreateSampleForecasts(5);
            weather.UpdateForecasts(initialForecasts);

            // Act
            weather.UpdateForecasts(newForecasts);

            // Assert
            weather.Forecasts.Count.Should().Be(5);
        }

        [Test]
        public void GetForecastForDate_WithExistingDate_ShouldReturnForecast()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var targetDate = DateTime.Today.AddDays(2);
            var forecasts = CreateSampleForecasts(5);
            weather.UpdateForecasts(forecasts);

            // Act
            var result = weather.GetForecastForDate(targetDate);

            // Assert
            result.Should().NotBeNull();
            result?.Date.Date.Should().Be(targetDate.Date);
        }

        [Test]
        public void GetForecastForDate_WithNonExistingDate_ShouldReturnNull()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecasts = CreateSampleForecasts(3);
            weather.UpdateForecasts(forecasts);

            // Act
            var result = weather.GetForecastForDate(DateTime.Today.AddDays(10));

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public void GetCurrentForecast_WithTodaysForecast_ShouldReturnForecast()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecasts = CreateSampleForecasts(5);
            weather.UpdateForecasts(forecasts);

            // Act
            var result = weather.GetCurrentForecast();

            // Assert
            result.Should().NotBeNull();
            result?.Date.Date.Should().Be(DateTime.Today);
        }

        [Test]
        public void GetCurrentForecast_WithoutTodaysForecast_ShouldReturnNull()
        {
            // Arrange
            var weather = new Weather(1, _validCoordinate, "America/Los_Angeles", -7);
            var forecasts = CreateSampleForecasts(3, startDaysFromToday: 1);
            weather.UpdateForecasts(forecasts);

            // Act
            var result = weather.GetCurrentForecast();

            // Assert
            result.Should().BeNull();
        }

        private List<WeatherForecast> CreateSampleForecasts(int count, int startDaysFromToday = 0)
        {
            var forecasts = new List<WeatherForecast>();
            for (int i = 0; i < count; i++)
            {
                var date = DateTime.Today.AddDays(startDaysFromToday + i);
                var forecast = new WeatherForecast(
                    1,
                    date,
                    date.AddHours(6),
                    date.AddHours(18),
                    20,
                    15,
                    25,
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
    }
}