using BoDi;
using FluentAssertions;
using Location.Photography.BDD.Tests.Drivers;
using Location.Photography.BDD.Tests.Models;
using Location.Photography.BDD.Tests.Support;
using Location.Photography.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Photography.BDD.Tests.StepDefinitions.SunCalculator
{
    [Binding]
    public class SunLocationSteps
    {
        private readonly ApiContext _context;
        private readonly SunCalculatorDriver _sunCalculatorDriver;
        private readonly IObjectContainer _objectContainer;

        public SunLocationSteps(ApiContext context, IObjectContainer objectContainer)
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
                Console.WriteLine("SunLocationSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SunLocationSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I am at location coordinates (.*), (.*)")]
        public void GivenIAmAtLocationCoordinates(double latitude, double longitude)
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

        [Given(@"I have a device with compass and orientation sensors")]
        public void GivenIHaveADeviceWithCompassAndOrientationSensors()
        {
            // Store sensor availability in context
            _context.StoreModel((object)true, "SensorsAvailable");
        }

        [Given(@"I want to track the sun location for photography")]
        public void GivenIWantToTrackTheSunLocationForPhotography()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            if (sunCalculationModel == null)
            {
                sunCalculationModel = new SunCalculationTestModel
                {
                    Id = 1,
                    Latitude = 40.7128,
                    Longitude = -74.0060,
                    DateTime = DateTime.Now,
                    Date = DateTime.Today,
                    Time = DateTime.Now.TimeOfDay
                };
                sunCalculationModel.SynchronizeDateTime();
            }

            // Set tracking mode
            _context.StoreModel("tracking", "SunLocationMode");
            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [Given(@"the device is pointing in direction (.*) degrees")]
        public void GivenTheDeviceIsPointingInDirectionDegrees(double azimuth)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            // Store device orientation
            _context.StoreModel((object)azimuth, "DeviceAzimuth");
        }

        [Given(@"the device is tilted at (.*) degrees elevation")]
        public void GivenTheDeviceIsTiltedAtDegreesElevation(double elevation)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            // Store device tilt
            _context.StoreModel((object)elevation, "DeviceTilt");
        }

        [Given(@"I have multiple sun tracking sessions:")]
        public void GivenIHaveMultipleSunTrackingSessions(Table table)
        {
            var sunCalculations = table.CreateSet<SunCalculationTestModel>().ToList();

            for (int i = 0; i < sunCalculations.Count; i++)
            {
                if (!sunCalculations[i].Id.HasValue)
                {
                    sunCalculations[i].Id = i + 1;
                }

                sunCalculations[i].SynchronizeDateTime();
                SetExpectedSunPosition(sunCalculations[i]);
            }

            _sunCalculatorDriver.SetupSunCalculations(sunCalculations);
            _context.StoreModel(sunCalculations, "SunTrackingSessions");
        }

        [Given(@"the sun is currently at azimuth (.*) and elevation (.*)")]
        public void GivenTheSunIsCurrentlyAtAzimuthAndElevation(double azimuth, double elevation)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            sunCalculationModel.SolarAzimuth = azimuth;
            sunCalculationModel.SolarElevation = elevation;
            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [When(@"I start sun location tracking")]
        public async Task WhenIStartSunLocationTracking()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            SetExpectedSunPosition(sunCalculationModel);
            await _sunCalculatorDriver.GetSunPositionAsync(sunCalculationModel);

            // Mark tracking as started
            _context.StoreModel((object)true, "TrackingStarted");
        }

        [When(@"I point my device toward the sun")]
        public async Task WhenIPointMyDeviceTowardTheSun()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            // Calculate current sun position
            SetExpectedSunPosition(sunCalculationModel);
            await _sunCalculatorDriver.GetSunPositionAsync(sunCalculationModel);

            // Set device pointing toward sun
            _context.StoreModel((object)sunCalculationModel.SolarAzimuth, "DeviceAzimuth");
            _context.StoreModel((object)sunCalculationModel.SolarElevation, "DeviceTilt");
        }

        [When(@"I check if my device is aligned with the sun")]
        public async Task WhenICheckIfMyDeviceIsAlignedWithTheSun()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            var deviceAzimuth = (double)_context.GetModel<object>("DeviceAzimuth");
            var deviceTilt = (double)_context.GetModel<object>("DeviceTilt");

            SetExpectedSunPosition(sunCalculationModel);
            await _sunCalculatorDriver.GetSunPositionAsync(sunCalculationModel);

            // Calculate alignment
            var azimuthDifference = Math.Abs(sunCalculationModel.SolarAzimuth - deviceAzimuth);
            var elevationDifference = Math.Abs(sunCalculationModel.SolarElevation - deviceTilt);
            var isAligned = azimuthDifference <= 5.0 && elevationDifference <= 5.0;

            _context.StoreModel((object)isAligned, "DeviceAligned");

            var alignmentResult = Location.Core.Application.Common.Models.Result<bool>.Success(isAligned);
            _context.StoreResult(alignmentResult);
        }

        [When(@"I update my location to coordinates (.*), (.*)")]
        public async Task WhenIUpdateMyLocationToCoordinates(double latitude, double longitude)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            sunCalculationModel.Latitude = latitude;
            sunCalculationModel.Longitude = longitude;
            _context.StoreSunCalculationData(sunCalculationModel);

            SetExpectedSunPosition(sunCalculationModel);
            await _sunCalculatorDriver.GetSunPositionAsync(sunCalculationModel);
        }

        [When(@"I track the sun for (.*) minutes")]
        public async Task WhenITrackTheSunForMinutes(int minutes)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            var trackingPositions = new List<SunCalculationTestModel>();
            var startTime = sunCalculationModel.DateTime;

            for (int i = 0; i <= minutes; i += 15) // Every 15 minutes
            {
                var trackingModel = sunCalculationModel.Clone();
                trackingModel.DateTime = startTime.AddMinutes(i);
                trackingModel.SynchronizeDateTime();

                SetExpectedSunPosition(trackingModel);
                trackingPositions.Add(trackingModel);

                await _sunCalculatorDriver.GetSunPositionAsync(trackingModel);
            }

            _context.StoreModel(trackingPositions, "TrackingHistory");
        }

        [When(@"I get the current sun direction")]
        public async Task WhenIGetTheCurrentSunDirection()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available");

            SetExpectedSunPosition(sunCalculationModel);
            await _sunCalculatorDriver.GetSunPositionAsync(sunCalculationModel);
        }

        [When(@"I calculate sun location for all tracking sessions")]
        public async Task WhenICalculateSunLocationForAllTrackingSessions()
        {
            var sessions = _context.GetModel<List<SunCalculationTestModel>>("SunTrackingSessions");
            sessions.Should().NotBeNull("Sun tracking sessions should be available");

            foreach (var session in sessions)
            {
                SetExpectedSunPosition(session);
                await _sunCalculatorDriver.GetSunPositionAsync(session);
            }
        }

        [Then(@"I should receive the current sun direction")]
        public void ThenIShouldReceiveTheCurrentSunDirection()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Azimuth.Should().BeGreaterOrEqualTo(0).And.BeLessThan(360, "Azimuth should be valid");
        }

        [Then(@"the device should be aligned with the sun")]
        public void ThenTheDeviceShouldBeAlignedWithTheSun()
        {
            var result = _context.GetLastResult<bool>();
            result.Should().NotBeNull("Alignment result should be available");
            result.IsSuccess.Should().BeTrue("Alignment check should be successful");
            result.Data.Should().BeTrue("Device should be aligned with the sun");
        }

        [Then(@"the device should not be aligned with the sun")]
        public void ThenTheDeviceShouldNotBeAlignedWithTheSun()
        {
            var result = _context.GetLastResult<bool>();
            result.Should().NotBeNull("Alignment result should be available");
            result.IsSuccess.Should().BeTrue("Alignment check should be successful");
            result.Data.Should().BeFalse("Device should not be aligned with the sun");
        }

        [Then(@"the sun location should update based on my new position")]
        public void ThenTheSunLocationShouldUpdateBasedOnMyNewPosition()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.IsSuccess.Should().BeTrue("Sun position calculation should be successful");
            result.Data.Should().NotBeNull("Sun position data should be available");

            var sunCalculationModel = _context.GetSunCalculationData();
            result.Data.Latitude.Should().BeApproximately(sunCalculationModel.Latitude, 0.0001, "Latitude should match updated location");
            result.Data.Longitude.Should().BeApproximately(sunCalculationModel.Longitude, 0.0001, "Longitude should match updated location");
        }

        [Then(@"I should see the sun position change over time")]
        public void ThenIShouldSeeTheSunPositionChangeOverTime()
        {
            var trackingHistory = _context.GetModel<List<SunCalculationTestModel>>("TrackingHistory");
            trackingHistory.Should().NotBeNull("Tracking history should be available");
            trackingHistory.Should().HaveCountGreaterThan(1, "Should have multiple tracking points");

            for (int i = 1; i < trackingHistory.Count; i++)
            {
                var current = trackingHistory[i];
                var previous = trackingHistory[i - 1];

                var azimuthDiff = Math.Abs(current.SolarAzimuth - previous.SolarAzimuth);
                var elevationDiff = Math.Abs(current.SolarElevation - previous.SolarElevation);

                (azimuthDiff > 0.1 || elevationDiff > 0.1).Should().BeTrue(
                    $"Sun position should change between time {previous.DateTime} and {current.DateTime}");
            }
        }

        [Then(@"all sun tracking sessions should be successful")]
        public void ThenAllSunTrackingSessionsShouldBeSuccessful()
        {
            var sessions = _context.GetModel<List<SunCalculationTestModel>>("SunTrackingSessions");
            sessions.Should().NotBeNull("Sun tracking sessions should be available");

            foreach (var session in sessions)
            {
                session.SolarAzimuth.Should().BeGreaterOrEqualTo(0).And.BeLessThan(360,
                    $"Azimuth for session {session.Id} should be valid");
                session.SolarElevation.Should().BeGreaterOrEqualTo(-90).And.BeLessOrEqualTo(90,
                    $"Elevation for session {session.Id} should be valid");
            }
        }

        [Then(@"the sun direction should be approximately (.*) degrees azimuth")]
        public void ThenTheSunDirectionShouldBeApproximatelyDegreesAzimuth(double expectedAzimuth)
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Azimuth.Should().BeApproximately(expectedAzimuth, 10.0,
                $"Sun azimuth should be approximately {expectedAzimuth} degrees");
        }

        [Then(@"the sun elevation should be approximately (.*) degrees")]
        public void ThenTheSunElevationShouldBeApproximatelyDegrees(double expectedElevation)
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.Data.Should().NotBeNull("Sun position data should be available");
            result.Data.Elevation.Should().BeApproximately(expectedElevation, 10.0,
                $"Sun elevation should be approximately {expectedElevation} degrees");
        }

        [Then(@"the tracking should be active")]
        public void ThenTheTrackingShouldBeActive()
        {
            var trackingStarted = (bool)_context.GetModel<object>("TrackingStarted");
            trackingStarted.Should().BeTrue("Sun location tracking should be active");
        }

        [Then(@"the sun location coordinates should match my current position")]
        public void ThenTheSunLocationCoordinatesShouldMatchMyCurrentPosition()
        {
            var result = _context.GetLastResult<SunPositionDto>();
            result.Should().NotBeNull("Sun position result should be available");
            result.Data.Should().NotBeNull("Sun position data should be available");

            var sunCalculationModel = _context.GetSunCalculationData();
            result.Data.Latitude.Should().BeApproximately(sunCalculationModel.Latitude, 0.0001, "Latitude should match");
            result.Data.Longitude.Should().BeApproximately(sunCalculationModel.Longitude, 0.0001, "Longitude should match");
        }

        private void SetExpectedSunPosition(SunCalculationTestModel model)
        {
            var timeHours = model.Time.TotalHours;

            // Simplified sun position calculation for testing
            if (timeHours < 6)
            {
                model.SolarAzimuth = 90; // East
                model.SolarElevation = -15; // Below horizon
            }
            else if (timeHours < 12)
            {
                model.SolarAzimuth = 135 + (timeHours - 6) * 7.5; // Southeast to South
                model.SolarElevation = Math.Max(-10, 60 - Math.Abs(timeHours - 12) * 10);
            }
            else if (timeHours < 18)
            {
                model.SolarAzimuth = 225 + (timeHours - 12) * 7.5; // South to Southwest
                model.SolarElevation = Math.Max(-10, 60 - Math.Abs(timeHours - 12) * 10);
            }
            else
            {
                model.SolarAzimuth = 270; // West
                model.SolarElevation = -15; // Below horizon
            }

            // Adjust for latitude
            var latitudeFactor = Math.Abs(model.Latitude) / 90.0;
            model.SolarElevation *= (1 - latitudeFactor * 0.3);
        }
    }
}