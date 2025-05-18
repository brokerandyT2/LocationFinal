using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using Location.Core.Application.Common.Interfaces;

using Location.Core.Application.Common.Models;
using Location.Core.Application.Queries.Weather;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Application.Weather.DTOs;

namespace Location.Core.Application.Tests.Weather.Queries.GetWeatherByLocation
{
    [Category("Weather")]
    [Category("Get")]
    [TestFixture]
    public class GetWeatherByLocationQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IWeatherRepository> _weatherRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private GetWeatherByLocationQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _weatherRepositoryMock = new Mock<IWeatherRepository>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Weather).Returns(_weatherRepositoryMock.Object);

            _handler = new GetWeatherByLocationQueryHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object);
        }

        [Test]
        public async Task Handle_WithValidLocationId_ShouldReturnWeatherData()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 1 };
            var weather = TestDataBuilder.CreateValidWeather(1);
            var weatherDto = TestDataBuilder.CreateValidWeatherDto();

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            _mapperMock
                .Setup(x => x.Map<WeatherDto>(weather))
                .Returns(weatherDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(weatherDto);

            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(x => x.Map<WeatherDto>(weather), Times.Once);
        }

        [Test]
        public async Task Handle_WithNoWeatherData_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 99 };

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Weather data not found for this location");

            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()), Times.Once);
            _mapperMock.Verify(x => x.Map<WeatherDto>(It.IsAny<Domain.Entities.Weather>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithRepositoryException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 1 };
            var exception = new Exception("Database error");

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve weather data");
            result.ErrorMessage.Should().Contain("Database error");

            _mapperMock.Verify(x => x.Map<WeatherDto>(It.IsAny<Domain.Entities.Weather>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithMappingException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 1 };
            var weather = TestDataBuilder.CreateValidWeather(1);
            var exception = new Exception("Mapping error");

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            _mapperMock
                .Setup(x => x.Map<WeatherDto>(weather))
                .Throws(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve weather data");
            result.ErrorMessage.Should().Contain("Mapping error");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 1 };
            var weather = TestDataBuilder.CreateValidWeather(1);
            var weatherDto = TestDataBuilder.CreateValidWeatherDto();
            var cancellationToken = new CancellationToken();

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, cancellationToken))
                .ReturnsAsync(weather);

            _mapperMock
                .Setup(x => x.Map<WeatherDto>(weather))
                .Returns(weatherDto);

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(query.LocationId, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WithZeroLocationId_ShouldStillAttemptToQuery()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 0 };

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Weather data not found for this location");

            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(0, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNegativeLocationId_ShouldStillAttemptToQuery()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = -1 };

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(-1, It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}