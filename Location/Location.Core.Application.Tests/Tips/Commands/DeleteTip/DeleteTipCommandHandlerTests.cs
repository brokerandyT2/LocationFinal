using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Tips.Commands.DeleteTip;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Commands.DeleteTip
{
    [Category("Tips")]
    [Category("Delete")]
    [TestFixture]
    public class DeleteTipCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ITipRepository> _tipRepositoryMock;
        private Mock<IMediator> _mediatorMock;
        private DeleteTipCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tipRepositoryMock = new Mock<ITipRepository>();
            _mediatorMock = new Mock<IMediator>();

            _unitOfWorkMock.Setup(u => u.Tips).Returns(_tipRepositoryMock.Object);

            _handler = new DeleteTipCommandHandler(_unitOfWorkMock.Object, _mediatorMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new DeleteTipCommandHandler(null, _mediatorMock.Object))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithValidId_ShouldDeleteTip()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = 1 };
            var tip = TestDataBuilder.CreateValidTip(1);

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            _tipRepositoryMock
                .Setup(x => x.DeleteAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _tipRepositoryMock.Verify(x => x.DeleteAsync(command.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = 999 };

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Failure("Tip not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Tip not found");

            _tipRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WhenDeleteFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = 1 };
            var tip = TestDataBuilder.CreateValidTip(1);

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            _tipRepositoryMock
                .Setup(x => x.DeleteAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Failed to delete tip"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to delete tip");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = 1 };
            var tip = TestDataBuilder.CreateValidTip(1);
            var cancellationToken = new CancellationToken();

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            _tipRepositoryMock
                .Setup(x => x.DeleteAsync(command.Id, cancellationToken))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _tipRepositoryMock.Verify(x => x.GetByIdAsync(command.Id, cancellationToken), Times.Once);
            _tipRepositoryMock.Verify(x => x.DeleteAsync(command.Id, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var command = new DeleteTipCommand { Id = 1 };
            var exception = new Exception("Database error");

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to delete tip");
            result.ErrorMessage.Should().Contain("Database error");
        }
    }
}