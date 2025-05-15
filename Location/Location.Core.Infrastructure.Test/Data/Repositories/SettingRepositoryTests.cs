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
    public class SettingRepositoryTests
    {
        private SettingRepository _repository;
        private DatabaseContext _context;
        private Mock<ILogger<SettingRepository>> _mockLogger;
        private Mock<ILogger<DatabaseContext>> _mockContextLogger;
        private string _testDbPath;
        [SetUp]
        public async Task Setup()
        {
            _mockLogger = new Mock<ILogger<SettingRepository>>();
            _mockContextLogger = new Mock<ILogger<DatabaseContext>>();
            _testDbPath = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid()}.db");
            _context = new DatabaseContext(_mockContextLogger.Object, _testDbPath);
            await _context.InitializeDatabaseAsync();
            _repository = new SettingRepository(_context, _mockLogger.Object);
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
        public async Task GetByIdAsync_WithExistingSetting_ShouldReturnSetting()
        {
            // Arrange
            var settingEntity = TestDataBuilder.CreateSettingEntity();
            await _context.InsertAsync(settingEntity);

            // Act
            var result = await _repository.GetByIdAsync(settingEntity.Id);

            // Assert
            result.Should().NotBeNull();
            result!.Id.Should().Be(settingEntity.Id);
            result.Key.Should().Be(settingEntity.Key);
            result.Value.Should().Be(settingEntity.Value);
            result.Description.Should().Be(settingEntity.Description);
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingSetting_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetByKeyAsync_WithExistingKey_ShouldReturnSetting()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting(key: "unique_key");
            await _repository.AddAsync(setting);

            // Act
            var result = await _repository.GetByKeyAsync("unique_key");

            // Assert
            result.Should().NotBeNull();
            result!.Key.Should().Be("unique_key");
            result.Value.Should().Be(setting.Value);
        }

        [Test]
        public async Task GetByKeyAsync_WithNonExistingKey_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByKeyAsync("non_existent_key");

            // Assert
            result.Should().BeNull();
        }

        [Test]
        public async Task GetAllAsync_WithMultipleSettings_ShouldReturnAllOrderedByKey()
        {
            // Arrange
            var setting1 = TestDataBuilder.CreateSettingEntity(id: 0, key: "zzz_key");
            var setting2 = TestDataBuilder.CreateSettingEntity(id: 0, key: "aaa_key");
            var setting3 = TestDataBuilder.CreateSettingEntity(id: 0, key: "mmm_key");

            await _context.InsertAsync(setting1);
            await _context.InsertAsync(setting2);
            await _context.InsertAsync(setting3);

            // Act
            var results = await _repository.GetAllAsync();

            // Assert
            var settingList = results.ToList();
            settingList.Should().HaveCount(3);
            settingList[0].Key.Should().Be("aaa_key");
            settingList[1].Key.Should().Be("mmm_key");
            settingList[2].Key.Should().Be("zzz_key");
        }

        [Test]
        public async Task GetByKeysAsync_WithMatchingKeys_ShouldReturnFilteredSettings()
        {
            // Arrange
            var setting1 = TestDataBuilder.CreateValidSetting(key: "key1");
            var setting2 = TestDataBuilder.CreateValidSetting(key: "key2");
            var setting3 = TestDataBuilder.CreateValidSetting(key: "key3");
            var setting4 = TestDataBuilder.CreateValidSetting(key: "key4");

            await _repository.AddAsync(setting1);
            await _repository.AddAsync(setting2);
            await _repository.AddAsync(setting3);
            await _repository.AddAsync(setting4);

            var keysToFind = new[] { "key1", "key3" };

            // Act
            var results = await _repository.GetByKeysAsync(keysToFind);

            // Assert
            var settingList = results.ToList();
            settingList.Should().HaveCount(2);
            settingList.Select(s => s.Key).Should().Contain(keysToFind);
        }

        [Test]
        public async Task AddAsync_WithValidSetting_ShouldPersistAndReturnWithId()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();

            // Act
            var result = await _repository.AddAsync(setting);

            // Assert
            result.Should().BeSameAs(setting);
            result.Id.Should().BeGreaterThan(0);

            // Verify persistence
            var retrieved = await _repository.GetByIdAsync(result.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Key.Should().Be(setting.Key);
        }

        [Test]
        public async Task AddAsync_WithDuplicateKey_ShouldThrowException()
        {
            // Arrange
            var setting1 = TestDataBuilder.CreateValidSetting(key: "duplicate_key");
            var setting2 = TestDataBuilder.CreateValidSetting(key: "duplicate_key");

            // Act
            await _repository.AddAsync(setting1);

            // Act & Assert
            Func<Task> act = async () => await _repository.AddAsync(setting2);

            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("*duplicate_key*already exists*");
        }

        [Test]
        public async Task Update_WithExistingSetting_ShouldPersistChanges()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            await _repository.AddAsync(setting);
            var originalTimestamp = setting.Timestamp;

            // Act
            setting.UpdateValue("updated_value");
            _repository.Update(setting);

            // Assert
            var retrieved = await _repository.GetByIdAsync(setting.Id);
            retrieved.Should().NotBeNull();
            retrieved!.Value.Should().Be("updated_value");
            retrieved.Timestamp.Should().BeAfter(originalTimestamp);
        }

        [Test]
        public async Task Delete_WithExistingSetting_ShouldRemove()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            await _repository.AddAsync(setting);

            // Act
            _repository.Delete(setting);

            // Assert
            var retrieved = await _repository.GetByIdAsync(setting.Id);
            retrieved.Should().BeNull();
        }

        [Test]
        public async Task UpsertAsync_WithNewKey_ShouldCreateSetting()
        {
            // Arrange
            var key = "new_key";
            var value = "new_value";
            var description = "new description";

            // Act
            var result = await _repository.UpsertAsync(key, value, description);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().BeGreaterThan(0);
            result.Key.Should().Be(key);
            result.Value.Should().Be(value);
            result.Description.Should().Be(description);

            // Verify persistence
            var retrieved = await _repository.GetByKeyAsync(key);
            retrieved.Should().NotBeNull();
        }

        [Test]
        public async Task UpsertAsync_WithExistingKey_ShouldUpdateSetting()
        {
            // Arrange
            var key = "existing_key";
            var originalValue = "original_value";
            var updatedValue = "updated_value";
            var setting = TestDataBuilder.CreateValidSetting(key: key, value: originalValue);
            await _repository.AddAsync(setting);

            // Act
            var result = await _repository.UpsertAsync(key, updatedValue);

            // Assert
            result.Value.Should().Be(updatedValue);
            result.Key.Should().Be(key);

            // Verify persistence
            var retrieved = await _repository.GetByKeyAsync(key);
            retrieved!.Value.Should().Be(updatedValue);
        }

        [Test]
        public async Task UpsertAsync_WithNullDescription_ShouldUseEmptyString()
        {
            // Arrange
            var key = "key_without_description";
            var value = "value";

            // Act
            var result = await _repository.UpsertAsync(key, value, null);

            // Assert
            result.Description.Should().BeEmpty();
        }

        [Test]
        public void Constructor_WithNullContext_ShouldThrowException()
        {
            // Act
            Action act = () => new SettingRepository(null!, _mockLogger.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("context");
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowException()
        {
            // Act
            Action act = () => new SettingRepository(_context, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public async Task GetBooleanValue_WithValidBooleanString_ShouldReturnCorrectValue()
        {
            // Arrange
            var trueSetting = TestDataBuilder.CreateValidSetting(key: "bool_true", value: "true");
            var falseSetting = TestDataBuilder.CreateValidSetting(key: "bool_false", value: "false");
            var invalidSetting = TestDataBuilder.CreateValidSetting(key: "bool_invalid", value: "invalid");

            await _repository.AddAsync(trueSetting);
            await _repository.AddAsync(falseSetting);
            await _repository.AddAsync(invalidSetting);

            // Act & Assert
            trueSetting.GetBooleanValue().Should().BeTrue();
            falseSetting.GetBooleanValue().Should().BeFalse();
            invalidSetting.GetBooleanValue().Should().BeFalse();
        }

        [Test]
        public async Task GetIntValue_WithValidIntegerString_ShouldReturnCorrectValue()
        {
            // Arrange
            var intSetting = TestDataBuilder.CreateValidSetting(key: "int_setting", value: "42");
            var invalidSetting = TestDataBuilder.CreateValidSetting(key: "invalid_int", value: "invalid");

            await _repository.AddAsync(intSetting);
            await _repository.AddAsync(invalidSetting);

            // Act & Assert
            intSetting.GetIntValue().Should().Be(42);
            invalidSetting.GetIntValue(99).Should().Be(99);
        }

        [Test]
        public async Task GetDateTimeValue_WithValidDateTimeString_ShouldReturnCorrectValue()
        {
            // Arrange
            var dateTime = new DateTime(2024, 1, 15, 14, 30, 0);
            var dateSetting = TestDataBuilder.CreateValidSetting(key: "date_setting", value: dateTime.ToString());
            var invalidSetting = TestDataBuilder.CreateValidSetting(key: "invalid_date", value: "invalid");

            await _repository.AddAsync(dateSetting);
            await _repository.AddAsync(invalidSetting);

            // Act & Assert
            dateSetting.GetDateTimeValue().Should().Be(dateTime);
            invalidSetting.GetDateTimeValue().Should().BeNull();
        }

        [Test]
        public async Task AddAsync_VerifyLogging()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();

            // Act
            await _repository.AddAsync(setting);

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
