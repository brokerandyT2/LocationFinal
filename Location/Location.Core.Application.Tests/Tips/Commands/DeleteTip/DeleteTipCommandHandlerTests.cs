using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Settings.Commands.CreateSetting;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Settings.Commands.CreateSetting
{
    [Category("Tips")]
    [Category("Delete")]
    [TestFixture]
    public class CreateSettingCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ISettingRepository> _settingRepositoryMock;
        private CreateSettingCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _settingRepositoryMock = new Mock<ISettingRepository>();

            _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingRepositoryMock.Object);

            _handler = new CreateSettingCommandHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => new CreateSettingCommandHandler(null));
        }

        [Test]
        public async Task Handle_WithValidData_ShouldCreateSetting()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "WeatherApiKey",
                Value = "test-api-key-123",
                Description = "API key for weather service"
            };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Not found"));

            var createdSetting = new Domain.Entities.Setting(
                command.Key,
                command.Value,
                command.Description);

            _settingRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(createdSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Key.Should().Be(command.Key);
            result.Data.Value.Should().Be(command.Value);
            result.Data.Description.Should().Be(command.Description);

            _settingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExistingKey_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "WeatherApiKey",
                Value = "new-api-key",
                Description = "Updated API key"
            };

            var existingSetting = new Domain.Entities.Setting(
                command.Key,
                "existing-value",
                "Existing description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be($"Setting with key '{command.Key}' already exists");

            _settingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithMinimalData_ShouldCreateSetting()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "SimpleKey",
                Value = "SimpleValue",
                Description = "" // Empty description
            };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Not found"));

            var createdSetting = new Domain.Entities.Setting(
                command.Key,
                command.Value,
                command.Description);

            _settingRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(createdSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Key.Should().Be(command.Key);
            result.Data.Value.Should().Be(command.Value);
            result.Data.Description.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WhenCreateFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "TestKey",
                Value = "TestValue",
                Description = "Test Description"
            };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Not found"));

            _settingRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Database error");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "TestKey",
                Value = "TestValue",
                Description = "Test Description"
            };

            var cancellationToken = new CancellationToken();

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Not found"));

            var createdSetting = TestDataBuilder.CreateValidSetting();

            _settingRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(createdSetting));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(command.Key, cancellationToken), Times.Once);
            _settingRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldReturnCreatedSettingId()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "TestKey",
                Value = "TestValue",
                Description = "Test Description"
            };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Not found"));

            var createdSetting = TestDataBuilder.CreateValidSetting();
            SetPrivateProperty(createdSetting, "Id", 42);

            _settingRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(createdSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Id.Should().Be(42);
        }

        [Test]
        public async Task Handle_ShouldSetTimestamp()
        {
            // Arrange
            var command = new CreateSettingCommand
            {
                Key = "TestKey",
                Value = "TestValue",
                Description = "Test Description"
            };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Not found"));

            var createdSetting = new Domain.Entities.Setting(
                command.Key,
                command.Value,
                command.Description);

            _settingRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(createdSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            property?.SetValue(obj, value);
        }
    }
}