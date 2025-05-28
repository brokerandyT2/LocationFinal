using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using Location.Core.Infrastructure.UnitOfWork;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Weather.Queries.GetWeatherForecast
{
    [Category("Weather")]
    [Category("Get")]
    [TestFixture]
    public class GetWeatherForecastQueryHandlerTests
    {
        private Mock<IWeatherService> _weatherServiceMock;
        private Mock<IMapper> _mapperMock;
        private GetWeatherForecastQueryHandler _handler;
        private IUnitOfWork _uow;
        [SetUp]
        public void SetUp()
        {
            _weatherServiceMock = new Mock<IWeatherService>();
            _mapperMock = new Mock<IMapper>();
            //_uow = new UnitOfWork();
            _handler = new GetWeatherForecastQueryHandler(
                _weatherServiceMock.Object,
                _mapperMock.Object, _uow);
        }

        [Test]
        public async Task Handle_WithValidCoordinates_ShouldReturnForecast()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 7
            };

            var forecastDto = TestDataBuilder.CreateValidWeatherForecastDto();

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Days,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherForecastDto>.Success(forecastDto));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().Be(forecastDto);
            result.Data.DailyForecasts.Should().HaveCount(7);

            _weatherServiceMock.Verify(x => x.GetForecastAsync(
                query.Latitude,
                query.Longitude,
                query.Days,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithDefaultDays_ShouldUseSevenDays()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321
                // Days defaults to 7
            };

            var forecastDto = TestDataBuilder.CreateValidWeatherForecastDto();

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    7,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherForecastDto>.Success(forecastDto));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            _weatherServiceMock.Verify(x => x.GetForecastAsync(
                query.Latitude,
                query.Longitude,
                7,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithReducedDays_ShouldRequestCorrectAmount()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 40.7128,
                Longitude = -74.0060,
                Days = 3
            };

            var forecastDto = new WeatherForecastDto
            {
                DailyForecasts = TestDataBuilder.CreateValidDailyForecasts(3)
            };

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Days,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherForecastDto>.Success(forecastDto));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.DailyForecasts.Should().HaveCount(3);
        }

        [Test]
        public async Task Handle_WhenWeatherServiceFails_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 7
            };

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Days,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherForecastDto>.Failure("Weather API error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Weather API error");
        }

        [Test]
        public async Task Handle_WithInvalidCoordinates_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 91.0, // Invalid
                Longitude = -122.3321,
                Days = 7
            };

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Days,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherForecastDto>.Failure("Invalid coordinates"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid coordinates");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 7
            };

            var cancellationToken = new CancellationToken();
            var forecastDto = TestDataBuilder.CreateValidWeatherForecastDto();

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Days,
                    cancellationToken))
                .ReturnsAsync(Result<WeatherForecastDto>.Success(forecastDto));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _weatherServiceMock.Verify(x => x.GetForecastAsync(
                query.Latitude,
                query.Longitude,
                query.Days,
                cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WithExtremeCoordinates_ShouldWork()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = -89.9999, // Near South Pole
                Longitude = 179.9999, // Near date line
                Days = 5
            };

            var forecastDto = TestDataBuilder.CreateValidWeatherForecastDto();

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Days,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherForecastDto>.Success(forecastDto));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task Handle_WithOneDayForecast_ShouldWork()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 1
            };

            var forecastDto = new WeatherForecastDto
            {
                DailyForecasts = TestDataBuilder.CreateValidDailyForecasts(1)
            };

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Days,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<WeatherForecastDto>.Success(forecastDto));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.DailyForecasts.Should().HaveCount(1);
        }

        [Test]
        public async Task Handle_WithException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 7
            };

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<int>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve weather forecast");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        [Test]
        public async Task Handle_ShouldReturnForecastDirectlyFromService()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 7
            };

            var serviceResult = Result<WeatherForecastDto>.Success(TestDataBuilder.CreateValidWeatherForecastDto());

            _weatherServiceMock
                .Setup(x => x.GetForecastAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Days,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().Be(serviceResult.IsSuccess);
            result.Data.Should().BeEquivalentTo(serviceResult.Data);
        }
    }
}