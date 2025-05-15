using NUnit.Framework;
using FluentAssertions;
using Location.Core.Infrastructure.Data;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Tests.Helpers;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace Location.Core.Infrastructure.Tests.Data.Repositories
{
    [TestFixture]
    public class TipTypeRepositoryTests
    {
        private TipTypeRepository _repository;
        private DatabaseContext _context;
        private Mock<ILogger<TipTypeRepository>> _mockLogger;
        private Mock<ILogger<DatabaseContext>> _mockContextLogger;
        private string _testDbPath;
        [SetUp]
        public async Task Setup()
        {
            _mockLogger = new Mock<ILogger<TipTypeRepository>>();
            _mockContextLogger = new Mock<ILogger<DatabaseContext>>();
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            _context = new DatabaseContext(_mockContextLogger.Object, _testDbPath);
            await _context.InitializeDatabaseAsync();
            _repository = new TipTypeRepository(_context, _mockLogger.Object);
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
        public async Task GetByIdAsync_WithExistingTipType_ShouldReturnTipType()
        {
            // Arrange
            var tipTypeEntity = TestDataBuilder.CreateTipTypeEntity();
            await _context.InsertAsync(tipTypeEntity);

            // Act
            var result = await _repository.GetByIdAsync(tipTypeEntity.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(tipTypeEntity.Id);
            result.Name.Should().Be(tipTypeEntity.Name);
            result.I8n.Should().Be(tipTypeEntity.I8n);
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingTipType_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetAllAsync_WithMultipleTipTypes_ShouldReturnAll()
        {
            // Arrange
            var tipType1 = TestDataBuilder.CreateTipTypeEntity(id: 0, name: "Landscape");
            var tipType2 = TestDataBuilder.CreateTipTypeEntity(id: 0, name: "Portrait");
            var tipType3 = TestDataBuilder.CreateTipTypeEntity(id: 0, name: "Wildlife");

            await _context.InsertAsync(tipType1);
            await _context.InsertAsync(tipType2);
            await _context.InsertAsync(tipType3);

            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            var tipTypeList = results.ToList();
            tipTypeList.Should().HaveCount(3);
            tipTypeList.Select(t => t.Name).Should().Contain(new[] { "Landscape", "Portrait", "Wildlife" });
        }

        [Test]
        public async Task AddAsync_WithValidTipType_ShouldPersistAndReturnWithId()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();

            // Act
            var result = await _repository.AddAsync(tipType);

            // Assert
            result.Should().BeSameAs(tipType);
            result.Id.Should().BeGreaterThan(0);

            // Verify persistence
            var retrieved = await _repository.GetByIdAsync(result.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be(tipType.Name);
        }

        [Test]
        public async Task Update_WithExistingTipType_ShouldPersistChanges()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType(name: "Original Name");
            await _repository.AddAsync(tipType);

            // Act
            // Use reflection to update the name (since it's private set)
            var nameProperty = tipType.GetType().GetProperty("Name");
            nameProperty!.SetValue(tipType, "Updated Name");
            tipType.SetLocalization("fr-FR");
            _repository.Update(tipType);

            // Assert
            var retrieved = await _repository.GetByIdAsync(tipType.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Name.Should().Be("Updated Name");
            retrieved.I8n.Should().Be("fr-FR");
        }

        [Test]
        public async Task Delete_WithExistingTipType_ShouldRemove()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            await _repository.AddAsync(tipType);

            // Act
            _repository.Delete(tipType);

            // Assert
            var retrieved = await _repository.GetByIdAsync(tipType.Id);
            retrieved.Should().BeNull();
        }

        [Test]
        public async Task GetByNameAsync_WithExistingName_ShouldReturnTipType()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType(name: "Unique Type Name");
            await _repository.AddAsync(tipType);

            // Act
            var result = await _repository.GetByNameAsync("Unique Type Name");

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Unique Type Name");
        }

        [Test]
        public async Task GetByNameAsync_WithNonExistingName_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByNameAsync("Non-Existent Name");

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetWithTipsAsync_WithTipTypeAndRelatedTips_ShouldReturnTipTypeWithTips()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType(name: "Photography Tips");
            await _repository.AddAsync(tipType);

            var tip1 = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: tipType.Id, title: "Tip 1");
            var tip2 = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: tipType.Id, title: "Tip 2");
            var tip3 = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: tipType.Id, title: "Tip 3");

            await _context.InsertAsync(tip1);
            await _context.InsertAsync(tip2);
            await _context.InsertAsync(tip3);

            // Act
            var result = await _repository.GetWithTipsAsync(tipType.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Photography Tips");
            result.Tips.Should().HaveCount(3);
            result.Tips.Select(t => t.Title).Should().Contain(new[] { "Tip 1", "Tip 2", "Tip 3" });
        }

        [Test]
        public async Task GetWithTipsAsync_WithTipTypeButNoTips_ShouldReturnTipTypeWithEmptyCollection()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType(name: "Empty Type");
            await _repository.AddAsync(tipType);

            // Act
            var result = await _repository.GetWithTipsAsync(tipType.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Empty Type");
            result.Tips.Should().BeEmpty();
        }

        [Test]
        public async Task GetWithTipsAsync_WithNonExistingTipType_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetWithTipsAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public void Constructor_WithNullContext_ShouldThrowException()
        {
            // Act
            Action act = () => new TipTypeRepository(null!, _mockLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new TipTypeRepository(_context, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public async Task AddTip_ToExistingTipType_ShouldAddToCollection()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            await _repository.AddAsync(tipType);

            // Create and save the tip to the database first
            var tipEntity = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: tipType.Id, title: "New Tip");
            await _context.InsertAsync(tipEntity);

            // Need to retrieve the tip type with its tips using GetWithTipsAsync
            var tipTypeWithTips = await _repository.GetWithTipsAsync(tipType.Id);
            tipTypeWithTips.Should().NotBeNull();

            // Act - Now add another tip
            var tip = TestDataBuilder.CreateValidTip(tipTypeId: tipType.Id, title: "Another Tip");

            // Set the ID using reflection since it's protected
            var idProperty = tip.GetType().GetProperty("Id");
            idProperty!.SetValue(tip, 2); // Give it a different ID

            tipTypeWithTips!.AddTip(tip);
            _repository.Update(tipTypeWithTips);

            // Assert
            var retrieved = await _repository.GetWithTipsAsync(tipType.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Tips.Should().HaveCount(2); // Should now have both tips
        }

        [Test]
        public async Task RemoveTip_FromExistingTipType_ShouldRemoveFromCollection()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            await _repository.AddAsync(tipType);

            var tip = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: tipType.Id);
            await _context.InsertAsync(tip);

            var loadedTipType = await _repository.GetWithTipsAsync(tipType.Id);
            loadedTipType.Should().NotBeNull();
            loadedTipType!.Tips.Should().HaveCount(1);

            var tipToRemove = loadedTipType.Tips.First();

            // Act
            loadedTipType.RemoveTip(tipToRemove);
            _repository.Update(loadedTipType);

            // Need to save the updated state to database
            await _context.UpdateAsync(TestDataBuilder.CreateTipTypeEntity(
                id: loadedTipType.Id,
                name: loadedTipType.Name
            ));

            // Assert - Get fresh from database
            var retrieved = await _repository.GetWithTipsAsync(tipType.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Tips.Should().BeEmpty();
        }

        [Test]
        public async Task AddAsync_VerifyLogging()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();

            // Act
            await _repository.AddAsync(tipType);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    It.Is<LogLevel>(level => level == LogLevel.Information),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()
                ),
                Times.AtLeastOnce
            );
        }

        [Test]
        public async Task GetAllAsync_WithEmptyDatabase_ShouldReturnEmptyCollection()
        {
            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            results.Should().BeEmpty();
        }
    }
}
