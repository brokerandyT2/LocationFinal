using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.Queries.GetAllWeather;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Weather.Queries.GetAllWeather
{
    [TestFixture]
    public class GetAllWeatherQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IWeatherRepository> _weatherRepositoryMock;
        private Mock<IMapper> _mapperMock;
        private GetAllWeatherQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _weatherRepositoryMock = new Mock<IWeatherRepository>();
            _mapperMock = new Mock<IMapper>();

            _unitOfWorkMock.Setup(u => u.Weather).Returns(_weatherRepositoryMock.Object);

            _handler = new GetAllWeatherQueryHandler(
                _unitOfWorkMock.Object,
                _mapperMock.Object);
        }

        [Test]
        public async Task Handle_WithIncludeExpiredFalse_ShouldReturnRecentWeather()
        {
            // Arrange
            var query = new GetAllWeatherQuery { IncludeExpired = false };
            var recentWeather = CreateWeatherList(10);

            _weatherRepositoryMock
                .Setup(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(recentWeather);

            var weatherDtos = CreateWeatherDtoList(10);

            SetupMapperForWeatherList(recentWeather, weatherDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(10);

            _weatherRepositoryMock.Verify(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()), Times.Once);
            _weatherRepositoryMock.Verify(x => x.GetRecentAsync(int.MaxValue, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithIncludeExpiredTrue_ShouldReturnAllWeather()
        {
            // Arrange
            var query = new GetAllWeatherQuery { IncludeExpired = true };
            var allWeather = CreateWeatherList(25);

            _weatherRepositoryMock
                .Setup(x => x.GetRecentAsync(int.MaxValue, It.IsAny<CancellationToken>()))
                .ReturnsAsync(allWeather);

            var weatherDtos = CreateWeatherDtoList(25);

            SetupMapperForWeatherList(allWeather, weatherDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(25);

            _weatherRepositoryMock.Verify(x => x.GetRecentAsync(int.MaxValue, It.IsAny<CancellationToken>()), Times.Once);
            _weatherRepositoryMock.Verify(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithNoWeatherData_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetAllWeatherQuery { IncludeExpired = false };
            var emptyList = new List<Domain.Entities.Weather>();

            _weatherRepositoryMock
                .Setup(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyList);

            SetupMapperForWeatherList(emptyList, new List<WeatherDto>());

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetAllWeatherQuery { IncludeExpired = false };

            _weatherRepositoryMock
                .Setup(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve weather data");
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetAllWeatherQuery { IncludeExpired = false };
            var cancellationToken = new CancellationToken();
            var weatherList = CreateWeatherList(3);

            _weatherRepositoryMock
                .Setup(x => x.GetRecentAsync(10, cancellationToken))
                .ReturnsAsync(weatherList);

            SetupMapperForWeatherList(weatherList, CreateWeatherDtoList(3));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _weatherRepositoryMock.Verify(x => x.GetRecentAsync(10, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldMapEachWeatherToDtoIndividually()
        {
            // Arrange
            var query = new GetAllWeatherQuery { IncludeExpired = false };
            var weatherList = CreateWeatherList(3);

            _weatherRepositoryMock
                .Setup(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(weatherList);

            foreach (var weather in weatherList)
            {
                var dto = new WeatherDto
                {
                    Id = weather.Id,
                    LocationId = weather.LocationId,
                    Latitude = weather.Coordinate.Latitude,
                    Longitude = weather.Coordinate.Longitude,
                    Timezone = weather.Timezone,
                    TimezoneOffset = weather.TimezoneOffset,
                    LastUpdate = weather.LastUpdate
                };

                _mapperMock
                    .Setup(x => x.Map<WeatherDto>(weather))
                    .Returns(dto);
            }

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);

            foreach (var weather in weatherList)
            {
                _mapperMock.Verify(x => x.Map<WeatherDto>(weather), Times.Once);
            }
        }

        [Test]
        public async Task Handle_ShouldPreserveOrderFromRepository()
        {
            // Arrange
            var query = new GetAllWeatherQuery { IncludeExpired = false };
            var weatherList = CreateWeatherList(5);

            // Set specific IDs to verify order
            for (int i = 0; i < weatherList.Count; i++)
            {
                SetPrivateProperty(weatherList[i], "Id", i + 100);
            }

            _weatherRepositoryMock
                .Setup(x => x.GetRecentAsync(10, It.IsAny<CancellationToken>()))
                .ReturnsAsync(weatherList);

            var weatherDtos = weatherList.Select(w => new WeatherDto
            {
                Id = w.Id,
                LocationId = w.LocationId
            }).ToList();

            SetupMapperForWeatherList(weatherList, weatherDtos);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(5);
            result.Data.Select(d => d.Id).Should().ContainInOrder(100, 101, 102, 103, 104);
        }

        [Test]
        public async Task Handle_WithLargDataset_ShouldProcessSuccessfully()
        {
            // Arrange
            var query = new GetAllWeatherQuery { IncludeExpired = true };
            var largeWeatherList = CreateWeatherList(1000);

            _weatherRepositoryMock
                .Setup(x => x.GetRecentAsync(int.MaxValue, It.IsAny<CancellationToken>()))
                .ReturnsAsync(largeWeatherList);

            var largeDtoList = CreateWeatherDtoList(1000);
            SetupMapperForWeatherList(largeWeatherList, largeDtoList);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1000);
        }

        #region Helper Methods

        private List<Domain.Entities.Weather> CreateWeatherList(int count)
        {
            var list = new List<Domain.Entities.Weather>();
            for (int i = 0; i < count; i++)
            {
                var weather = TestDataBuilder.CreateValidWeather(i + 1);
                SetPrivateProperty(weather, "Id", i + 1);
                list.Add(weather);
            }
            return list;
        }

        private List<WeatherDto> CreateWeatherDtoList(int count)
        {
            var list = new List<WeatherDto>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new WeatherDto
                {
                    Id = i + 1,
                    LocationId = i + 1,
                    Temperature = 20.0 + i,
                    Description = $"Weather {i + 1}",
                    LastUpdate = DateTime.UtcNow.AddHours(-i)
                });
            }
            return list;
        }

        private void SetupMapperForWeatherList(IEnumerable<Domain.Entities.Weather> weatherList, List<WeatherDto> dtos)
        {
            var weatherArray = weatherList.ToArray();
            for (int i = 0; i < weatherArray.Length; i++)
            {
                _mapperMock
                    .Setup(x => x.Map<WeatherDto>(weatherArray[i]))
                    .Returns(dtos[i]);
            }
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            property?.SetValue(obj, value);
        }

        #endregion
    }
}