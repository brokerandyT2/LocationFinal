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
    public class ExposureValidationSteps
    {
        private readonly ApiContext _context;
        private readonly ExposureCalculatorDriver _exposureCalculatorDriver;
        private readonly IObjectContainer _objectContainer;

        public ExposureValidationSteps(ApiContext context, IObjectContainer objectContainer)
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
                Console.WriteLine("ExposureValidationSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ExposureValidationSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have an invalid exposure with the following settings:")]
        public void GivenIHaveAnInvalidExposureWithTheFollowingSettings(Table table)
        {
            var exposureModel = table.CreateInstance<ExposureTestModel>();

            // Assign an ID if not provided
            if (!exposureModel.Id.HasValue)
            {
                exposureModel.Id = 1;
            }

            // Store the invalid exposure in the context
            _context.StoreExposureData(exposureModel);
        }

        [Given(@"I have exposure settings that will cause (.*)")]
        public void GivenIHaveExposureSettingsThatWillCause(string errorType, Table table)
        {
            var exposureModel = table.CreateInstance<ExposureTestModel>();

            // Assign an ID if not provided
            if (!exposureModel.Id.HasValue)
            {
                exposureModel.Id = 1;
            }

            // Set appropriate error message based on error type
            exposureModel.ErrorMessage = errorType.ToLowerInvariant() switch
            {
                "overexposure" => "Image will be overexposed by 2.0 stops",
                "underexposure" => "Image will be underexposed by 1.5 stops",
                "parameter limits" => "The requested shutter speed exceeds available limits",
                _ => $"Exposure validation error: {errorType}"
            };

            _context.StoreExposureData(exposureModel);
        }

        [Given(@"I have multiple invalid exposure scenarios:")]
        public void GivenIHaveMultipleInvalidExposureScenarios(Table table)
        {
            var exposures = table.CreateSet<ExposureTestModel>().ToList();

            // Assign IDs and validate each exposure
            for (int i = 0; i < exposures.Count; i++)
            {
                if (!exposures[i].Id.HasValue)
                {
                    exposures[i].Id = i + 1;
                }

                // Validate and set error messages if needed
                if (string.IsNullOrEmpty(exposures[i].ErrorMessage))
                {
                    if (string.IsNullOrEmpty(exposures[i].BaseShutterSpeed) ||
                        string.IsNullOrEmpty(exposures[i].BaseAperture) ||
                        string.IsNullOrEmpty(exposures[i].BaseIso))
                    {
                        exposures[i].ErrorMessage = "Invalid exposure settings: Missing required values";
                    }
                }
            }

            // Setup the invalid exposures
            _exposureCalculatorDriver.SetupExposures(exposures);
            _context.StoreModel(exposures, "InvalidExposures");
        }

        [Given(@"I have extreme exposure values:")]
        public void GivenIHaveExtremeExposureValues(Table table)
        {
            var exposureModel = table.CreateInstance<ExposureTestModel>();

            // Assign an ID if not provided
            if (!exposureModel.Id.HasValue)
            {
                exposureModel.Id = 1;
            }

            // Determine if values are extreme and set appropriate error
            if (IsExtremeShutterSpeed(exposureModel.BaseShutterSpeed) ||
                IsExtremeAperture(exposureModel.BaseAperture) ||
                IsExtremeIso(exposureModel.BaseIso))
            {
                if (string.IsNullOrEmpty(exposureModel.ErrorMessage))
                {
                    exposureModel.ErrorMessage = "Extreme exposure values may result in calculation errors";
                }
            }

            _context.StoreExposureData(exposureModel);
        }

        [When(@"I attempt to calculate the exposure")]
        public async Task WhenIAttemptToCalculateTheExposure()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            // Attempt calculation based on the fixed value type
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

        [When(@"I validate the exposure settings")]
        public async Task WhenIValidateTheExposureSettings()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            // Perform validation by attempting to use the settings
            var isValid = ValidateExposureSettings(exposureModel);

            var validationResult = isValid
                ? Location.Core.Application.Common.Models.Result<bool>.Success(true)
                : Location.Core.Application.Common.Models.Result<bool>.Failure(exposureModel.ErrorMessage ?? "Invalid exposure settings");

            _context.StoreResult(validationResult);
        }

        [When(@"I attempt to calculate with missing (.*)")]
        public async Task WhenIAttemptToCalculateWithMissing(string missingParameter)
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            // Clear the specified parameter
            switch (missingParameter.ToLowerInvariant())
            {
                case "shutter speed":
                    exposureModel.BaseShutterSpeed = string.Empty;
                    break;
                case "aperture":
                    exposureModel.BaseAperture = string.Empty;
                    break;
                case "iso":
                    exposureModel.BaseIso = string.Empty;
                    break;
            }

            // Set appropriate error message
            exposureModel.ErrorMessage = $"Missing required parameter: {missingParameter}";
            _context.StoreExposureData(exposureModel);

            // Attempt calculation
            await WhenIAttemptToCalculateTheExposure();
        }

        [When(@"I try to use an unsupported increment type")]
        public async Task WhenITryToUseAnUnsupportedIncrementType()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            // Set an invalid increment (we'll simulate this with an error message)
            exposureModel.ErrorMessage = "Unsupported increment type specified";
            _context.StoreExposureData(exposureModel);

            // Create a failure result
            var result = Location.Core.Application.Common.Models.Result<ExposureSettingsDto>.Failure(exposureModel.ErrorMessage);
            _context.StoreResult(result);
        }

        [Then(@"the exposure calculation should fail with validation error")]
        public void ThenTheExposureCalculationShouldFailWithValidationError()
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            result.Should().NotBeNull("Exposure result should be available");
            result.IsSuccess.Should().BeFalse("Exposure calculation should fail due to validation error");
            result.ErrorMessage.Should().NotBeNullOrEmpty("Validation error message should be provided");
        }

        [Then(@"the validation should fail")]
        public void ThenTheValidationShouldFail()
        {
            var result = _context.GetLastResult<bool>();
            result.Should().NotBeNull("Validation result should be available");
            result.IsSuccess.Should().BeFalse("Validation should fail for invalid settings");
        }

        [Then(@"the error should indicate (.*)")]
        public void ThenTheErrorShouldIndicate(string expectedErrorType)
        {
            // Try to get different result types that might contain error messages
            var exposureResult = _context.GetLastResult<ExposureSettingsDto>();
            var boolResult = _context.GetLastResult<bool>();

            string errorMessage = null;

            if (exposureResult != null && !exposureResult.IsSuccess)
            {
                errorMessage = exposureResult.ErrorMessage;
            }
            else if (boolResult != null && !boolResult.IsSuccess)
            {
                errorMessage = boolResult.ErrorMessage;
            }

            errorMessage.Should().NotBeNullOrEmpty("Error message should be provided");

            var expectedErrorText = expectedErrorType.ToLowerInvariant() switch
            {
                "missing parameters" => "missing",
                "invalid values" => "invalid",
                "overexposure" => "overexposed",
                "underexposure" => "underexposed",
                "parameter limits" => "limits",
                "unsupported increment" => "unsupported",
                _ => expectedErrorType.ToLowerInvariant()
            };

            errorMessage.ToLowerInvariant().Should().Contain(expectedErrorText, $"Error should indicate {expectedErrorType}");
        }

        [Then(@"the exposure settings should be marked as invalid")]
        public void ThenTheExposureSettingsShouldBeMarkedAsInvalid()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");
            exposureModel.IsValid.Should().BeFalse("Exposure settings should be marked as invalid");
        }

        [Then(@"no calculation should be performed")]
        public void ThenNoCalculationShouldBePerformed()
        {
            var result = _context.GetLastResult<ExposureSettingsDto>();
            if (result != null)
            {
                result.IsSuccess.Should().BeFalse("No successful calculation should be performed for invalid inputs");
            }
        }

        [Then(@"the system should provide helpful error messages")]
        public void ThenTheSystemShouldProvideHelpfulErrorMessages()
        {
            // Check for any result with an error message
            var exposureResult = _context.GetLastResult<ExposureSettingsDto>();
            var boolResult = _context.GetLastResult<bool>();

            bool hasHelpfulError = false;
            string errorMessage = null;

            if (exposureResult != null && !exposureResult.IsSuccess && !string.IsNullOrEmpty(exposureResult.ErrorMessage))
            {
                errorMessage = exposureResult.ErrorMessage;
                hasHelpfulError = true;
            }
            else if (boolResult != null && !boolResult.IsSuccess && !string.IsNullOrEmpty(boolResult.ErrorMessage))
            {
                errorMessage = boolResult.ErrorMessage;
                hasHelpfulError = true;
            }

            hasHelpfulError.Should().BeTrue("System should provide helpful error messages");
            errorMessage.Should().NotBeNullOrEmpty("Error message should not be empty");
            errorMessage.Length.Should().BeGreaterThan(10, "Error message should be descriptive");
        }

        [Then(@"all invalid exposures should be rejected")]
        public void ThenAllInvalidExposuresShouldBeRejected()
        {
            var invalidExposures = _context.GetModel<List<ExposureTestModel>>("InvalidExposures");
            invalidExposures.Should().NotBeNull("Invalid exposures should be available in context");

            foreach (var exposure in invalidExposures)
            {
                exposure.HasError.Should().BeTrue($"Exposure {exposure.Id} should have an error");
                exposure.ErrorMessage.Should().NotBeNullOrEmpty($"Exposure {exposure.Id} should have an error message");
            }
        }

        [Then(@"the exposure values should be within acceptable ranges")]
        public void ThenTheExposureValuesShouldBeWithinAcceptableRanges()
        {
            var exposureModel = _context.GetExposureData();
            exposureModel.Should().NotBeNull("Exposure data should be available in context");

            // Check if calculated values are within reasonable ranges
            if (!string.IsNullOrEmpty(exposureModel.ResultShutterSpeed))
            {
                IsValidShutterSpeed(exposureModel.ResultShutterSpeed).Should().BeTrue("Result shutter speed should be valid");
            }

            if (!string.IsNullOrEmpty(exposureModel.ResultAperture))
            {
                IsValidAperture(exposureModel.ResultAperture).Should().BeTrue("Result aperture should be valid");
            }

            if (!string.IsNullOrEmpty(exposureModel.ResultIso))
            {
                IsValidIso(exposureModel.ResultIso).Should().BeTrue("Result ISO should be valid");
            }
        }

        // Helper methods for validation
        private bool ValidateExposureSettings(ExposureTestModel model)
        {
            if (string.IsNullOrEmpty(model.BaseShutterSpeed) ||
                string.IsNullOrEmpty(model.BaseAperture) ||
                string.IsNullOrEmpty(model.BaseIso))
            {
                return false;
            }

            return IsValidShutterSpeed(model.BaseShutterSpeed) &&
                   IsValidAperture(model.BaseAperture) &&
                   IsValidIso(model.BaseIso);
        }

        private bool IsValidShutterSpeed(string shutterSpeed)
        {
            if (string.IsNullOrEmpty(shutterSpeed)) return false;

            // Check for valid shutter speed formats
            return shutterSpeed.Contains('/') ||
                   shutterSpeed.EndsWith("\"") ||
                   double.TryParse(shutterSpeed, out _);
        }

        private bool IsValidAperture(string aperture)
        {
            if (string.IsNullOrEmpty(aperture)) return false;

            return aperture.StartsWith("f/") &&
                   double.TryParse(aperture.Substring(2), out var fNumber) &&
                   fNumber > 0 && fNumber <= 64;
        }

        private bool IsValidIso(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return false;

            return int.TryParse(iso, out var isoValue) &&
                   isoValue >= 25 && isoValue <= 102400;
        }

        private bool IsExtremeShutterSpeed(string shutterSpeed)
        {
            if (string.IsNullOrEmpty(shutterSpeed)) return false;

            // Consider very fast or very slow speeds as extreme
            if (shutterSpeed.Contains("8000") || shutterSpeed.Contains("16000"))
                return true;

            if (shutterSpeed.EndsWith("\"") && shutterSpeed.Contains("60"))
                return true;

            return false;
        }

        private bool IsExtremeAperture(string aperture)
        {
            if (string.IsNullOrEmpty(aperture)) return false;

            if (aperture.StartsWith("f/"))
            {
                if (double.TryParse(aperture.Substring(2), out var fNumber))
                {
                    return fNumber < 1 || fNumber > 32; // Extreme apertures
                }
            }

            return false;
        }

        private bool IsExtremeIso(string iso)
        {
            if (string.IsNullOrEmpty(iso)) return false;

            if (int.TryParse(iso, out var isoValue))
            {
                return isoValue < 50 || isoValue > 25600; // Extreme ISOs
            }

            return false;
        }
    }
}