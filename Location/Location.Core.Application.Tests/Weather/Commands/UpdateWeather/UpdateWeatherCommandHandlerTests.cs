using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Application.Weather.DTOs;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Weather.Commands.UpdateWeather
{
    [Category("Weather")]
    [Category("Update")]
    [TestFixture]
    public class UpdateWeatherCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ILocationRepository> _locationRepositoryMock;
        private Mock<IWeatherService> _weatherServiceMock;
        private Mock<IMapper> _mapperMock;
        private Mock<IMediator> _mediatorMock;
        private UpdateWeatherCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _locationRepositoryMock = new Mock<ILocationRepository>();
            _weatherServiceMock = new Mock<IWeatherService>();
            _mapperMock = new Mock<IMapper>();
            _mediatorMock = new Mock<IMediator>();

            _unitOfWorkMock.Setup(u => u.Locations).Returns(_locationRepositoryMock.Object);

            _handler = new UpdateWeatherCommandHandler(
                _unitOfWorkMock.Object,
                _weatherServiceMock.Object,
                _mapperMock.Object,
                _mediatorMock.Object);
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
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _weatherServiceMock
                .Setup(x => x.UpdateWeatherForLocationAsync(
                    command.LocationId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Success(weatherDto));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(weatherDto);

            _weatherServiceMock.Verify(x => x.UpdateWeatherForLocationAsync(
                command.LocationId,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithValidLocationAndFreshCache_ShouldReturnCachedWeather()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1, ForceUpdate = false };
            var location = TestDataBuilder.CreateValidLocation(1);
            var weatherDto = TestDataBuilder.CreateValidWeatherDto();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _weatherServiceMock
                .Setup(x => x.UpdateWeatherForLocationAsync(
                    command.LocationId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Success(weatherDto));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(weatherDto);
        }

        [Test]
        public async Task Handle_WithForceUpdate_ShouldAlwaysFetchNewWeather()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1, ForceUpdate = true };
            var location = TestDataBuilder.CreateValidLocation(1);
            var weatherDto = TestDataBuilder.CreateValidWeatherDto();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _weatherServiceMock
                .Setup(x => x.UpdateWeatherForLocationAsync(
                    command.LocationId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Success(weatherDto));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(weatherDto);

            _weatherServiceMock.Verify(x => x.UpdateWeatherForLocationAsync(
                command.LocationId,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithStaleCache_ShouldFetchNewWeather()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 1, ForceUpdate = false };
            var location = TestDataBuilder.CreateValidLocation(1);
            var weatherDto = TestDataBuilder.CreateValidWeatherDto();

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _weatherServiceMock
                .Setup(x => x.UpdateWeatherForLocationAsync(
                    command.LocationId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Success(weatherDto));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(weatherDto);

            _weatherServiceMock.Verify(x => x.UpdateWeatherForLocationAsync(
                command.LocationId,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentLocation_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateWeatherCommand { LocationId = 999 };

            _locationRepositoryMock
                .Setup(x => x.GetByIdAsync(command.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Failure("Location not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location not found");

            _weatherServiceMock.Verify(x => x.UpdateWeatherForLocationAsync(
                It.IsAny<int>(),
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
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(location));

            _weatherServiceMock
                .Setup(x => x.UpdateWeatherForLocationAsync(
                    command.LocationId,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherDto>.Failure("Weather API error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to fetch and persist weather data");
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
    }
}