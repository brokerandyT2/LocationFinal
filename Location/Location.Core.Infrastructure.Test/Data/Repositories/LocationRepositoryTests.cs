
using FluentAssertions;
using Location.Core.Domain.ValueObjects;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Services;
using Location.Core.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace Location.Core.Infrastructure.Tests.Data.Repositories
{
    [TestFixture]
    public class LocationRepositoryTests
    {
        private LocationRepository _repository;
        private DatabaseContext _context;
        private Mock<ILogger<LocationRepository>> _mockLogger;
        private Mock<ILogger<DatabaseContext>> _mockContextLogger;
        private string _testDbPath;
        private Mock<IInfrastructureExceptionMappingService> _mockInfraLogger;

        [SetUp]
        public async Task Setup()
        {
            _mockInfraLogger = new Mock<IInfrastructureExceptionMappingService>();
            _mockLogger = new Mock<ILogger<LocationRepository>>();
            _mockContextLogger = new Mock<ILogger<DatabaseContext>>();
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            _context = new DatabaseContext(_mockContextLogger.Object, _testDbPath);
            await _context.InitializeDatabaseAsync();

            // FIX: Setup only the methods that actually exist in the interface
            _mockInfraLogger.Setup(x => x.MapToLocationDomainException(It.IsAny<Exception>(), It.IsAny<string>()))
                .Returns((Exception ex, string operation) =>
                    new Location.Core.Domain.Exceptions.LocationDomainException("TEST_ERROR", ex.Message, ex));

            _repository = new LocationRepository(_context, _mockLogger.Object, _mockInfraLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();

            if (File.Exists(_testDbPath))
            {
                File.Delete(_testDbPath);
            }
        }

        [Test]
        public async Task GetByIdAsync_WithExistingLocation_ShouldReturnLocation()
        {
            // Arrange
            var locationEntity = TestDataBuilder.CreateLocationEntity();
            await _context.InsertAsync(locationEntity);

            // Act
            var result = await _repository.GetByIdAsync(locationEntity.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(locationEntity.Id);
            result.Title.Should().Be(locationEntity.Title);
            result.Coordinate.Latitude.Should().Be(locationEntity.Latitude);
            result.Coordinate.Longitude.Should().Be(locationEntity.Longitude);
        }

       

        [Test]
        public async Task GetAllAsync_WithMultipleLocations_ShouldReturnAllSortedByTimestamp()
        {
            // Arrange
            var location1 = TestDataBuilder.CreateLocationEntity(id: 0, title: "First");
            location1.Timestamp = DateTime.UtcNow.AddMinutes(-2);
            var location2 = TestDataBuilder.CreateLocationEntity(id: 0, title: "Second");
            location2.Timestamp = DateTime.UtcNow.AddMinutes(-1);
            var location3 = TestDataBuilder.CreateLocationEntity(id: 0, title: "Third");
            location3.Timestamp = DateTime.UtcNow;

            await _context.InsertAsync(location1);
            await _context.InsertAsync(location2);
            await _context.InsertAsync(location3);

            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            var locationList = results.ToList();
            locationList.Should().HaveCount(3);
            locationList[0].Title.Should().Be("Third"); // Most recent first
            locationList[1].Title.Should().Be("Second");
            locationList[2].Title.Should().Be("First");
        }

        [Test]
        public async Task GetActiveAsync_WithMixedLocations_ShouldReturnOnlyActive()
        {
            // Arrange
            var activeLocation1 = TestDataBuilder.CreateLocationEntity(id: 0, isDeleted: false);
            var deletedLocation = TestDataBuilder.CreateLocationEntity(id: 0, isDeleted: true);
            var activeLocation2 = TestDataBuilder.CreateLocationEntity(id: 0, isDeleted: false);

            await _context.InsertAsync(activeLocation1);
            await _context.InsertAsync(deletedLocation);
            await _context.InsertAsync(activeLocation2);

            // Act
            var results = await _repository.GetActiveAsync();

            // Assert
            var activeList = results.ToList();
            activeList.Should().HaveCount(2);
            activeList.Should().NotContain(l => l.IsDeleted);
        }

        [Test]
        public async Task AddAsync_WithValidLocation_ShouldPersistAndReturnWithId()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();

            // Act
            var result = await _repository.AddAsync(location);

            // Assert
            result.Should().BeSameAs(location);
            result.Id.Should().BeGreaterThan(0);

            // Verify persistence
            var retrieved = await _repository.GetByIdAsync(result.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be(location.Title);
        }

        [Test]
        public void AddAsync_WithInvalidLocation_ShouldThrowException()
        {
            // Arrange
            // Act & Assert
            Func<Task> act = async () =>
            {
                var coordinate = new Coordinate(91, 0); // This will throw ArgumentOutOfRangeException
                var address = new Address("City", "State");
                var location = new Domain.Entities.Location("Title", "Description", coordinate, address);
                await _repository.AddAsync(location);
            };

            // The Coordinate constructor throws ArgumentOutOfRangeException for invalid latitude
            act.Should().ThrowAsync<ArgumentOutOfRangeException>()
                .WithParameterName("latitude");
        }

        [Test]
        public async Task Update_WithExistingLocation_ShouldPersistChanges()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _repository.AddAsync(location);

            // Act
            location.UpdateDetails("Updated Title", "Updated Description");
            _repository.UpdateAsync(location);

            // Assert
            var retrieved = await _repository.GetByIdAsync(location.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be("Updated Title");
            retrieved.Description.Should().Be("Updated Description");
        }

        [Test]
        public async Task Delete_WithExistingLocation_ShouldRemove()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _repository.AddAsync(location);

            // Act
            await _repository.DeleteAsync(location);

            // Assert - Fixed to avoid Sequence contains no elements
            var allLocations = await _repository.GetAllAsync();
            allLocations.Should().NotContain(l => l.Id == location.Id);
        }

        [Test]
        public async Task GetByTitleAsync_WithExistingTitle_ShouldReturnLocation()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation(title: "Unique Title");
            await _repository.AddAsync(location);

            // Act
            var result = await _repository.GetByTitleAsync("Unique Title");

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Unique Title");
        }

        [Test]
        public async Task GetByTitleAsync_WithNonExistingTitle_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByTitleAsync("Non-Existent Title");

            // Assert
            result.Should().BeNull();
        }

       

       

        [Test]
        public void AddAsync_WithNullCoordinate_ShouldFailValidation()
        {
            // Looking at CoordinateValidationRules.cs, it checks for Null Island (0,0)
            // But this validation is only applied in the repository AddAsync method

            // Arrange
            var locationEntity = TestDataBuilder.CreateLocationEntity(id: 0);
            locationEntity.Latitude = 0;
            locationEntity.Longitude = 0; // This represents Null Island (0,0)

            // Act & Assert
            Func<Task> act = async () =>
            {
                // The LocationRepository.AddAsync checks validation rules
                var location = new Domain.Entities.Location(
                    locationEntity.Title,
                    locationEntity.Description,
                    new Coordinate(0, 0), // Create invalid coordinates
                    new Address(locationEntity.City, locationEntity.State)
                );
                await _repository.AddAsync(location);
            };

            // The validation should fail in LocationRepository.AddAsync method
            act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*Null Island*");
        }

        [Test]
        public void Constructor_WithNullContext_ShouldThrowException()
        {
            // Act
            Action act = () => new LocationRepository(null!, _mockLogger.Object, _mockInfraLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new LocationRepository(_context, null!, _mockInfraLogger.Object  );

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }
    }
}
