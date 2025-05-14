using NUnit.Framework;
using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Services;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Tests.Services
{
    [TestFixture]
    public class IMediaServiceTests
    {
        private Mock<IMediaService> _mediaServiceMock;

        [SetUp]
        public void Setup()
        {
            _mediaServiceMock = new Mock<IMediaService>();
        }

        [Test]
        public async Task CapturePhotoAsync_WithCameraAvailable_ShouldReturnSuccess()
        {
            // Arrange
            var expectedPath = "/storage/photos/photo_20240115_103045.jpg";
            var expectedResult = Result<string>.Success(expectedPath);

            _mediaServiceMock.Setup(x => x.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.CapturePhotoAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(expectedPath);
        }

        [Test]
        public async Task CapturePhotoAsync_WithoutCameraPermission_ShouldReturnFailure()
        {
            // Arrange
            var expectedResult = Result<string>.Failure("Camera permission denied");

            _mediaServiceMock.Setup(x => x.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.CapturePhotoAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Camera permission denied");
        }

        [Test]
        public async Task PickPhotoAsync_WithValidSelection_ShouldReturnSuccess()
        {
            // Arrange
            var expectedPath = "/storage/gallery/IMG_1234.jpg";
            var expectedResult = Result<string>.Success(expectedPath);

            _mediaServiceMock.Setup(x => x.PickPhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.PickPhotoAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(expectedPath);
        }

        [Test]
        public async Task PickPhotoAsync_WhenCancelled_ShouldReturnFailure()
        {
            // Arrange
            var expectedResult = Result<string>.Failure("Photo selection cancelled");

            _mediaServiceMock.Setup(x => x.PickPhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.PickPhotoAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Photo selection cancelled");
        }

        [Test]
        public async Task IsCaptureSupported_OnSupportedDevice_ShouldReturnTrue()
        {
            // Arrange
            var expectedResult = Result<bool>.Success(true);

            _mediaServiceMock.Setup(x => x.IsCaptureSupported(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.IsCaptureSupported();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task IsCaptureSupported_OnUnsupportedDevice_ShouldReturnFalse()
        {
            // Arrange
            var expectedResult = Result<bool>.Success(false);

            _mediaServiceMock.Setup(x => x.IsCaptureSupported(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.IsCaptureSupported();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Test]
        public async Task DeletePhotoAsync_WithExistingFile_ShouldReturnSuccess()
        {
            // Arrange
            var filePath = "/storage/photos/test.jpg";
            var expectedResult = Result<bool>.Success(true);

            _mediaServiceMock.Setup(x => x.DeletePhotoAsync(filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.DeletePhotoAsync(filePath);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task DeletePhotoAsync_WithNonExistentFile_ShouldReturnFailure()
        {
            // Arrange
            var filePath = "/storage/photos/nonexistent.jpg";
            var expectedResult = Result<bool>.Failure("File not found");

            _mediaServiceMock.Setup(x => x.DeletePhotoAsync(filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.DeletePhotoAsync(filePath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("File not found");
        }

        [Test]
        public async Task DeletePhotoAsync_WithIOError_ShouldReturnFailure()
        {
            // Arrange
            var filePath = "/storage/photos/locked.jpg";
            var expectedResult = Result<bool>.Failure("File is in use by another process");

            _mediaServiceMock.Setup(x => x.DeletePhotoAsync(filePath, It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.DeletePhotoAsync(filePath);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("File is in use by another process");
        }

        [Test]
        public void GetPhotoStorageDirectory_ShouldReturnPath()
        {
            // Arrange
            var expectedPath = "/data/app/com.myapp/photos";
            _mediaServiceMock.Setup(x => x.GetPhotoStorageDirectory())
                .Returns(expectedPath);

            // Act
            var result = _mediaServiceMock.Object.GetPhotoStorageDirectory();

            // Assert
            result.Should().Be(expectedPath);
        }

        [Test]
        public async Task CapturePhotoAsync_WithCancellation_ShouldHandleCorrectly()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var expectedResult = Result<string>.Failure("Operation cancelled");

            _mediaServiceMock.Setup(x => x.CapturePhotoAsync(token))
                .ReturnsAsync(expectedResult);

            // Act
            cts.Cancel();
            var result = await _mediaServiceMock.Object.CapturePhotoAsync(token);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Operation cancelled");
        }

        [Test]
        public async Task PickPhotoAsync_WithInvalidFormat_ShouldReturnFailure()
        {
            // Arrange
            var expectedResult = Result<string>.Failure("Invalid image format");

            _mediaServiceMock.Setup(x => x.PickPhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mediaServiceMock.Object.PickPhotoAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid image format");
        }
    }
}