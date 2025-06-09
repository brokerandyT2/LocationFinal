using FluentAssertions;
using Location.Core.Application.Common.Models;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Common.Models
{
    [Category("View Model Support Class")]
    [Category("Error Tests")]
    [TestFixture]
    public class ErrorTests
    {
        [Test]
        public void Constructor_WithCodeAndMessage_ShouldCreateInstance()
        {
            // Arrange & Act
            var error = new Error("TEST_ERROR", "Test error message");

            // Assert
            error.Code.Should().Be("TEST_ERROR");
            error.Message.Should().Be("Test error message");
            error.PropertyName.Should().BeNull();
        }

        [Test]
        public void Constructor_WithAllParameters_ShouldCreateInstance()
        {
            // Arrange & Act
            var error = new Error("TEST_ERROR", "Test error message", "TestProperty");

            // Assert
            error.Code.Should().Be("TEST_ERROR");
            error.Message.Should().Be("Test error message");
            error.PropertyName.Should().Be("TestProperty");
        }

        [Test]
        public void Validation_ShouldCreateValidationError()
        {
            // Arrange & Act
            var error = Error.Validation("EmailAddress", "Email is invalid");

            // Assert
            error.Code.Should().Be("VALIDATION_ERROR");
            error.Message.Should().Be("Email is invalid");
            error.PropertyName.Should().Be("EmailAddress");
        }

        [Test]
        public void NotFound_ShouldCreateNotFoundError()
        {
            // Arrange & Act
            var error = Error.NotFound("Location not found");

            // Assert
            error.Code.Should().Be("NOT_FOUND");
            error.Message.Should().Be("Location not found");
            error.PropertyName.Should().BeNull();
        }

        [Test]
        public void Database_ShouldCreateDatabaseError()
        {
            // Arrange & Act
            var error = Error.Database("Connection failed");

            // Assert
            error.Code.Should().Be("DATABASE_ERROR");
            error.Message.Should().Be("Connection failed");
            error.PropertyName.Should().BeNull();
        }

        [Test]
        public void Domain_ShouldCreateDomainError()
        {
            // Arrange & Act
            var error = Error.Domain("Business rule violation");

            // Assert
            error.Code.Should().Be("DOMAIN_ERROR");
            error.Message.Should().Be("Business rule violation");
            error.PropertyName.Should().BeNull();
        }
    }
}