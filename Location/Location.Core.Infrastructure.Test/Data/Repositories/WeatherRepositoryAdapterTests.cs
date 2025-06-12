using FluentAssertions;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Tests.Helpers;
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

        [Test]
        public async Task GetByIdAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
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
            var weather = TestDataBuilder.CreateValidWeather();
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
            var weather = TestDataBuilder.CreateValidWeather();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            // Act
            var result = await _adapter.AddAsync(weather);

            // Assert
            result.Should().BeSameAs(weather);
            _mockInnerRepository.Verify(x => x.AddAsync(weather, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public void Update_ShouldCallInnerRepositoryUpdateAsync()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            _adapter.Update(weather);

            // Give Task.Run a moment to execute
            Thread.Sleep(100);

            // Assert
            _mockInnerRepository.Verify(x => x.UpdateAsync(weather, CancellationToken.None), Times.Once);
        }

        [Test]
        public void Delete_ShouldCallInnerRepositoryDeleteAsync()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
            _mockInnerRepository.Setup(x => x.DeleteAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            _adapter.Delete(weather);

            // Give Task.Run a moment to execute
            Thread.Sleep(100);

            // Assert
            _mockInnerRepository.Verify(x => x.DeleteAsync(weather, CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task GetRecentAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var weatherList = new List<Weather> { TestDataBuilder.CreateValidWeather() };
            _mockInnerRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weatherList);

            // Act
            var result = await _adapter.GetRecentAsync(5);

            // Assert
            result.Should().BeSameAs(weatherList);
            _mockInnerRepository.Verify(x => x.GetRecentAsync(5, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetExpiredAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var maxAge = TimeSpan.FromHours(24);
            var weatherList = new List<Weather> { TestDataBuilder.CreateValidWeather() };
            _mockInnerRepository.Setup(x => x.GetExpiredAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weatherList);

            // Act
            var result = await _adapter.GetExpiredAsync(maxAge);

            // Assert
            result.Should().BeSameAs(weatherList);
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
                .ReturnsAsync((Weather?)null);

            // Act
            var result = await _adapter.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
            _mockInnerRepository.Verify(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetByLocationIdAsync_WithNullResult_ShouldReturnNull()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByLocationIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Weather?)null);

            // Act
            var result = await _adapter.GetByLocationIdAsync(999);

            // Assert
            result.Should().BeNull();
            _mockInnerRepository.Verify(x => x.GetByLocationIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetRecentAsync_WithDefaultCount_ShouldUseDefault()
        {
            // Arrange
            var weatherList = new List<Weather>();
            _mockInnerRepository.Setup(x => x.GetRecentAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weatherList);

            // Act
            var result = await _adapter.GetRecentAsync();

            // Assert
            result.Should().BeSameAs(weatherList);
            _mockInnerRepository.Verify(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AddAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Weather>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            // Act
            await _adapter.AddAsync(weather, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.AddAsync(weather, cancellationToken), Times.Once);
        }
    }
}