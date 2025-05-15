using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Tips.Commands.CreateTip;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Commands.CreateTip
{
    [TestFixture]
    public class CreateTipCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ITipRepository> _tipRepositoryMock;
        private CreateTipCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tipRepositoryMock = new Mock<ITipRepository>();

            _unitOfWorkMock.Setup(u => u.Tips).Returns(_tipRepositoryMock.Object);

            _handler = new CreateTipCommandHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_WithValidData_ShouldCreateTip()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Rule of Thirds",
                Content = "Divide your frame into thirds",
                Fstop = "f/8",
                ShutterSpeed = "1/125",
                Iso = "ISO 100",
                I8n = "en-US"
            };

            var createdTip = new Domain.Entities.Tip(
                command.TipTypeId,
                command.Title,
                command.Content);

            createdTip.UpdatePhotographySettings(
                command.Fstop,
                command.ShutterSpeed,
                command.Iso);
            createdTip.SetLocalization(command.I8n);

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(createdTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Title.Should().Be(command.Title);
            result.Data.Content.Should().Be(command.Content);
            result.Data.Fstop.Should().Be(command.Fstop);
            result.Data.ShutterSpeed.Should().Be(command.ShutterSpeed);
            result.Data.Iso.Should().Be(command.Iso);
            result.Data.I8n.Should().Be(command.I8n);

            _tipRepositoryMock.Verify(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithMinimalData_ShouldCreateTip()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Basic Tip",
                Content = "Some content",
                I8n = "en-US"
                // No photography settings
            };

            var createdTip = new Domain.Entities.Tip(
                command.TipTypeId,
                command.Title,
                command.Content);
            createdTip.SetLocalization(command.I8n);

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(createdTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Title.Should().Be(command.Title);
            result.Data.Fstop.Should().BeEmpty();
            result.Data.ShutterSpeed.Should().BeEmpty();
            result.Data.Iso.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WithPartialPhotographySettings_ShouldSetProvidedValues()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Partial Settings",
                Content = "Some content",
                Fstop = "f/2.8",
                ShutterSpeed = "", // Empty
                Iso = "ISO 200",   // Set
                I8n = "en-US"
            };

            var createdTip = new Domain.Entities.Tip(
                command.TipTypeId,
                command.Title,
                command.Content);

            createdTip.UpdatePhotographySettings(
                command.Fstop,
                command.ShutterSpeed,
                command.Iso);
            createdTip.SetLocalization(command.I8n);

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(createdTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Fstop.Should().Be("f/2.8");
            result.Data.ShutterSpeed.Should().BeEmpty();
            result.Data.Iso.Should().Be("ISO 200");
        }

        [Test]
        public async Task Handle_WithDefaultLocalization_ShouldUseEnUS()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content"
                // I8n defaults to "en-US"
            };

            var createdTip = new Domain.Entities.Tip(
                command.TipTypeId,
                command.Title,
                command.Content);
            createdTip.SetLocalization(command.I8n);

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(createdTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.I8n.Should().Be("en-US");
        }

        [Test]
        public async Task Handle_WhenRepositoryFails_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content"
            };

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Failure("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Database error");
        }

        [Test]
        public async Task Handle_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content"
            };

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act & Assert
            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to create tip");
        }

        [Test]
        public async Task Handle_ShouldCallCreateAsyncOnce()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content"
            };

            var createdTip = TestDataBuilder.CreateValidTip(1);

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(createdTip));

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _tipRepositoryMock.Verify(x => x.CreateAsync(
                It.IsAny<Domain.Entities.Tip>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content"
            };

            var createdTip = TestDataBuilder.CreateValidTip(1);
            var cancellationToken = new CancellationToken();

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(createdTip));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _tipRepositoryMock.Verify(x => x.CreateAsync(
                It.IsAny<Domain.Entities.Tip>(),
                cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_ShouldReturnCreatedTipId()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content"
            };

            var createdTip = TestDataBuilder.CreateValidTip(42); // Id set by test builder

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(createdTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Id.Should().Be(42);
        }
    }
}