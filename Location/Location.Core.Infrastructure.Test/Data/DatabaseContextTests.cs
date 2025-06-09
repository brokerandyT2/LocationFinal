using FluentAssertions;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Location.Core.Infrastructure.Test.Data
{
    [TestFixture]
    public class DatabaseContextTests
    {
        private DatabaseContext _context;
        private Mock<ILogger<DatabaseContext>> _mockLogger;
        private string _testDbPath;

        [SetUp]
        public async Task Setup()
        {
            _mockLogger = new Mock<ILogger<DatabaseContext>>();
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            _context = new DatabaseContext(_mockLogger.Object, _testDbPath);
            await _context.InitializeDatabaseAsync();
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
        public async Task InitializeDatabaseAsync_ShouldCreateTablesSuccessfully()
        {
            // Arrange & Act - initialization happens in SetUp
            var connection = _context.GetConnection();

            // Assert
            connection.Should().NotBeNull();

            // Verify tables exist by trying to query them
            var locationCount = await _context.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='LocationEntity'");
            locationCount.Should().Be(1);
        }

        [Test]
        public async Task InsertAsync_WithValidEntity_ShouldPersist()
        {
            // Arrange
            var locationEntity = TestDataBuilder.CreateLocationEntity(id: 0);

            // Act
            var result = await _context.InsertAsync(locationEntity);

            // Assert
            result.Should().BeGreaterThan(0);
            locationEntity.Id.Should().BeGreaterThan(0);

            // Verify persistence
            var retrieved = await _context.GetAsync<LocationEntity>(locationEntity.Id);
            retrieved.Should().NotBeNull();
            retrieved.Title.Should().Be(locationEntity.Title);
        }

        [Test]
        public async Task UpdateAsync_WithExistingEntity_ShouldModify()
        {
            // Arrange
            var locationEntity = TestDataBuilder.CreateLocationEntity(id: 0);
            await _context.InsertAsync(locationEntity);

            locationEntity.Title = "Updated Title";

            // Act
            var result = await _context.UpdateAsync(locationEntity);

            // Assert
            result.Should().Be(1);

            var retrieved = await _context.GetAsync<LocationEntity>(locationEntity.Id);
            retrieved.Title.Should().Be("Updated Title");
        }

        [Test]
        public async Task DeleteAsync_WithExistingEntity_ShouldRemove()
        {
            // Arrange
            var locationEntity = TestDataBuilder.CreateLocationEntity(id: 0);
            await _context.InsertAsync(locationEntity);

            // Act
            var result = await _context.DeleteAsync(locationEntity);

            // Assert
            result.Should().Be(1);

            // Verify deletion
            Func<Task> act = async () => await _context.GetAsync<LocationEntity>(locationEntity.Id);
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [Test]
        public async Task GetAllAsync_WithMultipleEntities_ShouldReturnAll()
        {
            // Arrange
            var entity1 = TestDataBuilder.CreateLocationEntity(id: 0, title: "Location 1");
            var entity2 = TestDataBuilder.CreateLocationEntity(id: 0, title: "Location 2");

            await _context.InsertAsync(entity1);
            await _context.InsertAsync(entity2);

            // Act
            var results = await _context.GetAllAsync<LocationEntity>();

            // Assert
            results.Should().HaveCount(2);
            results.Should().Contain(l => l.Title == "Location 1");
            results.Should().Contain(l => l.Title == "Location 2");
        }

        [Test]
        public async Task BulkInsertAsync_WithMultipleEntities_ShouldInsertAll()
        {
            // Arrange
            var entities = new[]
            {
                TestDataBuilder.CreateLocationEntity(id: 0, title: "Bulk 1"),
                TestDataBuilder.CreateLocationEntity(id: 0, title: "Bulk 2"),
                TestDataBuilder.CreateLocationEntity(id: 0, title: "Bulk 3")
            };

            // Act
            var result = await _context.BulkInsertAsync(entities);

            // Assert
            result.Should().Be(3);

            var allEntities = await _context.GetAllAsync<LocationEntity>();
            allEntities.Should().HaveCount(3);
        }

        [Test]
        public async Task ExecuteInTransactionAsync_WithSuccess_ShouldCommit()
        {
            // Arrange
            var entity1 = TestDataBuilder.CreateLocationEntity(id: 0, title: "Transaction 1");
            var entity2 = TestDataBuilder.CreateLocationEntity(id: 0, title: "Transaction 2");

            // Act
            var result = await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.InsertAsync(entity1);
                await _context.InsertAsync(entity2);
                return 2;
            });

            // Assert
            result.Should().Be(2);

            var allEntities = await _context.GetAllAsync<LocationEntity>();
            allEntities.Should().HaveCount(2);
        }

        [Test]
        public async Task ExecuteInTransactionAsync_WithException_ShouldRollback()
        {
            // Arrange
            var entity1 = TestDataBuilder.CreateLocationEntity(id: 0, title: "Rollback 1");

            // Act
            Func<Task> act = async () => await _context.ExecuteInTransactionAsync(async () =>
            {
                await _context.InsertAsync(entity1);
                throw new InvalidOperationException("Test exception");
            });

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>();

            var allEntities = await _context.GetAllAsync<LocationEntity>();
            allEntities.Should().BeEmpty();
        }

        [Test]
        public async Task QueryAsync_WithValidSql_ShouldReturnResults()
        {
            // Arrange
            var entity = TestDataBuilder.CreateLocationEntity(id: 0, title: "Query Test");
            await _context.InsertAsync(entity);

            // Act
            var results = await _context.QueryAsync<LocationEntity>("SELECT * FROM LocationEntity WHERE Title = ?", "Query Test");

            // Assert
            results.Should().HaveCount(1);
            results[0].Title.Should().Be("Query Test");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new DatabaseContext(null!, _testDbPath);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public void GetConnection_WithoutInitialization_ShouldThrowException()
        {
            // Arrange
            var uninitializedContext = new DatabaseContext(_mockLogger.Object, _testDbPath);

            // Act
            Action act = () => uninitializedContext.GetConnection();

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*initialized*");

            uninitializedContext.Dispose();
        }
    }
}