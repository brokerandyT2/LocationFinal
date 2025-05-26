using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace Location.Core.Application.Tests.Common.Models
{
    [Category("View Model Support Class")]
    [Category("Result<T> Tests")]
    [TestFixture]
    public class ResultTests
    {
        [Test]
        public void Success_ShouldCreateSuccessfulResult()
        {
            // Act
            var result = Result.Success();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.ErrorMessage.Should().BeNull();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public void Failure_WithErrorMessage_ShouldCreateFailureResult()
        {
            // Arrange
            var errorMessage = "Operation failed";

            // Act
            var result = Result.Failure(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be(errorMessage);
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public void Failure_WithErrors_ShouldCreateFailureResult()
        {
            // Arrange
            var errors = new List<Error>
            {
                new Error("CODE1", "Error 1"),
                new Error("CODE2", "Error 2")
            };

            // Act
            var result = Result.Failure(errors);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().BeNull();
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain(errors);
        }

        [Test]
        public void Failure_WithSingleError_ShouldCreateFailureResult()
        {
            // Arrange
            var error = new Error("CODE", "Error message");

            // Act
            var result = Result.Failure(error);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().BeNull();
            result.Errors.Should().ContainSingle();
            result.Errors.Should().Contain(error);
        }
    }

    [TestFixture]
    public class ResultTTests
    {
        [Test]
        public void Success_WithData_ShouldCreateSuccessfulResultWithData()
        {
            // Arrange
            var data = "test data";

            // Act
            var result = Result<string>.Success(data);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(data);
            result.ErrorMessage.Should().BeNull();
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public void Failure_WithErrorMessage_ShouldCreateFailureResultWithNullData()
        {
            // Arrange
            var errorMessage = "Operation failed";

            // Act
            var result = Result<string>.Failure(errorMessage);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be(errorMessage);
            result.Errors.Should().BeEmpty();
        }

        [Test]
        public void Failure_WithErrors_ShouldCreateFailureResultWithNullData()
        {
            // Arrange
            var errors = new List<Error>
            {
                new Error("CODE1", "Error 1"),
                new Error("CODE2", "Error 2")
            };

            // Act
            var result = Result<int>.Failure(errors);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().Be(default(int));
            result.ErrorMessage.Should().BeNull();
            result.Errors.Should().HaveCount(2);
        }

        [Test]
        public void Failure_WithDomainException_ShouldCreateFailureResult()
        {
            // Arrange
            var exception = new Domain.Exceptions.LocationDomainException("DOMAIN_CODE", "Domain error");

            // Act
            var result = Result<string>.Failure(exception);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("Domain error");
            result.Errors.Should().ContainSingle();
            result.Errors.First().Code.Should().Be("DOMAIN_CODE");
            result.Errors.First().Message.Should().Be("Domain error");
        }

        [Test]
        public void Success_WithComplexType_ShouldReturnCorrectData()
        {
            // Arrange
            var complexData = new TestComplexType { Id = 1, Name = "Test" };

            // Act
            var result = Result<TestComplexType>.Success(complexData);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(1);
            result.Data.Name.Should().Be("Test");
        }

        private class TestComplexType
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }
    }
}