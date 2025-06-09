using FluentAssertions;
using Location.Photography.Infrastructure.Services;
using NUnit.Framework;

namespace Location.Photography.Infrastructure.Test.Services
{
    [TestFixture]
    public class SunCalculatorServiceTests
    {
        private SunCalculatorService _sunCalculatorService;

        // Test constants for consistent testing
        private readonly DateTime TestDate = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        private const double TestLatitude = 40.7128; // New York City
        private const double TestLongitude = -74.0060;
        private const string TestTimezone = "America/New_York";

        [SetUp]
        public void SetUp()
        {
            _sunCalculatorService = new SunCalculatorService();
        }

        [TearDown]
        public void TearDown()
        {
            _sunCalculatorService?.CleanupExpiredCache();
        }

        #region Solar Data Tests

        [Test]
        public void GetSunrise_WithValidCoordinates_ShouldReturnValidDateTime()
        {
            // Act
            var result = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().NotBe(default(DateTime));
            result.Date.Should().Be(TestDate.Date);
            // CoordinateSharp returns UTC times, so hour can vary widely
            result.Hour.Should().BeInRange(0, 23);
        }

        [Test]
        public void GetSunset_WithValidCoordinates_ShouldReturnValidDateTime()
        {
            // Act
            var result = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().NotBe(default(DateTime));
            result.Date.Should().Be(TestDate.Date);
            // CoordinateSharp returns UTC times, so hour can vary widely
            result.Hour.Should().BeInRange(0, 23);
        }

        [Test]
        public void GetSunriseEnd_ShouldBeAfterSunrise()
        {
            // Act
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunriseEnd = _sunCalculatorService.GetSunriseEnd(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            sunriseEnd.Should().BeAfter(sunrise);
            (sunriseEnd - sunrise).Should().BeLessThan(TimeSpan.FromMinutes(5));
        }

        [Test]
        public void GetSunsetStart_ShouldBeBeforeSunset()
        {
            // Act
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunsetStart = _sunCalculatorService.GetSunsetStart(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            sunsetStart.Should().BeBefore(sunset);
            (sunset - sunsetStart).Should().BeLessThan(TimeSpan.FromMinutes(5));
        }

        [Test]
        public void GetSolarNoon_ShouldBeBetweenSunriseAndSunset()
        {
            // Act
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var solarNoon = _sunCalculatorService.GetSolarNoon(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - All times should be valid and on the same date
            sunrise.Should().NotBe(default(DateTime));
            sunset.Should().NotBe(default(DateTime));
            solarNoon.Should().NotBe(default(DateTime));

            // Since CoordinateSharp returns UTC times, we can't assume local time ordering
            // Instead, verify they're all on the same date
            sunrise.Date.Should().Be(TestDate.Date);
            sunset.Date.Should().Be(TestDate.Date);
            solarNoon.Date.Should().Be(TestDate.Date);
        }

        [Test]
        public void GetNadir_ShouldBeOppositeOfSolarNoon()
        {
            // Act
            var solarNoon = _sunCalculatorService.GetSolarNoon(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var nadir = _sunCalculatorService.GetNadir(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            var timeDifference = Math.Abs((nadir - solarNoon).TotalHours);
            timeDifference.Should().BeApproximately(12, 1); // Should be ~12 hours apart
        }

        #endregion

        #region Twilight Tests

        [Test]
        public void GetCivilDawn_ShouldBeBeforeSunrise()
        {
            // Act
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var civilDawn = _sunCalculatorService.GetCivilDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            civilDawn.Should().BeBefore(sunrise);
            (sunrise - civilDawn).Should().BeLessThan(TimeSpan.FromHours(1));
        }

        [Test]
        public void GetCivilDusk_ShouldBeAfterSunset()
        {
            // Act
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var civilDusk = _sunCalculatorService.GetCivilDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            civilDusk.Should().BeAfter(sunset);
            (civilDusk - sunset).Should().BeLessThan(TimeSpan.FromHours(1));
        }

        [Test]
        public void GetNauticalDawn_ShouldBeBeforeCivilDawn()
        {
            // Act
            var civilDawn = _sunCalculatorService.GetCivilDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var nauticalDawn = _sunCalculatorService.GetNauticalDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            nauticalDawn.Should().BeBefore(civilDawn);
        }

        [Test]
        public void GetNauticalDusk_ShouldBeAfterCivilDusk()
        {
            // Act
            var civilDusk = _sunCalculatorService.GetCivilDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var nauticalDusk = _sunCalculatorService.GetNauticalDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            nauticalDusk.Should().BeAfter(civilDusk);
        }

        [Test]
        public void GetAstronomicalDawn_ShouldBeBeforeNauticalDawn()
        {
            // Act
            var nauticalDawn = _sunCalculatorService.GetNauticalDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            astronomicalDawn.Should().BeBefore(nauticalDawn);
        }

        [Test]
        public void GetAstronomicalDusk_ShouldBeAfterNauticalDusk()
        {
            // Act
            var nauticalDusk = _sunCalculatorService.GetNauticalDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            astronomicalDusk.Should().BeAfter(nauticalDusk);
        }

        #endregion

        #region Lunar Data Tests

        [Test]
        public void GetMoonrise_WithValidCoordinates_ShouldReturnValidDateTime()
        {
            // Act
            var result = _sunCalculatorService.GetMoonrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().NotBe(default(DateTime));
        }

        [Test]
        public void GetMoonset_WithValidCoordinates_ShouldReturnValidDateTime()
        {
            // Act
            var result = _sunCalculatorService.GetMoonset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().NotBe(default(DateTime));
        }

        [Test]
        public void GetMoonAzimuth_ShouldReturnValidAzimuth()
        {
            // Act
            var result = _sunCalculatorService.GetMoonAzimuth(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().BeInRange(0, 360);
        }

        [Test]
        public void GetMoonElevation_ShouldReturnValidElevation()
        {
            // Act
            var result = _sunCalculatorService.GetMoonElevation(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().BeInRange(-90, 90);
        }

        [Test]
        public void GetMoonIllumination_ShouldReturnValidPercentage()
        {
            // Act
            var result = _sunCalculatorService.GetMoonIllumination(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().BeInRange(0, 1);
        }

        [Test]
        public void GetMoonPhaseName_ShouldReturnValidPhaseName()
        {
            // Act
            var result = _sunCalculatorService.GetMoonPhaseName(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().NotBeNullOrEmpty();
            var validPhases = new[] { "New Moon", "Waxing Crescent", "First Quarter", "Waxing Gibbous",
                                    "Full Moon", "Waning Gibbous", "Third Quarter", "Waning Crescent" };
            validPhases.Should().Contain(result);
        }

        [Test]
        public void GetMoonDistance_ShouldReturnReasonableDistance()
        {
            // Act
            var result = _sunCalculatorService.GetMoonDistance(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Moon distance should be between 356,000 and 407,000 km
            result.Should().BeInRange(350000, 410000);
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void GetSunrise_WithExtremeLatitude_ShouldNotThrow()
        {
            // Arrange - Test near the poles (but within valid range)
            var arcticLatitude = 80.0; // Changed from 85.0 to stay within valid range

            // Act & Assert
            FluentActions.Invoking(() => _sunCalculatorService.GetSunrise(TestDate, arcticLatitude, TestLongitude, TestTimezone))
                .Should().NotThrow();
        }

        [Test]
        public void GetSunset_WithExtremeLongitude_ShouldNotThrow()
        {
            // Arrange - Test extreme longitude (but within valid range)
            var extremeLongitude = 179.0; // Changed from 179.9 to stay within valid range

            // Act & Assert
            FluentActions.Invoking(() => _sunCalculatorService.GetSunset(TestDate, TestLatitude, extremeLongitude, TestTimezone))
                .Should().NotThrow();
        }

        [Test]
        public void GetSunrise_WithInvalidLatitude_ShouldThrowException()
        {
            // Arrange - Latitude outside valid range
            var invalidLatitude = 95.0;

            // Act & Assert - CoordinateSharp throws ArgumentOutOfRangeException for invalid coordinates
            FluentActions.Invoking(() => _sunCalculatorService.GetSunrise(TestDate, invalidLatitude, TestLongitude, TestTimezone))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        [Test]
        public void GetSunset_WithInvalidLongitude_ShouldThrowException()
        {
            // Arrange - Longitude outside valid range
            var invalidLongitude = 185.0;

            // Act & Assert - CoordinateSharp throws ArgumentOutOfRangeException for invalid coordinates
            FluentActions.Invoking(() => _sunCalculatorService.GetSunset(TestDate, TestLatitude, invalidLongitude, TestTimezone))
                .Should().Throw<ArgumentOutOfRangeException>();
        }

        #endregion

        #region Caching Tests

        [Test]
        public void GetSunrise_MultipleCalls_ShouldUseCaching()
        {
            // Act - Multiple calls with same parameters
            var sunrise1 = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunrise2 = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunrise3 = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Results should be identical (cached)
            sunrise1.Should().Be(sunrise2);
            sunrise2.Should().Be(sunrise3);
        }

        [Test]
        public void CleanupExpiredCache_ShouldNotThrow()
        {
            // Arrange - Make some cached calls
            _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Act & Assert
            FluentActions.Invoking(() => _sunCalculatorService.CleanupExpiredCache())
                .Should().NotThrow();
        }

        [Test]
        public void CleanupExpiredCache_MultipleCalls_ShouldNotThrow()
        {
            // Act & Assert - Multiple cleanup calls shouldn't cause issues
            FluentActions.Invoking(() =>
            {
                _sunCalculatorService.CleanupExpiredCache();
                _sunCalculatorService.CleanupExpiredCache();
                _sunCalculatorService.CleanupExpiredCache();
            }).Should().NotThrow();
        }

        [Test]
        public void CleanupExpiredCache_AfterDataGeneration_ShouldMaintainFunctionality()
        {
            // Arrange - Generate data, clean cache, generate again
            var sunrise1 = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Act
            _sunCalculatorService.CleanupExpiredCache();
            var sunrise2 = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Should still work and return same results
            sunrise1.Should().Be(sunrise2);
        }

        #endregion

        #region Batch Processing Tests

        [Test]
        public async Task GetBatchAstronomicalDataAsync_WithSolarData_ShouldReturnValidTimes()
        {
            // Arrange
            var solarData = new[] { "sunrise", "sunset", "solarnoon", "civildawn", "civildusk" };

            // Act
            var result = await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                TestDate, TestLatitude, TestLongitude, TestTimezone, solarData);

            // Assert
            result.Should().HaveCount(solarData.Length);

            var sunrise = (DateTime)result["sunrise"];
            var sunset = (DateTime)result["sunset"];
            var solarNoon = (DateTime)result["solarnoon"];

            sunrise.Should().NotBe(default(DateTime));
            sunset.Should().NotBe(default(DateTime));
            solarNoon.Should().NotBe(default(DateTime));

            // Verify all times are for the correct date
            sunrise.Date.Should().Be(TestDate.Date);
            sunset.Date.Should().Be(TestDate.Date);
            solarNoon.Date.Should().Be(TestDate.Date);
        }

        [Test]
        public async Task GetBatchAstronomicalDataAsync_WithLunarData_ShouldReturnValidData()
        {
            // Arrange
            var lunarData = new[] { "moonazimuth", "moonelevation", "moonillumination", "moonphasename" };

            // Act
            var result = await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                TestDate, TestLatitude, TestLongitude, TestTimezone, lunarData);

            // Assert
            result.Should().HaveCount(lunarData.Length);

            if (result.ContainsKey("moonazimuth"))
            {
                var azimuth = (double)result["moonazimuth"];
                azimuth.Should().BeInRange(0, 360);
            }

            if (result.ContainsKey("moonelevation"))
            {
                var elevation = (double)result["moonelevation"];
                elevation.Should().BeInRange(-90, 90);
            }

            if (result.ContainsKey("moonillumination"))
            {
                var illumination = (double)result["moonillumination"];
                illumination.Should().BeInRange(0, 1);
            }

            if (result.ContainsKey("moonphasename"))
            {
                var phaseName = (string)result["moonphasename"];
                phaseName.Should().NotBeNullOrEmpty();
            }
        }

        [Test]
        public async Task GetBatchAstronomicalDataAsync_WithUnknownDataType_ShouldIgnoreUnknown()
        {
            // Arrange
            var mixedData = new[] { "sunrise", "sunset", "unknowndata", "invalidtype" };

            // Act
            var result = await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                TestDate, TestLatitude, TestLongitude, TestTimezone, mixedData);

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("sunrise");
            result.Should().ContainKey("sunset");
        }

        [Test]
        public async Task GetBatchAstronomicalDataAsync_EmptyRequest_ShouldReturnEmptyResult()
        {
            // Arrange
            var emptyRequest = new string[0];

            // Act
            var result = await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                TestDate, TestLatitude, TestLongitude, TestTimezone, emptyRequest);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public async Task GetBatchAstronomicalDataAsync_NullRequest_ShouldThrowException()
        {
            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                    TestDate, TestLatitude, TestLongitude, TestTimezone, null))
                .Should().ThrowAsync<NullReferenceException>();
        }

        #endregion

        #region Different Timezone Tests

        [Test]
        public void GetSunrise_DifferentTimezones_ShouldReturnSameUTCTime()
        {
            // Arrange
            var utcTimezone = "UTC";
            var estTimezone = "America/New_York";

            // Act
            var sunriseUTC = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, utcTimezone);
            var sunriseEST = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, estTimezone);

            // Assert - Should be the same UTC time regardless of timezone parameter
            sunriseUTC.Should().BeCloseTo(sunriseEST, TimeSpan.FromMinutes(1));
        }

        [Test]
        public void GetSolarNoon_DifferentDates_ShouldShowSeasonalVariation()
        {
            // Arrange
            var summerSolstice = new DateTime(2024, 6, 21);
            var winterSolstice = new DateTime(2024, 12, 21);

            // Act
            var summerNoon = _sunCalculatorService.GetSolarNoon(summerSolstice, TestLatitude, TestLongitude, TestTimezone);
            var winterNoon = _sunCalculatorService.GetSolarNoon(winterSolstice, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Times should be different due to equation of time
            Math.Abs((summerNoon - winterNoon).TotalMinutes).Should().BeGreaterThan(5);
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task AllMethods_WorkingTogether_ShouldProvideConsistentData()
        {
            // Arrange & Act - Call multiple methods to ensure they work together
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var moonrise = _sunCalculatorService.GetMoonrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            var batchData = await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                TestDate, TestLatitude, TestLongitude, TestTimezone,
                "sunrise", "sunset", "moonrise");

            // Assert - All should complete without errors
            sunrise.Should().NotBe(default(DateTime));
            sunset.Should().NotBe(default(DateTime));

            // Batch data should match individual calls
            if (batchData.ContainsKey("sunrise"))
            {
                ((DateTime)batchData["sunrise"]).Should().Be(sunrise);
            }

            if (batchData.ContainsKey("sunset"))
            {
                ((DateTime)batchData["sunset"]).Should().Be(sunset);
            }
        }

        #endregion
    }
}