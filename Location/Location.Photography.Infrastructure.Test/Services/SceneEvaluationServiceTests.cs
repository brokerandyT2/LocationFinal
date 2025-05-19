using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using SkiaSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Test.Services
{
    [TestFixture]
    public class SceneEvaluationServiceTests
    {
        private SceneEvaluationService _sceneEvaluationService;
        private Mock<ILogger<SceneEvaluationService>> _loggerMock;
        private Mock<IMediaService> _mediaServiceMock;
        private string _testImagePath;
        private string _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<SceneEvaluationService>>();
            _mediaServiceMock = new Mock<IMediaService>();

            // Create a temporary directory for test files
            _tempDirectory = Path.Combine(Path.GetTempPath(), "SceneEvaluationServiceTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);

            // Create a test image
            _testImagePath = Path.Combine(_tempDirectory, "test_image.png");
            CreateTestImage(_testImagePath);

            _sceneEvaluationService = new SceneEvaluationService(_loggerMock.Object, _mediaServiceMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up temporary files
            if (Directory.Exists(_tempDirectory))
            {
                try
                {
                    Directory.Delete(_tempDirectory, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SceneEvaluationService(null, _mediaServiceMock.Object))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public void Constructor_WithNullMediaService_ThrowsArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SceneEvaluationService(_loggerMock.Object, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("mediaService");
        }

        [Test]
        public async Task EvaluateSceneAsync_WhenCaptureSucceeds_ReturnsSuccessResult()
        {
            // Arrange
            _mediaServiceMock.Setup(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success(_testImagePath));

            // Act
            var result = await _sceneEvaluationService.EvaluateSceneAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ImagePath.Should().Be(_testImagePath);
            result.Data.RedHistogramPath.Should().NotBeNullOrEmpty();
            result.Data.GreenHistogramPath.Should().NotBeNullOrEmpty();
            result.Data.BlueHistogramPath.Should().NotBeNullOrEmpty();
            result.Data.ContrastHistogramPath.Should().NotBeNullOrEmpty();
            result.Data.Stats.Should().NotBeNull();
            result.Data.Stats.TotalPixels.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task EvaluateSceneAsync_WhenCaptureFails_ReturnsFailureResult()
        {
            // Arrange
            _mediaServiceMock.Setup(m => m.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Failure("Camera unavailable"));

            // Act
            var result = await _sceneEvaluationService.EvaluateSceneAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to capture image");
            result.ErrorMessage.Should().Contain("Camera unavailable");
        }

        [Test]
        public async Task EvaluateSceneAsync_WithCancellationToken_ThrowsWhenCancelled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _sceneEvaluationService.EvaluateSceneAsync(cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public async Task AnalyzeImageAsync_WithValidImage_ReturnsSuccessResult()
        {
            // Act
            var result = await _sceneEvaluationService.AnalyzeImageAsync(_testImagePath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ImagePath.Should().Be(_testImagePath);
            result.Data.RedHistogramPath.Should().NotBeNullOrEmpty();
            result.Data.GreenHistogramPath.Should().NotBeNullOrEmpty();
            result.Data.BlueHistogramPath.Should().NotBeNullOrEmpty();
            result.Data.ContrastHistogramPath.Should().NotBeNullOrEmpty();
            result.Data.Stats.Should().NotBeNull();
            result.Data.Stats.TotalPixels.Should().BeGreaterThan(0);
        }

        [Test]
        public async Task AnalyzeImageAsync_WithNonexistentImage_ReturnsFailureResult()
        {
            // Arrange
            string nonExistentPath = Path.Combine(_tempDirectory, "nonexistent.jpg");

            // Act
            var result = await _sceneEvaluationService.AnalyzeImageAsync(nonExistentPath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("File not found");
        }

        [Test]
        public async Task AnalyzeImageAsync_WithInvalidImage_ReturnsFailureResult()
        {
            // Arrange
            string invalidImagePath = Path.Combine(_tempDirectory, "invalid.jpg");
            File.WriteAllText(invalidImagePath, "This is not an image file");

            // Act
            var result = await _sceneEvaluationService.AnalyzeImageAsync(invalidImagePath);

            // Assert - update to match the actual error message
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to decode image");
        }

        [Test]
        public async Task AnalyzeImageAsync_WithCancellationToken_ThrowsWhenCancelled()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _sceneEvaluationService.AnalyzeImageAsync(_testImagePath, cts.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        [Test]
        public void CalculateHistograms_ShouldGenerateValidStatistics()
        {
            // This test would normally use reflection to test a private method,
            // but we can verify the functionality indirectly through the public methods

            // Act
            var result = _sceneEvaluationService.AnalyzeImageAsync(_testImagePath).Result;

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Stats.MeanRed.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(255);
            result.Data.Stats.MeanGreen.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(255);
            result.Data.Stats.MeanBlue.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(255);
            result.Data.Stats.MeanContrast.Should().BeGreaterThanOrEqualTo(0).And.BeLessThanOrEqualTo(255);

            result.Data.Stats.StdDevRed.Should().BeGreaterThanOrEqualTo(0);
            result.Data.Stats.StdDevGreen.Should().BeGreaterThanOrEqualTo(0);
            result.Data.Stats.StdDevBlue.Should().BeGreaterThanOrEqualTo(0);
            result.Data.Stats.StdDevContrast.Should().BeGreaterThanOrEqualTo(0);

            result.Data.Stats.TotalPixels.Should().Be(100 * 100); // Based on our test image size
        }

        [Test]
        public void SaveHistogramAsync_ShouldCreateValidImageFile()
        {
            // Again, this would normally test a private method,
            // but we can verify that histogram files are created properly

            // Act
            var result = _sceneEvaluationService.AnalyzeImageAsync(_testImagePath).Result;

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Check that the histogram files exist and are valid images
            File.Exists(result.Data.RedHistogramPath).Should().BeTrue();
            File.Exists(result.Data.GreenHistogramPath).Should().BeTrue();
            File.Exists(result.Data.BlueHistogramPath).Should().BeTrue();
            File.Exists(result.Data.ContrastHistogramPath).Should().BeTrue();

            // Verify that they're valid PNG files by checking the file magic numbers
            using (var stream = File.OpenRead(result.Data.RedHistogramPath))
            {
                byte[] header = new byte[8];
                stream.Read(header, 0, 8);
                // PNG file header: 89 50 4E 47 0D 0A 1A 0A
                header.Should().StartWith(new byte[] { 0x89, 0x50, 0x4E, 0x47 });
            }
        }

        #region Helpers

        private void CreateTestImage(string path)
        {
            // Create a simple test image with known dimensions and colors
            using (var bitmap = new SKBitmap(100, 100))
            {
                using (var canvas = new SKCanvas(bitmap))
                {
                    // Draw some gradients to ensure we have a range of colors
                    using (var paint = new SKPaint())
                    {
                        // Red gradient
                        for (int y = 0; y < 25; y++)
                        {
                            for (int x = 0; x < 100; x++)
                            {
                                byte intensity = (byte)(x * 255 / 100);
                                bitmap.SetPixel(x, y, new SKColor(intensity, 0, 0));
                            }
                        }

                        // Green gradient
                        for (int y = 25; y < 50; y++)
                        {
                            for (int x = 0; x < 100; x++)
                            {
                                byte intensity = (byte)(x * 255 / 100);
                                bitmap.SetPixel(x, y, new SKColor(0, intensity, 0));
                            }
                        }

                        // Blue gradient
                        for (int y = 50; y < 75; y++)
                        {
                            for (int x = 0; x < 100; x++)
                            {
                                byte intensity = (byte)(x * 255 / 100);
                                bitmap.SetPixel(x, y, new SKColor(0, 0, intensity));
                            }
                        }

                        // Grayscale gradient
                        for (int y = 75; y < 100; y++)
                        {
                            for (int x = 0; x < 100; x++)
                            {
                                byte intensity = (byte)(x * 255 / 100);
                                bitmap.SetPixel(x, y, new SKColor(intensity, intensity, intensity));
                            }
                        }
                    }
                }

                // Save the bitmap as a PNG file
                using (var stream = File.OpenWrite(path))
                {
                    bitmap.Encode(stream, SKEncodedImageFormat.Png, 100);
                }
            }
        }

        #endregion
    }
}