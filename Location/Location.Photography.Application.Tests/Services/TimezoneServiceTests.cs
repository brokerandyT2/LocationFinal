using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Services
{
    [TestFixture]
    public class TimezoneServiceTests
    {
        private TimezoneService _timezoneService;

        [SetUp]
        public void SetUp()
        {
            _timezoneService = new TimezoneService();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_ShouldInitializeWithoutError()
        {
            // Act
            var service = new TimezoneService();

            // Assert
            service.Should().NotBeNull();
        }

        #endregion

        #region GetTimezoneFromCoordinatesAsync Tests

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithNewYorkCoordinates_ShouldReturnAmericaNewYork()
        {
            // Arrange
            double latitude = 40.7128;
            double longitude = -74.0060;

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("America/New_York");
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithLosAngelesCoordinates_ShouldReturnAmericaLosAngeles()
        {
            // Arrange
            double latitude = 34.0522;
            double longitude = -118.2437;

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("America/Los_Angeles");
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithChicagoCoordinates_ShouldReturnAmericaChicago()
        {
            // Arrange
            double latitude = 41.8781;
            double longitude = -87.6298;

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("America/Chicago");
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithLondonCoordinates_ShouldReturnEuropeLondon()
        {
            // Arrange
            double latitude = 51.5074;
            double longitude = -0.1278;

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Europe/London");
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithTokyoCoordinates_ShouldReturnAsiaTokyo()
        {
            // Arrange
            double latitude = 35.6762;
            double longitude = 139.6503;

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Asia/Tokyo");
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithSydneyCoordinates_ShouldReturnAustraliaSydney()
        {
            // Arrange
            double latitude = -33.8688;
            double longitude = 151.2093;

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Australia/Sydney");
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithRemoteOceanCoordinates_ShouldReturnFallbackTimezone()
        {
            // Arrange - coordinates in middle of Pacific Ocean
            double latitude = 0.0;
            double longitude = -150.0; // Should fallback to Pacific/Honolulu timezone

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be("Pacific/Honolulu");
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithCachedCoordinates_ShouldReturnFromCache()
        {
            // Arrange
            double latitude = 40.7128;
            double longitude = -74.0060;

            // Act - First call to populate cache
            var firstResult = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Act - Second call should use cache
            var secondResult = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            firstResult.IsSuccess.Should().BeTrue();
            secondResult.IsSuccess.Should().BeTrue();
            firstResult.Data.Should().Be(secondResult.Data);
            secondResult.Data.Should().Be("America/New_York");
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithCancellationToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _timezoneService.GetTimezoneFromCoordinatesAsync(40.7128, -74.0060, cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithExtremeLatitude_ShouldHandleGracefully()
        {
            // Arrange - coordinates at North Pole
            double latitude = 90.0;
            double longitude = 0.0;

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert - Should return a fallback timezone
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
        }

        #endregion

        #region GetTimeZoneInfo Tests

        [Test]
        public void GetTimeZoneInfo_WithValidTimezoneId_ShouldReturnCorrectTimeZoneInfo()
        {
            // Arrange
            string timezoneId = "America/New_York";

            // Act
            var result = _timezoneService.GetTimeZoneInfo(timezoneId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(timezoneId);
        }

        [Test]
        public void GetTimeZoneInfo_WithInvalidTimezoneId_ShouldReturnLocalTimeZone()
        {
            // Arrange
            string invalidTimezoneId = "Invalid/Timezone";

            // Act
            var result = _timezoneService.GetTimeZoneInfo(invalidTimezoneId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(TimeZoneInfo.Local);
        }

        [Test]
        public void GetTimeZoneInfo_WithNullTimezoneId_ShouldReturnLocalTimeZone()
        {
            // Arrange
            string timezoneId = null;

            // Act
            var result = _timezoneService.GetTimeZoneInfo(timezoneId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(TimeZoneInfo.Local);
        }

        [Test]
        public void GetTimeZoneInfo_WithEmptyTimezoneId_ShouldReturnLocalTimeZone()
        {
            // Arrange
            string timezoneId = string.Empty;

            // Act
            var result = _timezoneService.GetTimeZoneInfo(timezoneId);

            // Assert
            result.Should().NotBeNull();
            result.Should().Be(TimeZoneInfo.Local);
        }

        #endregion

        #region GetBatchTimezonesFromCoordinatesAsync Tests

        [Test]
        public async Task GetBatchTimezonesFromCoordinatesAsync_WithValidCoordinates_ShouldReturnCorrectTimezones()
        {
            // Arrange
            var coordinates = new List<(double latitude, double longitude)>
            {
                (40.7128, -74.0060), // New York
                (34.0522, -118.2437), // Los Angeles
                (51.5074, -0.1278)   // London
            };

            // Act
            var result = await _timezoneService.GetBatchTimezonesFromCoordinatesAsync(coordinates, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);

            result.Data["40.7128_-74.0060"].Should().Be("America/New_York");
            result.Data["34.0522_-118.2437"].Should().Be("America/Los_Angeles");
            result.Data["51.5074_-0.1278"].Should().Be("Europe/London");
        }

        [Test]
        public async Task GetBatchTimezonesFromCoordinatesAsync_WithEmptyList_ShouldReturnEmptyDictionary()
        {
            // Arrange
            var coordinates = new List<(double latitude, double longitude)>();

            // Act
            var result = await _timezoneService.GetBatchTimezonesFromCoordinatesAsync(coordinates, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task GetBatchTimezonesFromCoordinatesAsync_WithNullList_ShouldReturnEmptyDictionary()
        {
            // Arrange
            List<(double latitude, double longitude)> coordinates = null;

            // Act
            var result = await _timezoneService.GetBatchTimezonesFromCoordinatesAsync(coordinates, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task GetBatchTimezonesFromCoordinatesAsync_WithCancellationToken_ShouldThrowOperationCanceledException()
        {
            // Arrange
            var coordinates = new List<(double latitude, double longitude)>
            {
                (40.7128, -74.0060),
                (34.0522, -118.2437)
            };
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _timezoneService.GetBatchTimezonesFromCoordinatesAsync(coordinates, cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task GetBatchTimezonesFromCoordinatesAsync_WithLargeList_ShouldProcessInParallel()
        {
            // Arrange
            var coordinates = new List<(double latitude, double longitude)>();
            for (int i = 0; i < 10; i++)
            {
                coordinates.Add((40.0 + i, -74.0 + i)); // Spread around New York area
            }

            // Act
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            var result = await _timezoneService.GetBatchTimezonesFromCoordinatesAsync(coordinates, CancellationToken.None);
            stopwatch.Stop();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(10);

            // Parallel processing should complete reasonably quickly
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
        }

        #endregion

        #region TimezoneBounds Tests

        [Test]
        public void TimezoneBounds_Constructor_ShouldSetPropertiesCorrectly()
        {
            // Arrange & Act
            var bounds = new TimezoneBounds(25.0, 49.0, -125.0, -67.0);

            // Assert
            bounds.MinLatitude.Should().Be(25.0);
            bounds.MaxLatitude.Should().Be(49.0);
            bounds.MinLongitude.Should().Be(-125.0);
            bounds.MaxLongitude.Should().Be(-67.0);
        }

        [Test]
        public void TimezoneBounds_Contains_WithCoordinatesInside_ShouldReturnTrue()
        {
            // Arrange
            var bounds = new TimezoneBounds(25.0, 49.0, -125.0, -67.0);

            // Act
            var result = bounds.Contains(40.7128, -74.0060); // New York coordinates

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void TimezoneBounds_Contains_WithCoordinatesOutside_ShouldReturnFalse()
        {
            // Arrange
            var bounds = new TimezoneBounds(25.0, 49.0, -125.0, -67.0);

            // Act
            var result = bounds.Contains(51.5074, -0.1278); // London coordinates

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void TimezoneBounds_Area_ShouldCalculateCorrectly()
        {
            // Arrange
            var bounds = new TimezoneBounds(25.0, 49.0, -125.0, -67.0);

            // Act
            var area = bounds.Area;

            // Assert
            area.Should().Be((49.0 - 25.0) * (-67.0 - (-125.0))); // 24 * 58 = 1392
            area.Should().Be(1392.0);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithBoundaryCoordinates_ShouldHandleCorrectly()
        {
            // Arrange - coordinates on timezone boundary
            double latitude = 49.0; // Max latitude for US timezones
            double longitude = -84.0; // Boundary between Eastern and Central

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task GetTimezoneFromCoordinatesAsync_WithDatelineCoordinates_ShouldHandleCorrectly()
        {
            // Arrange - coordinates near international date line
            double latitude = 0.0;
            double longitude = 180.0;

            // Act
            var result = await _timezoneService.GetTimezoneFromCoordinatesAsync(latitude, longitude, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNullOrEmpty();
        }

        #endregion
    }
}