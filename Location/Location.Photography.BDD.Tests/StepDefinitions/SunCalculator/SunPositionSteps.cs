using BoDi;
using FluentAssertions;
using Location.Photography.BDD.Tests.Drivers;
using Location.Photography.BDD.Tests.Models;
using Location.Photography.BDD.Tests.Support;
using Location.Photography.Domain.Models;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Photography.BDD.Tests.StepDefinitions.SunCalculator
{
    [Binding]
    public class SunPositionSteps
    {
        private readonly ApiContext _context;
        private readonly SunCalculatorDriver _sunCalculatorDriver;
        private readonly IObjectContainer _objectContainer;

        public SunPositionSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _sunCalculatorDriver = new SunCalculatorDriver(context);
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                Console.WriteLine("SunPositionSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SunPositionSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have a location with coordinates (.*), (.*)")]
        public void GivenIHaveALocationWithCoordinates(double latitude, double longitude)
        {
            var sunCalculationModel = new SunCalculationTestModel
            {
                Id = 1,
                Latitude = latitude,
                Longitude = longitude,
                DateTime = DateTime.Now,
                Date = DateTime.Today,
                Time = DateTime.Now.TimeOfDay
            };

            sunCalculationModel.SynchronizeDateTime();
            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [Given(@"I have a specific date and time:")]
        public void GivenIHaveASpecificDateAndTime(Table table)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            if (sunCalculationModel == null)
            {
                sunCalculationModel = new SunCalculationTestModel { Id = 1 };
            }

            var dateTimeData = table.CreateInstance<SunCalculationTestModel>();

            if (dateTimeData.Date != default)
                sunCalculationModel.Date = dateTimeData.Date;

            if (dateTimeData.Time != default)
                sunCalculationModel.Time = dateTimeData.Time;

            if (dateTimeData.DateTime != default)
                sunCalculationModel.DateTime = dateTimeData.DateTime;

            // Synchronize the DateTime properties
            sunCalculationModel.SynchronizeDateTime();
            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [Given(@"I have multiple locations for sun position calculation:")]
        public void GivenIHaveMultipleLocationsForSunPositionCalculation(Table table)
        {
            var sunCalculations = table.CreateSet<SunCalculationTestModel>().ToList();

            // Assign IDs and synchronize DateTime properties
            for (int i = 0; i < sunCalculations.Count; i++)
            {
                if (!sunCalculations[i].Id.HasValue)
                {
                    sunCalculations[i].Id = i + 1;
                }

                sunCalculations[i].SynchronizeDateTime();
            }

            // Setup the sun calculations in the driver
            _sunCalculatorDriver.SetupSunCalculations(sunCalculations);

            // Store all calculations in the context
            _context.StoreModel(sunCalculations, "AllSunPositions");
        }

        [Given(@"the current time is (.*)")]
        public void GivenTheCurrentTimeIs(string timeString)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available in context");

            if (TimeSpan.TryParse(timeString, out var time))
            {
                sunCalculationModel.Time = time;
                sunCalculationModel.DateTime = sunCalculationModel.Date.Add(time);
                _context.StoreSunCalculationData(sunCalculationModel);
            }
        }

        [Given(@"I want to track the sun position for (.*)")]
        public void GivenIWantToTrackTheSunPositionFor(string locationName)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            if (sunCalculationModel == null)
            {
                // Set default coordinates based on location name
                var coordinates = GetCoordinatesForLocation(locationName);
                sunCalculationModel = new SunCalculationTestModel
                {
                    Id = 1,
                    Latitude = coordinates.latitude,
                    Longitude = coordinates.longitude,
                    DateTime = DateTime.Now,
                    Date = DateTime.Today,
                    Time = DateTime.Now.TimeOfDay
                };
                sunCalculationModel.SynchronizeDateTime();
            }

            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [When(@"I calculate the sun position")]
        public async Task WhenICalculateTheSunPosition()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available in context");

            // Use realistic calculation for accurate sun position
            await _sunCalculatorDriver.GetRealisticSunPositionAsync(sunCalculationModel);
        }

        [When(@"I calculate the sun position at (.*)")]
        public async Task WhenICalculateTheSunPositionAt(string timeString)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available in context");

            if (TimeSpan.TryParse(timeString, out var time))
            {
                sunCalculationModel.Time = time;
                sunCalculationModel.DateTime = sunCalculationModel.Date.Add(time);
                _context.StoreSunCalculationData(sunCalculationModel);
            }

            await WhenICalculateTheSunPosition();
        }

        [When(@"I request the sun azimuth")]
        public async Task WhenIRequestTheSunAzimuth()
        {
            await WhenICalculateTheSunPosition();
        }

        [When(@"I request the sun elevation")]
        public async Task WhenIRequestTheSunElevation()
        {
            await WhenICalculateTheSunPosition();
        }

        [When(@"I calculate sun positions for all locations")]
        public async Task WhenICalculateSunPositionsForAllLocations()
        {
            var allSunPositions = _context.GetModel<List<SunCalculationTestModel>>("AllSunPositions");
            allSunPositions.Should().NotBeNull("Sun position data should be available in context");

            foreach (var sunPosition in allSunPositions)
            {
                // Use realistic calculation for all positions
                await _sunCalculatorDriver.GetRealisticSunPositionAsync(sunPosition);
            }
        }

        [When(@"I track the sun position over time")]
        public async Task WhenITrackTheSunPositionOverTime()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available in context");

            // Create multiple time points throughout the day
            var timePoints = new[]
            {
               TimeSpan.FromHours(6),   // Dawn
               TimeSpan.FromHours(9),   // Morning
               TimeSpan.FromHours(12),  // Noon
               TimeSpan.FromHours(15),  // Afternoon
               TimeSpan.FromHours(18)   // Evening
           };

            var sunPositions = new List<SunCalculationTestModel>();

            foreach (var timePoint in timePoints)
            {
                var positionModel = sunCalculationModel.Clone();
                positionModel.Time = timePoint;
                positionModel.DateTime = positionModel.Date.Add(timePoint);

                sunPositions.Add(positionModel);
                // Use realistic calculation for tracking
                await _sunCalculatorDriver.GetRealisticSunPositionAsync(positionModel);
            }

            _context.StoreModel(sunPositions, "SunPositionTracking");
        }

        [Then(@"I should receive the sun azimuth")]
        public void ThenIShouldReceiveTheSunAzimuth()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Azimuth.Should().BeGreaterOrEqualTo(0).And.BeLessThan(360, "Azimuth should be between 0 and 360 degrees");
        }

        [Then(@"I should receive the sun elevation")]
        public void ThenIShouldReceiveTheSunElevation()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Elevation.Should().BeGreaterOrEqualTo(-90).And.BeLessOrEqualTo(90, "Elevation should be between -90 and 90 degrees");
        }

        [Then(@"the sun azimuth should be approximately (.*) degrees")]
        public void ThenTheSunAzimuthShouldBeApproximatelyDegrees(double expectedAzimuth)
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Azimuth.Should().BeApproximately(expectedAzimuth, 5.0, $"Sun azimuth should be approximately {expectedAzimuth} degrees");
        }

        [Then(@"the sun elevation should be approximately (.*) degrees")]
        public void ThenTheSunElevationShouldBeApproximatelyDegrees(double expectedElevation)
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Elevation.Should().BeApproximately(expectedElevation, 5.0, $"Sun elevation should be approximately {expectedElevation} degrees");
        }

        [Then(@"the sun position should be calculated successfully")]
        public void ThenTheSunPositionShouldBeCalculatedSuccessfully()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");

            // Validate that we have reasonable values
            result.Data.Azimuth.Should().BeGreaterOrEqualTo(0).And.BeLessThan(360);
            result.Data.Elevation.Should().BeGreaterOrEqualTo(-90).And.BeLessOrEqualTo(90);
            result.Data.DateTime.Should().NotBe(default(DateTime));
            result.Data.Latitude.Should().BeGreaterOrEqualTo(-90).And.BeLessOrEqualTo(90);
            result.Data.Longitude.Should().BeGreaterOrEqualTo(-180).And.BeLessOrEqualTo(180);
        }

        [Then(@"the sun should be visible \(elevation > 0\)")]
        public void ThenTheSunShouldBeVisibleElevation0()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Elevation.Should().BeGreaterThan(0, "Sun should be visible (elevation > 0)");
        }

        [Then(@"the sun should be below the horizon \(elevation < 0\)")]
        public void ThenTheSunShouldBeBelowTheHorizonElevation0()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Elevation.Should().BeLessThan(0, "Sun should be below the horizon (elevation < 0)");
        }

        [Then(@"the sun should be roughly in the (.*) direction")]
        public void ThenTheSunShouldBeRoughlyInTheDirection(string direction)
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");

            var expectedAzimuthRange = GetAzimuthRangeForDirection(direction);

            // Handle north direction wraparound (315-360 and 0-45)
            if (direction.ToLowerInvariant() == "north")
            {
                var azimuth = result.Data.Azimuth;
                var inRange = (azimuth >= expectedAzimuthRange.min && azimuth <= 360) ||
                             (azimuth >= 0 && azimuth <= expectedAzimuthRange.max);
                inRange.Should().BeTrue($"Sun should be roughly in the {direction} direction (azimuth: {azimuth})");
            }
            else
            {
                result.Data.Azimuth.Should().BeInRange(expectedAzimuthRange.min, expectedAzimuthRange.max,
                    $"Sun should be roughly in the {direction} direction");
            }
        }

        [Then(@"all sun positions should be calculated successfully")]
        public void ThenAllSunPositionsShouldBeCalculatedSuccessfully()
        {
            var allSunPositions = _context.GetModel<List<SunCalculationTestModel>>("AllSunPositions");
            allSunPositions.Should().NotBeNull("Sun position data should be available in context");

            foreach (var sunPosition in allSunPositions)
            {
                sunPosition.SolarAzimuth.Should().BeGreaterOrEqualTo(0).And.BeLessThan(360,
                    $"Azimuth for location {sunPosition.Id} should be valid");
                sunPosition.SolarElevation.Should().BeGreaterOrEqualTo(-90).And.BeLessOrEqualTo(90,
                    $"Elevation for location {sunPosition.Id} should be valid");
            }
        }

        [Then(@"the sun position should change over time")]
        public void ThenTheSunPositionShouldChangeOverTime()
        {
            var sunPositionTracking = _context.GetModel<List<SunCalculationTestModel>>("SunPositionTracking");
            sunPositionTracking.Should().NotBeNull("Sun position tracking data should be available");
            sunPositionTracking.Should().HaveCountGreaterThan(1, "Should have multiple time points");

            // Verify that positions are different at different times
            for (int i = 1; i < sunPositionTracking.Count; i++)
            {
                var current = sunPositionTracking[i];
                var previous = sunPositionTracking[i - 1];

                // At least one of azimuth or elevation should be different
                var azimuthDiff = Math.Abs(current.SolarAzimuth - previous.SolarAzimuth);
                var elevationDiff = Math.Abs(current.SolarElevation - previous.SolarElevation);

                (azimuthDiff > 1 || elevationDiff > 1).Should().BeTrue(
                    $"Sun position should change between {previous.Time} and {current.Time}");
            }
        }

        [Then(@"the sun coordinates should match the location")]
        public void ThenTheSunCoordinatesShouldMatchTheLocation()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.Data.Should().NotBeNull("Sun position data should be available");

            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            result.Data.Latitude.Should().BeApproximately(sunCalculationModel.Latitude, 0.0001, "Latitude should match");
            result.Data.Longitude.Should().BeApproximately(sunCalculationModel.Longitude, 0.0001, "Longitude should match");
            result.Data.DateTime.Should().BeCloseTo(sunCalculationModel.DateTime, TimeSpan.FromMinutes(1), "DateTime should match");
        }

        // Helper methods
        private (double latitude, double longitude) GetCoordinatesForLocation(string locationName)
        {
            return locationName.ToLowerInvariant() switch
            {
                "new york" => (40.7128, -74.0060),
                "london" => (51.5074, -0.1278),
                "tokyo" => (35.6762, 139.6503),
                "sydney" => (-33.8688, 151.2093),
                "paris" => (48.8566, 2.3522),
                _ => (40.7128, -74.0060) // Default to New York
            };
        }

        private (double min, double max) GetAzimuthRangeForDirection(string direction)
        {
            return direction.ToLowerInvariant() switch
            {
                "north" => (315, 45),
                "northeast" => (22.5, 67.5),
                "east" => (67.5, 112.5),
                "southeast" => (112.5, 157.5),
                "south" => (157.5, 202.5),
                "southwest" => (202.5, 247.5),
                "west" => (247.5, 292.5),
                "northwest" => (292.5, 337.5),
                _ => (0, 360)
            };
        }
    }
}