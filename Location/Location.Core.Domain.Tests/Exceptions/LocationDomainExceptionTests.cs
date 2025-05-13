using NUnit.Framework;
using FluentAssertions;
using Location.Core.Domain.Exceptions;
using System;

namespace Location.Core.Domain.Tests.Exceptions
{
    [TestFixture]
    public class LocationDomainExceptionTests
    {
        [Test]
        public void Constructor_WithMessage_ShouldCreateInstance()
        {
            // Arrange & Act
            var exception = new LocationDomainException("Test error message");

            // Assert
            exception.Message.Should().Be("Test error message");
            exception.Code.Should().Be("DOMAIN_ERROR");
            exception.InnerException.Should().BeNull();
        }

        [Test]
        public void Constructor_WithMessageAndCode_ShouldCreateInstance()
        {
            // Arrange & Act
            var exception = new LocationDomainException("Test error message", "CUSTOM_CODE");

            // Assert
            exception.Message.Should().Be("Test error message");
            exception.Code.Should().Be("CUSTOM_CODE");
            exception.InnerException.Should().BeNull();
        }

        [Test]
        public void Constructor_WithMessageAndInnerException_ShouldCreateInstance()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var exception = new LocationDomainException("Test error message", innerException);

            // Assert
            exception.Message.Should().Be("Test error message");
            exception.Code.Should().Be("DOMAIN_ERROR");
            exception.InnerException.Should().Be(innerException);
        }

        [Test]
        public void Constructor_WithMessageInnerExceptionAndCode_ShouldCreateInstance()
        {
            // Arrange
            var innerException = new InvalidOperationException("Inner exception");

            // Act
            var exception = new LocationDomainException("Test error message", innerException, "CUSTOM_CODE");

            // Assert
            exception.Message.Should().Be("Test error message");
            exception.Code.Should().Be("CUSTOM_CODE");
            exception.InnerException.Should().Be(innerException);
        }

        [Test]
        public void InheritsFromException()
        {
            // Arrange & Act
            var exception = new LocationDomainException("Test");

            // Assert
            exception.Should().BeAssignableTo<Exception>();
        }

        [Test]
        public void Code_ShouldBeReadOnly()
        {
            // Arrange
            var exception = new LocationDomainException("Test", "CUSTOM_CODE");

            // Act
            var codeProperty = exception.GetType().GetProperty("Code");

            // Assert
            codeProperty.Should().NotBeNull();
            codeProperty.CanWrite.Should().BeFalse();
        }
    }
}