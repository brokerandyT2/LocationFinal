using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Settings.Commands.UpdateSetting
{
    [Category("Settings")]
    [Category("Update")]
    [TestFixture]
    public class UpdateSettingCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ISettingRepository> _settingRepositoryMock;
        private Mock<IMediator> _mediatorMock;
        private UpdateSettingCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _settingRepositoryMock = new Mock<ISettingRepository>();
            _mediatorMock = new Mock<IMediator>();

            _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingRepositoryMock.Object);

            _handler = new UpdateSettingCommandHandler(_unitOfWorkMock.Object, _mediatorMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => new UpdateSettingCommandHandler(null, _mediatorMock.Object));
        }

        [Test]
        public async Task Handle_WithValidUpdate_ShouldUpdateSetting()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "WeatherApiKey",
                Value = "updated-api-key-123",
                Description = "Updated weather API key"
            };

            var existingSetting = new Domain.Entities.Setting(
                command.Key,
                "old-api-key",
                "Old description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            _settingRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Key.Should().Be(command.Key);
            result.Data.Value.Should().Be(command.Value);

            _settingRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentKey_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "NonExistentKey",
                Value = "new-value",
                Description = "This key doesn't exist"
            };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Setting not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be($"Setting with key '{command.Key}' not found");

            _settingRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithValueOnly_ShouldUpdateValue()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "SimpleKey",
                Value = "UpdatedValue"
                // Description is null
            };

            var existingSetting = new Domain.Entities.Setting(
                command.Key,
                "OldValue",
                "Existing description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            _settingRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Value.Should().Be(command.Value);
            // Description should remain unchanged
            result.Data.Description.Should().Be("Existing description");
        }

        [Test]
        public async Task Handle_WhenUpdateFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "TestKey",
                Value = "TestValue"
            };

            var existingSetting = TestDataBuilder.CreateValidSetting();

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            _settingRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Failed to update setting"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to update setting");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "TestKey",
                Value = "TestValue"
            };

            var existingSetting = TestDataBuilder.CreateValidSetting();
            var cancellationToken = new CancellationToken();

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            _settingRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(command.Key, cancellationToken), Times.Once);
            _settingRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldCallUpdateValueOnDomain()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "TestKey",
                Value = "NewValue"
            };

            var existingSetting = TestDataBuilder.CreateValidSetting();
            Domain.Entities.Setting capturedSetting = null;

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            _settingRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .Callback<Domain.Entities.Setting, CancellationToken>((s, ct) => capturedSetting = s)
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedSetting.Should().NotBeNull();
            capturedSetting.Value.Should().Be(command.Value);
        }

        [Test]
        public async Task Handle_ShouldReturnUpdatedTimestamp()
        {
            // Arrange
            var command = new UpdateSettingCommand
            {
                Key = "TestKey",
                Value = "UpdatedValue"
            };

            var existingSetting = new Domain.Entities.Setting(
                command.Key,
                "OldValue",
                "Description");
            var oldTimestamp = existingSetting.Timestamp;

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            _settingRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Setting>(), It.IsAny<CancellationToken>()))
                .Callback<Domain.Entities.Setting, CancellationToken>((s, ct) =>
                {
                    s.UpdateValue(command.Value);
                })
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(existingSetting));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Timestamp.Should().BeAfter(oldTimestamp);
        }
    }
}