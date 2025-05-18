using FluentValidation.TestHelper;
using Location.Core.Application.Weather.Queries.GetWeatherForecast;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Weather.Queries.GetWeatherForecast
{
    [Category("Weather")]
    [Category("Get")]
    [TestFixture]
    public class GetWeatherForecastQueryValidatorTests
    {
        private GetWeatherForecastQueryValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetWeatherForecastQueryValidator();
        }

        [Test]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 5
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Test]
        public void Validate_WithLatitudeBelowMin_ShouldHaveError()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = -91,
                Longitude = -122.3321,
                Days = 5
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Latitude)
                .WithErrorMessage("Latitude must be between -90 and 90 degrees");
        }

        [Test]
        public void Validate_WithLatitudeAboveMax_ShouldHaveError()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 91,
                Longitude = -122.3321,
                Days = 5
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Latitude)
                .WithErrorMessage("Latitude must be between -90 and 90 degrees");
        }

        [Test]
        public void Validate_WithLongitudeBelowMin_ShouldHaveError()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -181,
                Days = 5
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Longitude)
                .WithErrorMessage("Longitude must be between -180 and 180 degrees");
        }

        [Test]
        public void Validate_WithLongitudeAboveMax_ShouldHaveError()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = 181,
                Days = 5
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Longitude)
                .WithErrorMessage("Longitude must be between -180 and 180 degrees");
        }

        [Test]
        public void Validate_WithDaysBelowMin_ShouldHaveError()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 0
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Days)
                .WithErrorMessage("Days must be between 1 and 7");
        }

        [Test]
        public void Validate_WithDaysAboveMax_ShouldHaveError()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                Days = 8
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Days)
                .WithErrorMessage("Days must be between 1 and 7");
        }

        [Test]
        public void Validate_WithBoundaryValues_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetWeatherForecastQuery
            {
                Latitude = 90,
                Longitude = 180,
                Days = 7
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}