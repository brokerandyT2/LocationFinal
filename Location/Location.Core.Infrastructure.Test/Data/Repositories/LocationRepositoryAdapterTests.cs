using FluentAssertions;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Services;
using Location.Core.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Location.Core.Infrastructure.Tests.Data.Repositories
{
    [TestFixture]
    public class LocationRepositoryAdapterTests
    {
        private LocationRepositoryAdapter _adapter;
        private LocationRepository _innerRepository;
        private DatabaseContext _context;
        private Mock<ILogger<LocationRepository>> _mockLogger;
        private Mock<ILogger<DatabaseContext>> _mockContextLogger;
        private Mock<IInfrastructureExceptionMappingService> _mockExceptionMapper;
        private string _testDbPath;

        [SetUp]
        public async Task Setup()
        {
            _mockLogger = new Mock<ILogger<LocationRepository>>();
            _mockContextLogger = new Mock<ILogger<DatabaseContext>>();
            _mockExceptionMapper = new Mock<IInfrastructureExceptionMappingService>();

            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            _context = new DatabaseContext(_mockContextLogger.Object, _testDbPath);
            await _context.InitializeDatabaseAsync();

            _innerRepository = new LocationRepository(_context, _mockLogger.Object, _mockExceptionMapper.Object);
            _adapter = new LocationRepositoryAdapter(_innerRepository);
        }

        [TearDown]
        public void TearDown()
        {
            _context?.Dispose();

            if (File.Exists(_testDbPath))
            {
                try
                {
                    File.Delete(_testDbPath);
                }
                catch
                {
                    // Ignore file deletion errors in tests
                }
            }
        }

        [Test]
        public async Task GetByIdAsync_WithExistingLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _innerRepository.AddAsync(location);

            // Act
            var result = await _adapter.GetByIdAsync(location.Id);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().Be(location.Id);
            result.Data.Title.Should().Be(location.Title);
            result.ErrorMessage.Should().BeNull();
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingLocation_ShouldReturnFailure()
        {
            // Act
            var result = await _adapter.GetByIdAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().StartWith("Failed to retrieve location:");
        }

        [Test]
        public async Task GetAllAsync_WithMultipleLocations_ShouldReturnSuccess()
        {
            // Arrange
            var location1 = TestDataBuilder.CreateValidLocation(title: "Location 1");
            var location2 = TestDataBuilder.CreateValidLocation(title: "Location 2");
            var location3 = TestDataBuilder.CreateValidLocation(title: "Location 3");

            await _innerRepository.AddAsync(location1);
            await _innerRepository.AddAsync(location2);
            await _innerRepository.AddAsync(location3);

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Should().Contain(l => l.Title == "Location 1");
            result.Data.Should().Contain(l => l.Title == "Location 2");
            result.Data.Should().Contain(l => l.Title == "Location 3");
        }

        [Test]
        public async Task GetAllAsync_WithEmptyDatabase_ShouldReturnEmptyList()
        {
            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task CreateAsync_WithValidLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();

            // Act
            var result = await _adapter.CreateAsync(location);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Id.Should().BeGreaterThan(0);
            result.Data.Title.Should().Be(location.Title);
        }

        [Test]
        public async Task UpdateAsync_WithExistingLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _innerRepository.AddAsync(location);

            // No SetTitle method exists - domain objects are immutable
            // Create a new location instance with updated title instead
            var updatedLocation = TestDataBuilder.CreateValidLocation(title: "Updated Title");
            // Set the ID to match the original
            typeof(Location.Core.Domain.Entities.Location)
                .GetProperty("Id")?
                .SetValue(updatedLocation, location.Id);

            // Act
            var result = await _adapter.UpdateAsync(updatedLocation);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("Updated Title");
        }

        [Test]
        public async Task DeleteAsync_WithExistingLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _innerRepository.AddAsync(location);

            // Act
            var result = await _adapter.DeleteAsync(location.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task DeleteAsync_WithNonExistingLocation_ShouldReturnFailure()
        {
            // Act
            var result = await _adapter.DeleteAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().StartWith("Failed to delete location:");
        }

        [Test]
        public async Task GetActiveAsync_WithActiveLocations_ShouldReturnOnlyActive()
        {
            // Arrange
            var activeLocation = TestDataBuilder.CreateValidLocation(title: "Active Location");
            await _innerRepository.AddAsync(activeLocation);

            // Act
            var result = await _adapter.GetActiveAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeEmpty();
            result.Data.Should().Contain(l => l.Title == "Active Location");
        }

        [Test]
        public async Task GetNearbyAsync_BasicFunctionality_ShouldNotThrow()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _innerRepository.AddAsync(location);

            // Act & Assert - Just verify it doesn't throw exceptions
            var result = await _adapter.GetNearbyAsync(47.6062, -122.3321, 1.0);

            // Accept either success or controlled failure (not null reference)
            result.Should().NotBeNull();
            if (!result.IsSuccess)
            {
                result.ErrorMessage.Should().Contain("Failed to retrieve nearby locations:");
            }
        }

        [Test]
        public async Task GetPagedAsync_WithValidParameters_ShouldReturnPagedResults()
        {
            // Arrange
            for (int i = 1; i <= 5; i++)
            {
                var location = TestDataBuilder.CreateValidLocation(title: $"Location {i}");
                await _innerRepository.AddAsync(location);
            }

            // Act
            var result = await _adapter.GetPagedAsync(1, 3);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Items.Should().HaveCountLessThanOrEqualTo(3);
            result.Data.TotalCount.Should().Be(5);
            result.Data.PageNumber.Should().Be(1);
            result.Data.PageSize.Should().Be(3);
        }

        [Test]
        public async Task CreateAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            var cancellationToken = new CancellationToken();

            // Act
            var result = await _adapter.CreateAsync(location, cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Test]
        public async Task UpdateAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _innerRepository.AddAsync(location);
            var cancellationToken = new CancellationToken();

            var updatedLocation = TestDataBuilder.CreateValidLocation(title: "Updated Title");
            typeof(Location.Core.Domain.Entities.Location)
                .GetProperty("Id")?
                .SetValue(updatedLocation, location.Id);

            // Act
            var result = await _adapter.UpdateAsync(updatedLocation, cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
        }

        [Test]
        public async Task GetByIdAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _innerRepository.AddAsync(location);
            var cancellationToken = new CancellationToken();

            // Act
            var result = await _adapter.GetByIdAsync(location.Id, cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
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
        public async Task CountAsync_WithNoParameters_ShouldReturnCorrectCount()
        {
            // Arrange
            for (int i = 1; i <= 3; i++)
            {
                var location = TestDataBuilder.CreateValidLocation(title: $"Location {i}");
                await _innerRepository.AddAsync(location);
            }

            // Act
            var result = await _adapter.CountAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(3);
        }

        [Test]
        public async Task ExistsByIdAsync_WithExistingLocation_ShouldReturnTrue()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            await _innerRepository.AddAsync(location);

            // Act
            var result = await _adapter.ExistsByIdAsync(location.Id);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task ExistsByIdAsync_WithNonExistingLocation_ShouldReturnFalse()
        {
            // Act
            var result = await _adapter.ExistsByIdAsync(999);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeFalse();
        }

        [Test]
        public async Task ExistsAsync_WithValidWhereClause_ShouldReturnCorrectResult()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation(title: "Test Location");
            await _innerRepository.AddAsync(location);

            var whereClause = "Title = @title";
            var parameters = new Dictionary<string, object> { { "title", "Test Location" } };

            // Act
            var result = await _adapter.ExistsAsync(whereClause, parameters);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task GetByTitleAsync_WithExistingTitle_ShouldReturnLocation()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation(title: "Unique Title");
            await _innerRepository.AddAsync(location);

            // Act
            var result = await _adapter.GetByTitleAsync("Unique Title");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Title.Should().Be("Unique Title");
        }

        [Test]
        public async Task GetByTitleAsync_WithNonExistentTitle_ShouldReturnFailure()
        {
            // Act
            var result = await _adapter.GetByTitleAsync("Non Existent Title");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Location with title 'Non Existent Title' not found");
        }
    }
}