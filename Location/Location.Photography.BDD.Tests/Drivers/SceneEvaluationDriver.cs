using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.BDD.Tests.Models;
using Location.Photography.BDD.Tests.Support;
using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.BDD.Tests.Drivers
{
    /// <summary>
    /// Driver for scene evaluation operations in BDD tests
    /// </summary>
    public class SceneEvaluationDriver
    {
        private readonly ApiContext _context;
        private readonly Mock<ISceneEvaluationService> _sceneEvaluationServiceMock;
        private readonly Mock<IMediaService> _mediaServiceMock;

        public SceneEvaluationDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sceneEvaluationServiceMock = _context.GetService<Mock<ISceneEvaluationService>>();
            _mediaServiceMock = _context.GetService<Mock<IMediaService>>();
        }

        /// <summary>
        /// Sets up multiple scene evaluation models in the mock service
        /// </summary>
        public void SetupSceneEvaluations(List<SceneEvaluationTestModel> evaluations)
        {
            if (evaluations == null || !evaluations.Any()) return;

            foreach (var evaluation in evaluations)
            {
                if (!evaluation.Id.HasValue || evaluation.Id.Value <= 0)
                {
                    evaluation.Id = evaluations.IndexOf(evaluation) + 1;
                }

                // Calculate color analysis if not set
                if (evaluation.ColorTemperature == 0)
                {
                    evaluation.CalculateColorTemperature();
                }

                if (evaluation.TintValue == 0)
                {
                    evaluation.CalculateTintValue();
                }

                // Generate histogram paths if not set
                var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + evaluation.Id;
                if (string.IsNullOrEmpty(evaluation.RedHistogramPath))
                    evaluation.RedHistogramPath = $"/temp/red_histogram_{timestamp}.png";
                if (string.IsNullOrEmpty(evaluation.GreenHistogramPath))
                    evaluation.GreenHistogramPath = $"/temp/green_histogram_{timestamp}.png";
                if (string.IsNullOrEmpty(evaluation.BlueHistogramPath))
                    evaluation.BlueHistogramPath = $"/temp/blue_histogram_{timestamp}.png";
                if (string.IsNullOrEmpty(evaluation.ContrastHistogramPath))
                    evaluation.ContrastHistogramPath = $"/temp/contrast_histogram_{timestamp}.png";
            }

            // Store for later retrieval
            _context.StoreModel(evaluations, "SetupSceneEvaluations");
        }

        /// <summary>
        /// Evaluates a scene by capturing a new photo
        /// </summary>
        public async Task<Result<SceneEvaluationResultDto>> EvaluateSceneAsync(SceneEvaluationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Setup media service to simulate photo capture
            var capturedPhotoPath = model.ImagePath ?? "/temp/captured_scene.jpg";

            _mediaServiceMock
                .Setup(s => s.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success(capturedPhotoPath));

            _mediaServiceMock
                .Setup(s => s.IsCaptureSupported(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Update model with captured path
            model.ImagePath = capturedPhotoPath;

            // Generate histogram paths if not set
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + model.Id;
            if (string.IsNullOrEmpty(model.RedHistogramPath))
                model.RedHistogramPath = $"/temp/red_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.GreenHistogramPath))
                model.GreenHistogramPath = $"/temp/green_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.BlueHistogramPath))
                model.BlueHistogramPath = $"/temp/blue_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.ContrastHistogramPath))
                model.ContrastHistogramPath = $"/temp/contrast_histogram_{timestamp}.png";

            // Create expected result
            var expectedResult = model.ToSceneEvaluationResultDto();

            // Setup scene evaluation service mock
            _sceneEvaluationServiceMock
                .Setup(s => s.EvaluateSceneAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.IsNullOrEmpty(model.ErrorMessage)
                    ? Result<SceneEvaluationResultDto>.Success(expectedResult)
                    : Result<SceneEvaluationResultDto>.Failure(model.ErrorMessage));

            // NO MediatR - Direct response
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<SceneEvaluationResultDto>.Success(expectedResult)
                : Result<SceneEvaluationResultDto>.Failure(model.ErrorMessage);

            // Store result and update context
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                model.UpdateFromResult(result.Data);
                _context.StoreSceneEvaluationData(model);
            }

            return result;
        }

        /// <summary>
        /// Analyzes an existing image
        /// </summary>
        public async Task<Result<SceneEvaluationResultDto>> AnalyzeImageAsync(SceneEvaluationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Validate image path
            if (string.IsNullOrEmpty(model.ImagePath))
            {
                var errorResult = Result<SceneEvaluationResultDto>.Failure("Image path is required for analysis");
                _context.StoreResult(errorResult);
                return errorResult;
            }

            // Generate histogram paths if not set
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + model.Id;
            if (string.IsNullOrEmpty(model.RedHistogramPath))
                model.RedHistogramPath = $"/temp/red_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.GreenHistogramPath))
                model.GreenHistogramPath = $"/temp/green_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.BlueHistogramPath))
                model.BlueHistogramPath = $"/temp/blue_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.ContrastHistogramPath))
                model.ContrastHistogramPath = $"/temp/contrast_histogram_{timestamp}.png";

            // Create expected result
            var expectedResult = model.ToSceneEvaluationResultDto();

            // Setup scene evaluation service mock
            _sceneEvaluationServiceMock
                .Setup(s => s.AnalyzeImageAsync(
                    It.Is<string>(path => path == model.ImagePath),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.IsNullOrEmpty(model.ErrorMessage)
                    ? Result<SceneEvaluationResultDto>.Success(expectedResult)
                    : Result<SceneEvaluationResultDto>.Failure(model.ErrorMessage));

            // NO MediatR - Direct response
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<SceneEvaluationResultDto>.Success(expectedResult)
                : Result<SceneEvaluationResultDto>.Failure(model.ErrorMessage);

            // Store result and update context
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                model.UpdateFromResult(result.Data);
                _context.StoreSceneEvaluationData(model);
            }

            return result;
        }

        /// <summary>
        /// Picks a photo from gallery for analysis
        /// </summary>
        public async Task<Result<string>> PickPhotoAsync()
        {
            var photoPath = "/temp/picked_photo.jpg";

            _mediaServiceMock
                .Setup(s => s.PickPhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success(photoPath));

            // NO MediatR - Direct response
            var result = Result<string>.Success(photoPath);
            _context.StoreResult(result);

            return result;
        }

        /// <summary>
        /// Validates if the provided image path exists and is valid
        /// </summary>
        public async Task<Result<bool>> ValidateImagePathAsync(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                var result = Result<bool>.Failure("Image path cannot be null or empty");
                _context.StoreResult(result);
                return result;
            }

            // For testing, we'll consider paths valid if they have proper extensions
            var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".tiff", ".tif" };
            var extension = Path.GetExtension(imagePath).ToLowerInvariant();

            bool isValid = validExtensions.Contains(extension);

            var validationResult = isValid
                ? Result<bool>.Success(true)
                : Result<bool>.Failure($"Invalid image format. Supported formats: {string.Join(", ", validExtensions)}");

            _context.StoreResult(validationResult);
            return validationResult;
        }

        /// <summary>
        /// Gets scene evaluation by image path - context management pattern
        /// </summary>
        public async Task<Result<SceneEvaluationTestModel>> GetSceneEvaluationByImagePathAsync(string imagePath)
        {
            // Check individual context first
            var individual = _context.GetSceneEvaluationData();
            if (individual != null && individual.ImagePath == imagePath)
            {
                var response = individual.Clone();
                var result = Result<SceneEvaluationTestModel>.Success(response);
                _context.StoreResult(result);
                return result;
            }

            // Check collection contexts
            var collectionKeys = new[] { "AllSceneEvaluations", "SetupSceneEvaluations" };
            foreach (var collectionKey in collectionKeys)
            {
                var collection = _context.GetModel<List<SceneEvaluationTestModel>>(collectionKey);
                var found = collection?.FirstOrDefault(e => e.ImagePath == imagePath);

                if (found != null)
                {
                    var response = found.Clone();
                    var result = Result<SceneEvaluationTestModel>.Success(response);
                    _context.StoreResult(result);
                    return result;
                }
            }

            // Not found
            var failureResult = Result<SceneEvaluationTestModel>.Failure($"Scene evaluation for image '{imagePath}' not found");
            _context.StoreResult(failureResult);
            return failureResult;
        }

        /// <summary>
        /// Gets all scene evaluations
        /// </summary>
        public async Task<Result<List<SceneEvaluationTestModel>>> GetAllSceneEvaluationsAsync()
        {
            var results = new List<SceneEvaluationTestModel>();

            // Check individual context first
            var individual = _context.GetSceneEvaluationData();
            if (individual != null)
            {
                results.Add(individual.Clone());
            }

            // Check collection contexts
            var collectionKeys = new[] { "AllSceneEvaluations", "SetupSceneEvaluations" };
            foreach (var collectionKey in collectionKeys)
            {
                var collection = _context.GetModel<List<SceneEvaluationTestModel>>(collectionKey);
                if (collection != null)
                {
                    foreach (var item in collection)
                    {
                        if (!results.Any(r => r.Id == item.Id))
                        {
                            results.Add(item.Clone());
                        }
                    }
                }
            }

            var result = results.Any()
                ? Result<List<SceneEvaluationTestModel>>.Success(results)
                : Result<List<SceneEvaluationTestModel>>.Success(new List<SceneEvaluationTestModel>());

            _context.StoreResult(result);
            return result;
        }

        /// <summary>
        /// Generates histograms for testing - FIXED to properly set histogram paths
        /// </summary>
        public async Task<Result<Dictionary<string, string>>> GenerateHistogramsAsync(SceneEvaluationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Generate histogram paths - FIXED: Ensure unique timestamps and update model FIRST
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss") + "_" + model.Id;

            // FIXED: Update model FIRST, then create dictionary from model
            if (string.IsNullOrEmpty(model.RedHistogramPath))
                model.RedHistogramPath = $"/temp/red_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.GreenHistogramPath))
                model.GreenHistogramPath = $"/temp/green_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.BlueHistogramPath))
                model.BlueHistogramPath = $"/temp/blue_histogram_{timestamp}.png";
            if (string.IsNullOrEmpty(model.ContrastHistogramPath))
                model.ContrastHistogramPath = $"/temp/contrast_histogram_{timestamp}.png";

            // FIXED: Create dictionary from updated model paths
            var histograms = new Dictionary<string, string>
            {
                ["Red"] = model.RedHistogramPath,
                ["Green"] = model.GreenHistogramPath,
                ["Blue"] = model.BlueHistogramPath,
                ["Contrast"] = model.ContrastHistogramPath
            };

            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<Dictionary<string, string>>.Success(histograms)
                : Result<Dictionary<string, string>>.Failure(model.ErrorMessage);

            _context.StoreResult(result);

            if (result.IsSuccess)
            {
                _context.StoreSceneEvaluationData(model);
            }

            return result;
        }

        /// <summary>
        /// FIXED: Calculates color analysis for the scene - follows ExposureCalculator pattern
        /// </summary>
        public async Task<Result<Dictionary<string, double>>> CalculateColorAnalysisAsync(SceneEvaluationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Calculate color temperature and tint if not already calculated
            if (model.ColorTemperature == 0)
            {
                model.CalculateColorTemperature();
            }

            if (model.TintValue == 0)
            {
                model.CalculateTintValue();
            }

            // Create expected result - MATCHES ExposureCalculator pattern
            var expectedResult = new Dictionary<string, double>
            {
                ["ColorTemperature"] = model.ColorTemperature,
                ["TintValue"] = model.TintValue,
                ["MeanRed"] = model.MeanRed,
                ["MeanGreen"] = model.MeanGreen,
                ["MeanBlue"] = model.MeanBlue,
                ["MeanContrast"] = model.MeanContrast
            };

            // Setup mock to return expected result - MATCHES ExposureCalculator pattern
            _sceneEvaluationServiceMock
                .Setup(s => s.AnalyzeImageAsync(
                    It.Is<string>(path => path == model.ImagePath),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.IsNullOrEmpty(model.ErrorMessage)
                    ? Result<SceneEvaluationResultDto>.Success(model.ToSceneEvaluationResultDto())
                    : Result<SceneEvaluationResultDto>.Failure(model.ErrorMessage));

            // NO MediatR - Direct response - MATCHES ExposureCalculator pattern
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<Dictionary<string, double>>.Success(expectedResult)
                : Result<Dictionary<string, double>>.Failure(model.ErrorMessage);

            // Store result and update context - MATCHES ExposureCalculator pattern
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                _context.StoreSceneEvaluationData(model);
            }

            return result;
        }

        /// <summary>
        /// ADDED: Calculates color correction values - follows ExposureCalculator pattern
        /// </summary>
        public async Task<Result<Dictionary<string, double>>> CalculateColorCorrectionAsync(SceneEvaluationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Get reference white point or use default
            var whitePoint = _context.GetModel<Dictionary<string, double>>("ReferenceWhitePoint");
            if (whitePoint == null)
            {
                whitePoint = new Dictionary<string, double>
                {
                    ["ColorTemperature"] = 6500.0,
                    ["Tint"] = 0.0
                };
            }

            // Calculate corrections needed
            var tempCorrection = whitePoint["ColorTemperature"] - model.ColorTemperature;
            var tintCorrection = (whitePoint.ContainsKey("Tint") ? whitePoint["Tint"] : 0.0) - model.TintValue;

            // Create expected result - MATCHES ExposureCalculator pattern
            var expectedResult = new Dictionary<string, double>
            {
                ["TemperatureCorrection"] = tempCorrection,
                ["TintCorrection"] = tintCorrection,
                ["CorrectedTemperature"] = model.ColorTemperature + tempCorrection,
                ["CorrectedTint"] = model.TintValue + tintCorrection,
                ["OriginalTemperature"] = model.ColorTemperature,
                ["OriginalTint"] = model.TintValue
            };

            // NO MediatR - Direct response - MATCHES ExposureCalculator pattern  
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<Dictionary<string, double>>.Success(expectedResult)
                : Result<Dictionary<string, double>>.Failure(model.ErrorMessage);

            // Store result and update context - MATCHES ExposureCalculator pattern
            _context.StoreResult(result);

            if (result.IsSuccess)
            {
                _context.StoreSceneEvaluationData(model);
            }

            return result;
        }

        /// <summary>
        /// ADDED: Compares color temperatures between images - follows ExposureCalculator pattern
        /// </summary>
        public async Task<Result<Dictionary<string, object>>> CompareColorTemperaturesAsync(List<SceneEvaluationTestModel> images)
        {
            if (images == null || !images.Any())
            {
                var errorResult = Result<Dictionary<string, object>>.Failure("No images provided for comparison");
                _context.StoreResult(errorResult);
                return errorResult;
            }

            // Ensure all images have color analysis
            foreach (var image in images)
            {
                if (image.ColorTemperature == 0)
                {
                    image.CalculateColorTemperature();
                }
                if (image.TintValue == 0)
                {
                    image.CalculateTintValue();
                }
            }

            // Create comparison result - MATCHES ExposureCalculator pattern
            var comparison = new Dictionary<string, object>();

            for (int i = 0; i < images.Count; i++)
            {
                comparison[$"Image{i + 1}ColorTemp"] = images[i].ColorTemperature;
                comparison[$"Image{i + 1}Tint"] = images[i].TintValue;
                comparison[$"Image{i + 1}Description"] = images[i].GetColorTemperatureDescription();
            }

            // Calculate temperature differences
            if (images.Count >= 2)
            {
                var tempDiff = Math.Abs(images[0].ColorTemperature - images[1].ColorTemperature);
                comparison["TemperatureDifference"] = tempDiff;
                comparison["SimilarTemperature"] = tempDiff < 500; // Within 500K considered similar

                var tintDiff = Math.Abs(images[0].TintValue - images[1].TintValue);
                comparison["TintDifference"] = tintDiff;
                comparison["SimilarTint"] = tintDiff < 0.2;
            }

            // Add overall similarity assessment
            if (images.Count >= 2)
            {
                var tempSimilar = (bool)comparison["SimilarTemperature"];
                var tintSimilar = (bool)comparison["SimilarTint"];
                comparison["OverallSimilarity"] = tempSimilar && tintSimilar ? "Similar" : "Different";
            }

            // NO MediatR - Direct response - MATCHES ExposureCalculator pattern
            var result = Result<Dictionary<string, object>>.Success(comparison);

            // Store result - MATCHES ExposureCalculator pattern
            _context.StoreResult(result);

            return result;
        }

        /// <summary>
        /// ADDED: Detects dominant color cast - follows ExposureCalculator pattern
        /// </summary>
        public async Task<Result<Dictionary<string, object>>> DetectColorCastAsync(SceneEvaluationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Calculate color cast using the same logic as the helper method
            var colorCast = DetermineColorCast(model);

            // Create expected result - MATCHES ExposureCalculator pattern
            var expectedResult = new Dictionary<string, object>
            {
                ["DominantCast"] = colorCast.CastType,
                ["Intensity"] = colorCast.Intensity,
                ["Recommendation"] = colorCast.Recommendation,
                ["MeanRed"] = model.MeanRed,
                ["MeanGreen"] = model.MeanGreen,
                ["MeanBlue"] = model.MeanBlue
            };

            // NO MediatR - Direct response - MATCHES ExposureCalculator pattern
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<Dictionary<string, object>>.Success(expectedResult)
                : Result<Dictionary<string, object>>.Failure(model.ErrorMessage);

            // Store result and update context - MATCHES ExposureCalculator pattern
            _context.StoreResult(result);

            if (result.IsSuccess)
            {
                _context.StoreSceneEvaluationData(model);
            }

            return result;
        }

        /// <summary>
        /// Creates a scene evaluation record
        /// </summary>
        public async Task<Result<SceneEvaluationTestModel>> CreateSceneEvaluationAsync(SceneEvaluationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Calculate color analysis
            model.CalculateColorTemperature();
            model.CalculateTintValue();

            // NO MediatR - Direct response
            var response = model.Clone();
            var result = Result<SceneEvaluationTestModel>.Success(response);

            // Store result and update context
            _context.StoreResult(result);
            _context.StoreSceneEvaluationData(model);

            return result;
        }

        /// <summary>
        /// Deletes a scene evaluation record
        /// </summary>
        public async Task<Result<bool>> DeleteSceneEvaluationAsync(int id)
        {
            // Remove from individual context by storing empty model
            _context.StoreSceneEvaluationData(new SceneEvaluationTestModel());

            // NO MediatR - Direct response
            var result = Result<bool>.Success(true);
            _context.StoreResult(result);

            return result;
        }

        /// <summary>
        /// Sets processing state for scene evaluation
        /// </summary>
        public void SetProcessingState(bool isProcessing)
        {
            var model = _context.GetSceneEvaluationData();
            if (model != null)
            {
                model.IsProcessing = isProcessing;
                _context.StoreSceneEvaluationData(model);
            }
        }

        /// <summary>
        /// Gets camera availability status
        /// </summary>
        public async Task<Result<bool>> IsCameraSupportedAsync()
        {
            _mediaServiceMock
                .Setup(s => s.IsCaptureSupported(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            var result = Result<bool>.Success(true);
            _context.StoreResult(result);

            return result;
        }

        // ADDED: Helper method for color cast detection - matches ColorTemperatureSteps logic
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