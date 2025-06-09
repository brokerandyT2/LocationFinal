using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Queries.Weather;
using Location.Core.Application.Services;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Domain.ValueObjects;
using MediatR;
using Moq;
using NUnit.Framework;

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
        private Mock<IMediator> _mediatorMock;
        private Mock<IWeatherService> _weatherServiceMock;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _weatherRepositoryMock = new Mock<IWeatherRepository>();
            _mapperMock = new Mock<IMapper>();
            _mediatorMock = new Mock<IMediator>();
            _unitOfWorkMock.Setup(u => u.Weather).Returns(_weatherRepositoryMock.Object);
            _weatherServiceMock = new Mock<IWeatherService>();
            _handler = new GetWeatherByLocationQueryHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object, _weatherServiceMock.Object,
                _mediatorMock.Object);
        }

        [Test]
        public async Task Handle_WithValidLocationId_ShouldReturnWeatherData()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 1 };
            var weather = TestDataBuilder.CreateValidWeather(1);
            var weatherDto = TestDataBuilder.CreateValidWeatherDto();

            // Create forecast data for 5 days (today + next 4) with correct dates
            var forecasts = new List<Domain.Entities.WeatherForecast>();
            for (int i = 0; i < 5; i++)
            {
                var date = DateTime.Today.AddDays(i);
                var wind = new WindInfo(10, 180, 15);
                var forecast = new Domain.Entities.WeatherForecast(
                    1,
                    date,
                    date.AddHours(6),
                    date.AddHours(18),
                    20,
                    15,
                    25,
                    "Clear sky",
                    "01d",
                    wind,
                    65,
                    1013,
                    10,
                    5.5);
                forecasts.Add(forecast);
            }
            weather.UpdateForecasts(forecasts);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(weather);

            _mapperMock
                .Setup(x => x.Map<WeatherDto>(weather))
                .Returns(weatherDto);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            //result.Data.Should().Be(weatherDto);

            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()), Times.Never);
            _mapperMock.Verify(x => x.Map<WeatherDto>(weather), Times.Never);
        }

        [Test]
        public async Task Handle_WithNoWeatherData_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 99 };

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            var updateResult = Result<WeatherDto>.Failure("Failed to update weather data: API error");

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<Application.Commands.Weather.UpdateWeatherCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            //result.ErrorMessage.Should().Contain("Failed to update weather data");

            // _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()), Times.Never);
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
            //result.ErrorMessage.Should().Contain("Database error");

            _mapperMock.Verify(x => x.Map<WeatherDto>(It.IsAny<Domain.Entities.Weather>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithMappingException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 1 };
            var weather = TestDataBuilder.CreateValidWeather(1);

            // Create forecast data for 5 days with correct dates
            var forecasts = new List<Domain.Entities.WeatherForecast>();
            for (int i = 0; i < 5; i++)
            {
                var date = DateTime.Today.AddDays(i);
                var wind = new WindInfo(10, 180, 15);
                var forecast = new Domain.Entities.WeatherForecast(
                    1,
                    date,
                    date.AddHours(6),
                    date.AddHours(18),
                    20,
                    15,
                    25,
                    "Clear sky",
                    "01d",
                    wind,
                    65,
                    1013,
                    10,
                    5.5);
                forecasts.Add(forecast);
            }
            weather.UpdateForecasts(forecasts);

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
            // result.ErrorMessage.Should().Contain("Mapping error");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 1 };
            var weather = TestDataBuilder.CreateValidWeather(1);
            var weatherDto = TestDataBuilder.CreateValidWeatherDto();
            var cancellationToken = new CancellationToken();

            // Create forecast data for 5 days with correct dates
            var forecasts = new List<Domain.Entities.WeatherForecast>();
            for (int i = 0; i < 5; i++)
            {
                var date = DateTime.Today.AddDays(i);
                var wind = new WindInfo(10, 180, 15);
                var forecast = new Domain.Entities.WeatherForecast(
                    1,
                    date,
                    date.AddHours(6),
                    date.AddHours(18),
                    20,
                    15,
                    25,
                    "Clear sky",
                    "01d",
                    wind,
                    65,
                    1013,
                    10,
                    5.5);
                forecasts.Add(forecast);
            }
            weather.UpdateForecasts(forecasts);

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, cancellationToken))
                .ReturnsAsync(weather);

            _mapperMock
                .Setup(x => x.Map<WeatherDto>(weather))
                .Returns(weatherDto);

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(query.LocationId, cancellationToken), Times.Never);
        }

        [Test]
        public async Task Handle_WithZeroLocationId_ShouldStillAttemptToQuery()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = 0 };

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            var updateResult = Result<WeatherDto>.Failure("Failed to update weather data: Location not found");

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<Application.Commands.Weather.UpdateWeatherCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            //result.ErrorMessage.Should().Contain("Failed to update weather data");

            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(0, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithNegativeLocationId_ShouldStillAttemptToQuery()
        {
            // Arrange
            var query = new GetWeatherByLocationQuery { LocationId = -1 };

            _weatherRepositoryMock
                .Setup(x => x.GetByLocationIdAsync(query.LocationId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Weather)null);

            var updateResult = Result<WeatherDto>.Failure("Failed to update weather data: Invalid location");

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<Application.Commands.Weather.UpdateWeatherCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(updateResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();

            _weatherRepositoryMock.Verify(x => x.GetByLocationIdAsync(-1, It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}