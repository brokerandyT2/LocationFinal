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
    public class HistogramSteps
    {
        private readonly ApiContext _context;
        private readonly SceneEvaluationDriver _sceneEvaluationDriver;
        private readonly IObjectContainer _objectContainer;

        public HistogramSteps(ApiContext context, IObjectContainer objectContainer)
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
                Console.WriteLine("HistogramSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HistogramSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I want to generate histograms for an image")]
        public void GivenIWantToGenerateHistogramsForAnImage()
        {
            var sceneEvaluationModel = new SceneEvaluationTestModel
            {
                Id = 1,
                ImagePath = "/test/images/histogram_test.jpg",
                MeanRed = 128.0,
                MeanGreen = 128.0,
                MeanBlue = 128.0,
                MeanContrast = 128.0,
                StdDevRed = 64.0,
                StdDevGreen = 64.0,
                StdDevBlue = 64.0,
                StdDevContrast = 64.0,
                TotalPixels = 1920000
            };

            _context.StoreSceneEvaluationData(sceneEvaluationModel);
            _context.StoreModel("histogram_generation", "AnalysisMode");
        }

        [Given(@"I have captured a scene for histogram analysis")]
        public void GivenIHaveCapturedASceneForHistogramAnalysis()
        {
            var sceneEvaluationModel = new SceneEvaluationTestModel
            {
                Id = 1,
                ImagePath = "/temp/captured_scene.jpg",
                MeanRed = 135.0,
                MeanGreen = 125.0,
                MeanBlue = 115.0,
                MeanContrast = 125.0,
                StdDevRed = 70.0,
                StdDevGreen = 65.0,
                StdDevBlue = 60.0,
                StdDevContrast = 65.0,
                TotalPixels = 2073600
            };

            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"I have multiple images for histogram generation:")]
        public void GivenIHaveMultipleImagesForHistogramGeneration(Table table)
        {
            var sceneEvaluations = table.CreateSet<SceneEvaluationTestModel>().ToList();

            for (int i = 0; i < sceneEvaluations.Count; i++)
            {
                if (!sceneEvaluations[i].Id.HasValue)
                {
                    sceneEvaluations[i].Id = i + 1;
                }

                SetDefaultHistogramValues(sceneEvaluations[i]);
            }

            _sceneEvaluationDriver.SetupSceneEvaluations(sceneEvaluations);
            _context.StoreModel(sceneEvaluations, "HistogramImages");
        }

        [Given(@"the image has histogram data:")]
        public void GivenTheImageHasHistogramData(Table table)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var histogramData = table.CreateInstance<SceneEvaluationTestModel>();

            if (histogramData.MeanRed > 0) sceneEvaluationModel.MeanRed = histogramData.MeanRed;
            if (histogramData.MeanGreen > 0) sceneEvaluationModel.MeanGreen = histogramData.MeanGreen;
            if (histogramData.MeanBlue > 0) sceneEvaluationModel.MeanBlue = histogramData.MeanBlue;
            if (histogramData.MeanContrast > 0) sceneEvaluationModel.MeanContrast = histogramData.MeanContrast;
            if (histogramData.StdDevRed > 0) sceneEvaluationModel.StdDevRed = histogramData.StdDevRed;
            if (histogramData.StdDevGreen > 0) sceneEvaluationModel.StdDevGreen = histogramData.StdDevGreen;
            if (histogramData.StdDevBlue > 0) sceneEvaluationModel.StdDevBlue = histogramData.StdDevBlue;
            if (histogramData.StdDevContrast > 0) sceneEvaluationModel.StdDevContrast = histogramData.StdDevContrast;

            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"I want to analyze the (.*)")]
        public void GivenIWantToAnalyzeThe(string histogramType)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            if (sceneEvaluationModel == null)
            {
                sceneEvaluationModel = new SceneEvaluationTestModel
                {
                    Id = 1,
                    ImagePath = "/test/images/analysis_test.jpg"
                };
                SetDefaultHistogramValues(sceneEvaluationModel);
            }

            _context.StoreModel(histogramType.ToLowerInvariant(), "HistogramType");
            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"the scene has (.*)")]
        public void GivenTheSceneHas(string sceneCharacteristic)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            SetSceneCharacteristics(sceneEvaluationModel, sceneCharacteristic);
            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [When(@"I generate the histograms")]
        public async Task WhenIGenerateTheHistograms()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            await _sceneEvaluationDriver.GenerateHistogramsAsync(sceneEvaluationModel);
        }

        [When(@"I evaluate the scene")]
        public async Task WhenIEvaluateTheScene()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            await _sceneEvaluationDriver.EvaluateSceneAsync(sceneEvaluationModel);
        }

        [When(@"I generate RGB histograms")]
        public async Task WhenIGenerateRGBHistograms()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            _context.StoreModel("rgb", "HistogramType");
            await _sceneEvaluationDriver.GenerateHistogramsAsync(sceneEvaluationModel);
        }

        [When(@"I generate the contrast histogram")]
        public async Task WhenIGenerateTheContrastHistogram()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            _context.StoreModel("contrast", "HistogramType");
            await _sceneEvaluationDriver.GenerateHistogramsAsync(sceneEvaluationModel);
        }

        [When(@"I analyze the (.*) histogram")]
        public async Task WhenIAnalyzeTheHistogram(string histogramType)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            _context.StoreModel(histogramType.ToLowerInvariant(), "HistogramType");
            await _sceneEvaluationDriver.GenerateHistogramsAsync(sceneEvaluationModel);
        }

        [When(@"I generate histograms for all images")]
        public async Task WhenIGenerateHistogramsForAllImages()
        {
            var histogramImages = _context.GetModel<List<SceneEvaluationTestModel>>("HistogramImages");
            histogramImages.Should().NotBeNull("Histogram images should be available");

            foreach (var image in histogramImages)
            {
                await _sceneEvaluationDriver.GenerateHistogramsAsync(image);
            }
        }

        [When(@"I compare histograms between images")]
        public async Task WhenICompareHistogramsBetweenImages()
        {
            var histogramImages = _context.GetModel<List<SceneEvaluationTestModel>>("HistogramImages");
            histogramImages.Should().NotBeNull("Histogram images should be available");
            histogramImages.Should().HaveCountGreaterThan(1, "Need at least 2 images for comparison");

            // Generate histograms for comparison
            foreach (var image in histogramImages)
            {
                await _sceneEvaluationDriver.GenerateHistogramsAsync(image);
            }

            // Create comparison result
            var comparison = new Dictionary<string, object>
            {
                ["Image1"] = histogramImages[0].GetAnalysisSummary(),
                ["Image2"] = histogramImages[1].GetAnalysisSummary(),
                ["Similarity"] = CalculateHistogramSimilarity(histogramImages[0], histogramImages[1])
            };

            _context.StoreModel(comparison, "HistogramComparison");

            var comparisonResult = Location.Core.Application.Common.Models.Result<Dictionary<string, object>>.Success(comparison);
            _context.StoreResult(comparisonResult);
        }

        [Then(@"I should receive histogram images")]
        public void ThenIShouldReceiveHistogramImages()
        {
            var result = _context.GetLastResult<Dictionary<string, string>>();
            result.Should().NotBeNull("Histogram result should be available");
            result.IsSuccess.Should().BeTrue("Histogram generation should be successful");
            result.Data.Should().NotBeNull("Histogram data should be available");
            result.Data.Should().ContainKey("Red", "Should have red histogram");
            result.Data.Should().ContainKey("Green", "Should have green histogram");
            result.Data.Should().ContainKey("Blue", "Should have blue histogram");
            result.Data.Should().ContainKey("Contrast", "Should have contrast histogram");
        }

        [Then(@"the (.*) histogram should be generated")]
        public void ThenTheHistogramShouldBeGenerated(string histogramType)
        {
            var result = _context.GetLastResult<Dictionary<string, string>>();
            result.Should().NotBeNull("Histogram result should be available");
            result.Data.Should().NotBeNull("Histogram data should be available");

            var expectedKey = histogramType.Substring(0, 1).ToUpper() + histogramType.Substring(1).ToLower();
            result.Data.Should().ContainKey(expectedKey, $"Should have {histogramType} histogram");
            result.Data[expectedKey].Should().NotBeNullOrEmpty($"{histogramType} histogram path should not be empty");
        }

        [Then(@"the histogram should show (.*)")]
        public void ThenTheHistogramShouldShow(string expectedCharacteristic)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            ValidateHistogramCharacteristic(sceneEvaluationModel, expectedCharacteristic);
        }

        [Then(@"the RGB histograms should be balanced")]
        public void ThenTheRGBHistogramsShouldBeBalanced()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var redGreenDiff = Math.Abs(sceneEvaluationModel.MeanRed - sceneEvaluationModel.MeanGreen);
            var redBlueDiff = Math.Abs(sceneEvaluationModel.MeanRed - sceneEvaluationModel.MeanBlue);
            var greenBlueDiff = Math.Abs(sceneEvaluationModel.MeanGreen - sceneEvaluationModel.MeanBlue);

            redGreenDiff.Should().BeLessThan(30, "Red and Green channels should be balanced");
            redBlueDiff.Should().BeLessThan(30, "Red and Blue channels should be balanced");
            greenBlueDiff.Should().BeLessThan(30, "Green and Blue channels should be balanced");
        }

        [Then(@"the contrast histogram should show (.*)")]
        public void ThenTheContrastHistogramShouldShow(string contrastLevel)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var actualContrastLevel = sceneEvaluationModel.GetContrastLevel();
            actualContrastLevel.Should().Contain(contrastLevel,
                $"Contrast histogram should show {contrastLevel}", StringComparison.OrdinalIgnoreCase);
        }

        [Then(@"all histogram images should be generated")]
        public void ThenAllHistogramImagesShouldBeGenerated()
        {
            var histogramImages = _context.GetModel<List<SceneEvaluationTestModel>>("HistogramImages");
            histogramImages.Should().NotBeNull("Histogram images should be available");

            foreach (var image in histogramImages)
            {
                image.HasResults.Should().BeTrue($"Image {image.Id} should have histogram results");
                image.RedHistogramPath.Should().NotBeNullOrEmpty($"Image {image.Id} should have red histogram");
                image.GreenHistogramPath.Should().NotBeNullOrEmpty($"Image {image.Id} should have green histogram");
                image.BlueHistogramPath.Should().NotBeNullOrEmpty($"Image {image.Id} should have blue histogram");
                image.ContrastHistogramPath.Should().NotBeNullOrEmpty($"Image {image.Id} should have contrast histogram");
            }
        }

        [Then(@"the histogram data should be accurate")]
        public void ThenTheHistogramDataShouldBeAccurate()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            sceneEvaluationModel.MeanRed.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(255, "Mean red should be valid");
            sceneEvaluationModel.MeanGreen.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(255, "Mean green should be valid");
            sceneEvaluationModel.MeanBlue.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(255, "Mean blue should be valid");
            sceneEvaluationModel.MeanContrast.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(255, "Mean contrast should be valid");
            sceneEvaluationModel.TotalPixels.Should().BeGreaterThan(0, "Total pixels should be greater than zero");
        }

        [Then(@"the histogram comparison should show (.*)")]
        public void ThenTheHistogramComparisonShouldShow(string expectedResult)
        {
            var comparison = _context.GetModel<Dictionary<string, object>>("HistogramComparison");
            comparison.Should().NotBeNull("Histogram comparison should be available");
            comparison.Should().ContainKey("Similarity", "Should have similarity measure");

            var similarity = (double)comparison["Similarity"];

            switch (expectedResult.ToLowerInvariant())
            {
                case "similar images":
                    similarity.Should().BeGreaterThan(0.7, "Images should be similar");
                    break;
                case "different images":
                    similarity.Should().BeLessThan(0.5, "Images should be different");
                    break;
                case "very similar images":
                    similarity.Should().BeGreaterThan(0.9, "Images should be very similar");
                    break;
            }
        }

        [Then(@"the red channel should be dominant")]
        public void ThenTheRedChannelShouldBeDominant()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            sceneEvaluationModel.MeanRed.Should().BeGreaterThan(sceneEvaluationModel.MeanGreen, "Red should be greater than green");
            sceneEvaluationModel.MeanRed.Should().BeGreaterThan(sceneEvaluationModel.MeanBlue, "Red should be greater than blue");
        }

        [Then(@"the green channel should be dominant")]
        public void ThenTheGreenChannelShouldBeDominant()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            sceneEvaluationModel.MeanGreen.Should().BeGreaterThan(sceneEvaluationModel.MeanRed, "Green should be greater than red");
            sceneEvaluationModel.MeanGreen.Should().BeGreaterThan(sceneEvaluationModel.MeanBlue, "Green should be greater than blue");
        }

        [Then(@"the blue channel should be dominant")]
        public void ThenTheBlueChannelShouldBeDominant()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            sceneEvaluationModel.MeanBlue.Should().BeGreaterThan(sceneEvaluationModel.MeanRed, "Blue should be greater than red");
            sceneEvaluationModel.MeanBlue.Should().BeGreaterThan(sceneEvaluationModel.MeanGreen, "Blue should be greater than green");
        }

        [Then(@"the histogram should indicate (.*) exposure")]
        public void ThenTheHistogramShouldIndicateExposure(string exposureType)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var brightnessLevel = sceneEvaluationModel.GetBrightnessLevel();

            switch (exposureType.ToLowerInvariant())
            {
                case "under":
                    brightnessLevel.Should().Contain("Dark", "Should indicate underexposure");
                    break;
                case "over":
                    brightnessLevel.Should().Contain("Bright", "Should indicate overexposure");
                    break;
                case "proper":
                    brightnessLevel.Should().Contain("Medium", "Should indicate proper exposure");
                    break;
            }
        }

        [Then(@"the scene evaluation should be complete")]
        public void ThenTheSceneEvaluationShouldBeComplete()
        {
            var result = _context.GetLastResult<SceneEvaluationResultDto>();
            result.Should().NotBeNull("Scene evaluation result should be available");
            result.IsSuccess.Should().BeTrue("Scene evaluation should be successful");
            result.Data.Should().NotBeNull("Scene evaluation data should be available");
            result.Data.Stats.Should().NotBeNull("Scene evaluation statistics should be available");
            result.Data.RedHistogramPath.Should().NotBeNullOrEmpty("Red histogram should be generated");
            result.Data.GreenHistogramPath.Should().NotBeNullOrEmpty("Green histogram should be generated");
            result.Data.BlueHistogramPath.Should().NotBeNullOrEmpty("Blue histogram should be generated");
            result.Data.ContrastHistogramPath.Should().NotBeNullOrEmpty("Contrast histogram should be generated");
        }

        private void SetDefaultHistogramValues(SceneEvaluationTestModel model)
        {
            if (model.MeanRed == 0) model.MeanRed = 128.0;
            if (model.MeanGreen == 0) model.MeanGreen = 128.0;
            if (model.MeanBlue == 0) model.MeanBlue = 128.0;
            if (model.MeanContrast == 0) model.MeanContrast = 128.0;
            if (model.StdDevRed == 0) model.StdDevRed = 64.0;
            if (model.StdDevGreen == 0) model.StdDevGreen = 64.0;
            if (model.StdDevBlue == 0) model.StdDevBlue = 64.0;
            if (model.StdDevContrast == 0) model.StdDevContrast = 64.0;
            if (model.TotalPixels == 0) model.TotalPixels = 1000000;

            // Generate histogram paths
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            if (string.IsNullOrEmpty(model.RedHistogramPath))
                model.RedHistogramPath = $"/temp/red_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.GreenHistogramPath))
                model.GreenHistogramPath = $"/temp/green_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.BlueHistogramPath))
                model.BlueHistogramPath = $"/temp/blue_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.ContrastHistogramPath))
                model.ContrastHistogramPath = $"/temp/contrast_histogram_{timestamp}.png";
        }

        private void SetSceneCharacteristics(SceneEvaluationTestModel model, string characteristic)
        {
            switch (characteristic.ToLowerInvariant())
            {
                case "high contrast":
                    model.StdDevRed = 95.0;
                    model.StdDevGreen = 100.0;
                    model.StdDevBlue = 90.0;
                    model.StdDevContrast = 95.0;
                    break;
                case "low contrast":
                    model.StdDevRed = 20.0;
                    model.StdDevGreen = 18.0;
                    model.StdDevBlue = 22.0;
                    model.StdDevContrast = 20.0;
                    break;
                case "bright lighting":
                    model.MeanRed = 200.0;
                    model.MeanGreen = 210.0;
                    model.MeanBlue = 190.0;
                    model.MeanContrast = 200.0;
                    break;
                case "dark lighting":
                    model.MeanRed = 50.0;
                    model.MeanGreen = 45.0;
                    model.MeanBlue = 55.0;
                    model.MeanContrast = 50.0;
                    break;
                case "red dominant colors":
                    model.MeanRed = 180.0;
                    model.MeanGreen = 100.0;
                    model.MeanBlue = 90.0;
                    break;
                case "green dominant colors":
                    model.MeanRed = 90.0;
                    model.MeanGreen = 180.0;
                    model.MeanBlue = 100.0;
                    break;
                case "blue dominant colors":
                    model.MeanRed = 90.0;
                    model.MeanGreen = 100.0;
                    model.MeanBlue = 180.0;
                    break;
            }
        }

        private void ValidateHistogramCharacteristic(SceneEvaluationTestModel model, string characteristic)
        {
            switch (characteristic.ToLowerInvariant())
            {
                case "high contrast":
                case "good contrast":
                    model.GetContrastLevel().Should().Contain("High", "Should show high contrast");
                    break;
                case "low contrast":
                case "poor contrast":
                    model.GetContrastLevel().Should().Contain("Low", "Should show low contrast");
                    break;
                case "bright tones":
                    model.GetBrightnessLevel().Should().Contain("Bright", "Should show bright tones");
                    break;
                case "dark tones":
                    model.GetBrightnessLevel().Should().Contain("Dark", "Should show dark tones");
                    break;
                case "balanced exposure":
                    model.GetBrightnessLevel().Should().Contain("Medium", "Should show balanced exposure");
                    break;
            }
        }

        private double CalculateHistogramSimilarity(SceneEvaluationTestModel image1, SceneEvaluationTestModel image2)
        {
            // Simplified similarity calculation based on mean values
            var redDiff = Math.Abs(image1.MeanRed - image2.MeanRed) / 255.0;
            var greenDiff = Math.Abs(image1.MeanGreen - image2.MeanGreen) / 255.0;
            var blueDiff = Math.Abs(image1.MeanBlue - image2.MeanBlue) / 255.0;
            var contrastDiff = Math.Abs(image1.MeanContrast - image2.MeanContrast) / 255.0;

            var avgDiff = (redDiff + greenDiff + blueDiff + contrastDiff) / 4.0;
            return 1.0 - avgDiff; // Higher value = more similar
        }
    }
}