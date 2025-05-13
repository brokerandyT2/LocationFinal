using NUnit.Framework;
using FluentAssertions;
using Location.Core.Domain.Rules;
using Location.Core.Domain.Entities;
using Location.Core.Domain.ValueObjects;
using System.Collections.Generic;
using System.Linq;

namespace Location.Core.Domain.Tests.Rules
{
    [TestFixture]
    public class LocationValidationRulesTests
    {
        private Coordinate _validCoordinate;
        private Address _validAddress;

        [SetUp]
        public void Setup()
        {
            _validCoordinate = new Coordinate(47.6062, -122.3321);
            _validAddress = new Address("Seattle", "WA");
        }

        [Test]
        public void IsValid_WithValidLocation_ShouldReturnTrue()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Space Needle", "Landmark", _validCoordinate, _validAddress);
            List<string> errors;

            // Act
            var result = LocationValidationRules.IsValid(location, out errors);

            // Assert
            result.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Test]
        public void IsValid_WithNullLocation_ShouldReturnFalse()
        {
            // Arrange
            List<string> errors;

            // Act
            var result = LocationValidationRules.IsValid(null, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Location cannot be null");
        }

        [Test]
        public void IsValid_WithEmptyTitle_ShouldReturnTrue()
        {
            // Note: The validation rules that are commented out in the actual code
            // Arrange
            var location = CreateLocationWithPrivateConstructor("", "Description", _validCoordinate, _validAddress);
            List<string> errors;

            // Act
            var result = LocationValidationRules.IsValid(location, out errors);

            // Assert
            result.Should().BeTrue(); // Because title validation is commented out
            errors.Should().BeEmpty();
        }

        [Test]
        public void IsValid_WithLongDescription_ShouldReturnFalse()
        {
            // Arrange
            var longDescription = new string('a', 501);
            var location = new Location.Core.Domain.Entities.Location("Title", longDescription, _validCoordinate, _validAddress);
            List<string> errors;

            // Act
            var result = LocationValidationRules.IsValid(location, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Location description cannot exceed 500 characters");
        }

        [Test]
        public void IsValid_WithNullCoordinate_ShouldReturnFalse()
        {
            // Arrange
            var location = CreateLocationWithPrivateConstructor("Title", "Description", null, _validAddress);
            List<string> errors;

            // Act
            var result = LocationValidationRules.IsValid(location, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().ContainSingle();
            errors.Should().Contain("Location coordinates are required");
        }

        [Test]
        public void IsValid_WithValidPhotoPath_ShouldReturnTrue()
        {
            // Arrange
            var location = new Location.Core.Domain.Entities.Location("Title", "Description", _validCoordinate, _validAddress);
            location.AttachPhoto("/valid/path/photo.jpg");
            List<string> errors;

            // Act
            var result = LocationValidationRules.IsValid(location, out errors);

            // Assert
            result.Should().BeTrue();
            errors.Should().BeEmpty();
        }

        [Test]
        public void IsValid_WithMultipleErrors_ShouldReturnAllErrors()
        {
            // Arrange
            var longDescription = new string('a', 501);
            var location = CreateLocationWithPrivateConstructor("Title", longDescription, null, _validAddress);
            List<string> errors;

            // Act
            var result = LocationValidationRules.IsValid(location, out errors);

            // Assert
            result.Should().BeFalse();
            errors.Should().HaveCount(2);
            errors.Should().Contain("Location description cannot exceed 500 characters");
            errors.Should().Contain("Location coordinates are required");
        }

        // Helper method to create Location with specific values using reflection
        private Location.Core.Domain.Entities.Location CreateLocationWithPrivateConstructor(
            string title, string description, Coordinate coordinate, Address address)
        {
            var location = System.Activator.CreateInstance(
                typeof(Location.Core.Domain.Entities.Location),
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new object[] { }, null) as Location.Core.Domain.Entities.Location;

            // First try to find backing fields by common naming patterns
            var type = location.GetType();
            var allFields = type.GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Set values directly on the properties' backing store
            // This approach uses a more generic way to find backing fields
            foreach (var prop in type.GetProperties())
            {
                var backingField = allFields.FirstOrDefault(f =>
                    f.Name == $"<{prop.Name}>k__BackingField" || // Auto-property backing field
                    f.Name == $"_{char.ToLower(prop.Name[0])}{prop.Name.Substring(1)}" || // _camelCase
                    f.Name == $"m_{prop.Name}" // m_PropertyName
                );

                if (backingField != null)
                {
                    switch (prop.Name)
                    {
                        case "Title":
                            backingField.SetValue(location, title);
                            break;
                        case "Description":
                            backingField.SetValue(location, description);
                            break;
                        case "Coordinate":
                            backingField.SetValue(location, coordinate);
                            break;
                        case "Address":
                            backingField.SetValue(location, address);
                            break;
                    }
                }
            }

            return location;
        }
    }
}