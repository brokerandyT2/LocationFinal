using FluentValidation.TestHelper;
using Location.Photography.Application.Commands.ExposureCalculator;
using Location.Photography.Application.Services;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Commands.ExposureCalculator
{
    [TestFixture]
    public class CalculateExposureCommandValidatorTests
    {
        private CalculateExposureCommandValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new CalculateExposureCommandValidator();
        }

        [Test]
        public void Validate_WhenBaseExposureIsNull_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = null,
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BaseExposure)
                .WithErrorMessage("Base exposure settings are required");
        }

        [Test]
        public void Validate_WhenBaseExposureHasEmptyShutterSpeed_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = string.Empty,
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BaseExposure.ShutterSpeed)
                .WithErrorMessage("Base shutter speed is required");
        }

        [Test]
        public void Validate_WhenBaseExposureHasEmptyAperture_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = string.Empty,
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BaseExposure.Aperture)
                .WithErrorMessage("Base aperture is required");
        }

        [Test]
        public void Validate_WhenBaseExposureHasEmptyIso_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = string.Empty
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.BaseExposure.Iso)
                .WithErrorMessage("Base ISO is required");
        }

        [Test]
        public void Validate_WhenCalculatingShutterSpeed_WithEmptyTargetAperture_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = string.Empty, // Empty target aperture
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetAperture)
                .WithErrorMessage("Target aperture is required");
        }

        [Test]
        public void Validate_WhenCalculatingShutterSpeed_WithEmptyTargetIso_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = string.Empty, // Empty target ISO
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetIso)
                .WithErrorMessage("Target ISO is required");
        }

        [Test]
        public void Validate_WhenCalculatingAperture_WithEmptyTargetShutterSpeed_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = string.Empty, // Empty target shutter speed
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.Aperture
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetShutterSpeed)
                .WithErrorMessage("Target shutter speed is required");
        }

        [Test]
        public void Validate_WhenCalculatingAperture_WithEmptyTargetIso_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = "1/60",
                TargetIso = string.Empty, // Empty target ISO
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.Aperture
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetIso)
                .WithErrorMessage("Target ISO is required");
        }

        [Test]
        public void Validate_WhenCalculatingIso_WithEmptyTargetShutterSpeed_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = string.Empty, // Empty target shutter speed
                TargetAperture = "f/11",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ISO
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetShutterSpeed)
                .WithErrorMessage("Target shutter speed is required");
        }

        [Test]
        public void Validate_WhenCalculatingIso_WithEmptyTargetAperture_ShouldHaveError()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = "1/60",
                TargetAperture = string.Empty, // Empty target aperture
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ISO
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.TargetAperture)
                .WithErrorMessage("Target aperture is required");
        }

        [Test]
        public void Validate_WithValidCommand_ForShutterSpeedCalculation_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetAperture = "f/11",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ShutterSpeeds,
                EvCompensation = 0.0
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithValidCommand_ForApertureCalculation_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = "1/60",
                TargetIso = "200",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.Aperture,
                EvCompensation = 0.0
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithValidCommand_ForIsoCalculation_ShouldNotHaveErrors()
        {
            // Arrange
            var command = new CalculateExposureCommand
            {
                BaseExposure = new ExposureTriangleDto
                {
                    ShutterSpeed = "1/125",
                    Aperture = "f/8",
                    Iso = "100"
                },
                TargetShutterSpeed = "1/60",
                TargetAperture = "f/11",
                Increments = ExposureIncrements.Full,
                ToCalculate = FixedValue.ISO,
                EvCompensation = 0.0
            };

            // Act
            var result = _validator.TestValidate(command);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}