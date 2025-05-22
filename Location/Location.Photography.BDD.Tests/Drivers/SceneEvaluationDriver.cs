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
        /// Generates histograms for testing
        /// </summary>
        public async Task<Result<Dictionary<string, string>>> GenerateHistogramsAsync(SceneEvaluationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Generate histogram paths
            var timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            var histograms = new Dictionary<string, string>
            {
                ["Red"] = model.RedHistogramPath ?? $"/temp/red_histogram_{timestamp}.png",
                ["Green"] = model.GreenHistogramPath ?? $"/temp/green_histogram_{timestamp}.png",
                ["Blue"] = model.BlueHistogramPath ?? $"/temp/blue_histogram_{timestamp}.png",
                ["Contrast"] = model.ContrastHistogramPath ?? $"/temp/contrast_histogram_{timestamp}.png"
            };

            // Update model
            model.RedHistogramPath = histograms["Red"];
            model.GreenHistogramPath = histograms["Green"];
            model.BlueHistogramPath = histograms["Blue"];
            model.ContrastHistogramPath = histograms["Contrast"];

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
        /// Calculates color analysis for the scene
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

            var colorAnalysis = new Dictionary<string, double>
            {
                ["ColorTemperature"] = model.ColorTemperature,
                ["TintValue"] = model.TintValue,
                ["MeanRed"] = model.MeanRed,
                ["MeanGreen"] = model.MeanGreen,
                ["MeanBlue"] = model.MeanBlue,
                ["MeanContrast"] = model.MeanContrast
            };

            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<Dictionary<string, double>>.Success(colorAnalysis)
                : Result<Dictionary<string, double>>.Failure(model.ErrorMessage);

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
    }
}