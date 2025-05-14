namespace Location.Core.Application.Tests.Locations.Commands.SaveLocation
{
    using FluentValidation.TestHelper;
    using Location.Core.Application.Commands.Locations;
    using System.IO;
    using Xunit;

    public class SaveLocationCommandValidatorTests
    {
        private readonly SaveLocationCommandValidator _validator;

        public SaveLocationCommandValidatorTests()
        {
            _validator = new SaveLocationCommandValidator();
        }

        [Fact]
        public void Should_Have_Error_When_Title_Is_Empty()
        {
            // Arrange
            var command = new SaveLocationCommand { Title = string.Empty };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title is required");
        }

        [Fact]
        public void Should_Have_Error_When_Title_Exceeds_Max_Length()
        {
            // Arrange
            var command = new SaveLocationCommand { Title = new string('a', 101) };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Title)
                .WithErrorMessage("Title must not exceed 100 characters");
        }

        [Fact]
        public void Should_Have_Error_When_Description_Exceeds_Max_Length()
        {
            // Arrange
            var command = new SaveLocationCommand { Description = new string('a', 501) };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Description)
                .WithErrorMessage("Description must not exceed 500 characters");
        }

        [Fact]
        public void Should_Have_Error_When_Latitude_Is_Less_Than_Minus_90()
        {
            // Arrange
            var command = new SaveLocationCommand { Latitude = -91 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Latitude)
                .WithErrorMessage("Latitude must be between -90 and 90 degrees");
        }

        [Fact]
        public void Should_Have_Error_When_Latitude_Is_Greater_Than_90()
        {
            // Arrange
            var command = new SaveLocationCommand { Latitude = 91 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Latitude)
                .WithErrorMessage("Latitude must be between -90 and 90 degrees");
        }

        [Fact]
        public void Should_Have_Error_When_Longitude_Is_Less_Than_Minus_180()
        {
            // Arrange
            var command = new SaveLocationCommand { Longitude = -181 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Longitude)
                .WithErrorMessage("Longitude must be between -180 and 180 degrees");
        }

        [Fact]
        public void Should_Have_Error_When_Longitude_Is_Greater_Than_180()
        {
            // Arrange
            var command = new SaveLocationCommand { Longitude = 181 };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Longitude)
                .WithErrorMessage("Longitude must be between -180 and 180 degrees");
        }

        [Fact]
        public void Should_Have_Error_When_Coordinates_Are_Null_Island()
        {
            // Arrange
            var command = new SaveLocationCommand
            {
                Title = "Test",
                Latitude = 0,
                Longitude = 0
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor("Coordinates")
                .WithErrorMessage("Invalid coordinates: Cannot use Null Island (0,0)");
        }

        [Fact]
        public void Should_Not_Have_Error_For_Valid_Location()
        {
            // Arrange
            var command = new SaveLocationCommand
            {
                Title = "Valid Location",
                Description = "A valid description",
                Latitude = 40.7128,
                Longitude = -74.0060,
                City = "New York",
                State = "NY"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Have_Error_When_PhotoPath_Is_Invalid()
        {
            // Arrange
            var command = new SaveLocationCommand
            {
                Title = "Test",
                Latitude = 40.7128,
                Longitude = -74.0060,
                PhotoPath = "invalid|<>:path"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.PhotoPath)
                .WithErrorMessage("Photo path is not valid");
        }

        [Fact]
        public void Should_Not_Have_Error_When_PhotoPath_Is_Valid()
        {
            // Arrange
            var command = new SaveLocationCommand
            {
                Title = "Test",
                Latitude = 40.7128,
                Longitude = -74.0060,
                PhotoPath = "/path/to/valid/photo.jpg"
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PhotoPath);
        }

        [Fact]
        public void Should_Not_Have_Error_When_PhotoPath_Is_Null()
        {
            // Arrange
            var command = new SaveLocationCommand
            {
                Title = "Test",
                Latitude = 40.7128,
                Longitude = -74.0060,
                PhotoPath = null
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveValidationErrorFor(x => x.PhotoPath);
        }
    }
}