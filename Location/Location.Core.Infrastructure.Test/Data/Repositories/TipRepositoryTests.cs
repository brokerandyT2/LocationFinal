
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
    public class TipRepositoryTests
    {
        private TipRepository _repository;
        private DatabaseContext _context;
        private Mock<ILogger<TipRepository>> _mockLogger;
        private Mock<ILogger<DatabaseContext>> _mockContextLogger;
        private string _testDbPath;

        [SetUp]
        public async Task Setup()
        {
            _mockLogger = new Mock<ILogger<TipRepository>>();
            _mockContextLogger = new Mock<ILogger<DatabaseContext>>();
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            _context = new DatabaseContext(_mockContextLogger.Object, _testDbPath);
            await _context.InitializeDatabaseAsync();
            _repository = new TipRepository(_context, _mockLogger.Object);
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
        public async Task GetByIdAsync_WithExistingTip_ShouldReturnTip()
        {
            // Arrange
            var tipEntity = TestDataBuilder.CreateTipEntity();
            await _context.InsertAsync(tipEntity);

            // Act
            var result = await _repository.GetByIdAsync(tipEntity.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(tipEntity.Id);
            result.Title.Should().Be(tipEntity.Title);
            result.Content.Should().Be(tipEntity.Content);
            result.TipTypeId.Should().Be(tipEntity.TipTypeId);
            result.Fstop.Should().Be(tipEntity.Fstop);
            result.ShutterSpeed.Should().Be(tipEntity.ShutterSpeed);
            result.Iso.Should().Be(tipEntity.Iso);
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingTip_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetAllAsync_WithMultipleTips_ShouldReturnAll()
        {
            // Arrange
            var tip1 = TestDataBuilder.CreateTipEntity(id: 0, title: "Tip 1");
            var tip2 = TestDataBuilder.CreateTipEntity(id: 0, title: "Tip 2");
            var tip3 = TestDataBuilder.CreateTipEntity(id: 0, title: "Tip 3");

            await _context.InsertAsync(tip1);
            await _context.InsertAsync(tip2);
            await _context.InsertAsync(tip3);

            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            var tipList = results.ToList();
            tipList.Should().HaveCount(3);
            tipList.Select(t => t.Title).Should().Contain(new[] { "Tip 1", "Tip 2", "Tip 3" });
        }

        [Test]
        public async Task GetByTipTypeIdAsync_WithMatchingTips_ShouldReturnFilteredTips()
        {
            // Arrange
            var tipTypeId = 1;
            var tip1 = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: tipTypeId, title: "Type 1 Tip 1");
            var tip2 = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: tipTypeId, title: "Type 1 Tip 2");
            var tip3 = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: 2, title: "Type 2 Tip");

            await _context.InsertAsync(tip1);
            await _context.InsertAsync(tip2);
            await _context.InsertAsync(tip3);

            // Act
            var results = await _repository.GetByTipTypeIdAsync(tipTypeId);

            // Assert
            var tipList = results.ToList();
            tipList.Should().HaveCount(2);
            tipList.Should().OnlyContain(t => t.TipTypeId == tipTypeId);
        }

        [Test]
        public async Task AddAsync_WithValidTip_ShouldPersistAndReturnWithId()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();

            // Act
            var result = await _repository.AddAsync(tip);

            // Assert
            result.Should().BeSameAs(tip);
            result.Id.Should().BeGreaterThan(0);

            // Verify persistence
            var retrieved = await _repository.GetByIdAsync(result.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be(tip.Title);
        }

        [Test]
        public void Update_WithExistingTip_ShouldPersistChanges()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            _repository.AddAsync(tip).Wait();

            // Act
            tip.UpdateContent("Updated Title", "Updated Content");
            tip.UpdatePhotographySettings("f/4", "1/1000", "ISO 400");
            _repository.UpdateAsync(tip);

            // Assert
            var retrieved = _repository.GetByIdAsync(tip.Id).Result;
            retrieved.Should().NotBeNull();
            retrieved!.Title.Should().Be("Updated Title");
            retrieved.Content.Should().Be("Updated Content");
            retrieved.Fstop.Should().Be("f/4");
            retrieved.ShutterSpeed.Should().Be("1/1000");
            retrieved.Iso.Should().Be("ISO 400");
        }

        [Test]
        public void Delete_WithExistingTip_ShouldRemove()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            _repository.AddAsync(tip).Wait();

            // Act
            _repository.DeleteAsync(tip);

            // Assert
            var retrieved = _repository.GetByIdAsync(tip.Id).Result;
            retrieved.Should().BeNull();
        }

        [Test]
        public async Task GetByTitleAsync_WithExistingTitle_ShouldReturnTip()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip(title: "Unique Tip Title");
            await _repository.AddAsync(tip);

            // Act
            var result = await _repository.GetByTitleAsync("Unique Tip Title");

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Unique Tip Title");
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
        public async Task GetRandomByTypeAsync_WithMultipleTips_ShouldReturnRandomTip()
        {
            // Arrange
            var tipTypeId = 1;
            var tips = new[]
            {
                TestDataBuilder.CreateValidTip(tipTypeId: tipTypeId, title: "Tip 1"),
                TestDataBuilder.CreateValidTip(tipTypeId: tipTypeId, title: "Tip 2"),
                TestDataBuilder.CreateValidTip(tipTypeId: tipTypeId, title: "Tip 3")
            };

            foreach (var tip in tips)
            {
                await _repository.AddAsync(tip);
            }

            // Act - Get random tips multiple times
            var results = new[]
            {
                await _repository.GetRandomByTypeAsync(tipTypeId),
                await _repository.GetRandomByTypeAsync(tipTypeId),
                await _repository.GetRandomByTypeAsync(tipTypeId)
            };

            // Assert
            results.Should().AllSatisfy(r => r.Should().NotBeNull());
            results.Should().AllSatisfy(r => r!.TipTypeId.Should().Be(tipTypeId));
            results.Select(r => r!.Title).Should().Contain(t => tips.Any(tip => tip.Title == t));
        }

        [Test]
        public async Task GetRandomByTypeAsync_WithNoTipsForType_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetRandomByTypeAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetRandomByTypeAsync_WithSingleTip_ShouldReturnThatTip()
        {
            // Arrange
            var tipTypeId = 1;
            var tip = TestDataBuilder.CreateValidTip(tipTypeId: tipTypeId, title: "Only Tip");
            await _repository.AddAsync(tip);

            // Act
            var result = await _repository.GetRandomByTypeAsync(tipTypeId);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Only Tip");
        }

        [Test]
        public void Constructor_WithNullContext_ShouldThrowException()
        {
            // Act
            Action act = () => new TipRepository(null!, _mockLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new TipRepository(_context, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public async Task Update_WithLocalization_ShouldPersist()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            await _repository.AddAsync(tip);

            // Act
            tip.SetLocalization("es-ES");
            await _repository.UpdateAsync(tip);

            // Assert
            var retrieved = await _repository.GetByIdAsync(tip.Id);
            retrieved.Should().NotBeNull();
            retrieved!.I8n.Should().Be("es-ES");
        }

        [Test]
        public async Task GetByTipTypeIdAsync_WithNoMatchingTips_ShouldReturnEmpty()
        {
            // Arrange
            var tip = TestDataBuilder.CreateTipEntity(id: 0, tipTypeId: 1);
            await _context.InsertAsync(tip);

            // Act
            var results = await _repository.GetByTipTypeIdAsync(999); // Non-existent type

            // Assert
            results.Should().BeEmpty();
        }

        [Test]
        public async Task AddAsync_VerifyLogging()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();

            // Act
            await _repository.AddAsync(tip);

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
    }
}