using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Moq;
using NUnit.Framework;
using static Location.Photography.Application.Commands.SunLocation.CalculateSunTimesCommand;

namespace Location.Photography.Application.Tests.Queries.SunLocation
{
    [TestFixture]
    public class CalculateSunTimesCommandHandlerTests
    {
        private Mock<ISunService> _sunServiceMock;
        private CalculateSunTimesCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _sunServiceMock = new Mock<ISunService>();
            _handler = new CalculateSunTimesCommandHandler(_sunServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullSunService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new CalculateSunTimesCommandHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithValidCoordinates_ShouldReturnSunTimes()
        {
            // Arrange
            var command = new CalculateSunTimesCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 6, 15)
            };

            var now = DateTime.Now;
            var sunTimes = new SunTimesDto
            {
                Date = command.Date,
                Latitude = command.Latitude,
                Longitude = command.Longitude,
                Sunrise = now.Date.AddHours(5).AddMinutes(30),
                Sunset = now.Date.AddHours(20).AddMinutes(45),
                SolarNoon = now.Date.AddHours(13).AddMinutes(10),
                AstronomicalDawn = now.Date.AddHours(3).AddMinutes(45),
                AstronomicalDusk = now.Date.AddHours(22).AddMinutes(30),
                NauticalDawn = now.Date.AddHours(4).AddMinutes(20),
                NauticalDusk = now.Date.AddHours(21).AddMinutes(55),
                CivilDawn = now.Date.AddHours(5).AddMinutes(0),
                CivilDusk = now.Date.AddHours(21).AddMinutes(15),
                GoldenHourMorningStart = now.Date.AddHours(5).AddMinutes(30),
                GoldenHourMorningEnd = now.Date.AddHours(6).AddMinutes(30),
                GoldenHourEveningStart = now.Date.AddHours(19).AddMinutes(45),
                GoldenHourEveningEnd = now.Date.AddHours(20).AddMinutes(45)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    command.Latitude,
                    command.Longitude,
                    command.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Date.Should().Be(command.Date);
            result.Data.Latitude.Should().Be(command.Latitude);
            result.Data.Longitude.Should().Be(command.Longitude);
            result.Data.Sunrise.Should().Be(sunTimes.Sunrise);
            result.Data.Sunset.Should().Be(sunTimes.Sunset);
            result.Data.SolarNoon.Should().Be(sunTimes.SolarNoon);
            result.Data.AstronomicalDawn.Should().Be(sunTimes.AstronomicalDawn);
            result.Data.AstronomicalDusk.Should().Be(sunTimes.AstronomicalDusk);
            result.Data.NauticalDawn.Should().Be(sunTimes.NauticalDawn);
            result.Data.NauticalDusk.Should().Be(sunTimes.NauticalDusk);
            result.Data.CivilDawn.Should().Be(sunTimes.CivilDawn);
            result.Data.CivilDusk.Should().Be(sunTimes.CivilDusk);
            result.Data.GoldenHourMorningStart.Should().Be(sunTimes.GoldenHourMorningStart);
            result.Data.GoldenHourMorningEnd.Should().Be(sunTimes.GoldenHourMorningEnd);
            result.Data.GoldenHourEveningStart.Should().Be(sunTimes.GoldenHourEveningStart);
            result.Data.GoldenHourEveningEnd.Should().Be(sunTimes.GoldenHourEveningEnd);

            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                command.Latitude,
                command.Longitude,
                command.Date,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WhenSunServiceFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new CalculateSunTimesCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 6, 15)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    command.Latitude,
                    command.Longitude,
                    command.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Failure("Failed to calculate sun times"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to calculate sun times");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new CalculateSunTimesCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 6, 15)
            };
            var cancellationToken = new CancellationToken();

            var sunTimes = new SunTimesDto
            {
                Date = command.Date,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    command.Latitude,
                    command.Longitude,
                    command.Date,
                    cancellationToken))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                command.Latitude,
                command.Longitude,
                command.Date,
                cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            var command = new CalculateSunTimesCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 6, 15)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    command.Latitude,
                    command.Longitude,
                    command.Date,
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        [Test]
        public async Task Handle_ShouldReturnResultFromSunService()
        {
            // Arrange
            var command = new CalculateSunTimesCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 6, 15)
            };

            var sunTimes = new SunTimesDto
            {
                Date = command.Date,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            };

            var serviceResult = Result<SunTimesDto>.Success(sunTimes);

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    command.Latitude,
                    command.Longitude,
                    command.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(serviceResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().BeSameAs(serviceResult);
        }

        [Test]
        public async Task Handle_WithExtremeDateValues_ShouldWork()
        {
            // Arrange
            var command = new CalculateSunTimesCommand
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 12, 21) // Winter solstice
            };

            var sunTimes = new SunTimesDto
            {
                Date = command.Date,
                Latitude = command.Latitude,
                Longitude = command.Longitude
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    command.Latitude,
                    command.Longitude,
                    command.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }
    }
}