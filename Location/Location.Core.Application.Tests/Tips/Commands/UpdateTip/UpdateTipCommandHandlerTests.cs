using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Tips.Commands.UpdateTip;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Commands.UpdateTip
{
    [Category("Tips")]
    [Category("Update")]
    [TestFixture]
    public class UpdateTipCommandHandlerTests
    {
        private Mock<ITipRepository> _tipRepositoryMock;
        private UpdateTipCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _tipRepositoryMock = new Mock<ITipRepository>();
            _handler = new UpdateTipCommandHandler(_tipRepositoryMock.Object);
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            NUnit.Framework.Assert.Throws<ArgumentNullException>(() => new UpdateTipCommandHandler(null));
        }

        [Test]
        public async Task Handle_WithValidUpdate_ShouldUpdateTip()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
                TipTypeId = 2,
                Title = "Updated Rule of Thirds",
                Content = "Updated content about composition",
                Fstop = "f/11",
                ShutterSpeed = "1/250",
                Iso = "ISO 200",
                I8n = "es-ES"
            };

            var existingTip = TestDataBuilder.CreateValidTip(1);

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            _tipRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(command.Id);
            result.Data.Title.Should().Be(command.Title);
            result.Data.Content.Should().Be(command.Content);
            result.Data.Fstop.Should().Be(command.Fstop);
            result.Data.ShutterSpeed.Should().Be(command.ShutterSpeed);
            result.Data.Iso.Should().Be(command.Iso);
            result.Data.I8n.Should().Be(command.I8n);

            _tipRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentTip_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 999,
                TipTypeId = 1,
                Title = "Updated Tip",
                Content = "Updated content"
            };

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Failure("Tip not found"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Tip not found");

            _tipRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithPartialUpdate_ShouldUpdateOnlyProvidedFields()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
                TipTypeId = 2,
                Title = "New Title Only",
                Content = "New Content Only",
                Fstop = "", // Empty
                ShutterSpeed = "", // Empty
                Iso = "", // Empty
                I8n = "en-US"
            };

            var existingTip = TestDataBuilder.CreateValidTip(1);
            existingTip.UpdatePhotographySettings("f/8", "1/125", "ISO 100");

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            _tipRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Title.Should().Be(command.Title);
            result.Data.Content.Should().Be(command.Content);
            result.Data.Fstop.Should().BeEmpty();
            result.Data.ShutterSpeed.Should().BeEmpty();
            result.Data.Iso.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WhenUpdateFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
                TipTypeId = 1,
                Title = "Updated Tip",
                Content = "Updated content"
            };

            var existingTip = TestDataBuilder.CreateValidTip(1);

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            _tipRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Failure("Failed to update tip"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to update tip");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
                TipTypeId = 1,
                Title = "Updated Tip",
                Content = "Updated content"
            };

            var existingTip = TestDataBuilder.CreateValidTip(1);
            var cancellationToken = new CancellationToken();

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            _tipRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _tipRepositoryMock.Verify(x => x.GetByIdAsync(command.Id, cancellationToken), Times.Once);
            _tipRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldCallCorrectDomainMethods()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
                TipTypeId = 1,
                Title = "Updated Title",
                Content = "Updated Content",
                Fstop = "f/5.6",
                ShutterSpeed = "1/500",
                Iso = "ISO 400",
                I8n = "fr-FR"
            };

            var existingTip = TestDataBuilder.CreateValidTip(1);
            Domain.Entities.Tip capturedTip = null;

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            _tipRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .Callback<Domain.Entities.Tip, CancellationToken>((tip, ct) => capturedTip = tip)
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedTip.Should().NotBeNull();
            capturedTip.Title.Should().Be(command.Title);
            capturedTip.Content.Should().Be(command.Content);
            capturedTip.Fstop.Should().Be(command.Fstop);
            capturedTip.ShutterSpeed.Should().Be(command.ShutterSpeed);
            capturedTip.Iso.Should().Be(command.Iso);
            capturedTip.I8n.Should().Be(command.I8n);
        }
    }
}