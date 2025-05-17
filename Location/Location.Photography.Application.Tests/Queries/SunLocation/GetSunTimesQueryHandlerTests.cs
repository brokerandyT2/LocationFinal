using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Queries.SunLocation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Queries.SunLocation
{
    [TestFixture]
    public class GetSunTimesQueryHandlerTests
    {
        private GetSunTimesQuery.GetSunTimesQueryHandler _handler;
        private Mock<ISunService> _sunServiceMock;

        [SetUp]
        public void SetUp()
        {
            _sunServiceMock = new Mock<ISunService>();
            _handler = new GetSunTimesQuery.GetSunTimesQueryHandler(_sunServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullSunService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetSunTimesQuery.GetSunTimesQueryHandler(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("sunService");
        }

        [Test]
        public async Task Handle_WithValidQuery_ShouldReturnSunTimes()
        {
            // Arrange
            var query = new GetSunTimesQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 5, 15)
            };

            var date = query.Date;
            var sunTimes = new SunTimesDto
            {
                Date = date,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                Sunrise = date.AddHours(5).AddMinutes(30),
                Sunset = date.AddHours(20).AddMinutes(45),
                SolarNoon = date.AddHours(13).AddMinutes(7),
                CivilDawn = date.AddHours(5),
                CivilDusk = date.AddHours(21).AddMinutes(15),
                NauticalDawn = date.AddHours(4).AddMinutes(30),
                NauticalDusk = date.AddHours(21).AddMinutes(45),
                AstronomicalDawn = date.AddHours(4),
                AstronomicalDusk = date.AddHours(22).AddMinutes(15),
                GoldenHourMorningStart = date.AddHours(5).AddMinutes(30),
                GoldenHourMorningEnd = date.AddHours(6).AddMinutes(30),
                GoldenHourEveningStart = date.AddHours(19).AddMinutes(45),
                GoldenHourEveningEnd = date.AddHours(20).AddMinutes(45)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().Be(sunTimes);

            // Verify solar times are calculated correctly
            result.Data.Sunrise.TimeOfDay.Should().Be(TimeSpan.FromHours(5.5));
            result.Data.Sunset.TimeOfDay.Should().Be(TimeSpan.FromHours(20.75));
            result.Data.SolarNoon.TimeOfDay.Should().BeCloseTo(TimeSpan.FromHours(13.12), TimeSpan.FromMinutes(1));

            // Verify golden hour calculations
            result.Data.GoldenHourMorningStart.Should().Be(sunTimes.Sunrise);
            result.Data.GoldenHourMorningEnd.Should().Be(sunTimes.Sunrise.AddHours(1));
            result.Data.GoldenHourEveningStart.Should().Be(sunTimes.Sunset.AddHours(-1));
            result.Data.GoldenHourEveningEnd.Should().Be(sunTimes.Sunset);

            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                query.Latitude,
                query.Longitude,
                query.Date,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WhenServiceReturnsFailure_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetSunTimesQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 5, 15)
            };

            var errorMessage = "Error calculating sun times";

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Failure(errorMessage));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be(errorMessage);
        }

        [Test]
        public async Task Handle_WithExtremeLatitude_ShouldPassToService()
        {
            // Arrange - Arctic Circle in summer
            var query = new GetSunTimesQuery
            {
                Latitude = 78.0, // Far north
                Longitude = 15.0,
                Date = new DateTime(2024, 6, 21) // Summer solstice
            };

            var date = query.Date;
            var sunTimes = new SunTimesDto
            {
                Date = date,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                // In polar day, no sunrise/sunset (they're the same time)
                Sunrise = date.AddHours(0),
                Sunset = date.AddHours(0),
                SolarNoon = date.AddHours(12),
                // Other properties set to valid values
                CivilDawn = date.AddHours(0),
                CivilDusk = date.AddHours(0),
                NauticalDawn = date.AddHours(0),
                NauticalDusk = date.AddHours(0),
                AstronomicalDawn = date.AddHours(0),
                AstronomicalDusk = date.AddHours(0),
                GoldenHourMorningStart = date.AddHours(0),
                GoldenHourMorningEnd = date.AddHours(1),
                GoldenHourEveningStart = date.AddHours(23),
                GoldenHourEveningEnd = date.AddHours(0)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the extreme latitude was passed to the service
            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                78.0,
                It.IsAny<double>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExtremeSouthLatitude_ShouldPassToService()
        {
            // Arrange - Antarctic Circle in winter
            var query = new GetSunTimesQuery
            {
                Latitude = -78.0, // Far south
                Longitude = 0.0,
                Date = new DateTime(2024, 6, 21) // Winter in southern hemisphere
            };

            var date = query.Date;
            var sunTimes = new SunTimesDto
            {
                Date = date,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                // In polar night, no sunrise/sunset (same time)
                Sunrise = date.AddHours(12),
                Sunset = date.AddHours(12),
                SolarNoon = date.AddHours(12),
                // Other properties set to valid values
                CivilDawn = date.AddHours(12),
                CivilDusk = date.AddHours(12),
                NauticalDawn = date.AddHours(12),
                NauticalDusk = date.AddHours(12),
                AstronomicalDawn = date.AddHours(12),
                AstronomicalDusk = date.AddHours(12),
                GoldenHourMorningStart = date.AddHours(12),
                GoldenHourMorningEnd = date.AddHours(13),
                GoldenHourEveningStart = date.AddHours(11),
                GoldenHourEveningEnd = date.AddHours(12)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the extreme latitude was passed to the service
            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                -78.0,
                It.IsAny<double>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExtremeLongitude_ShouldPassToService()
        {
            // Arrange
            var query = new GetSunTimesQuery
            {
                Latitude = 47.6062,
                Longitude = 179.9, // Near International Date Line
                Date = new DateTime(2024, 5, 15)
            };

            var date = query.Date;
            var sunTimes = new SunTimesDto
            {
                Date = date,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                Sunrise = date.AddHours(5),
                Sunset = date.AddHours(19),
                SolarNoon = date.AddHours(12),
                CivilDawn = date.AddHours(4).AddMinutes(30),
                CivilDusk = date.AddHours(19).AddMinutes(30),
                NauticalDawn = date.AddHours(4),
                NauticalDusk = date.AddHours(20),
                AstronomicalDawn = date.AddHours(3).AddMinutes(30),
                AstronomicalDusk = date.AddHours(20).AddMinutes(30),
                GoldenHourMorningStart = date.AddHours(5),
                GoldenHourMorningEnd = date.AddHours(6),
                GoldenHourEveningStart = date.AddHours(18),
                GoldenHourEveningEnd = date.AddHours(19)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the extreme longitude was passed to the service
            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                It.IsAny<double>(),
                179.9,
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithHistoricalDate_ShouldPassToService()
        {
            // Arrange
            var query = new GetSunTimesQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(1900, 1, 1) // Historical date
            };

            var date = query.Date;
            var sunTimes = new SunTimesDto
            {
                Date = date,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                Sunrise = date.AddHours(7).AddMinutes(30),
                Sunset = date.AddHours(16).AddMinutes(30),
                SolarNoon = date.AddHours(12),
                // Other properties set similarly
                CivilDawn = date.AddHours(7),
                CivilDusk = date.AddHours(17),
                NauticalDawn = date.AddHours(6).AddMinutes(30),
                NauticalDusk = date.AddHours(17).AddMinutes(30),
                AstronomicalDawn = date.AddHours(6),
                AstronomicalDusk = date.AddHours(18),
                GoldenHourMorningStart = date.AddHours(7).AddMinutes(30),
                GoldenHourMorningEnd = date.AddHours(8).AddMinutes(30),
                GoldenHourEveningStart = date.AddHours(15).AddMinutes(30),
                GoldenHourEveningEnd = date.AddHours(16).AddMinutes(30)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the historical date was passed to the service
            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                new DateTime(1900, 1, 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithFutureDate_ShouldPassToService()
        {
            // Arrange
            var query = new GetSunTimesQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2050, 1, 1) // Future date
            };

            var date = query.Date;
            var sunTimes = new SunTimesDto
            {
                Date = date,
                Latitude = query.Latitude,
                Longitude = query.Longitude,
                Sunrise = date.AddHours(7).AddMinutes(30),
                Sunset = date.AddHours(16).AddMinutes(30),
                SolarNoon = date.AddHours(12),
                // Other properties set similarly
                CivilDawn = date.AddHours(7),
                CivilDusk = date.AddHours(17),
                NauticalDawn = date.AddHours(6).AddMinutes(30),
                NauticalDusk = date.AddHours(17).AddMinutes(30),
                AstronomicalDawn = date.AddHours(6),
                AstronomicalDusk = date.AddHours(18),
                GoldenHourMorningStart = date.AddHours(7).AddMinutes(30),
                GoldenHourMorningEnd = date.AddHours(8).AddMinutes(30),
                GoldenHourEveningStart = date.AddHours(15).AddMinutes(30),
                GoldenHourEveningEnd = date.AddHours(16).AddMinutes(30)
            };

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Date,
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SunTimesDto>.Success(sunTimes));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();

            // Verify the future date was passed to the service
            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                It.IsAny<double>(),
                It.IsAny<double>(),
                new DateTime(2050, 1, 1),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItToService()
        {
            // Arrange
            var query = new GetSunTimesQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Date = new DateTime(2024, 5, 15)
            };

            var cancellationToken = new CancellationToken();

            _sunServiceMock
                .Setup(x => x.GetSunTimesAsync(
                    query.Latitude,
                    query.Longitude,
                    query.Date,
                    cancellationToken))
                .ReturnsAsync(Result<SunTimesDto>.Success(new SunTimesDto()));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _sunServiceMock.Verify(x => x.GetSunTimesAsync(
                query.Latitude,
                query.Longitude,
                query.Date,
                cancellationToken), Times.Once);
        }
    }
}