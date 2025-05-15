using NUnit.Framework;
using FluentAssertions;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Tests.Helpers;
using Location.Core.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Tests.Data.Repositories
{
    [TestFixture]
    public class WeatherRepositoryTests
    {
        private WeatherRepository _repository;
        private DatabaseContext _context;
        private Mock<ILogger<WeatherRepository>> _mockLogger;
        private Mock<ILogger<DatabaseContext>> _mockContextLogger;
        private string _testDbPath;

        [SetUp]
        public async Task Setup()
        {
            _mockLogger = new Mock<ILogger<WeatherRepository>>();
            _mockContextLogger = new Mock<ILogger<DatabaseContext>>();
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            _context = new DatabaseContext(_mockContextLogger.Object, _testDbPath);
            await _context.InitializeDatabaseAsync();
            _repository = new WeatherRepository(_context, _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();

            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }

        [Test]
        public async Task GetByIdAsync_WithExistingWeather_ShouldReturnWeatherWithForecasts()
        {
            // Arrange
            var weatherEntity = TestDataBuilder.CreateWeatherEntity();
            await _context.InsertAsync(weatherEntity);

            var forecastEntity1 = TestDataBuilder.CreateWeatherForecastEntity(
                id: 0,
                weatherId: weatherEntity.Id,
                date: DateTime.Today
            );
            var forecastEntity2 = TestDataBuilder.CreateWeatherForecastEntity(
                id: 0,
                weatherId: weatherEntity.Id,
                date: DateTime.Today.AddDays(1)
            );
            await _context.InsertAsync(forecastEntity1);
            await _context.InsertAsync(forecastEntity2);

            // Act
            var result = await _repository.GetByIdAsync(weatherEntity.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(weatherEntity.Id);
            result.LocationId.Should().Be(weatherEntity.LocationId);
            result.Forecasts.Should().HaveCount(2);
            result.Forecasts.Should().BeInAscendingOrder(f => f.Date);
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingWeather_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetByLocationIdAsync_WithExistingWeather_ShouldReturnMostRecent()
        {
            // Arrange
            var locationId = 1;

            var olderWeather = TestDataBuilder.CreateWeatherEntity(id: 0, locationId: locationId);
            olderWeather.LastUpdate = DateTime.UtcNow.AddHours(-2);
            await _context.InsertAsync(olderWeather);

            var newerWeather = TestDataBuilder.CreateWeatherEntity(id: 0, locationId: locationId);
            newerWeather.LastUpdate = DateTime.UtcNow;
            await _context.InsertAsync(newerWeather);

            // Act
            var result = await _repository.GetByLocationIdAsync(locationId);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(newerWeather.Id);
            result.LastUpdate.Should().Be(newerWeather.LastUpdate);
        }

        [Test]
        public async Task GetByLocationIdAsync_WithNonExistingLocation_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByLocationIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task AddAsync_WithWeatherAndForecasts_ShouldPersistAll()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
            var forecasts = new List<Domain.Entities.WeatherForecast>
            {
                TestDataBuilder.CreateValidWeatherForecast(weatherId: 0, date: DateTime.Today),
                TestDataBuilder.CreateValidWeatherForecast(weatherId: 0, date: DateTime.Today.AddDays(1))
            };
            weather.UpdateForecasts(forecasts);

            // Act
            var result = await _repository.AddAsync(weather);

            // Assert
            result.Should().BeSameAs(weather);
            result.Id.Should().BeGreaterThan(0);
            result.Forecasts.Should().HaveCount(2);
            result.Forecasts.All(f => f.Id > 0).Should().BeTrue();

            // Verify persistence
            var retrieved = await _repository.GetByIdAsync(result.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Forecasts.Should().HaveCount(2);
        }

        [Test]
        public void Update_WithExistingWeather_ShouldUpdateWeatherAndForecasts()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
            _repository.AddAsync(weather).Wait();

            // Act - Update weather with new forecasts
            var newForecasts = new List<Domain.Entities.WeatherForecast>
            {
                TestDataBuilder.CreateValidWeatherForecast(weatherId: weather.Id, temperature: 25),
                TestDataBuilder.CreateValidWeatherForecast(weatherId: weather.Id, temperature: 26),
                TestDataBuilder.CreateValidWeatherForecast(weatherId: weather.Id, temperature: 27)
            };
            weather.UpdateForecasts(newForecasts);
            _repository.Update(weather);

            // Assert
            var retrieved = _repository.GetByIdAsync(weather.Id).Result;
            retrieved.Should().NotBeNull();
            retrieved!.Forecasts.Should().HaveCount(3);
            retrieved.Forecasts.Select(f => f.Temperature.Celsius)
                .Should().BeEquivalentTo(new[] { 25.0, 26.0, 27.0 });
        }

        [Test]
        public void Delete_WithExistingWeather_ShouldDeleteWeatherAndForecasts()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
            var forecasts = new List<Domain.Entities.WeatherForecast>
            {
                TestDataBuilder.CreateValidWeatherForecast(weatherId: 0)
            };
            weather.UpdateForecasts(forecasts);
            _repository.AddAsync(weather).Wait();

            // Act
            _repository.Delete(weather);

            // Assert
            var retrieved = _repository.GetByIdAsync(weather.Id).Result;
            retrieved.Should().BeNull();

            // Verify forecasts are also deleted
            var remainingForecasts = _context.Table<WeatherForecastEntity>()
                .Where(f => f.WeatherId == weather.Id)
                .ToListAsync().Result;
            remainingForecasts.Should().BeEmpty();
        }

        [Test]
        public async Task GetRecentAsync_WithMultipleWeathers_ShouldReturnMostRecent()
        {
            // Arrange
            var weathers = new List<Domain.Entities.Weather>();
            for (int i = 0; i < 15; i++)
            {
                var weather = TestDataBuilder.CreateValidWeather(locationId: i + 1);
                // Use reflection to set LastUpdate without going through UpdateForecasts
                var lastUpdateProperty = weather.GetType().GetProperty("LastUpdate");
                lastUpdateProperty!.SetValue(weather, DateTime.UtcNow.AddMinutes(-i));
                weathers.Add(weather);
                await _repository.AddAsync(weather);
            }

            // Act
            var result = await _repository.GetRecentAsync(10);

            // Assert
            var recentList = result.ToList();
            recentList.Should().HaveCount(10);
            recentList.Should().BeInDescendingOrder(w => w.LastUpdate);
            recentList.First().LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        }

        [Test]
        public async Task GetExpiredAsync_WithMixedDates_ShouldReturnExpiredOnly()
        {
            // Arrange
            var cutoffTime = TimeSpan.FromHours(1);

            var freshWeather = TestDataBuilder.CreateValidWeather(locationId: 1);
            await _repository.AddAsync(freshWeather);

            var expiredWeather = TestDataBuilder.CreateValidWeather(locationId: 2);
            // Use reflection to set LastUpdate
            var lastUpdateProperty = expiredWeather.GetType().GetProperty("LastUpdate");
            lastUpdateProperty!.SetValue(expiredWeather, DateTime.UtcNow.AddHours(-2));
            await _repository.AddAsync(expiredWeather);

            // Act
            var result = await _repository.GetExpiredAsync(cutoffTime);

            // Assert
            var expiredList = result.ToList();
            expiredList.Should().HaveCount(1);
            expiredList[0].LocationId.Should().Be(2);
        }

        [Test]
        public async Task AddAsync_WithoutForecasts_ShouldCreateWeatherOnly()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();

            // Act
            var result = await _repository.AddAsync(weather);

            // Assert
            result.Id.Should().BeGreaterThan(0);
            result.Forecasts.Should().BeEmpty();

            var retrieved = await _repository.GetByIdAsync(result.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Forecasts.Should().BeEmpty();
        }

        [Test]
        public async Task Update_ReplacesAllForecasts()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
            var initialForecasts = new List<Domain.Entities.WeatherForecast>
            {
                TestDataBuilder.CreateValidWeatherForecast(weatherId: 0, temperature: 20)
            };
            weather.UpdateForecasts(initialForecasts);
            await _repository.AddAsync(weather);

            // Act - Replace with completely different forecasts
            var newForecasts = new List<Domain.Entities.WeatherForecast>
            {
                TestDataBuilder.CreateValidWeatherForecast(weatherId: weather.Id, temperature: 30),
                TestDataBuilder.CreateValidWeatherForecast(weatherId: weather.Id, temperature: 31)
            };
            weather.UpdateForecasts(newForecasts);
            _repository.Update(weather);

            // Assert
            var retrieved = await _repository.GetByIdAsync(weather.Id);
            retrieved!.Forecasts.Should().HaveCount(2);
            retrieved.Forecasts.Select(f => f.Temperature.Celsius)
                .Should().BeEquivalentTo(new[] { 30.0, 31.0 });
        }

        [Test]
        public void Constructor_WithNullContext_ShouldThrowException()
        {
            // Act
            Action act = () => new WeatherRepository(null!, _mockLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new WeatherRepository(_context, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public async Task GetByIdAsync_VerifyLogging()
        {
            // Arrange
            var weatherEntity = TestDataBuilder.CreateWeatherEntity();
            await _context.InsertAsync(weatherEntity);

            // Act
            await _repository.GetByIdAsync(weatherEntity.Id);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }
    }
}