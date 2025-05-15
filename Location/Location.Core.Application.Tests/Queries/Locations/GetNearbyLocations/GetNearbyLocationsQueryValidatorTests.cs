using FluentValidation.TestHelper;
using Location.Core.Application.Queries.Locations;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Queries.Locations.GetNearbyLocations
{
    [TestFixture]
    public class GetNearbyLocationsQueryValidatorTests
    {
        private GetNearbyLocationsQueryValidator _validator;

        [SetUp]
        public void SetUp()
        {
            _validator = new GetNearbyLocationsQueryValidator();
        }

        [Test]
        public void Validate_WithValidData_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DistanceKm = 10.0
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
            var query = new GetNearbyLocationsQuery
            {
                Latitude = -91,
                Longitude = -122.3321,
                DistanceKm = 10.0
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
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 91,
                Longitude = -122.3321,
                DistanceKm = 10.0
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
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -181,
                DistanceKm = 10.0
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
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = 181,
                DistanceKm = 10.0
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.Longitude)
                .WithErrorMessage("Longitude must be between -180 and 180 degrees");
        }

        [Test]
        public void Validate_WithZeroDistance_ShouldHaveError()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DistanceKm = 0
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DistanceKm)
                .WithErrorMessage("Distance must be greater than 0");
        }

        [Test]
        public void Validate_WithDistanceExceedingMax_ShouldHaveError()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 47.6062,
                Longitude = -122.3321,
                DistanceKm = 101
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldHaveValidationErrorFor(x => x.DistanceKm)
                .WithErrorMessage("Distance must not exceed 100km");
        }

        [Test]
        public void Validate_WithBoundaryValues_ShouldNotHaveErrors()
        {
            // Arrange
            var query = new GetNearbyLocationsQuery
            {
                Latitude = 90,
                Longitude = 180,
                DistanceKm = 100
            };

            // Act
            var result = _validator.TestValidate(query);

            // Assert
            result.ShouldNotHaveAnyValidationErrors();
        }
    }
}