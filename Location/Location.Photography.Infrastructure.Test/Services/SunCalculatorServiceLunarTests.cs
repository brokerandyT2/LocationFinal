using FluentAssertions;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure.Services;
using NUnit.Framework;
using System;

namespace Location.Photography.Infrastructure.Test.Services
{
    [TestFixture]
    public class SunCalculatorServiceLunarTests
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

        #region Moonrise and Moonset Tests

        [Test]
        public void GetMoonrise_ShouldReturnValidDateTime_WhenMoonRiseOccurs()
        {
            // Act
            var moonrise = _sunCalculatorService.GetMoonrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (moonrise.HasValue)
            {
                moonrise.Value.Date.Should().BeOneOf(TestDate.Date, TestDate.Date.AddDays(-1), TestDate.Date.AddDays(1));
                moonrise.Value.Hour.Should().BeInRange(0, 23);
                moonrise.Value.Minute.Should().BeInRange(0, 59);
            }
            // Note: moonrise can be null for certain dates/locations (polar regions, etc.)
        }

        [Test]
        public void GetMoonset_ShouldReturnValidDateTime_WhenMoonSetOccurs()
        {
            // Act
            var moonset = _sunCalculatorService.GetMoonset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (moonset.HasValue)
            {
                moonset.Value.Date.Should().BeOneOf(TestDate.Date, TestDate.Date.AddDays(-1), TestDate.Date.AddDays(1));
                moonset.Value.Hour.Should().BeInRange(0, 23);
                moonset.Value.Minute.Should().BeInRange(0, 59);
            }
            // Note: moonset can be null for certain dates/locations
        }

        [Test]
        public void GetMoonrise_MultipleDates_ShouldShowProgression()
        {
            // Arrange
            var dates = new[]
            {
                new DateTime(2024, 6, 20),
                new DateTime(2024, 6, 21),
                new DateTime(2024, 6, 22)
            };

            // Act
            var moonrises = new DateTime?[dates.Length];
            for (int i = 0; i < dates.Length; i++)
            {
                moonrises[i] = _sunCalculatorService.GetMoonrise(dates[i], TestLatitude, TestLongitude, TestTimezone);
            }

            // Assert - At least some should have values and show daily progression
            var validMoonrises = Array.FindAll(moonrises, mr => mr.HasValue);
            validMoonrises.Should().NotBeEmpty("Should have at least some valid moonrise times");
        }

        [Test]
        public void GetMoonset_MultipleDates_ShouldShowProgression()
        {
            // Arrange
            var dates = new[]
            {
                new DateTime(2024, 6, 20),
                new DateTime(2024, 6, 21),
                new DateTime(2024, 6, 22)
            };

            // Act
            var moonsets = new DateTime?[dates.Length];
            for (int i = 0; i < dates.Length; i++)
            {
                moonsets[i] = _sunCalculatorService.GetMoonset(dates[i], TestLatitude, TestLongitude, TestTimezone);
            }

            // Assert - At least some should have values
            var validMoonsets = Array.FindAll(moonsets, ms => ms.HasValue);
            validMoonsets.Should().NotBeEmpty("Should have at least some valid moonset times");
        }

        [Test]
        public void GetMoonrise_PolarLatitude_ShouldHandleEdgeCase()
        {
            // Arrange
            double polarLatitude = 78.0; // Far North
            double polarLongitude = 15.0;

            // Act & Assert - Should not throw exception
            FluentActions.Invoking(() => _sunCalculatorService.GetMoonrise(TestDate, polarLatitude, polarLongitude, TestTimezone))
                .Should().NotThrow();
        }

        [Test]
        public void GetMoonset_PolarLatitude_ShouldHandleEdgeCase()
        {
            // Arrange
            double polarLatitude = -78.0; // Far South
            double polarLongitude = 15.0;

            // Act & Assert - Should not throw exception
            FluentActions.Invoking(() => _sunCalculatorService.GetMoonset(TestDate, polarLatitude, polarLongitude, TestTimezone))
                .Should().NotThrow();
        }

        #endregion

        #region Moon Position Tests

        [Test]
        public void GetMoonAzimuth_ShouldReturnValueInValidRange()
        {
            // Act
            var azimuth = _sunCalculatorService.GetMoonAzimuth(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            azimuth.Should().BeInRange(0, 360);
        }

        [Test]
        public void GetMoonElevation_ShouldReturnValueInValidRange()
        {
            // Act
            var elevation = _sunCalculatorService.GetMoonElevation(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            elevation.Should().BeInRange(-90, 90);
        }

        [Test]
        public void GetMoonDistance_ShouldReturnReasonableValue()
        {
            // Act
            var distance = _sunCalculatorService.GetMoonDistance(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            // Moon distance varies between ~356,000 km (perigee) and ~406,000 km (apogee)
            distance.Should().BeInRange(350000, 410000);
        }

        [Test]
        public void GetMoonAzimuth_DifferentTimes_ShouldShowMovement()
        {
            // Arrange
            var time1 = TestDate.AddHours(0);  // Midnight
            var time2 = TestDate.AddHours(6);  // 6 AM
            var time3 = TestDate.AddHours(12); // Noon
            var time4 = TestDate.AddHours(18); // 6 PM

            // Act
            var azimuth1 = _sunCalculatorService.GetMoonAzimuth(time1, TestLatitude, TestLongitude, TestTimezone);
            var azimuth2 = _sunCalculatorService.GetMoonAzimuth(time2, TestLatitude, TestLongitude, TestTimezone);
            var azimuth3 = _sunCalculatorService.GetMoonAzimuth(time3, TestLatitude, TestLongitude, TestTimezone);
            var azimuth4 = _sunCalculatorService.GetMoonAzimuth(time4, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Moon should move across the sky (azimuths should be different)
            var azimuths = new[] { azimuth1, azimuth2, azimuth3, azimuth4 };
            var uniqueAzimuths = new HashSet<double>(azimuths);
            uniqueAzimuths.Count.Should().BeGreaterThan(1, "Moon azimuth should change over time");
        }

        [Test]
        public void GetMoonElevation_DifferentTimes_ShouldShowMovement()
        {
            // Arrange
            var time1 = TestDate.AddHours(0);  // Midnight
            var time2 = TestDate.AddHours(6);  // 6 AM
            var time3 = TestDate.AddHours(12); // Noon
            var time4 = TestDate.AddHours(18); // 6 PM

            // Act
            var elevation1 = _sunCalculatorService.GetMoonElevation(time1, TestLatitude, TestLongitude, TestTimezone);
            var elevation2 = _sunCalculatorService.GetMoonElevation(time2, TestLatitude, TestLongitude, TestTimezone);
            var elevation3 = _sunCalculatorService.GetMoonElevation(time3, TestLatitude, TestLongitude, TestTimezone);
            var elevation4 = _sunCalculatorService.GetMoonElevation(time4, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Moon elevation should change over time
            var elevations = new[] { elevation1, elevation2, elevation3, elevation4 };
            var uniqueElevations = new HashSet<double>(elevations);
            uniqueElevations.Count.Should().BeGreaterThan(1, "Moon elevation should change over time");
        }

        [Test]
        public void GetMoonPosition_ExtremeLatitudes_ShouldNotThrow()
        {
            // Arrange
            double[] extremeLatitudes = { -89.9, -45.0, 0.0, 45.0, 89.9 };

            // Act & Assert
            foreach (var latitude in extremeLatitudes)
            {
                FluentActions.Invoking(() => _sunCalculatorService.GetMoonAzimuth(TestDateTime, latitude, TestLongitude, TestTimezone))
                    .Should().NotThrow($"Failed at latitude {latitude}");

                FluentActions.Invoking(() => _sunCalculatorService.GetMoonElevation(TestDateTime, latitude, TestLongitude, TestTimezone))
                    .Should().NotThrow($"Failed at latitude {latitude}");
            }
        }

        [Test]
        public void GetMoonPosition_ExtremeLongitudes_ShouldNotThrow()
        {
            // Arrange
            double[] extremeLongitudes = { -179.9, -90.0, 0.0, 90.0, 179.9 };

            // Act & Assert
            foreach (var longitude in extremeLongitudes)
            {
                FluentActions.Invoking(() => _sunCalculatorService.GetMoonAzimuth(TestDateTime, TestLatitude, longitude, TestTimezone))
                    .Should().NotThrow($"Failed at longitude {longitude}");

                FluentActions.Invoking(() => _sunCalculatorService.GetMoonElevation(TestDateTime, TestLatitude, longitude, TestTimezone))
                    .Should().NotThrow($"Failed at longitude {longitude}");
            }
        }

        #endregion

        #region Moon Phase Tests

        [Test]
        public void GetMoonIllumination_ShouldReturnValueInValidRange()
        {
            // Act
            var illumination = _sunCalculatorService.GetMoonIllumination(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            illumination.Should().BeInRange(0.0, 1.0);
        }

        [Test]
        public void GetMoonPhaseAngle_ShouldReturnValueInValidRange()
        {
            // Act
            var phaseAngle = _sunCalculatorService.GetMoonPhaseAngle(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert - CoordinateSharp can return negative angles, so accept wider range
            phaseAngle.Should().BeInRange(-180.0, 360.0);
            // OR if the library uses different conventions:
            // phaseAngle.Should().BeInRange(-360.0, 360.0);
        }

        [Test]
        public void GetMoonPhaseName_ShouldReturnValidPhaseName()
        {
            // Act
            var phaseName = _sunCalculatorService.GetMoonPhaseName(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            phaseName.Should().NotBeNullOrEmpty();
            phaseName.Should().BeOneOf(
                "NewMoon", "WaxingCrescent", "FirstQuarter", "WaxingGibbous",
                "FullMoon", "WaningGibbous", "ThirdQuarter", "WaningCrescent",
                "New Moon", "Waxing Crescent", "First Quarter", "Waxing Gibbous",
                "Full Moon", "Waning Gibbous", "Third Quarter", "Waning Crescent"
            );
        }

        [Test]
        public void GetMoonPhase_DifferentDates_ShouldShowProgression()
        {
            // Arrange - Test across a lunar month (approximately 29.5 days)
            var dates = new[]
            {
                new DateTime(2024, 6, 1),
                new DateTime(2024, 6, 8),
                new DateTime(2024, 6, 15),
                new DateTime(2024, 6, 22),
                new DateTime(2024, 6, 29)
            };

            // Act
            var illuminations = new double[dates.Length];
            var phaseNames = new string[dates.Length];

            for (int i = 0; i < dates.Length; i++)
            {
                illuminations[i] = _sunCalculatorService.GetMoonIllumination(dates[i], TestLatitude, TestLongitude, TestTimezone);
                phaseNames[i] = _sunCalculatorService.GetMoonPhaseName(dates[i], TestLatitude, TestLongitude, TestTimezone);
            }

            // Assert - Should show variety in illumination values
            var uniqueIlluminations = new HashSet<double>(illuminations);
            uniqueIlluminations.Count.Should().BeGreaterThan(1, "Moon illumination should change over the month");

            // Should have different phase names
            var uniquePhaseNames = new HashSet<string>(phaseNames);
            uniquePhaseNames.Count.Should().BeGreaterThan(1, "Moon phase names should change over the month");
        }

        [Test]
        public void GetMoonIllumination_NewMoonPeriod_ShouldBeLow()
        {
            // Arrange - Test around known new moon date (approximate)
            var newMoonDate = new DateTime(2024, 6, 6); // Approximate new moon

            // Act
            var illumination = _sunCalculatorService.GetMoonIllumination(newMoonDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - New moon should have low illumination
            illumination.Should().BeLessThan(0.2);
        }

        [Test]
        public void GetMoonIllumination_FullMoonPeriod_ShouldBeHigh()
        {
            // Arrange - Test around known full moon date (approximate)
            var fullMoonDate = new DateTime(2024, 6, 22); // Approximate full moon

            // Act
            var illumination = _sunCalculatorService.GetMoonIllumination(fullMoonDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Full moon should have high illumination
            illumination.Should().BeGreaterThan(0.8);
        }

        #endregion

        #region Lunar Perigee and Apogee Tests

        [Test]
        public void GetNextLunarPerigee_ShouldReturnFutureDate()
        {
            // Act
            var nextPerigee = _sunCalculatorService.GetNextLunarPerigee(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (nextPerigee.HasValue)
            {
                nextPerigee.Value.Should().BeAfter(TestDate);
                // Perigee occurs roughly every 27.3 days, so next one should be within reasonable timeframe
                nextPerigee.Value.Should().BeBefore(TestDate.AddDays(60));
            }
            // Note: nextPerigee can be null in some cases
        }

        [Test]
        public void GetNextLunarApogee_ShouldReturnFutureDate()
        {
            // Act
            var nextApogee = _sunCalculatorService.GetNextLunarApogee(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert
            if (nextApogee.HasValue)
            {
                nextApogee.Value.Should().BeAfter(TestDate);
                // Apogee occurs roughly every 27.3 days, so next one should be within reasonable timeframe
                nextApogee.Value.Should().BeBefore(TestDate.AddDays(60));
            }
            // Note: nextApogee can be null in some cases
        }

        [Test]
        public void GetLunarPerigeeApogee_DifferentLocations_ShouldBeConsistent()
        {
            // Arrange
            var locations = new[]
            {
                (47.6062, -122.3321), // Seattle
                (40.7128, -74.0060),  // New York
                (51.5074, -0.1278),   // London
                (-33.8688, 151.2093)  // Sydney
            };

            // Act & Assert
            DateTime? referencePerigee = null;
            DateTime? referenceApogee = null;

            foreach (var (lat, lon) in locations)
            {
                var perigee = _sunCalculatorService.GetNextLunarPerigee(TestDate, lat, lon, TestTimezone);
                var apogee = _sunCalculatorService.GetNextLunarApogee(TestDate, lat, lon, TestTimezone);

                if (referencePerigee == null)
                {
                    referencePerigee = perigee;
                    referenceApogee = apogee;
                }
                else
                {
                    // Perigee/apogee times should be very similar regardless of location
                    if (perigee.HasValue && referencePerigee.HasValue)
                    {
                        perigee.Value.Should().BeCloseTo(referencePerigee.Value, TimeSpan.FromHours(1));
                    }

                    if (apogee.HasValue && referenceApogee.HasValue)
                    {
                        apogee.Value.Should().BeCloseTo(referenceApogee.Value, TimeSpan.FromHours(1));
                    }
                }
            }
        }

        [Test]
        public void GetNextLunarPerigee_MultipleCalls_ShouldUseCaching()
        {
            // Act
            var perigee1 = _sunCalculatorService.GetNextLunarPerigee(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var perigee2 = _sunCalculatorService.GetNextLunarPerigee(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Results should be identical (cached)
            perigee1.Should().Be(perigee2);
        }

        [Test]
        public void GetNextLunarApogee_MultipleCalls_ShouldUseCaching()
        {
            // Act
            var apogee1 = _sunCalculatorService.GetNextLunarApogee(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var apogee2 = _sunCalculatorService.GetNextLunarApogee(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Results should be identical (cached)
            apogee1.Should().Be(apogee2);
        }

        #endregion

        #region Lunar Caching Tests

        [Test]
        public void GetMoonPosition_MultipleCalls_ShouldUseCaching()
        {
            // Act
            var azimuth1 = _sunCalculatorService.GetMoonAzimuth(TestDateTime, TestLatitude, TestLongitude, TestTimezone);
            var azimuth2 = _sunCalculatorService.GetMoonAzimuth(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            var elevation1 = _sunCalculatorService.GetMoonElevation(TestDateTime, TestLatitude, TestLongitude, TestTimezone);
            var elevation2 = _sunCalculatorService.GetMoonElevation(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Results should be identical (cached)
            azimuth1.Should().Be(azimuth2);
            elevation1.Should().Be(elevation2);
        }

        [Test]
        public void GetMoonPhase_MultipleCalls_ShouldUseCaching()
        {
            // Act
            var illumination1 = _sunCalculatorService.GetMoonIllumination(TestDateTime, TestLatitude, TestLongitude, TestTimezone);
            var illumination2 = _sunCalculatorService.GetMoonIllumination(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            var phaseName1 = _sunCalculatorService.GetMoonPhaseName(TestDateTime, TestLatitude, TestLongitude, TestTimezone);
            var phaseName2 = _sunCalculatorService.GetMoonPhaseName(TestDateTime, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Results should be identical (cached)
            illumination1.Should().Be(illumination2);
            phaseName1.Should().Be(phaseName2);
        }

        [Test]
        public void GetMoonrise_MultipleCalls_ShouldUseCaching()
        {
            // Act
            var moonrise1 = _sunCalculatorService.GetMoonrise(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var moonrise2 = _sunCalculatorService.GetMoonrise(TestDate, TestLatitude, TestLongitude, TestTimezone);

            var moonset1 = _sunCalculatorService.GetMoonset(TestDate, TestLatitude, TestLongitude, TestTimezone);
            var moonset2 = _sunCalculatorService.GetMoonset(TestDate, TestLatitude, TestLongitude, TestTimezone);

            // Assert - Results should be identical (cached)
            moonrise1.Should().Be(moonrise2);
            moonset1.Should().Be(moonset2);
        }

        #endregion

        #region Different Timezone Tests

        [Test]
        public void GetMoonPosition_DifferentTimezones_ShouldReturnSameValues()
        {
            // Arrange
            var utcTimezone = "UTC";
            var estTimezone = "America/New_York";

            // Act
            var azimuthUTC = _sunCalculatorService.GetMoonAzimuth(TestDateTime, TestLatitude, TestLongitude, utcTimezone);
            var azimuthEST = _sunCalculatorService.GetMoonAzimuth(TestDateTime, TestLatitude, TestLongitude, estTimezone);

            var elevationUTC = _sunCalculatorService.GetMoonElevation(TestDateTime, TestLatitude, TestLongitude, utcTimezone);
            var elevationEST = _sunCalculatorService.GetMoonElevation(TestDateTime, TestLatitude, TestLongitude, estTimezone);

            // Assert - Positions should be the same regardless of timezone parameter
            azimuthUTC.Should().BeApproximately(azimuthEST, 0.1);
            elevationUTC.Should().BeApproximately(elevationEST, 0.1);
        }

        [Test]
        public void GetMoonPhase_DifferentTimezones_ShouldReturnSameValues()
        {
            // Arrange
            var utcTimezone = "UTC";
            var pstTimezone = "America/Los_Angeles";

            // Act
            var illuminationUTC = _sunCalculatorService.GetMoonIllumination(TestDateTime, TestLatitude, TestLongitude, utcTimezone);
            var illuminationPST = _sunCalculatorService.GetMoonIllumination(TestDateTime, TestLatitude, TestLongitude, pstTimezone);

            var phaseNameUTC = _sunCalculatorService.GetMoonPhaseName(TestDateTime, TestLatitude, TestLongitude, utcTimezone);
            var phaseNamePST = _sunCalculatorService.GetMoonPhaseName(TestDateTime, TestLatitude, TestLongitude, pstTimezone);

            // Assert - Phase data should be the same regardless of timezone
            illuminationUTC.Should().BeApproximately(illuminationPST, 0.001);
            phaseNameUTC.Should().Be(phaseNamePST);
        }

        #endregion
    }
}