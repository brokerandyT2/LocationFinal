﻿using FluentAssertions;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data.Repositories;
using Moq;
using NUnit.Framework;

namespace Location.Core.Infrastructure.Tests.Data.Repositories
{
    [TestFixture]
    public class WeatherRepositoryAdapterTests
    {
        private WeatherRepositoryAdapter _adapter;
        private Mock<IWeatherRepository> _mockInnerRepository;

        [SetUp]
        public void Setup()
        {
            _mockInnerRepository = new Mock<IWeatherRepository>();
            _adapter = new WeatherRepositoryAdapter(_mockInnerRepository.Object);
        }

        private static Weather CreateValidWeather(int locationId = 1)
        {
            var coordinate = new Coordinate(40.7128, -74.0060); // New York City coordinates
            return new Weather(locationId, coordinate, "America/New_York", -5);
        }

        [Test]
        public async Task GetByIdAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var weather = CreateValidWeather();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            // Act
            var result = await _adapter.GetByIdAsync(1);

            // Assert
            result.Should().BeSameAs(weather);
            _mockInnerRepository.Verify(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetByLocationIdAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var weather = CreateValidWeather();
            _mockInnerRepository.Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            // Act
            var result = await _adapter.GetByLocationIdAsync(1);

            // Assert
            result.Should().BeSameAs(weather);
            _mockInnerRepository.Verify(x => x.GetByLocationIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AddAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var weather = CreateValidWeather();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            // Act
            var result = await _adapter.AddAsync(weather);

            // Assert
            result.Should().BeSameAs(weather);
            _mockInnerRepository.Verify(x => x.AddAsync(weather, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Update_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var weather = CreateValidWeather();
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            _adapter.Update(weather);

            // Wait for the Task.Run to complete
            await Task.Delay(200);

            // Assert
            _mockInnerRepository.Verify(x => x.UpdateAsync(weather, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Delete_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var weather = CreateValidWeather();
            _mockInnerRepository.Setup(x => x.DeleteAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            _adapter.Delete(weather);

            // Wait for the Task.Run to complete
            await Task.Delay(200);

            // Assert
            _mockInnerRepository.Verify(x => x.DeleteAsync(weather, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetRecentAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var weathers = new List<Weather>
            {
                CreateValidWeather(1),
                CreateValidWeather(2),
                CreateValidWeather(3)
            };
            _mockInnerRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weathers);

            // Act
            var result = await _adapter.GetRecentAsync(5);

            // Assert
            result.Should().BeEquivalentTo(weathers);
            _mockInnerRepository.Verify(x => x.GetRecentAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetRecentAsync_WithDefaultCount_ShouldUse10()
        {
            // Arrange
            var weathers = new List<Weather>();
            _mockInnerRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weathers);

            // Act
            var result = await _adapter.GetRecentAsync();

            // Assert
            _mockInnerRepository.Verify(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetExpiredAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var maxAge = TimeSpan.FromHours(2);
            var expiredWeathers = new List<Weather>
            {
                CreateValidWeather(1),
                CreateValidWeather(2)
            };
            _mockInnerRepository.Setup(x => x.GetExpiredAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expiredWeathers);

            // Act
            var result = await _adapter.GetExpiredAsync(maxAge);

            // Assert
            result.Should().BeEquivalentTo(expiredWeathers);
            _mockInnerRepository.Verify(x => x.GetExpiredAsync(maxAge, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowException()
        {
            // Act
            Action act = () => new WeatherRepositoryAdapter(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("innerRepository");
        }

        [Test]
        public async Task GetByIdAsync_WithNullResult_ShouldReturnNull()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather)null);

            // Act
            var result = await _adapter.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetByLocationIdAsync_WithNullResult_ShouldReturnNull()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather)null);

            // Act
            var result = await _adapter.GetByLocationIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetRecentAsync_WithEmptyResult_ShouldReturnEmptyCollection()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Weather>());

            // Act
            var result = await _adapter.GetRecentAsync();

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetExpiredAsync_WithEmptyResult_ShouldReturnEmptyCollection()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetExpiredAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Weather>());

            // Act
            var result = await _adapter.GetExpiredAsync(TimeSpan.FromHours(1));

            // Assert
            result.Should().BeEmpty();
        }

        [Test]
        public async Task AddAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var weather = CreateValidWeather();
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            // Act
            await _adapter.AddAsync(weather, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.AddAsync(weather, cancellationToken), Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather)null);

            // Act
            await _adapter.GetByIdAsync(1, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.GetByIdAsync(1, cancellationToken), Times.Once);
        }

        [Test]
        public async Task GetByLocationIdAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather)null);

            // Act
            await _adapter.GetByLocationIdAsync(1, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.GetByLocationIdAsync(1, cancellationToken), Times.Once);
        }

        [Test]
        public async Task GetRecentAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Weather>());

            // Act
            await _adapter.GetRecentAsync(5, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.GetRecentAsync(5, cancellationToken), Times.Once);
        }

        [Test]
        public async Task GetExpiredAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var maxAge = TimeSpan.FromHours(1);
            _mockInnerRepository.Setup(x => x.GetExpiredAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Weather>());

            // Act
            await _adapter.GetExpiredAsync(maxAge, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.GetExpiredAsync(maxAge, cancellationToken), Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_WhenInnerRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database error");
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await FluentActions.Invoking(async () => await _adapter.GetByIdAsync(1))
                .Should().ThrowAsync<InvalidOperationException>()
                .Where(ex => ex == expectedException);
        }

        [Test]
        public async Task GetByLocationIdAsync_WhenInnerRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database error");
            _mockInnerRepository.Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await FluentActions.Invoking(async () => await _adapter.GetByLocationIdAsync(1))
                .Should().ThrowAsync<InvalidOperationException>()
                .Where(ex => ex == expectedException);
        }

        [Test]
        public async Task AddAsync_WhenInnerRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var weather = CreateValidWeather();
            var expectedException = new InvalidOperationException("Database error");
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await FluentActions.Invoking(async () => await _adapter.AddAsync(weather))
                .Should().ThrowAsync<InvalidOperationException>()
                .Where(ex => ex == expectedException);
        }

        [Test]
        public async Task GetRecentAsync_WhenInnerRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database error");
            _mockInnerRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await FluentActions.Invoking(async () => await _adapter.GetRecentAsync(10))
                .Should().ThrowAsync<InvalidOperationException>()
                .Where(ex => ex == expectedException);
        }

        [Test]
        public async Task GetExpiredAsync_WhenInnerRepositoryThrows_ShouldPropagateException()
        {
            // Arrange
            var expectedException = new InvalidOperationException("Database error");
            _mockInnerRepository.Setup(x => x.GetExpiredAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await FluentActions.Invoking(async () => await _adapter.GetExpiredAsync(TimeSpan.FromHours(1)))
                .Should().ThrowAsync<InvalidOperationException>()
                .Where(ex => ex == expectedException);
        }

        [Test]
        public void Update_WithNullWeather_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            FluentActions.Invoking(() => _adapter.Update(null!))
                .Should().NotThrow();
        }

        [Test]
        public void Delete_WithNullWeather_ShouldNotThrow()
        {
            // Arrange & Act & Assert
            FluentActions.Invoking(() => _adapter.Delete(null!))
                .Should().NotThrow();
        }
    }
}