using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Settings.Commands.DeleteSetting;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Moq;
using NUnit.Framework;

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

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _settingRepositoryMock = new Mock<ISettingRepository>();

            _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingRepositoryMock.Object);

            _handler = new DeleteSettingCommandHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => new DeleteSettingCommandHandler(null));
        }

        [Test]
        public async Task Handle_WithValidKey_ShouldDeleteSetting()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "WeatherApiKey" };

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _settingRepositoryMock.Verify(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentKey_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "NonExistentKey" };

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Setting not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Setting not found");

            _settingRepositoryMock.Verify(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithEmptyKey_ShouldAttemptDelete()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = string.Empty };

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Invalid key"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid key");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "TestKey" };
            var cancellationToken = new CancellationToken();

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, cancellationToken))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _settingRepositoryMock.Verify(x => x.DeleteAsync(command.Key, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "TestKey" };

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Failed to delete setting"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to delete setting");
        }

        [Test]
        public async Task Handle_ShouldReturnResultFromRepository()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "TestKey" };
            var repositoryResult = Result<bool>.Success(true);

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(repositoryResult);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.Should().Be(repositoryResult);
        }

        [Test]
        public async Task Handle_WithSpecialCharacterKey_ShouldAttemptDelete()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "Key@With#Special$Chars%" };

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _settingRepositoryMock.Verify(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithLongKey_ShouldAttemptDelete()
        {
            // Arrange
            var longKey = new string('a', 255);
            var command = new DeleteSettingCommand { Key = longKey };

            _settingRepositoryMock
                .Setup(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _settingRepositoryMock.Verify(x => x.DeleteAsync(longKey, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_MultipleCallsForSameKey_ShouldEachAttemptDelete()
        {
            // Arrange
            var command = new DeleteSettingCommand { Key = "TestKey" };

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

            _settingRepositoryMock.Verify(x => x.DeleteAsync(command.Key, It.IsAny<CancellationToken>()), Times.Exactly(2));
        }
    }
}