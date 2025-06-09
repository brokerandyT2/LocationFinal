using FluentValidation.TestHelper;
using Location.Photography.Application.Commands.SceneEvaluation;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Commands.SceneEvaluation
{
    [TestFixture]
    public class AnalyzeImageCommandValidatorTests
    {
        private AnalyzeImageCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new AnalyzeImageCommandValidator();
        }

        [Test]
        public void Validate_WithValidJpgImagePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/images/photo.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithValidJpegImagePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/images/photo.jpeg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithValidPngImagePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/images/photo.png"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithValidBmpImagePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/images/photo.bmp"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithValidGifImagePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/images/photo.gif"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithEmptyImagePath_ShouldHaveError()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = string.Empty
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorMessage("Image path is required");
        }

        [Test]
        public void Validate_WithNullImagePath_ShouldHaveError()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorMessage("Image path is required");
        }

        [Test]
        public void Validate_WithWhitespaceImagePath_ShouldHaveError()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "   "
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorMessage("Image path is required");
        }

        [Test]
        public void Validate_WithUnsupportedFileExtension_ShouldHaveError()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/images/document.pdf"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorMessage("Image must be a valid image file (jpg, jpeg, png, bmp, gif)");
        }

        [Test]
        public void Validate_WithUnsupportedVideoExtension_ShouldHaveError()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/videos/movie.mp4"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorMessage("Image must be a valid image file (jpg, jpeg, png, bmp, gif)");
        }

        [Test]
        public void Validate_WithInvalidPathCharacters_ShouldHaveError()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/images/photo<invalid>.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorMessage("Image path contains invalid characters");
        }

        [Test]
        public void Validate_WithPathTooLong_ShouldHaveError()
        {
            // Arrange - Create a path longer than 260 characters
            var longPath = "/storage/images/" + new string('a', 250) + ".jpg";
            var command = new AnalyzeImageCommand
            {
                ImagePath = longPath
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorMessage("Image path is too long (maximum 260 characters)");
        }

        [Test]
        public void Validate_WithCaseInsensitiveExtensions_ShouldNotHaveErrors()
        {
            // Arrange
            var commands = new[]
            {
                new AnalyzeImageCommand { ImagePath = "/storage/images/photo.JPG" },
                new AnalyzeImageCommand { ImagePath = "/storage/images/photo.JPEG" },
                new AnalyzeImageCommand { ImagePath = "/storage/images/photo.PNG" },
                new AnalyzeImageCommand { ImagePath = "/storage/images/photo.BMP" },
                new AnalyzeImageCommand { ImagePath = "/storage/images/photo.GIF" }
            };

            // Act & Assert
            foreach (var command in commands)
            {
                var result = _validator.TestValidate(command);
                result.ShouldNotHaveAnyValidationErrors();
            }
        }

        [Test]
        public void Validate_WithWindowsStylePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = @"C:\Users\Photos\image.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithRelativePath_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "./images/photo.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithNoExtension_ShouldHaveError()
        {
            // Arrange
            var command = new AnalyzeImageCommand
            {
                ImagePath = "/storage/images/photo"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.ImagePath)
                .WithErrorMessage("Image must be a valid image file (jpg, jpeg, png, bmp, gif)");
        }
    }
}