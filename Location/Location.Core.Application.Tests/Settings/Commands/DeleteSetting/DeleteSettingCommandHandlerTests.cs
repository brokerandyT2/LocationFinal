using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Settings.Commands.DeleteSetting;
using MediatR;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Tests.Settings.Commands.DeleteSetting
{
    [Category("Setting")]
    [Category("Delete")]
    [TestFixture]
    public class DeleteSettingCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ISettingRepository> _settingRepositoryMock;
        private DeleteSettingCommandHandler _handler;
        private Mock<IMediator> _mediatorMock;

        [SetUp]
        public void SetUp()
        {
            _mediatorMock = new Mock<IMediator>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _settingRepositoryMock = new Mock<ISettingRepository>();

            _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingRepositoryMock.Object);

            _handler = new DeleteSettingCommandHandler(_unitOfWorkMock.Object, _mediatorMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => new DeleteSettingCommandHandler(null, _mediatorMock.Object));
        }

        [Test]
        public async Task Handle_WithValidKey_ShouldDeleteSetting()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "WeatherApiKey" };
            var setting = new Domain.Entities.Setting("WeatherApiKey", "value", "description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()), Times.Once);
            _settingRepositoryMock.Verify(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentKey_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "NonExistentKey" };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Setting with key 'NonExistentKey' not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Setting with key 'NonExistentKey' not found");

            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()), Times.Once);
            _settingRepositoryMock.Verify(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithEmptyKey_ShouldAttemptDelete()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = string.Empty };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Setting with key '' not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Setting with key '' not found");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "TestKey" };
            var setting = new Domain.Entities.Setting("TestKey", "value", "description");
            var cancellationToken = new CancellationToken();

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, cancellationToken))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(command.Key, cancellationToken), Times.Once);
            _settingRepositoryMock.Verify(x => x.DeleteAsync(command.Key, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "TestKey" };
            var setting = new Domain.Entities.Setting("TestKey", "value", "description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Failed to delete setting"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to delete setting");
        }

        [Test]
        public async Task Handle_ShouldReturnResultFromRepository()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "TestKey" };
            var setting = new Domain.Entities.Setting("TestKey", "value", "description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task Handle_WithSpecialCharacterKey_ShouldAttemptDelete()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "Key@With#Special$Chars%" };
            var setting = new Domain.Entities.Setting(command.Key, "value", "description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
        }

        [Test]
        public async Task Handle_WithLongKey_ShouldAttemptDelete()
        {
            // Arrange
            var longKey = new string('a', 255);
            var command = new DeleteSettingCommand { Key = longKey };
            var setting = new Domain.Entities.Setting(longKey, "value", "description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task Handle_MultipleCallsForSameKey_ShouldEachAttemptDelete()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "TestKey" };
            var setting = new Domain.Entities.Setting("TestKey", "value", "description");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            _settingRepositoryMock
                .SetupSequence(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true))
                .ReturnsAsync(Result<bool>.Failure("Setting not found"));

            // Act
            var result1 = await _handler.Handle(command, CancellationToken.None);
            var result2 = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result1.IsSuccess.Should().BeTrue();
            result2.IsSuccess.Should().BeFalse();
        }
    }
}