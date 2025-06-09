using FluentAssertions;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Services
{
    [TestFixture]
    public class EnhancedSunServiceTests
    {
        private SunService _sunService;
        private Mock<ISunCalculatorService> _sunCalculatorServiceMock;

        [SetUp]
        public void SetUp()
        {
            _sunCalculatorServiceMock = new Mock<ISunCalculatorService>();
            _sunService = new SunService(_sunCalculatorServiceMock.Object);
        }

        #region Batch Operations Tests

        [Test]
        public async Task GetBatchSunPositionsAsync_WithValidRequests_ShouldReturnAllPositions()
        {
            // Arrange
            var requests = new List<(double latitude, double longitude, DateTime dateTime)>
            {
                (47.6062, -122.3321, new DateTime(2024, 5, 15, 12, 0, 0)),
                (40.7128, -74.0060, new DateTime(2024, 5, 15, 12, 0, 0)),
                (51.5074, -0.1278, new DateTime(2024, 5, 15, 12, 0, 0))
            };

            // Set up mocks for each request
            foreach (var request in requests)
            {
                _sunCalculatorServiceMock
                    .Setup(x => x.GetSolarAzimuth(request.dateTime, request.latitude, request.longitude, It.IsAny<string>()))
                    .Returns(180.0);
                _sunCalculatorServiceMock
                    .Setup(x => x.GetSolarElevation(request.dateTime, request.latitude, request.longitude, It.IsAny<string>()))
                    .Returns(45.0);
            }

            // Act
            var result = await _sunService.GetBatchSunPositionsAsync(requests, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.All(p => p.Azimuth == 180.0).Should().BeTrue();
            result.Data.All(p => p.Elevation == 45.0).Should().BeTrue();
        }

        [Test]
        public async Task GetBatchSunPositionsAsync_WithEmptyRequests_ShouldReturnEmptyList()
        {
            // Arrange
            var requests = new List<(double latitude, double longitude, DateTime dateTime)>();

            // Act
            var result = await _sunService.GetBatchSunPositionsAsync(requests, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task GetBatchSunPositionsAsync_WithNullRequests_ShouldReturnEmptyList()
        {
            // Act
            var result = await _sunService.GetBatchSunPositionsAsync(null, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task GetBatchSunTimesAsync_WithValidRequests_ShouldReturnAllSunTimes()
        {
            // Arrange
            var requests = new List<(double latitude, double longitude, DateTime date)>
            {
                (47.6062, -122.3321, new DateTime(2024, 5, 15)),
                (40.7128, -74.0060, new DateTime(2024, 5, 16)),
                (51.5074, -0.1278, new DateTime(2024, 5, 17))
            };

            // Set up mocks for each request
            foreach (var request in requests)
            {
                SetupSunTimeMocks(request.date, request.latitude, request.longitude);
            }

            // Act
            var result = await _sunService.GetBatchSunTimesAsync(requests, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.All(s => s != null).Should().BeTrue();
        }

        [Test]
        public async Task GetBatchSunTimesAsync_WithCancellationToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var requests = new List<(double latitude, double longitude, DateTime date)>
            {
                (47.6062, -122.3321, new DateTime(2024, 5, 15))
            };

            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _sunService.GetBatchSunTimesAsync(requests, cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region Date Range Tests

        [Test]
        public async Task GetSunTimesRangeAsync_WithValidDateRange_ShouldReturnAllDates()
        {
            // Arrange
            var startDate = new DateTime(2024, 5, 15);
            var endDate = new DateTime(2024, 5, 17);
            var latitude = 47.6062;
            var longitude = -122.3321;

            // Set up mocks for each date in range
            for (var date = startDate; date <= endDate; date = date.AddDays(1))
            {
                SetupSunTimeMocks(date, latitude, longitude);
            }

            // Act
            var result = await _sunService.GetSunTimesRangeAsync(latitude, longitude, startDate, endDate, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3); // 3 days
            result.Data.Keys.Should().Contain(startDate.Date);
            result.Data.Keys.Should().Contain(startDate.AddDays(1).Date);
            result.Data.Keys.Should().Contain(endDate.Date);
        }

        [Test]
        public async Task GetSunTimesRangeAsync_WithSingleDay_ShouldReturnOneDate()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            var latitude = 47.6062;
            var longitude = -122.3321;

            SetupSunTimeMocks(date, latitude, longitude);

            // Act
            var result = await _sunService.GetSunTimesRangeAsync(latitude, longitude, date, date, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(1);
            result.Data.Keys.Should().Contain(date.Date);
        }

        [Test]
        public async Task GetSunTimesRangeAsync_WithEndBeforeStart_ShouldReturnEmpty()
        {
            // Arrange
            var startDate = new DateTime(2024, 5, 17);
            var endDate = new DateTime(2024, 5, 15);
            var latitude = 47.6062;
            var longitude = -122.3321;

            // Act
            var result = await _sunService.GetSunTimesRangeAsync(latitude, longitude, startDate, endDate, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        #endregion

        #region Optimal Photo Times Tests

        [Test]
        public async Task GetOptimalPhotoTimesAsync_WithValidData_ShouldReturnOptimalTimes()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            var latitude = 47.6062;
            var longitude = -122.3321;

            SetupSunTimeMocks(date, latitude, longitude);

            // Act
            var result = await _sunService.GetOptimalPhotoTimesAsync(latitude, longitude, date, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            result.Data.Should().HaveCountGreaterThan(3); // Should have blue hour, golden hour times

            // Verify we have expected time types
            var timeTypes = result.Data.Select(t => t.Type).ToList();
            timeTypes.Should().Contain("Blue Hour");
            timeTypes.Should().Contain("Golden Hour");
        }

        [Test]
        public async Task GetOptimalPhotoTimesAsync_WithSunServiceFailure_ShouldReturnFailure()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            var latitude = 47.6062;
            var longitude = -122.3321;

            _sunCalculatorServiceMock
                .Setup(x => x.GetSunrise(date, latitude, longitude, It.IsAny<string>()))
                .Throws(new Exception("Calculator error"));

            // Act
            var result = await _sunService.GetOptimalPhotoTimesAsync(latitude, longitude, date, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Calculator error");
        }

        [Test]
        public async Task GetOptimalPhotoTimesAsync_ShouldReturnTimesInOrder()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            var latitude = 47.6062;
            var longitude = -122.3321;

            SetupSunTimeMocks(date, latitude, longitude);

            // Act
            var result = await _sunService.GetOptimalPhotoTimesAsync(latitude, longitude, date, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            var times = result.Data.Select(t => t.StartTime).ToList();
            times.Should().BeInAscendingOrder();
        }

        #endregion

        #region Cache Tests

        [Test]
        public void CleanupExpiredCache_ShouldNotThrow()
        {
            // Act & Assert
            FluentActions.Invoking(() => _sunService.CleanupExpiredCache())
                .Should().NotThrow();
        }

        [Test]
        public async Task PreloadUpcomingSunDataAsync_WithValidParameters_ShouldNotThrow()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;

            // Set up mocks for multiple days
            for (int i = 0; i <= 7; i++)
            {
                var date = DateTime.Today.AddDays(i);
                SetupSunTimeMocks(date, latitude, longitude);
            }

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _sunService.PreloadUpcomingSunDataAsync(latitude, longitude, 7, CancellationToken.None))
                .Should().NotThrowAsync();
        }

        [Test]
        public async Task PreloadUpcomingSunDataAsync_WithCancellationToken_ShouldNotThrowDueToCatchAll()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            // Note: The actual SunService implementation catches all exceptions including OperationCanceledException
            // because preloading is optional, so this should not throw
            await FluentActions.Invoking(async () =>
                await _sunService.PreloadUpcomingSunDataAsync(latitude, longitude, 7, cts.Token))
                .Should().NotThrowAsync();
        }

        #endregion

        #region Timezone Parameter Validation Tests

        [Test]
        public async Task GetSunPositionAsync_ShouldPassCorrectTimezoneToCalculator()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;
            var dateTime = new DateTime(2024, 5, 15, 12, 0, 0);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(dateTime, latitude, longitude, It.IsAny<string>()))
                .Returns(180.0);
            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(dateTime, latitude, longitude, It.IsAny<string>()))
                .Returns(45.0);

            // Act
            await _sunService.GetSunPositionAsync(latitude, longitude, dateTime, CancellationToken.None);

            // Assert
            _sunCalculatorServiceMock.Verify(x => x.GetSolarAzimuth(
                dateTime, latitude, longitude,
                It.Is<string>(tz => !string.IsNullOrEmpty(tz))), Times.Once);
            _sunCalculatorServiceMock.Verify(x => x.GetSolarElevation(
                dateTime, latitude, longitude,
                It.Is<string>(tz => !string.IsNullOrEmpty(tz))), Times.Once);
        }

        [Test]
        public async Task GetSunTimesAsync_ShouldPassCorrectTimezoneToAllCalculatorMethods()
        {
            // Arrange
            var date = new DateTime(2024, 5, 15);
            var latitude = 47.6062;
            var longitude = -122.3321;

            SetupSunTimeMocks(date, latitude, longitude);

            // Act
            await _sunService.GetSunTimesAsync(latitude, longitude, date, CancellationToken.None);

            // Assert - Verify timezone is passed to all methods
            _sunCalculatorServiceMock.Verify(x => x.GetSunrise(
                date, latitude, longitude,
                It.Is<string>(tz => !string.IsNullOrEmpty(tz))), Times.Once);
            _sunCalculatorServiceMock.Verify(x => x.GetSunset(
                date, latitude, longitude,
                It.Is<string>(tz => !string.IsNullOrEmpty(tz))), Times.Once);
            _sunCalculatorServiceMock.Verify(x => x.GetSolarNoon(
                date, latitude, longitude,
                It.Is<string>(tz => !string.IsNullOrEmpty(tz))), Times.Once);
        }

        #endregion

        #region Performance and Edge Case Tests

        [Test]
        public async Task GetSunPositionAsync_WithCaching_ShouldUseCachedResult()
        {
            // Arrange
            var latitude = 47.6062;
            var longitude = -122.3321;
            var dateTime = new DateTime(2024, 5, 15, 12, 0, 0);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(dateTime, latitude, longitude, It.IsAny<string>()))
                .Returns(180.0);
            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(dateTime, latitude, longitude, It.IsAny<string>()))
                .Returns(45.0);

            // Act - Call twice with same parameters
            var result1 = await _sunService.GetSunPositionAsync(latitude, longitude, dateTime, CancellationToken.None);
            var result2 = await _sunService.GetSunPositionAsync(latitude, longitude, dateTime, CancellationToken.None);

            // Assert
            result1.IsSuccess.Should().BeTrue();
            result2.IsSuccess.Should().BeTrue();
            result1.Data.Azimuth.Should().Be(result2.Data.Azimuth);
            result1.Data.Elevation.Should().Be(result2.Data.Elevation);
        }

        [Test]
        public async Task GetSunTimesAsync_WithExtremeCoordinates_ShouldHandleGracefully()
        {
            // Arrange - North Pole
            var date = new DateTime(2024, 6, 21); // Summer solstice
            var latitude = 90.0;
            var longitude = 0.0;

            SetupSunTimeMocks(date, latitude, longitude);

            // Act
            var result = await _sunService.GetSunTimesAsync(latitude, longitude, date, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Test]
        public async Task GetSunPositionAsync_WithExtremeDateTime_ShouldHandleGracefully()
        {
            // Arrange - Far future date
            var latitude = 47.6062;
            var longitude = -122.3321;
            var dateTime = new DateTime(2100, 12, 31, 23, 59, 59);

            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarAzimuth(dateTime, latitude, longitude, It.IsAny<string>()))
                .Returns(270.0);
            _sunCalculatorServiceMock
                .Setup(x => x.GetSolarElevation(dateTime, latitude, longitude, It.IsAny<string>()))
                .Returns(-45.0);

            // Act
            var result = await _sunService.GetSunPositionAsync(latitude, longitude, dateTime, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Azimuth.Should().Be(270.0);
            result.Data.Elevation.Should().Be(-45.0);
        }

        #endregion

        #region Helper Methods

        private void SetupSunTimeMocks(DateTime date, double latitude, double longitude)
        {
            _sunCalculatorServiceMock.Setup(x => x.GetSunrise(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(6));
            _sunCalculatorServiceMock.Setup(x => x.GetSunset(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(20));
            _sunCalculatorServiceMock.Setup(x => x.GetSolarNoon(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(13));
            _sunCalculatorServiceMock.Setup(x => x.GetCivilDawn(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(5).AddMinutes(30));
            _sunCalculatorServiceMock.Setup(x => x.GetCivilDusk(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(20).AddMinutes(30));
            _sunCalculatorServiceMock.Setup(x => x.GetNauticalDawn(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(5));
            _sunCalculatorServiceMock.Setup(x => x.GetNauticalDusk(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(21));
            _sunCalculatorServiceMock.Setup(x => x.GetAstronomicalDawn(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(4).AddMinutes(30));
            _sunCalculatorServiceMock.Setup(x => x.GetAstronomicalDusk(date, latitude, longitude, It.IsAny<string>()))
                .Returns(date.Date.AddHours(21).AddMinutes(30));
        }

        #endregion
    }
}