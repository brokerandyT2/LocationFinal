using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Application.Weather.DTOs;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Weather.Commands.UpdateWeather
{
    [TestFixture]
    public class UpdateWeatherCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ILocationRepository> _locationRepositoryMock;
        private Mock<IWeatherRepository> _weatherRepositoryMock;
        private Mock<IWeatherService> _weatherServiceMock;
        private Mock<IMapper> _mapperMock;
        private UpdateWeatherCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _weatherRepositoryMock = new Mock<IWeatherRepository>();
            _weatherServiceMock = new Mock<IWeatherService>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);
            _unitOfWorkMock.Setup(u => u.Weather).Returns(_weatherRepositoryMock.Object);

            _handler = new UpdateWeatherCommandHandler(
                _unitOfWorkMock.Object,
                _weatherServiceMock.Object,
                _mapperMock.Object);
        }

        [Test]
        public async Task Handle_WithValidLocationAndNoCache_ShouldFetchNewWeather()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1, ForceUpdate = false };
            var location = TestDataBuilder.CreateValidLocation(1);
            var weatherDto = TestDataBuilder.CreateValidWeatherDto();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            _weatherServiceMock
                .Setup(x => x.GetWeatherAsync(
                    location.Coordinate.Latitude,
                    location.Coordinate.Longitude,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Success(weatherDto));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(weatherDto);

            _weatherServiceMock.Verify(x => x.GetWeatherAsync(
                location.Coordinate.Latitude,
                location.Coordinate.Longitude,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithValidLocationAndFreshCache_ShouldReturnCachedWeather()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1, ForceUpdate = false };
            var location = TestDataBuilder.CreateValidLocation(1);
            var existingWeather = TestDataBuilder.CreateValidWeather(1);
            SetPrivateField(existingWeather, "_lastUpdate", DateTime.UtcNow.AddHours(-6)); // 6 hours old

            var cachedWeatherDto = TestDataBuilder.CreateValidWeatherDto();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWeather);

            _mapperMock
                .Setup(x => x.Map<WeatherDto>(existingWeather))
                .Returns(cachedWeatherDto);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(cachedWeatherDto);

            _weatherServiceMock.Verify(x => x.GetWeatherAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithForceUpdate_ShouldAlwaysFetchNewWeather()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1, ForceUpdate = true };
            var location = TestDataBuilder.CreateValidLocation(1);
            var existingWeather = TestDataBuilder.CreateValidWeather(1);
            SetPrivateField(existingWeather, "_lastUpdate", DateTime.UtcNow.AddHours(-6)); // 6 hours old

            var weatherDto = TestDataBuilder.CreateValidWeatherDto();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWeather);

            _weatherServiceMock
                .Setup(x => x.GetWeatherAsync(
                    location.Coordinate.Latitude,
                    location.Coordinate.Longitude,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Success(weatherDto));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(weatherDto);

            _weatherServiceMock.Verify(x => x.GetWeatherAsync(
                location.Coordinate.Latitude,
                location.Coordinate.Longitude,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithStaleCache_ShouldFetchNewWeather()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1, ForceUpdate = false };
            var location = TestDataBuilder.CreateValidLocation(1);
            var existingWeather = TestDataBuilder.CreateValidWeather(1);
            SetPrivateField(existingWeather, "_lastUpdate", DateTime.UtcNow.AddDays(-2)); // 2 days old

            var weatherDto = TestDataBuilder.CreateValidWeatherDto();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingWeather);

            _weatherServiceMock
                .Setup(x => x.GetWeatherAsync(
                    location.Coordinate.Latitude,
                    location.Coordinate.Longitude,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Success(weatherDto));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(weatherDto);

            _weatherServiceMock.Verify(x => x.GetWeatherAsync(
                location.Coordinate.Latitude,
                location.Coordinate.Longitude,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentLocation_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 999 };

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");

            _weatherServiceMock.Verify(x => x.GetWeatherAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WhenWeatherServiceFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1, ForceUpdate = false };
            var location = TestDataBuilder.CreateValidLocation(1);

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            _weatherServiceMock
                .Setup(x => x.GetWeatherAsync(
                    location.Coordinate.Latitude,
                    location.Coordinate.Longitude,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Failure("Weather API error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to fetch weather data");
        }

        [Test]
        public async Task Handle_WhenException_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1 };
            var exception = new Exception("Unexpected error");

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to update weather");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }
    }
}