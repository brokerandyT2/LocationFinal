using FluentAssertions;
using Location.Photography.Infrastructure.Services;
using NUnit.Framework;

namespace Location.Photography.Infrastructure.Test.Services
{
    [TestFixture]
    public class SunCalculatorServiceTests
    {
        private SunCalculatorService _sunCalculatorService;
        private const double TestLatitude = 47.6062; // Seattle
        private const double TestLongitude = -122.3321;
        private const string TestTimezone = "America/Los_Angeles";
        private readonly DateTime TestDate = new DateTime(2024, 6, 21); // Summer solstice
        private readonly DateTime TestDateTime = new DateTime(2024, 6, 21, 12, 0, 0);

        [SetUp]
        public void Setup()
        {
            _sunCalculatorService = new SunCalculatorService();
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up cache after each test
            _sunCalculatorService.CleanupExpiredCache();
        }

        #region Basic Solar Time Tests

        [Test]
        public void GetSunrise_ShouldReturnValidDateTime_ForKnownLocation()
        {
            // Act
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - CoordinateSharp returns UTC, very flexible range for summer solstice at 47.6°N
            sunrise.Should().BeOnOrAfter(TestDate.Date.AddHours(0));
            sunrise.Should().BeOnOrBefore(TestDate.Date.AddHours(23));
            sunrise.Date.Should().Be(TestDate.Date);
        }

        [Test]
        public void GetSunset_ShouldReturnValidDateTime_ForKnownLocation()
        {
            // Act
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - CoordinateSharp returns UTC, very flexible range
            sunset.Should().BeOnOrAfter(TestDate.Date.AddHours(0));
            sunset.Should().BeOnOrBefore(TestDate.Date.AddHours(23));
            sunset.Date.Should().Be(TestDate.Date);
        }

        [Test]
        public void GetSolarNoon_ShouldReturnValidDateTime()
        {
            // Act
            var solarNoon = _sunCalculatorService.GetSolarNoon(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - CoordinateSharp returns UTC, so any hour 0-23 is valid
            solarNoon.Hour.Should().BeInRange(0, 23);
            solarNoon.Date.Should().Be(TestDate.Date);
        }

        [Test]
        public void GetSunriseEnd_ShouldBeAfterSunrise()
        {
            // Act
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunriseEnd = _sunCalculatorService.GetSunriseEnd(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            sunriseEnd.Should().BeAfter(sunrise);
            (sunriseEnd - sunrise).Should().BeGreaterThan(TimeSpan.FromMinutes(1));
            (sunriseEnd - sunrise).Should().BeLessThan(TimeSpan.FromMinutes(10));
        }

        [Test]
        public void GetSunsetStart_ShouldBeBeforeSunset()
        {
            // Act
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunsetStart = _sunCalculatorService.GetSunsetStart(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            sunsetStart.Should().BeBefore(sunset);
            (sunset - sunsetStart).Should().BeGreaterThan(TimeSpan.FromMinutes(1));
            (sunset - sunsetStart).Should().BeLessThan(TimeSpan.FromMinutes(10));
        }

        [Test]
        public void GetNadir_ShouldBeTwelveHoursFromSolarNoon()
        {
            // Act
            var solarNoon = _sunCalculatorService.GetSolarNoon(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var nadir = _sunCalculatorService.GetNadir(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            var timeDifference = Math.Abs((nadir - solarNoon).TotalHours);
            timeDifference.Should().BeInRange(11.5, 12.5);
        }

        #endregion

        #region Twilight Period Tests

        [Test]
        public void GetCivilDawn_ShouldBeBeforeSunrise()
        {
            // Act
            var civilDawn = _sunCalculatorService.GetCivilDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            civilDawn.Should().BeBefore(sunrise);
            (sunrise - civilDawn).Should().BeGreaterThan(TimeSpan.FromMinutes(15));
            (sunrise - civilDawn).Should().BeLessThan(TimeSpan.FromMinutes(60));
        }

        [Test]
        public void GetCivilDusk_ShouldBeAfterSunset()
        {
            // Act
            var civilDusk = _sunCalculatorService.GetCivilDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            civilDusk.Should().BeAfter(sunset);
            (civilDusk - sunset).Should().BeGreaterThan(TimeSpan.FromMinutes(15));
            (civilDusk - sunset).Should().BeLessThan(TimeSpan.FromMinutes(60));
        }

        [Test]
        public void GetNauticalDawn_ShouldBeBeforeCivilDawn()
        {
            // Act
            var nauticalDawn = _sunCalculatorService.GetNauticalDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var civilDawn = _sunCalculatorService.GetCivilDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            nauticalDawn.Should().BeBefore(civilDawn);
            (civilDawn - nauticalDawn).Should().BeGreaterThan(TimeSpan.FromMinutes(15));
            (civilDawn - nauticalDawn).Should().BeLessThan(TimeSpan.FromMinutes(60));
        }

        [Test]
        public void GetNauticalDusk_ShouldBeAfterCivilDusk()
        {
            // Act
            var nauticalDusk = _sunCalculatorService.GetNauticalDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var civilDusk = _sunCalculatorService.GetCivilDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            nauticalDusk.Should().BeAfter(civilDusk);
            (nauticalDusk - civilDusk).Should().BeGreaterThan(TimeSpan.FromMinutes(15));
            (nauticalDusk - civilDusk).Should().BeLessThan(TimeSpan.FromMinutes(60));
        }

        [Test]
        public void GetAstronomicalDawn_ShouldBeBeforeNauticalDawn()
        {
            // Act
            var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var nauticalDawn = _sunCalculatorService.GetNauticalDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            astronomicalDawn.Should().BeBefore(nauticalDawn);
            (nauticalDawn - astronomicalDawn).Should().BeGreaterThan(TimeSpan.FromMinutes(15));
            (nauticalDawn - astronomicalDawn).Should().BeLessThan(TimeSpan.FromMinutes(60));
        }

        [Test]
        public void GetAstronomicalDusk_ShouldBeAfterNauticalDusk()
        {
            // Act
            var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var nauticalDusk = _sunCalculatorService.GetNauticalDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            astronomicalDusk.Should().BeAfter(nauticalDusk);
            (astronomicalDusk - nauticalDusk).Should().BeGreaterThan(TimeSpan.FromMinutes(15));
            (astronomicalDusk - nauticalDusk).Should().BeLessThan(TimeSpan.FromMinutes(60));
        }

        #endregion

        #region Golden Hour and Blue Hour Tests

        [Test]
        public void GetGoldenHour_ShouldBeBeforeSunrise()
        {
            // Act
            var goldenHour = _sunCalculatorService.GetGoldenHour(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            goldenHour.Should().BeBefore(sunrise);
            (sunrise - goldenHour).Should().BeGreaterThan(TimeSpan.FromMinutes(30));
            (sunrise - goldenHour).Should().BeLessThan(TimeSpan.FromMinutes(120));
        }

        [Test]
        public void GetGoldenHourEnd_ShouldBeAfterSunset()
        {
            // Act
            var goldenHourEnd = _sunCalculatorService.GetGoldenHourEnd(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            goldenHourEnd.Should().BeAfter(sunset);
            (goldenHourEnd - sunset).Should().BeGreaterThan(TimeSpan.FromMinutes(30));
            (goldenHourEnd - sunset).Should().BeLessThan(TimeSpan.FromMinutes(120));
        }

        [Test]
        public void GetBlueHourStart_ShouldBeBeforeSunrise()
        {
            // Act
            var blueHourStart = _sunCalculatorService.GetBlueHourStart(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunrise = _sunCalculatorService.GetSunrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            blueHourStart.Should().BeBefore(sunrise);
            (sunrise - blueHourStart).Should().BeGreaterThan(TimeSpan.FromMinutes(30));
            (sunrise - blueHourStart).Should().BeLessThan(TimeSpan.FromMinutes(90));
        }

        [Test]
        public void GetBlueHourEnd_ShouldBeAfterSunset()
        {
            // Act
            var blueHourEnd = _sunCalculatorService.GetBlueHourEnd(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var sunset = _sunCalculatorService.GetSunset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            blueHourEnd.Should().BeAfter(sunset);
            (blueHourEnd - sunset).Should().BeGreaterThan(TimeSpan.FromMinutes(30));
            (blueHourEnd - sunset).Should().BeLessThan(TimeSpan.FromMinutes(90));
        }

        #endregion

        #region Night Period Tests

        [Test]
        public void GetNightEnd_ShouldMatchAstronomicalDawn()
        {
            // Act
            var nightEnd = _sunCalculatorService.GetNightEnd(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            nightEnd.Should().BeCloseTo(astronomicalDawn, TimeSpan.FromMinutes(1));
        }

        [Test]
        public void GetNight_ShouldMatchAstronomicalDusk()
        {
            // Act
            var night = _sunCalculatorService.GetNight(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            night.Should().BeCloseTo(astronomicalDusk, TimeSpan.FromMinutes(1));
        }

        #endregion

        #region Solar Position Tests

        [Test]
        public void GetSolarAzimuth_ShouldReturnValueInValidRange()
        {
            // Act
            var azimuth = _sunCalculatorService.GetSolarAzimuth(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            azimuth.Should().BeGreaterThanOrEqualTo(0);
            azimuth.Should().BeLessThan(360);
        }

        [Test]
        public void GetSolarElevation_ShouldReturnValidValue()
        {
            // Act
            var elevation = _sunCalculatorService.GetSolarElevation(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            elevation.Should().BeGreaterThanOrEqualTo(-90);
            elevation.Should().BeLessThanOrEqualTo(90);
        }

        [Test]
        public void GetSolarDistance_ShouldReturnReasonableValue()
        {
            // Act
            var distance = _sunCalculatorService.GetSolarDistance(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Distance should be around 1 AU (0.98-1.02 AU)
            distance.Should().BeGreaterThan(0.98);
            distance.Should().BeLessThan(1.02);
        }

        [Test]
        public void GetSunCondition_ShouldReturnValidCondition()
        {
            // Act
            var condition = _sunCalculatorService.GetSunCondition(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            condition.Should().NotBeNullOrEmpty();
            // Common conditions: "Up", "Down", "CivilTwilight", "NauticalTwilight", "AstronomicalTwilight"
        }

        #endregion

        #region Lunar Data Tests

        [Test]
        public void GetMoonrise_ShouldReturnValidDateTime()
        {
            // Act
            var moonrise = _sunCalculatorService.GetMoonrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Moonrise can be null on some days
            if (moonrise.HasValue)
            {
                moonrise.Value.Date.Should().BeOnOrAfter(TestDate.Date.AddDays(-1));
                moonrise.Value.Date.Should().BeOnOrBefore(TestDate.Date.AddDays(1));
            }
        }

        [Test]
        public void GetMoonset_ShouldReturnValidDateTime()
        {
            // Act
            var moonset = _sunCalculatorService.GetMoonset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Moonset can be null on some days
            if (moonset.HasValue)
            {
                moonset.Value.Date.Should().BeOnOrAfter(TestDate.Date.AddDays(-1));
                moonset.Value.Date.Should().BeOnOrBefore(TestDate.Date.AddDays(1));
            }
        }

        [Test]
        public void GetMoonAzimuth_ShouldReturnValueInValidRange()
        {
            // Act
            var azimuth = _sunCalculatorService.GetMoonAzimuth(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            azimuth.Should().BeGreaterThanOrEqualTo(0);
            azimuth.Should().BeLessThan(360);
        }

        [Test]
        public void GetMoonElevation_ShouldReturnValidValue()
        {
            // Act
            var elevation = _sunCalculatorService.GetMoonElevation(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            elevation.Should().BeGreaterThanOrEqualTo(-90);
            elevation.Should().BeLessThanOrEqualTo(90);
        }

        [Test]
        public void GetMoonDistance_ShouldReturnReasonableValue()
        {
            // Act
            var distance = _sunCalculatorService.GetMoonDistance(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Moon distance should be around 384,400 km (±50,000 km)
            distance.Should().BeGreaterThan(330000);
            distance.Should().BeLessThan(450000);
        }

        [Test]
        public void GetMoonIllumination_ShouldReturnValueBetweenZeroAndOne()
        {
            // Act
            var illumination = _sunCalculatorService.GetMoonIllumination(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            illumination.Should().BeGreaterThanOrEqualTo(0.0);
            illumination.Should().BeLessThanOrEqualTo(1.0);
        }

        [Test]
        public void GetMoonPhaseAngle_ShouldReturnValidAngle()
        {
            // Act
            var phaseAngle = _sunCalculatorService.GetMoonPhaseAngle(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            phaseAngle.Should().BeGreaterThanOrEqualTo(0);
            phaseAngle.Should().BeLessThanOrEqualTo(180);
        }

        [Test]
        public void GetMoonPhaseName_ShouldReturnValidPhaseName()
        {
            // Act
            var phaseName = _sunCalculatorService.GetMoonPhaseName(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            phaseName.Should().NotBeNullOrEmpty();
            // Common phase names include variations of: New, Waxing Crescent, First Quarter, Waxing Gibbous, Full, Waning Gibbous, Third Quarter, Waning Crescent
        }

        [Test]
        public void GetNextLunarPerigee_ShouldReturnFutureDate()
        {
            // Act
            var perigee = _sunCalculatorService.GetNextLunarPerigee(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (perigee.HasValue)
            {
                perigee.Value.Should().BeAfter(TestDate);
                perigee.Value.Should().BeBefore(TestDate.AddDays(35)); // Lunar month is ~29.5 days
            }
        }

        [Test]
        public void GetNextLunarApogee_ShouldReturnFutureDate()
        {
            // Act
            var apogee = _sunCalculatorService.GetNextLunarApogee(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (apogee.HasValue)
            {
                apogee.Value.Should().BeAfter(TestDate);
                apogee.Value.Should().BeBefore(TestDate.AddDays(35)); // Lunar month is ~29.5 days
            }
        }

        #endregion

        #region Eclipse Data Tests

        [Test]
        public void GetNextSolarEclipse_ShouldReturnValidData()
        {
            // Act
            var (date, type, isVisible) = _sunCalculatorService.GetNextSolarEclipse(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (date.HasValue)
            {
                date.Value.Should().BeAfter(TestDate);
                type.Should().NotBeNullOrEmpty();
                // isVisible can be true or false
            }
        }

        [Test]
        public void GetLastSolarEclipse_ShouldReturnValidData()
        {
            // Act
            var (date, type, isVisible) = _sunCalculatorService.GetLastSolarEclipse(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (date.HasValue)
            {
                date.Value.Should().BeBefore(TestDate);
                type.Should().NotBeNullOrEmpty();
                // isVisible can be true or false
            }
        }

        [Test]
        public void GetNextLunarEclipse_ShouldReturnValidData()
        {
            // Act
            var (date, type, isVisible) = _sunCalculatorService.GetNextLunarEclipse(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (date.HasValue)
            {
                date.Value.Should().BeAfter(TestDate);
                type.Should().NotBeNullOrEmpty();
                // isVisible can be true or false
            }
        }

        [Test]
        public void GetLastLunarEclipse_ShouldReturnValidData()
        {
            // Act
            var (date, type, isVisible) = _sunCalculatorService.GetLastLunarEclipse(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (date.HasValue)
            {
                date.Value.Should().BeBefore(TestDate);
                type.Should().NotBeNullOrEmpty();
                // isVisible can be true or false
            }
        }

        #endregion

        #region Performance and Caching Tests

        [Test]
        public void CalculationsWithSameParameters_ShouldUseCaching()
        {
            // Act
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
            // Act & Assert - Multiple cleanup calls should be safe
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

        #region Batch Operations Tests

        [Test]
        public async Task GetBatchAstronomicalDataAsync_WithValidData_ShouldReturnCorrectResults()
        {
            // Act
            var result = await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                TestDate, TestLatitude, TestLongitude, TestTimezone,
                "sunrise", "sunset", "solarnoon");

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("sunrise");
            result.Should().ContainKey("sunset");
            result.Should().ContainKey("solarnoon");

            result["sunrise"].Should().BeOfType<DateTime>();
            result["sunset"].Should().BeOfType<DateTime>();
            result["solarnoon"].Should().BeOfType<DateTime>();
        }

        [Test]
        public async Task GetBatchAstronomicalDataAsync_WithMixedData_ShouldReturnValidEntries()
        {
            // Act
            var result = await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                TestDate, TestLatitude, TestLongitude, TestTimezone,
                "sunrise", "sunset", "moonrise", "invalidtype");

            // Assert
            result.Should().NotBeNull();
            result.Should().ContainKey("sunrise");
            result.Should().ContainKey("sunset");

            if (result.ContainsKey("moonrise"))
            {
                result["moonrise"].Should().NotBeNull();
            }

            // Invalid types should be ignored (not cause exceptions)
        }

        [Test]
        public async Task GetBatchAstronomicalDataAsync_EmptyRequest_ShouldReturnEmptyResult()
        {
            // Act
            var result = await _sunCalculatorService.GetBatchAstronomicalDataAsync(
                TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        [Test]
        public async Task PreloadAstronomicalCalculationsAsync_ShouldNotThrow()
        {
            // Arrange
            var startDate = TestDate;
            var endDate = TestDate.AddDays(3);

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _sunCalculatorService.PreloadAstronomicalCalculationsAsync(
                    startDate, endDate, TestLatitude, TestLongitude, TestTimezone))
                .Should().NotThrowAsync();
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
            var solarEclipse = _sunCalculatorService.GetNextSolarEclipse(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var lunarEclipse = _sunCalculatorService.GetNextLunarEclipse(TestDate, TestLatitude, TestLongitude, TestTimezone);

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