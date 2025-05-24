
using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Tests.Helpers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Infrastructure.Tests.Data.Repositories
{
    [TestFixture]
    public class LocationRepositoryAdapterTests
    {
        private LocationRepositoryAdapter _adapter;
        private Mock<ILocationRepository> _mockInnerRepository;

        [SetUp]
        public void Setup()
        {
            _mockInnerRepository = new Mock<ILocationRepository>();
            _adapter = new LocationRepositoryAdapter(_mockInnerRepository.Object);
        }

        [Test]
        public async Task GetByIdAsync_WithExistingLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            // Act
            var result = await _adapter.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(location);
            result.ErrorMessage.Should().BeNull();
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingLocation_ShouldReturnFailure()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location)null);

            // Act
            var result = await _adapter.GetByIdAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("Location with ID 999 not found");
        }

        [Test]
        public async Task GetByIdAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetByIdAsync(1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve location: Database error");
        }

        [Test]
        public async Task GetAllAsync_WithMultipleLocations_ShouldReturnSuccess()
        {
            // Arrange
            var locations = new[]
            {
                TestDataBuilder.CreateValidLocation(title: "Location 1"),
                TestDataBuilder.CreateValidLocation(title: "Location 2"),
                TestDataBuilder.CreateValidLocation(title: "Location 3")
            };
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(locations);

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Should().BeEquivalentTo(locations);
        }

        [Test]
        public async Task GetAllAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve locations: Database error");
        }

        [Test]
        public async Task GetActiveAsync_WithActiveLocations_ShouldReturnSuccess()
        {
            // Arrange
            var activeLocations = new[]
            {
                TestDataBuilder.CreateValidLocation(title: "Active 1"),
                TestDataBuilder.CreateValidLocation(title: "Active 2")
            };
            _mockInnerRepository.Setup(x => x.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(activeLocations);

            // Act
            var result = await _adapter.GetActiveAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Select(l => l.Title).Should().Contain("Active 1", "Active 2");
        }

        [Test]
        public async Task CreateAsync_WithValidLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);

            // Act
            var result = await _adapter.CreateAsync(location);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(location);
            _mockInnerRepository.Verify(x => x.AddAsync(location, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.CreateAsync(location);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create location: Database error");
        }

        [Test]
        public async Task UpdateAsync_WithValidLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _adapter.UpdateAsync(location);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(location);
            _mockInnerRepository.Verify(x => x.UpdateAsync(location, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
      .ThrowsAsync(exception);

            // Act
            var result = await _adapter.UpdateAsync(location);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to update location: Database error");
        }

        [Test]
        public async Task DeleteAsync_WithExistingLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);
            _mockInnerRepository.Setup(x => x.DeleteAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _adapter.DeleteAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockInnerRepository.Verify(x => x.DeleteAsync(location, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_WithNonExistingLocation_ShouldReturnFailure()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location)null);

            // Act
            var result = await _adapter.DeleteAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location with ID 999 not found");
            _mockInnerRepository.Verify(x => x.DeleteAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task SoftDeleteAsync_WithExistingLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(location);
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _adapter.SoftDeleteAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            location.IsDeleted.Should().BeTrue();
            _mockInnerRepository.Verify(x => x.UpdateAsync(location, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetByCoordinatesAsync_WithMatchingLocations_ShouldReturnSuccess()
        {
            // Arrange
            var locations = new[]
            {
                TestDataBuilder.CreateValidLocation(title: "Near 1"),
                TestDataBuilder.CreateValidLocation(title: "Near 2")
            };
            _mockInnerRepository.Setup(x => x.GetNearbyAsync(
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(locations);

            // Act
            var result = await _adapter.GetByCoordinatesAsync(47.6062, -122.3321, 10);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.Select(l => l.Title).Should().Contain("Near 1", "Near 2");
        }

        [Test]
        public async Task GetByCoordinatesAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetNearbyAsync(
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetByCoordinatesAsync(47.6062, -122.3321);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve locations by coordinates: Database error");
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowException()
        {
            // Act
            Action act = () => new LocationRepositoryAdapter(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("innerRepository");
        }

        [Test]
        public async Task DeleteAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.DeleteAsync(1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to delete location: Database error");
        }

        [Test]
        public async Task SoftDeleteAsync_WithNonExistingLocation_ShouldReturnFailure()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location)null);

            // Act
            var result = await _adapter.SoftDeleteAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location with ID 999 not found");
        }
    }
}