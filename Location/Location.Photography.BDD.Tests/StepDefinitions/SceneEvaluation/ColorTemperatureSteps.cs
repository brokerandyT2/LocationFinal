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

namespace Location.Photography.BDD.Tests.StepDefinitions.SceneEvaluation
{
    [Binding]
    public class ColorTemperatureSteps
    {
        private readonly ApiContext _context;
        private readonly SceneEvaluationDriver _sceneEvaluationDriver;
        private readonly IObjectContainer _objectContainer;

        public ColorTemperatureSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _sceneEvaluationDriver = new SceneEvaluationDriver(context);
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                Console.WriteLine("ColorTemperatureSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ColorTemperatureSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I want to analyze color temperature in an image")]
        public void GivenIWantToAnalyzeColorTemperatureInAnImage()
        {
            var sceneEvaluationModel = new SceneEvaluationTestModel
            {
                Id = 1,
                ImagePath = "/test/images/color_temp_analysis.jpg",
                MeanRed = 128.0,
                MeanGreen = 128.0,
                MeanBlue = 128.0,
                ColorTemperature = 5500.0,
                TintValue = 0.0,
                TotalPixels = 1920000
            };

            _context.StoreSceneEvaluationData(sceneEvaluationModel);
            _context.StoreModel("color_temperature", "AnalysisMode");
        }

        [Given(@"the image has color characteristics:")]
        public void GivenTheImageHasColorCharacteristics(Table table)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var colorData = table.CreateInstance<SceneEvaluationTestModel>();

            if (colorData.MeanRed > 0) sceneEvaluationModel.MeanRed = colorData.MeanRed;
            if (colorData.MeanGreen > 0) sceneEvaluationModel.MeanGreen = colorData.MeanGreen;
            if (colorData.MeanBlue > 0) sceneEvaluationModel.MeanBlue = colorData.MeanBlue;
            if (colorData.ColorTemperature > 0) sceneEvaluationModel.ColorTemperature = colorData.ColorTemperature;
            if (colorData.TintValue != 0) sceneEvaluationModel.TintValue = colorData.TintValue;

            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"I have multiple images with different color temperatures:")]
        public void GivenIHaveMultipleImagesWithDifferentColorTemperatures(Table table)
        {
            var sceneEvaluations = table.CreateSet<SceneEvaluationTestModel>().ToList();

            for (int i = 0; i < sceneEvaluations.Count; i++)
            {
                if (!sceneEvaluations[i].Id.HasValue)
                {
                    sceneEvaluations[i].Id = i + 1;
                }

                SetDefaultColorValues(sceneEvaluations[i]);
                sceneEvaluations[i].CalculateColorTemperature();
                sceneEvaluations[i].CalculateTintValue();
            }

            _sceneEvaluationDriver.SetupSceneEvaluations(sceneEvaluations);
            _context.StoreModel(sceneEvaluations, "ColorTemperatureImages");
        }

        [Given(@"the lighting condition is (.*)")]
        public void GivenTheLightingConditionIs(string lightingCondition)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            if (sceneEvaluationModel == null)
            {
                sceneEvaluationModel = new SceneEvaluationTestModel
                {
                    Id = 1,
                    ImagePath = "/test/images/lighting_test.jpg"
                };
            }

            SetLightingCondition(sceneEvaluationModel, lightingCondition);
            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"I want to correct white balance")]
        public void GivenIWantToCorrectWhiteBalance()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            if (sceneEvaluationModel == null)
            {
                sceneEvaluationModel = new SceneEvaluationTestModel
                {
                    Id = 1,
                    ImagePath = "/test/images/white_balance_test.jpg",
                    MeanRed = 140.0,
                    MeanGreen = 120.0,
                    MeanBlue = 110.0
                };
                sceneEvaluationModel.CalculateColorTemperature();
                sceneEvaluationModel.CalculateTintValue();
            }

            _context.StoreModel("white_balance_correction", "AnalysisMode");
            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"the image was taken under (.*) lighting")]
        public void GivenTheImageWasTakenUnderLighting(string lightingType)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            SetLightingType(sceneEvaluationModel, lightingType);
            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"I have a reference white point")]
        public void GivenIHaveAReferenceWhitePoint()
        {
            var whitePoint = new Dictionary<string, double>
            {
                ["Red"] = 255.0,
                ["Green"] = 255.0,
                ["Blue"] = 255.0,
                ["ColorTemperature"] = 6500.0
            };

            _context.StoreModel(whitePoint, "ReferenceWhitePoint");
        }

        [When(@"I analyze the color temperature")]
        public async Task WhenIAnalyzeTheColorTemperature()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            // Don't recalculate if already set by lighting condition
            if (sceneEvaluationModel.ColorTemperature == 0 || sceneEvaluationModel.ColorTemperature == 5500.0)
            {
                sceneEvaluationModel.CalculateColorTemperature();
            }

            if (sceneEvaluationModel.TintValue == 0)
            {
                sceneEvaluationModel.CalculateTintValue();
            }

            await _sceneEvaluationDriver.CalculateColorAnalysisAsync(sceneEvaluationModel);
        }

        [When(@"I measure the white balance")]
        public async Task WhenIMeasureTheWhiteBalance()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            await _sceneEvaluationDriver.CalculateColorAnalysisAsync(sceneEvaluationModel);
        }

        [When(@"I calculate color correction values")]
        public async Task WhenICalculateColorCorrectionValues()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var whitePoint = _context.GetModel<Dictionary<string, double>>("ReferenceWhitePoint");
            if (whitePoint == null)
            {
                whitePoint = new Dictionary<string, double>
                {
                    ["ColorTemperature"] = 6500.0,
                    ["Tint"] = 0.0
                };
            }

            // Calculate correction needed
            var tempCorrection = whitePoint["ColorTemperature"] - sceneEvaluationModel.ColorTemperature;
            var tintCorrection = (whitePoint.ContainsKey("Tint") ? whitePoint["Tint"] : 0.0) - sceneEvaluationModel.TintValue;

            var corrections = new Dictionary<string, double>
            {
                ["TemperatureCorrection"] = tempCorrection,
                ["TintCorrection"] = tintCorrection,
                ["CorrectedTemperature"] = sceneEvaluationModel.ColorTemperature + tempCorrection,
                ["CorrectedTint"] = sceneEvaluationModel.TintValue + tintCorrection
            };

            _context.StoreModel(corrections, "ColorCorrections");

            var correctionResult = Location.Core.Application.Common.Models.Result<Dictionary<string, double>>.Success(corrections);
            _context.StoreResult(correctionResult);
        }

        [When(@"I analyze color temperature for all images")]
        public async Task WhenIAnalyzeColorTemperatureForAllImages()
        {
            var colorTempImages = _context.GetModel<List<SceneEvaluationTestModel>>("ColorTemperatureImages");
            colorTempImages.Should().NotBeNull("Color temperature images should be available");

            foreach (var image in colorTempImages)
            {
                image.CalculateColorTemperature();
                image.CalculateTintValue();
                await _sceneEvaluationDriver.CalculateColorAnalysisAsync(image);
            }
        }

        [When(@"I compare color temperatures between images")]
        public async Task WhenICompareColorTemperaturesBetweenImages()
        {
            var colorTempImages = _context.GetModel<List<SceneEvaluationTestModel>>("ColorTemperatureImages");
            colorTempImages.Should().NotBeNull("Color temperature images should be available");
            colorTempImages.Should().HaveCountGreaterThan(1, "Need at least 2 images for comparison");

            // Analyze all images first
            foreach (var image in colorTempImages)
            {
                await _sceneEvaluationDriver.CalculateColorAnalysisAsync(image);
            }

            // Create comparison
            var comparison = new Dictionary<string, object>();
            for (int i = 0; i < colorTempImages.Count; i++)
            {
                comparison[$"Image{i + 1}ColorTemp"] = colorTempImages[i].ColorTemperature;
                comparison[$"Image{i + 1}Tint"] = colorTempImages[i].TintValue;
                comparison[$"Image{i + 1}Description"] = colorTempImages[i].GetColorTemperatureDescription();
            }

            // Calculate temperature differences
            if (colorTempImages.Count >= 2)
            {
                var tempDiff = Math.Abs(colorTempImages[0].ColorTemperature - colorTempImages[1].ColorTemperature);
                comparison["TemperatureDifference"] = tempDiff;
                comparison["SimilarTemperature"] = tempDiff < 500; // Within 500K considered similar
            }

            _context.StoreModel(comparison, "ColorTemperatureComparison");

            var comparisonResult = Location.Core.Application.Common.Models.Result<Dictionary<string, object>>.Success(comparison);
            _context.StoreResult(comparisonResult);
        }

        [When(@"I detect the dominant color cast")]
        public async Task WhenIDetectTheDominantColorCast()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var colorCast = DetermineColorCast(sceneEvaluationModel);

            var castResult = new Dictionary<string, object>
            {
                ["DominantCast"] = colorCast.CastType,
                ["Intensity"] = colorCast.Intensity,
                ["Recommendation"] = colorCast.Recommendation
            };

            _context.StoreModel(castResult, "ColorCast");

            var result = Location.Core.Application.Common.Models.Result<Dictionary<string, object>>.Success(castResult);
            _context.StoreResult(result);
        }

        [Then(@"I should receive color temperature analysis")]
        public void ThenIShouldReceiveColorTemperatureAnalysis()
        {
            var result = _context.GetLastResult<Dictionary<string, double>>();
            result.Should().NotBeNull("Color analysis result should be available");
            result.IsSuccess.Should().BeTrue("Color temperature analysis should be successful");
            result.Data.Should().NotBeNull("Color analysis data should be available");
            result.Data.Should().ContainKey("ColorTemperature", "Should have color temperature");
            result.Data.Should().ContainKey("TintValue", "Should have tint value");
        }

        [Then(@"the color temperature should be approximately (.*) Kelvin")]
        public void ThenTheColorTemperatureShouldBeApproximatelyKelvin(double expectedTemperature)
        {
            var result = _context.GetLastResult<Dictionary<string, double>>();
            if (result != null && result.Data != null && result.Data.ContainsKey("ColorTemperature"))
            {
                result.Data["ColorTemperature"].Should().BeApproximately(expectedTemperature, 300,
                    $"Color temperature should be approximately {expectedTemperature}K");
            }
            else
            {
                var sceneEvaluationModel = _context.GetSceneEvaluationData();
                sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");
                sceneEvaluationModel.ColorTemperature.Should().BeApproximately(expectedTemperature, 300,
                    $"Color temperature should be approximately {expectedTemperature}K");
            }
        }

        [Then(@"the tint should be approximately (.*)")]
        public void ThenTheTintShouldBeApproximately(double expectedTint)
        {
            var result = _context.GetLastResult<Dictionary<string, double>>();
            if (result != null && result.Data != null && result.Data.ContainsKey("TintValue"))
            {
                result.Data["TintValue"].Should().BeApproximately(expectedTint, 0.2,
                    $"Tint should be approximately {expectedTint}");
            }
            else
            {
                var sceneEvaluationModel = _context.GetSceneEvaluationData();
                sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");
                sceneEvaluationModel.TintValue.Should().BeApproximately(expectedTint, 0.2,
                    $"Tint should be approximately {expectedTint}");
            }
        }

        [Then(@"the color temperature should indicate (.*) lighting")]
        public void ThenTheColorTemperatureShouldIndicateLighting(string expectedLighting)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var temperatureDescription = sceneEvaluationModel.GetColorTemperatureDescription();

            var expectedDescription = expectedLighting.ToLowerInvariant() switch
            {
                "warm" or "tungsten" or "incandescent" => "Warm",
                "cool" or "daylight" or "overcast" => "Cool",
                "neutral" or "flash" => "Neutral",
                "very warm" or "candlelight" => "Very Warm",
                "very cool" or "shade" => "Very Cool",
                _ => expectedLighting
            };

            temperatureDescription.Should().Contain(expectedDescription,
                $"Color temperature should indicate {expectedLighting} lighting");
        }

        [Then(@"I should receive white balance measurements")]
        public void ThenIShouldReceiveWhiteBalanceMeasurements()
        {
            var result = _context.GetLastResult<Dictionary<string, double>>();
            result.Should().NotBeNull("White balance result should be available");
            result.IsSuccess.Should().BeTrue("White balance measurement should be successful");
            result.Data.Should().NotBeNull("White balance data should be available");
            result.Data.Should().ContainKey("ColorTemperature", "Should have color temperature measurement");
        }

        [Then(@"I should receive color correction values")]
        public void ThenIShouldReceiveColorCorrectionValues()
        {
            var result = _context.GetLastResult<Dictionary<string, double>>();
            result.Should().NotBeNull("Color correction result should be available");
            result.IsSuccess.Should().BeTrue("Color correction calculation should be successful");
            result.Data.Should().NotBeNull("Color correction data should be available");
            result.Data.Should().ContainKey("TemperatureCorrection", "Should have temperature correction");
            result.Data.Should().ContainKey("TintCorrection", "Should have tint correction");
        }

        [Then(@"all images should have color temperature analysis")]
        public void ThenAllImagesShouldHaveColorTemperatureAnalysis()
        {
            var colorTempImages = _context.GetModel<List<SceneEvaluationTestModel>>("ColorTemperatureImages");
            colorTempImages.Should().NotBeNull("Color temperature images should be available");

            foreach (var image in colorTempImages)
            {
                image.ColorTemperature.Should().BeGreaterThan(1000, $"Image {image.Id} should have valid color temperature");
                image.ColorTemperature.Should().BeLessThan(12000, $"Image {image.Id} color temperature should be within realistic range");
            }
        }

        [Then(@"the color temperature comparison should show (.*)")]
        public void ThenTheColorTemperatureComparisonShouldShow(string expectedResult)
        {
            // Get the comparison directly instead of relying on stored results
            var colorTempImages = _context.GetModel<List<SceneEvaluationTestModel>>("ColorTemperatureImages");
            colorTempImages.Should().NotBeNull("Color temperature images should be available");

            // Create the comparison on-demand
            var comparison = new Dictionary<string, object>();
            for (int i = 0; i < colorTempImages.Count; i++)
            {
                comparison[$"Image{i + 1}ColorTemp"] = colorTempImages[i].ColorTemperature;
                comparison[$"Image{i + 1}Tint"] = colorTempImages[i].TintValue;
                comparison[$"Image{i + 1}Description"] = colorTempImages[i].GetColorTemperatureDescription();
            }

            if (colorTempImages.Count >= 2)
            {
                var tempDiff = Math.Abs(colorTempImages[0].ColorTemperature - colorTempImages[1].ColorTemperature);
                comparison["TemperatureDifference"] = tempDiff;
                comparison["SimilarTemperature"] = tempDiff < 500;
            }

            // Store as a proper Result for CommonSteps
            var comparisonResult = Location.Core.Application.Common.Models.Result<Dictionary<string, object>>.Success(comparison);
            _context.StoreResult(comparisonResult);

            // Now do the actual assertion
            switch (expectedResult.ToLowerInvariant())
            {
                case "similar temperatures":
                    comparison.Should().ContainKey("SimilarTemperature", "Should have similarity indicator");
                    ((bool)comparison["SimilarTemperature"]).Should().BeTrue("Images should have similar temperatures");
                    break;
                case "different temperatures":
                    comparison.Should().ContainKey("SimilarTemperature", "Should have similarity indicator");
                    ((bool)comparison["SimilarTemperature"]).Should().BeFalse("Images should have different temperatures");
                    break;
                case "significant difference":
                    comparison.Should().ContainKey("TemperatureDifference", "Should have temperature difference");
                    ((double)comparison["TemperatureDifference"]).Should().BeGreaterThan(1000, "Should show significant difference");
                    break;
            }
        }

        [Then(@"the dominant color cast should be (.*)")]
        public void ThenTheDominantColorCastShouldBe(string expectedCast)
        {
            // Get the model and determine color cast on-demand
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var colorCast = DetermineColorCast(sceneEvaluationModel);
            var castResult = new Dictionary<string, object>
            {
                ["DominantCast"] = colorCast.CastType,
                ["Intensity"] = colorCast.Intensity,
                ["Recommendation"] = colorCast.Recommendation
            };

            // Store as a proper Result for CommonSteps
            var result = Location.Core.Application.Common.Models.Result<Dictionary<string, object>>.Success(castResult);
            _context.StoreResult(result);

            // Do the assertion
            castResult["DominantCast"].ToString().Should().Be(expectedCast, $"Dominant color cast should be {expectedCast}");
        }

        [Then(@"the white balance should be (.*)")]
        public void ThenTheWhiteBalanceShouldBe(string balanceState)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            switch (balanceState.ToLowerInvariant())
            {
                case "accurate" or "correct":
                    sceneEvaluationModel.ColorTemperature.Should().BeInRange(5000, 7000, "Should be in neutral range for accurate white balance");
                    Math.Abs(sceneEvaluationModel.TintValue).Should().BeLessThan(0.3, "Tint should be near neutral for accurate white balance");
                    break;
                case "too warm":
                    sceneEvaluationModel.ColorTemperature.Should().BeLessThan(4501, "Should be warm for 'too warm' white balance");
                    break;
                case "too cool":
                    sceneEvaluationModel.ColorTemperature.Should().BeGreaterThan(7500, "Should be cool for 'too cool' white balance");
                    break;
            }
        }

        [Then(@"the temperature correction should be approximately (.*) Kelvin")]
        public void ThenTheTemperatureCorrectionShouldBeApproximatelyKelvin(double expectedCorrection)
        {
            var corrections = _context.GetModel<Dictionary<string, double>>("ColorCorrections");
            corrections.Should().NotBeNull("Color corrections should be available");
            corrections.Should().ContainKey("TemperatureCorrection", "Should have temperature correction");
            corrections["TemperatureCorrection"].Should().BeApproximately(expectedCorrection, 100,
                $"Temperature correction should be approximately {expectedCorrection}K");
        }

        [Then(@"the tint correction should be approximately (.*)")]
        public void ThenTheTintCorrectionShouldBeApproximately(double expectedCorrection)
        {
            var corrections = _context.GetModel<Dictionary<string, double>>("ColorCorrections");
            corrections.Should().NotBeNull("Color corrections should be available");
            corrections.Should().ContainKey("TintCorrection", "Should have tint correction");
            corrections["TintCorrection"].Should().BeApproximately(expectedCorrection, 0.1,
                $"Tint correction should be approximately {expectedCorrection}");
        }

        [Then(@"I should receive color cast detection")]
        public void ThenIShouldReceiveColorCastDetection()
        {
            var result = _context.GetLastResult<Dictionary<string, object>>();
            result.Should().NotBeNull("Color cast result should be available");
            result.IsSuccess.Should().BeTrue("Color cast detection should be successful");
            result.Data.Should().NotBeNull("Color cast data should be available");
            result.Data.Should().ContainKey("DominantCast", "Should have dominant cast");
            result.Data.Should().ContainKey("Intensity", "Should have cast intensity");
        }

        private void SetDefaultColorValues(SceneEvaluationTestModel model)
        {
            if (model.MeanRed == 0) model.MeanRed = 128.0;
            if (model.MeanGreen == 0) model.MeanGreen = 128.0;
            if (model.MeanBlue == 0) model.MeanBlue = 128.0;
            if (model.TotalPixels == 0) model.TotalPixels = 1000000;
        }

        private void SetLightingCondition(SceneEvaluationTestModel model, string condition)
        {
            switch (condition.ToLowerInvariant())
            {
                case "daylight":
                    model.MeanRed = 128.0;
                    model.MeanGreen = 128.0;
                    model.MeanBlue = 128.0;
                    model.ColorTemperature = 5500.0;
                    break;
                case "tungsten":
                case "incandescent":
                    model.MeanRed = 180.0;
                    model.MeanGreen = 140.0;
                    model.MeanBlue = 90.0;
                    model.ColorTemperature = 3200.0;
                    break;
                case "fluorescent":
                    model.MeanRed = 120.0;
                    model.MeanGreen = 140.0;
                    model.MeanBlue = 130.0;
                    model.ColorTemperature = 4000.0;
                    break;
                case "overcast":
                    model.MeanRed = 110.0;
                    model.MeanGreen = 120.0;
                    model.MeanBlue = 140.0;
                    model.ColorTemperature = 7000.0;
                    break;
                case "shade":
                    model.MeanRed = 100.0;
                    model.MeanGreen = 115.0;
                    model.MeanBlue = 150.0;
                    model.ColorTemperature = 8000.0;
                    break;
            }

            // Don't call CalculateColorTemperature() - use the preset value
            model.CalculateTintValue();
        }

        private void SetLightingType(SceneEvaluationTestModel model, string lightingType)
        {
            SetLightingCondition(model, lightingType);
        }

        private (string CastType, double Intensity, string Recommendation) DetermineColorCast(SceneEvaluationTestModel model)
        {
            var redDominance = model.MeanRed - (model.MeanGreen + model.MeanBlue) / 2.0;
            var greenDominance = model.MeanGreen - (model.MeanRed + model.MeanBlue) / 2.0;
            var blueDominance = model.MeanBlue - (model.MeanRed + model.MeanGreen) / 2.0;

            var maxDominance = Math.Max(Math.Max(Math.Abs(redDominance), Math.Abs(greenDominance)), Math.Abs(blueDominance));

            if (maxDominance < 10)
            {
                return ("Neutral", 0.1, "No correction needed");
            }

            string castType;
            string recommendation;

            if (Math.Abs(redDominance) == maxDominance)
            {
                castType = redDominance > 0 ? "Red" : "Cyan";
                recommendation = redDominance > 0 ? "Reduce red or add cyan" : "Reduce cyan or add red";
            }
            else if (Math.Abs(greenDominance) == maxDominance)
            {
                castType = greenDominance > 0 ? "Green" : "Magenta";
                recommendation = greenDominance > 0 ? "Reduce green or add magenta" : "Reduce magenta or add green";
            }
            else
            {
                castType = blueDominance > 0 ? "Blue" : "Yellow";
                recommendation = blueDominance > 0 ? "Reduce blue or add yellow" : "Reduce yellow or add blue";
            }

            var intensity = maxDominance / 255.0;
            return (castType, intensity, recommendation);
        }
    }
}