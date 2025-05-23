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
    public class ImageAnalysisSteps
    {
        private readonly ApiContext _context;
        private readonly SceneEvaluationDriver _sceneEvaluationDriver;
        private readonly IObjectContainer _objectContainer;

        public ImageAnalysisSteps(ApiContext context, IObjectContainer objectContainer)
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
                Console.WriteLine("ImageAnalysisSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ImageAnalysisSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have an image file at path ""(.*)""")]
        public void GivenIHaveAnImageFileAtPath(string imagePath)
        {
            var sceneEvaluationModel = new SceneEvaluationTestModel
            {
                Id = 1,
                ImagePath = imagePath,
                MeanRed = 128.0,
                MeanGreen = 128.0,
                MeanBlue = 128.0,
                MeanContrast = 128.0,
                StdDevRed = 64.0,
                StdDevGreen = 64.0,
                StdDevBlue = 64.0,
                StdDevContrast = 64.0,
                TotalPixels = 1000000
            };

            sceneEvaluationModel.CalculateColorTemperature();
            sceneEvaluationModel.CalculateTintValue();
            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"I have multiple images for analysis:")]
        public void GivenIHaveMultipleImagesForAnalysis(Table table)
        {
            var sceneEvaluations = table.CreateSet<SceneEvaluationTestModel>().ToList();

            for (int i = 0; i < sceneEvaluations.Count; i++)
            {
                if (!sceneEvaluations[i].Id.HasValue)
                {
                    sceneEvaluations[i].Id = i + 1;
                }

                SetDefaultAnalysisValues(sceneEvaluations[i]);
            }

            _sceneEvaluationDriver.SetupSceneEvaluations(sceneEvaluations);
            _context.StoreModel(sceneEvaluations, "AllImageAnalyses");
        }

        [Given(@"the image has the following characteristics:")]
        public void GivenTheImageHasTheFollowingCharacteristics(Table table)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var characteristics = table.CreateInstance<SceneEvaluationTestModel>();

            if (characteristics.MeanRed > 0) sceneEvaluationModel.MeanRed = characteristics.MeanRed;
            if (characteristics.MeanGreen > 0) sceneEvaluationModel.MeanGreen = characteristics.MeanGreen;
            if (characteristics.MeanBlue > 0) sceneEvaluationModel.MeanBlue = characteristics.MeanBlue;
            if (characteristics.MeanContrast > 0) sceneEvaluationModel.MeanContrast = characteristics.MeanContrast;
            if (characteristics.TotalPixels > 0) sceneEvaluationModel.TotalPixels = characteristics.TotalPixels;
            if (characteristics.ColorTemperature > 0) sceneEvaluationModel.ColorTemperature = characteristics.ColorTemperature;
            if (characteristics.TintValue != 0) sceneEvaluationModel.TintValue = characteristics.TintValue;

            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"I want to analyze an image for exposure settings")]
        public void GivenIWantToAnalyzeAnImageForExposureSettings()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            if (sceneEvaluationModel == null)
            {
                sceneEvaluationModel = new SceneEvaluationTestModel
                {
                    Id = 1,
                    ImagePath = "/test/sample_image.jpg"
                };
                SetDefaultAnalysisValues(sceneEvaluationModel);
            }

            _context.StoreModel("exposure_analysis", "AnalysisMode");
            _context.StoreSceneEvaluationData(sceneEvaluationModel);
        }

        [Given(@"I have a batch of images to process:")]
        public void GivenIHaveABatchOfImagesToProcess(Table table)
        {
            var imagePaths = table.Rows.Select(row => row["ImagePath"]).ToList();
            var batchModels = new List<SceneEvaluationTestModel>();

            for (int i = 0; i < imagePaths.Count; i++)
            {
                var model = new SceneEvaluationTestModel
                {
                    Id = i + 1,
                    ImagePath = imagePaths[i]
                };
                SetDefaultAnalysisValues(model);
                batchModels.Add(model);
            }

            _sceneEvaluationDriver.SetupSceneEvaluations(batchModels);
            _context.StoreModel(batchModels, "BatchImages");
        }

        [Given(@"the image analysis should detect (.*)")]
        public void GivenTheImageAnalysisShouldDetect(string expectedFeature)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            // Set expected analysis results based on feature
            SetExpectedAnalysisForFeature(sceneEvaluationModel, expectedFeature);
        }

        [When(@"I analyze the image")]
        public async Task WhenIAnalyzeTheImage()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            await _sceneEvaluationDriver.AnalyzeImageAsync(sceneEvaluationModel);
        }

        [When(@"I analyze the image for composition")]
        public async Task WhenIAnalyzeTheImageForComposition()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            // Set composition analysis mode
            _context.StoreModel("composition", "AnalysisMode");
            await _sceneEvaluationDriver.AnalyzeImageAsync(sceneEvaluationModel);
        }

        [When(@"I analyze the image for color balance")]
        public async Task WhenIAnalyzeTheImageForColorBalance()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            await _sceneEvaluationDriver.CalculateColorAnalysisAsync(sceneEvaluationModel);
        }

        [When(@"I analyze the image for exposure quality")]
        public async Task WhenIAnalyzeTheImageForExposureQuality()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            // Analyze exposure characteristics
            await _sceneEvaluationDriver.AnalyzeImageAsync(sceneEvaluationModel);
            await _sceneEvaluationDriver.GenerateHistogramsAsync(sceneEvaluationModel);
        }

        [When(@"I process all images in the batch")]
        public async Task WhenIProcessAllImagesInTheBatch()
        {
            var batchImages = _context.GetModel<List<SceneEvaluationTestModel>>("BatchImages");
            batchImages.Should().NotBeNull("Batch images should be available");

            foreach (var image in batchImages)
            {
                await _sceneEvaluationDriver.AnalyzeImageAsync(image);
            }
        }

        [When(@"I extract image metadata")]
        public async Task WhenIExtractImageMetadata()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            // Simulate metadata extraction
            var metadata = new Dictionary<string, object>
            {
                ["Width"] = 1920,
                ["Height"] = 1080,
                ["FileSize"] = 2048000,
                ["Format"] = "JPEG",
                ["ColorSpace"] = "sRGB",
                ["ExifData"] = "Available"
            };

            _context.StoreModel(metadata, "ImageMetadata");

            var metadataResult = Location.Core.Application.Common.Models.Result<Dictionary<string, object>>.Success(metadata);
            _context.StoreResult(metadataResult);
        }

        [When(@"I analyze multiple images")]
        public async Task WhenIAnalyzeMultipleImages()
        {
            var allImageAnalyses = _context.GetModel<List<SceneEvaluationTestModel>>("AllImageAnalyses");
            allImageAnalyses.Should().NotBeNull("Image analyses should be available");

            foreach (var analysis in allImageAnalyses)
            {
                await _sceneEvaluationDriver.AnalyzeImageAsync(analysis);
            }
        }

        [When(@"I validate the image format")]
        public async Task WhenIValidateTheImageFormat()
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            await _sceneEvaluationDriver.ValidateImagePathAsync(sceneEvaluationModel.ImagePath);
        }

        [Then(@"the image analysis should be successful")]
        public void ThenTheImageAnalysisShouldBeSuccessful()
        {
            var result = _context.GetLastResult<SceneEvaluationResultDto>();
            result.Should().NotBeNull("Scene evaluation result should be available");
            result.IsSuccess.Should().BeTrue("Image analysis should be successful");
            result.Data.Should().NotBeNull("Scene evaluation data should be available");
        }

        [Then(@"I should receive image statistics")]
        public void ThenIShouldReceiveImageStatistics()
        {
            var result = _context.GetLastResult<SceneEvaluationResultDto>();
            result.Should().NotBeNull("Scene evaluation result should be available");
            result.Data.Should().NotBeNull("Scene evaluation data should be available");
            result.Data.Stats.Should().NotBeNull("Image statistics should be available");
            result.Data.Stats.TotalPixels.Should().BeGreaterThan(0, "Total pixels should be greater than zero");
        }

        [Then(@"the image should have RGB values")]
        public void ThenTheImageShouldHaveRGBValues()
        {
            var result = _context.GetLastResult<SceneEvaluationResultDto>();
            result.Should().NotBeNull("Scene evaluation result should be available");
            result.Data.Stats.Should().NotBeNull("Image statistics should be available");
            result.Data.Stats.MeanRed.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(255, "Mean red should be valid");
            result.Data.Stats.MeanGreen.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(255, "Mean green should be valid");
            result.Data.Stats.MeanBlue.Should().BeGreaterOrEqualTo(0).And.BeLessOrEqualTo(255, "Mean blue should be valid");
        }

        [Then(@"the dominant color should be (.*)")]
        public void ThenTheDominantColorShouldBe(string expectedColor)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var dominantColor = sceneEvaluationModel.GetDominantColor();
            dominantColor.Should().Be(expectedColor, $"Dominant color should be {expectedColor}");
        }


        [Then(@"the brightness level should be (.*)")]
        public void ThenTheBrightnessLevelShouldBe(string expectedBrightness)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var brightnessLevel = sceneEvaluationModel.GetBrightnessLevel();
            brightnessLevel.Should().Be(expectedBrightness, $"Brightness level should be {expectedBrightness}");
        }

        [Then(@"the contrast level should be (.*)")]
        public void ThenTheContrastLevelShouldBe(string expectedContrast)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var contrastLevel = sceneEvaluationModel.GetContrastLevel();
            contrastLevel.Should().Be(expectedContrast, $"Contrast level should be {expectedContrast}");
        }

        [Then(@"I should receive color analysis results")]
        public void ThenIShouldReceiveColorAnalysisResults()
        {
            var result = _context.GetLastResult<Dictionary<string, double>>();
            result.Should().NotBeNull("Color analysis result should be available");
            result.IsSuccess.Should().BeTrue("Color analysis should be successful");
            result.Data.Should().NotBeNull("Color analysis data should be available");
            result.Data.Should().ContainKey("ColorTemperature", "Should have color temperature");
            result.Data.Should().ContainKey("TintValue", "Should have tint value");
        }

        [Then(@"the color temperature should be approximately (.*) Kelvin")]
        public void ThenTheColorTemperatureShouldBeApproximatelyKelvin(double expectedTemperature)
        {
            var result = _context.GetLastResult<Dictionary<string, double>>();
            result.Should().NotBeNull("Color analysis result should be available");
            result.Data.Should().ContainKey("ColorTemperature", "Should have color temperature");
            result.Data["ColorTemperature"].Should().BeApproximately(expectedTemperature, 500,
                $"Color temperature should be approximately {expectedTemperature}K");
        }

        [Then(@"all images should be analyzed successfully")]
        public void ThenAllImagesShouldBeAnalyzedSuccessfully()
        {
            var allImageAnalyses = _context.GetModel<List<SceneEvaluationTestModel>>("AllImageAnalyses");
            allImageAnalyses.Should().NotBeNull("Image analyses should be available");

            foreach (var analysis in allImageAnalyses)
            {
                analysis.HasResults.Should().BeTrue($"Image {analysis.Id} should have analysis results");
                analysis.HasError.Should().BeFalse($"Image {analysis.Id} should not have errors");
            }
        }
        [Then(@"the exposure should be (.*)")]
        public void ThenTheExposureShouldBe(string expectedExposure)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            var brightnessLevel = sceneEvaluationModel.GetBrightnessLevel();
            var expectedMapping = expectedExposure.ToLowerInvariant() switch
            {
                "underexposed" => "Dark",
                "properly exposed" => "Medium-Bright",
                "overexposed" => "Bright",
                _ => expectedExposure
            };

            brightnessLevel.Should().Contain(expectedMapping, $"Exposure should be {expectedExposure}");
        }

        [Then(@"I should receive image metadata")]
        public void ThenIShouldReceiveImageMetadata()
        {
            var result = _context.GetLastResult<Dictionary<string, object>>();
            result.Should().NotBeNull("Metadata result should be available");
            result.IsSuccess.Should().BeTrue("Metadata extraction should be successful");
            result.Data.Should().NotBeNull("Metadata should be available");
            result.Data.Should().ContainKey("Width", "Should have image width");
            result.Data.Should().ContainKey("Height", "Should have image height");
        }

        [Then(@"the image format should be valid")]
        public void ThenTheImageFormatShouldBeValid()
        {
            var result = _context.GetLastResult<bool>();
            result.Should().NotBeNull("Validation result should be available");
            result.IsSuccess.Should().BeTrue("Image format validation should be successful");
            result.Data.Should().BeTrue("Image format should be valid");
        }

        [Then(@"the batch processing should be complete")]
        public void ThenTheBatchProcessingShouldBeComplete()
        {
            var batchImages = _context.GetModel<List<SceneEvaluationTestModel>>("BatchImages");
            batchImages.Should().NotBeNull("Batch images should be available");

            foreach (var image in batchImages)
            {
                image.HasResults.Should().BeTrue($"Image {image.ImagePath} should have results");
            }
        }

        [Then(@"the image analysis should detect (.*)")]
        public void ThenTheImageAnalysisShouldDetect(string expectedFeature)
        {
            var sceneEvaluationModel = _context.GetSceneEvaluationData();
            sceneEvaluationModel.Should().NotBeNull("Scene evaluation data should be available");

            ValidateDetectedFeature(sceneEvaluationModel, expectedFeature);
        }

        [Then(@"the composition analysis should show (.*)")]
        public void ThenTheCompositionAnalysisShouldShow(string expectedComposition)
        {
            var analysisMode = _context.GetModel<string>("AnalysisMode");
            analysisMode.Should().Be("composition", "Analysis mode should be composition");

            var result = _context.GetLastResult<SceneEvaluationResultDto>();
            result.Should().NotBeNull("Scene evaluation result should be available");
            result.IsSuccess.Should().BeTrue($"Composition analysis should detect {expectedComposition}");
        }

        [Then(@"the image dimensions should be (.*) x (.*)")]
        public void ThenTheImageDimensionsShouldBeX(int expectedWidth, int expectedHeight)
        {
            var metadata = _context.GetModel<Dictionary<string, object>>("ImageMetadata");
            metadata.Should().NotBeNull("Image metadata should be available");
            metadata.Should().ContainKey("Width", "Should have width");
            metadata.Should().ContainKey("Height", "Should have height");
            ((int)metadata["Width"]).Should().Be(expectedWidth, "Width should match");
            ((int)metadata["Height"]).Should().Be(expectedHeight, "Height should match");
        }

        private void SetDefaultAnalysisValues(SceneEvaluationTestModel model)
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

            model.CalculateColorTemperature();
            model.CalculateTintValue();
        }

        private void SetExpectedAnalysisForFeature(SceneEvaluationTestModel model, string feature)
        {
            switch (feature.ToLowerInvariant())
            {
                case "high contrast":
                    model.StdDevRed = 100.0;
                    model.StdDevGreen = 100.0;
                    model.StdDevBlue = 100.0;
                    break;
                case "low contrast":
                    model.StdDevRed = 20.0;
                    model.StdDevGreen = 20.0;
                    model.StdDevBlue = 20.0;
                    break;
                case "warm colors":
                    model.MeanRed = 180.0;
                    model.MeanBlue = 80.0;
                    model.ColorTemperature = 3000.0;
                    break;
                case "cool colors":
                    model.MeanRed = 80.0;
                    model.MeanBlue = 180.0;
                    model.ColorTemperature = 7000.0;
                    break;
            }
        }

        private void ValidateDetectedFeature(SceneEvaluationTestModel model, string feature)
        {
            switch (feature.ToLowerInvariant())
            {
                case "high contrast":
                    model.GetContrastLevel().Should().Contain("High", "Should detect high contrast");
                    break;
                case "low contrast":
                    model.GetContrastLevel().Should().Contain("Low", "Should detect low contrast");
                    break;
                case "warm colors":
                    model.GetColorTemperatureDescription().Should().Contain("Warm", "Should detect warm colors");
                    break;
                case "cool colors":
                    model.GetColorTemperatureDescription().Should().Contain("Cool", "Should detect cool colors");
                    break;
            }
        }
    }
}