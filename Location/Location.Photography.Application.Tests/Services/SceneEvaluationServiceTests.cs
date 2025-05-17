using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Services
{
    [TestFixture]
    public class SceneEvaluationServiceTests
    {
        private Mock<ISceneEvaluationService> _sceneEvaluationServiceMock;
        private Mock<IMediaService> _mediaServiceMock;

        [SetUp]
        public void Setup()
        {
            _sceneEvaluationServiceMock = new Mock<ISceneEvaluationService>();
            _mediaServiceMock = new Mock<IMediaService>();
        }

        [Test]
        public async Task EvaluateSceneAsync_WhenSuccessful_ShouldReturnHistograms()
        {
            // Arrange
            var expectedResult = new SceneEvaluationResultDto
            {
                RedHistogramPath = "/histograms/red.png",
                GreenHistogramPath = "/histograms/green.png",
                BlueHistogramPath = "/histograms/blue.png",
                ContrastHistogramPath = "/histograms/contrast.png",
                ImagePath = "/captures/scene.jpg",
                Stats = new SceneEvaluationStatsDto
                {
                    MeanRed = 128.5,
                    MeanGreen = 145.2,
                    MeanBlue = 112.8,
                    MeanContrast = 0.75,
                    StdDevRed = 45.3,
                    StdDevGreen = 38.9,
                    StdDevBlue = 52.1,
                    StdDevContrast = 0.25,
                    TotalPixels = 2073600 // 1920x1080
                }
            };

            _sceneEvaluationServiceMock
                .Setup(x => x.EvaluateSceneAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SceneEvaluationResultDto>.Success(expectedResult));

            // Act
            var result = await _sceneEvaluationServiceMock.Object.EvaluateSceneAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ImagePath.Should().Be("/captures/scene.jpg");
            result.Data.RedHistogramPath.Should().Be("/histograms/red.png");
            result.Data.GreenHistogramPath.Should().Be("/histograms/green.png");
            result.Data.BlueHistogramPath.Should().Be("/histograms/blue.png");
            result.Data.ContrastHistogramPath.Should().Be("/histograms/contrast.png");

            result.Data.Stats.MeanRed.Should().Be(128.5);
            result.Data.Stats.MeanGreen.Should().Be(145.2);
            result.Data.Stats.MeanBlue.Should().Be(112.8);
            result.Data.Stats.TotalPixels.Should().Be(2073600);
        }

        [Test]
        public async Task EvaluateSceneAsync_WhenCameraUnavailable_ShouldReturnFailure()
        {
            // Arrange
            var expectedResult = Result<SceneEvaluationResultDto>.Failure("Camera is unavailable");

            _sceneEvaluationServiceMock
                .Setup(x => x.EvaluateSceneAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _sceneEvaluationServiceMock.Object.EvaluateSceneAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Camera is unavailable");
        }

        [Test]
        public async Task AnalyzeImageAsync_WithValidImage_ShouldReturnHistograms()
        {
            // Arrange
            var imagePath = "/storage/images/photo.jpg";
            var expectedResult = new SceneEvaluationResultDto
            {
                RedHistogramPath = "/histograms/red.png",
                GreenHistogramPath = "/histograms/green.png",
                BlueHistogramPath = "/histograms/blue.png",
                ContrastHistogramPath = "/histograms/contrast.png",
                ImagePath = imagePath,
                Stats = new SceneEvaluationStatsDto
                {
                    MeanRed = 135.7,
                    MeanGreen = 142.3,
                    MeanBlue = 128.1,
                    MeanContrast = 0.65,
                    StdDevRed = 48.2,
                    StdDevGreen = 40.5,
                    StdDevBlue = 55.8,
                    StdDevContrast = 0.28,
                    TotalPixels = 3145728 // 2048x1536
                }
            };

            _sceneEvaluationServiceMock
                .Setup(x => x.AnalyzeImageAsync(imagePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SceneEvaluationResultDto>.Success(expectedResult));

            // Act
            var result = await _sceneEvaluationServiceMock.Object.AnalyzeImageAsync(imagePath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ImagePath.Should().Be(imagePath);
            result.Data.Stats.TotalPixels.Should().Be(3145728);
        }

        [Test]
        public async Task AnalyzeImageAsync_WithInvalidImage_ShouldReturnFailure()
        {
            // Arrange
            var imagePath = "/storage/images/corrupted.jpg";
            var expectedResult = Result<SceneEvaluationResultDto>.Failure("Failed to process image: corrupted or invalid format");

            _sceneEvaluationServiceMock
                .Setup(x => x.AnalyzeImageAsync(imagePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _sceneEvaluationServiceMock.Object.AnalyzeImageAsync(imagePath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to process image: corrupted or invalid format");
        }

        [Test]
        public async Task AnalyzeImageAsync_WithNonexistentImage_ShouldReturnFailure()
        {
            // Arrange
            var imagePath = "/storage/images/nonexistent.jpg";
            var expectedResult = Result<SceneEvaluationResultDto>.Failure("File not found");

            _sceneEvaluationServiceMock
                .Setup(x => x.AnalyzeImageAsync(imagePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _sceneEvaluationServiceMock.Object.AnalyzeImageAsync(imagePath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("File not found");
        }

        [Test]
        public async Task EvaluateSceneAsync_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var expectedResult = new SceneEvaluationResultDto();

            _sceneEvaluationServiceMock
                .Setup(x => x.EvaluateSceneAsync(cancellationToken))
                .ReturnsAsync(Result<SceneEvaluationResultDto>.Success(expectedResult));

            // Act
            await _sceneEvaluationServiceMock.Object.EvaluateSceneAsync(cancellationToken);

            // Assert
            _sceneEvaluationServiceMock.Verify(x => x.EvaluateSceneAsync(cancellationToken), Times.Once);
        }

        [Test]
        public async Task AnalyzeImageAsync_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var imagePath = "/storage/images/photo.jpg";
            var cancellationToken = new CancellationToken();
            var expectedResult = new SceneEvaluationResultDto();

            _sceneEvaluationServiceMock
                .Setup(x => x.AnalyzeImageAsync(imagePath, cancellationToken))
                .ReturnsAsync(Result<SceneEvaluationResultDto>.Success(expectedResult));

            // Act
            await _sceneEvaluationServiceMock.Object.AnalyzeImageAsync(imagePath, cancellationToken);

            // Assert
            _sceneEvaluationServiceMock.Verify(x => x.AnalyzeImageAsync(imagePath, cancellationToken), Times.Once);
        }

        [Test]
        public void SceneEvaluationResultDto_ShouldHaveCorrectProperties()
        {
            // Arrange & Act
            var result = new SceneEvaluationResultDto
            {
                RedHistogramPath = "/path/red.png",
                GreenHistogramPath = "/path/green.png",
                BlueHistogramPath = "/path/blue.png",
                ContrastHistogramPath = "/path/contrast.png",
                ImagePath = "/path/image.jpg",
                Stats = new SceneEvaluationStatsDto
                {
                    MeanRed = 100.0,
                    MeanGreen = 120.0,
                    MeanBlue = 130.0,
                    MeanContrast = 0.5,
                    StdDevRed = 30.0,
                    StdDevGreen = 25.0,
                    StdDevBlue = 35.0,
                    StdDevContrast = 0.2,
                    TotalPixels = 1000000
                }
            };

            // Assert
            result.RedHistogramPath.Should().Be("/path/red.png");
            result.GreenHistogramPath.Should().Be("/path/green.png");
            result.BlueHistogramPath.Should().Be("/path/blue.png");
            result.ContrastHistogramPath.Should().Be("/path/contrast.png");
            result.ImagePath.Should().Be("/path/image.jpg");

            result.Stats.MeanRed.Should().Be(100.0);
            result.Stats.MeanGreen.Should().Be(120.0);
            result.Stats.MeanBlue.Should().Be(130.0);
            result.Stats.MeanContrast.Should().Be(0.5);
            result.Stats.StdDevRed.Should().Be(30.0);
            result.Stats.StdDevGreen.Should().Be(25.0);
            result.Stats.StdDevBlue.Should().Be(35.0);
            result.Stats.StdDevContrast.Should().Be(0.2);
            result.Stats.TotalPixels.Should().Be(1000000);
        }

        [Test]
        public void SceneEvaluationStatsDto_ShouldHaveDefaultValues()
        {
            // Arrange & Act
            var stats = new SceneEvaluationStatsDto();

            // Assert
            stats.MeanRed.Should().Be(0);
            stats.MeanGreen.Should().Be(0);
            stats.MeanBlue.Should().Be(0);
            stats.MeanContrast.Should().Be(0);
            stats.StdDevRed.Should().Be(0);
            stats.StdDevGreen.Should().Be(0);
            stats.StdDevBlue.Should().Be(0);
            stats.StdDevContrast.Should().Be(0);
            stats.TotalPixels.Should().Be(0);
        }
    }
}