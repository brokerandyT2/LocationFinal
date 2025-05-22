using BoDi;
using FluentAssertions;
using Location.Photography.Application.Services;
using Location.Photography.BDD.Tests.Drivers;
using Location.Photography.BDD.Tests.Models;
using Location.Photography.BDD.Tests.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Photography.BDD.Tests.StepDefinitions.ExposureCalculator
{
    [Binding]
    public class ExposureCalculatorSteps
    {
        private readonly ApiContext _context;
        private readonly ExposureCalculatorDriver _exposureCalculatorDriver;
        private readonly IObjectContainer _objectContainer;

        public ExposureCalculatorSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _exposureCalculatorDriver = new ExposureCalculatorDriver(context);
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                Console.WriteLine("ExposureCalculatorSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExposureCalculatorSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have a base exposure with the following settings:")]
        public void GivenIHaveABaseExposureWithTheFollowingSettings(Table table)
        {
            var exposureModel = table.CreateInstance<ExposureTestModel>();

            // Assign an ID if not provided
            if (!exposureModel.Id.HasValue)
            {
                exposureModel.Id = 1;
            }

            // Store the exposure in the context
            _context.StoreExposureData(exposureModel);
        }

        [Given(@"I have multiple exposure scenarios:")]
        public void GivenIHaveMultipleExposureScenarios(Table table)
        {
            var exposures = table.CreateSet<ExposureTestModel>().ToList();

            // Assign IDs if not provided
            for (int i = 0; i < exposures.Count; i++)
            {
                if (!exposures[i].Id.HasValue)
                {
                    exposures[i].Id = i + 1;
                }
            }

            // Setup the exposures in the repository
            _exposureCalculatorDriver.SetupExposures(exposures);

            // Store all exposures in the context
            _context.StoreModel(exposures, "AllExposures");
        }

        [Given(@"I want to calculate the (.*) with the following target settings:")]
        public void GivenIWantToCalculateTheWithTheFollowingTargetSettings(string calculationType, Table table)
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Base exposure data should be available in context");

            var targetData = table.CreateInstance<ExposureTestModel>();

            // Update target settings
            if (!string.IsNullOrEmpty(targetData.TargetShutterSpeed))
                exposureModel.TargetShutterSpeed = targetData.TargetShutterSpeed;
            if (!string.IsNullOrEmpty(targetData.TargetAperture))
                exposureModel.TargetAperture = targetData.TargetAperture;
            if (!string.IsNullOrEmpty(targetData.TargetIso))
                exposureModel.TargetIso = targetData.TargetIso;

            // Set the calculation type
            exposureModel.FixedValue = calculationType.ToLowerInvariant() switch
            {
                "shutter speed" => FixedValue.ShutterSpeeds,
                "aperture" => FixedValue.Aperture,
                "iso" => FixedValue.ISO,
                _ => FixedValue.ShutterSpeeds
            };

            // Update other properties if provided
            if (targetData.Increments != default)
                exposureModel.Increments = targetData.Increments;
            if (targetData.EvCompensation != 0)
                exposureModel.EvCompensation = targetData.EvCompensation;

            _context.StoreExposureData(exposureModel);
        }

        [Given(@"I am using (.*) stops increments")]
        public void GivenIAmUsingStopsIncrements(string incrementType)
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            exposureModel.Increments = incrementType.ToLowerInvariant() switch
            {
                "full" => ExposureIncrements.Full,
                "half" => ExposureIncrements.Half,
                "third" => ExposureIncrements.Third,
                _ => ExposureIncrements.Full
            };

            _context.StoreExposureData(exposureModel);
        }

        [Given(@"I apply an EV compensation of (.*)")]
        public void GivenIApplyAnEVCompensationOf(double evCompensation)
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            exposureModel.EvCompensation = evCompensation;
            _context.StoreExposureData(exposureModel);
        }

        [When(@"I calculate the equivalent exposure")]
        public async Task WhenICalculateTheEquivalentExposure()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            // Calculate based on the fixed value type
            switch (exposureModel.FixedValue)
            {
                case FixedValue.ShutterSpeeds:
                    await _exposureCalculatorDriver.CalculateShutterSpeedAsync(exposureModel);
                    break;
                case FixedValue.Aperture:
                    await _exposureCalculatorDriver.CalculateApertureAsync(exposureModel);
                    break;
                case FixedValue.ISO:
                    await _exposureCalculatorDriver.CalculateIsoAsync(exposureModel);
                    break;
                default:
                    await _exposureCalculatorDriver.CalculateShutterSpeedAsync(exposureModel);
                    break;
            }
        }

        [When(@"I calculate the shutter speed")]
        public async Task WhenICalculateTheShutterSpeed()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            await _exposureCalculatorDriver.CalculateShutterSpeedAsync(exposureModel);
        }

        [When(@"I calculate the aperture")]
        public async Task WhenICalculateTheAperture()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            await _exposureCalculatorDriver.CalculateApertureAsync(exposureModel);
        }

        [When(@"I calculate the ISO")]
        public async Task WhenICalculateTheISO()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            await _exposureCalculatorDriver.CalculateIsoAsync(exposureModel);
        }

        [When(@"I request the available shutter speeds for (.*) stops")]
        public async Task WhenIRequestTheAvailableShutterSpeedsForStops(string incrementType)
        {
            var increments = incrementType.ToLowerInvariant() switch
            {
                "full" => ExposureIncrements.Full,
                "half" => ExposureIncrements.Half,
                "third" => ExposureIncrements.Third,
                _ => ExposureIncrements.Full
            };

            await _exposureCalculatorDriver.GetShutterSpeedsAsync(increments);
        }

        [When(@"I request the available apertures for (.*) stops")]
        public async Task WhenIRequestTheAvailableAperturesForStops(string incrementType)
        {
            var increments = incrementType.ToLowerInvariant() switch
            {
                "full" => ExposureIncrements.Full,
                "half" => ExposureIncrements.Half,
                "third" => ExposureIncrements.Third,
                _ => ExposureIncrements.Full
            };

            await _exposureCalculatorDriver.GetAperturesAsync(increments);
        }

        [When(@"I request the available ISOs for (.*) stops")]
        public async Task WhenIRequestTheAvailableISOsForStops(string incrementType)
        {
            var increments = incrementType.ToLowerInvariant() switch
            {
                "full" => ExposureIncrements.Full,
                "half" => ExposureIncrements.Half,
                "third" => ExposureIncrements.Third,
                _ => ExposureIncrements.Full
            };

            await _exposureCalculatorDriver.GetIsosAsync(increments);
        }

        [Then(@"the calculated shutter speed should be ""(.*)""")]
        public void ThenTheCalculatedShutterSpeedShouldBe(string expectedShutterSpeed)
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            result.Should().NotBeNull("Exposure result should be available");
            result.IsSuccess.Should().BeTrue("Exposure calculation should be successful");
            result.Data.Should().NotBeNull("Exposure data should be available");
            result.Data.ShutterSpeed.Should().Be(expectedShutterSpeed, "Calculated shutter speed should match expected value");
        }

        [Then(@"the calculated aperture should be ""(.*)""")]
        public void ThenTheCalculatedApertureShouldBe(string expectedAperture)
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            result.Should().NotBeNull("Exposure result should be available");
            result.IsSuccess.Should().BeTrue("Exposure calculation should be successful");
            result.Data.Should().NotBeNull("Exposure data should be available");
            result.Data.Aperture.Should().Be(expectedAperture, "Calculated aperture should match expected value");
        }

        [Then(@"the calculated ISO should be ""(.*)""")]
        public void ThenTheCalculatedISOShouldBe(string expectedIso)
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            result.Should().NotBeNull("Exposure result should be available");
            result.IsSuccess.Should().BeTrue("Exposure calculation should be successful");
            result.Data.Should().NotBeNull("Exposure data should be available");
            result.Data.Iso.Should().Be(expectedIso, "Calculated ISO should match expected value");
        }

        [Then(@"the exposure calculation should be successful")]
        public void ThenTheExposureCalculationShouldBeSuccessful()
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            result.Should().NotBeNull("Exposure result should be available");
            result.IsSuccess.Should().BeTrue("Exposure calculation should be successful");
            result.Data.Should().NotBeNull("Exposure data should be available");
        }

        [Then(@"the available shutter speeds should include:")]
        public void ThenTheAvailableShutterSpeedsShouldInclude(Table table)
        {
            var result = _context.GetLastResult<string[]>();
            result.Should().NotBeNull("Shutter speeds result should be available");
            result.IsSuccess.Should().BeTrue("Getting shutter speeds should be successful");
            result.Data.Should().NotBeNull("Shutter speeds data should be available");

            foreach (var row in table.Rows)
            {
                string expectedSpeed = row["ShutterSpeed"];
                result.Data.Should().Contain(expectedSpeed, $"Available shutter speeds should include '{expectedSpeed}'");
            }
        }

        [Then(@"the available apertures should include:")]
        public void ThenTheAvailableAperturesShouldInclude(Table table)
        {
            var result = _context.GetLastResult<string[]>();
            result.Should().NotBeNull("Apertures result should be available");
            result.IsSuccess.Should().BeTrue("Getting apertures should be successful");
            result.Data.Should().NotBeNull("Apertures data should be available");

            foreach (var row in table.Rows)
            {
                string expectedAperture = row["Aperture"];
                result.Data.Should().Contain(expectedAperture, $"Available apertures should include '{expectedAperture}'");
            }
        }

        [Then(@"the available ISOs should include:")]
        public void ThenTheAvailableISOsShouldInclude(Table table)
        {
            var result = _context.GetLastResult<string[]>();
            result.Should().NotBeNull("ISOs result should be available");
            result.IsSuccess.Should().BeTrue("Getting ISOs should be successful");
            result.Data.Should().NotBeNull("ISOs data should be available");

            foreach (var row in table.Rows)
            {
                string expectedIso = row["ISO"];
                result.Data.Should().Contain(expectedIso, $"Available ISOs should include '{expectedIso}'");
            }
        }

        [Then(@"the exposure calculation should fail")]
        public void ThenTheExposureCalculationShouldFail()
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            result.Should().NotBeNull("Exposure result should be available");
            result.IsSuccess.Should().BeFalse("Exposure calculation should fail");
        }

        [Then(@"the exposure error should indicate ""(.*)""")]
        public void ThenTheExposureErrorShouldIndicate(string expectedError)
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            result.Should().NotBeNull("Exposure result should be available");
            result.IsSuccess.Should().BeFalse("Exposure calculation should fail");
            result.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be provided");
            result.ErrorMessage.Should().Contain(expectedError, $"Error message should contain '{expectedError}'");
        }

        [Then(@"the calculated exposure should have the following settings:")]
        public void ThenTheCalculatedExposureShouldHaveTheFollowingSettings(Table table)
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            result.Should().NotBeNull("Exposure result should be available");
            result.IsSuccess.Should().BeTrue("Exposure calculation should be successful");
            result.Data.Should().NotBeNull("Exposure data should be available");

            var expectedSettings = table.CreateInstance<ExposureTestModel>();

            if (!string.IsNullOrEmpty(expectedSettings.ResultShutterSpeed))
            {
                result.Data.ShutterSpeed.Should().Be(expectedSettings.ResultShutterSpeed, "Shutter speed should match expected value");
            }

            if (!string.IsNullOrEmpty(expectedSettings.ResultAperture))
            {
                result.Data.Aperture.Should().Be(expectedSettings.ResultAperture, "Aperture should match expected value");
            }

            if (!string.IsNullOrEmpty(expectedSettings.ResultIso))
            {
                result.Data.Iso.Should().Be(expectedSettings.ResultIso, "ISO should match expected value");
            }
        }

        [Then(@"the shutter speeds list should contain (.*) values")]
        public void ThenTheShutterSpeedsListShouldContainValues(int expectedCount)
        {
            var result = _context.GetLastResult<string[]>();
            result.Should().NotBeNull("Shutter speeds result should be available");
            result.IsSuccess.Should().BeTrue("Getting shutter speeds should be successful");
            result.Data.Should().NotBeNull("Shutter speeds data should be available");
            result.Data.Length.Should().Be(expectedCount, $"Shutter speeds list should contain {expectedCount} values");
        }

        [Then(@"the apertures list should contain (.*) values")]
        public void ThenTheAperturesListShouldContainValues(int expectedCount)
        {
            var result = _context.GetLastResult<string[]>();
            result.Should().NotBeNull("Apertures result should be available");
            result.IsSuccess.Should().BeTrue("Getting apertures should be successful");
            result.Data.Should().NotBeNull("Apertures data should be available");
            result.Data.Length.Should().Be(expectedCount, $"Apertures list should contain {expectedCount} values");
        }

        [Then(@"the ISOs list should contain (.*) values")]
        public void ThenTheISOsListShouldContainValues(int expectedCount)
        {
            var result = _context.GetLastResult<string[]>();
            result.Should().NotBeNull("ISOs result should be available");
            result.IsSuccess.Should().BeTrue("Getting ISOs should be successful");
            result.Data.Should().NotBeNull("ISOs data should be available");
            result.Data.Length.Should().Be(expectedCount, $"ISOs list should contain {expectedCount} values");
        }
    }
}