
using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
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
    public class SettingRepositoryAdapterTests
    {
        private SettingRepositoryAdapter _adapter;
        private Mock<ISettingRepository> _mockInnerRepository;
        [SetUp]
        public void Setup()
        {
            _mockInnerRepository = new Mock<ISettingRepository>();
            _adapter = new SettingRepositoryAdapter(_mockInnerRepository.Object);
        }

        [Test]
        public async Task GetByKeyAsync_WithExistingSetting_ShouldReturnSuccess()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting(key: "test_key");
            _mockInnerRepository.Setup(x => x.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(setting);

            // Act
            var result = await _adapter.GetByKeyAsync("test_key");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(setting);
            result.ErrorMessage.Should().BeNull();
        }

        [Test]
        public async Task GetByKeyAsync_WithNonExistingSetting_ShouldReturnFailure()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Setting?)null);

            // Act
            var result = await _adapter.GetByKeyAsync("non_existent_key");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("Setting with key 'non_existent_key' not found");
        }

        [Test]
        public async Task GetByKeyAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetByKeyAsync("test_key");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve setting: Database error");
        }

        [Test]
        public async Task GetAllAsync_WithMultipleSettings_ShouldReturnSuccess()
        {
            // Arrange
            var settings = new[]
            {
            TestDataBuilder.CreateValidSetting(key: "key1"),
            TestDataBuilder.CreateValidSetting(key: "key2"),
            TestDataBuilder.CreateValidSetting(key: "key3")
        };
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Should().BeEquivalentTo(settings);
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
            result.ErrorMessage.Should().Be("Failed to retrieve settings: Database error");
        }

        [Test]
        public async Task CreateAsync_WithValidSetting_ShouldReturnSuccess()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(setting);

            // Act
            var result = await _adapter.CreateAsync(setting);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(setting);
            _mockInnerRepository.Verify(x => x.AddAsync(setting, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Setting>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.CreateAsync(setting);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create setting: Database error");
        }

        [Test]
        public async Task UpdateAsync_WithValidSetting_ShouldReturnSuccess()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<Setting>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            var result = await _adapter.UpdateAsync(setting);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(setting);
            _mockInnerRepository.Verify(x => x.UpdateAsync(setting, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<Setting>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.UpdateAsync(setting);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to update setting: Database error");
        }

        [Test]
        public async Task DeleteAsync_WithExistingSetting_ShouldReturnSuccess()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting(key: "test_key");
            _mockInnerRepository.Setup(x => x.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(setting);
            _mockInnerRepository.Setup(x => x.DeleteAsync(It.IsAny<Setting>(), It.IsAny<CancellationToken>()))
                .Verifiable();

            // Act
            var result = await _adapter.DeleteAsync("test_key");

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockInnerRepository.Verify(x => x.DeleteAsync(setting, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_WithNonExistingSetting_ShouldReturnFailure()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Setting?)null);

            // Act
            var result = await _adapter.DeleteAsync("non_existent_key");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Setting with key 'non_existent_key' not found");
            _mockInnerRepository.Verify(x => x.DeleteAsync(It.IsAny<Setting>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task DeleteAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetByKeyAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.DeleteAsync("test_key");

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to delete setting: Database error");
        }

        [Test]
        public async Task UpsertAsync_WithValidSetting_ShouldReturnSuccess()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            var upsertedSetting = TestDataBuilder.CreateValidSetting(key: setting.Key, value: "new_value");
            _mockInnerRepository.Setup(x => x.UpsertAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(upsertedSetting);

            // Act
            var result = await _adapter.UpsertAsync(setting);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(upsertedSetting);
            _mockInnerRepository.Verify(x => x.UpsertAsync(
                setting.Key,
                setting.Value,
                setting.Description,
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpsertAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.UpsertAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string?>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.UpsertAsync(setting);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to upsert setting: Database error");
        }

        [Test]
        public async Task GetAllAsDictionaryAsync_WithSettings_ShouldReturnSuccess()
        {
            // Arrange
            var settings = new[]
            {
            TestDataBuilder.CreateValidSetting(key: "key1", value: "value1"),
            TestDataBuilder.CreateValidSetting(key: "key2", value: "value2"),
            TestDataBuilder.CreateValidSetting(key: "key3", value: "value3")
        };
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            // Act
            var result = await _adapter.GetAllAsDictionaryAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Should().ContainKey("key1").WhoseValue.Should().Be("value1");
            result.Data.Should().ContainKey("key2").WhoseValue.Should().Be("value2");
            result.Data.Should().ContainKey("key3").WhoseValue.Should().Be("value3");
        }

        [Test]
        public async Task GetAllAsDictionaryAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetAllAsDictionaryAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve settings as dictionary: Database error");
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowException()
        {
            // Act
            Action act = () => new SettingRepositoryAdapter(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("innerRepository");
        }

        [Test]
        public async Task GetAllAsync_WithEmptyResult_ShouldReturnEmptyList()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Setting>());

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task GetAllAsDictionaryAsync_WithEmptyResult_ShouldReturnEmptyDictionary()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Setting>());

            // Act
            var result = await _adapter.GetAllAsDictionaryAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task CreateAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var setting = TestDataBuilder.CreateValidSetting();
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(setting);

            // Act
            await _adapter.CreateAsync(setting, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.AddAsync(setting, cancellationToken), Times.Once);
        }

        [Test]
        public async Task GetAllAsDictionaryAsync_WithDuplicateKeys_ShouldReturnFailure()
        {
            // Arrange
            var settings = new[]
            {
            TestDataBuilder.CreateValidSetting(key: "duplicate", value: "value1"),
            TestDataBuilder.CreateValidSetting(key: "duplicate", value: "value2")
        };
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(settings);

            // Act
            var result = await _adapter.GetAllAsDictionaryAsync();

            // Assert - The implementation should wrap the exception in a Result
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve settings as dictionary");
        }
    }
}