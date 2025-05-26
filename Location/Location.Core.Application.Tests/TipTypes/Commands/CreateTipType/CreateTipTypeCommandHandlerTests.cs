using FluentAssertions;
using Location.Core.Application.Commands.TipTypes;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using MediatR;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.Application.Tests.TipTypes.Commands.CreateTipType
{
    [Category("Tips")]
    [Category("Create")]
    [TestFixture]
    public class CreateTipTypeCommandHandlerTests
    {
        private Mock<ITipTypeRepository> _tipTypeRepositoryMock;
        private CreateTipTypeCommandHandler _handler;
        private Mock<IMediator> _mediatorMock;
        [SetUp]
        public void SetUp()
        {
            _mediatorMock = new Mock<IMediator>();
            _tipTypeRepositoryMock = new Mock<ITipTypeRepository>();
            _handler = new CreateTipTypeCommandHandler(_tipTypeRepositoryMock.Object, _mediatorMock.Object);
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new CreateTipTypeCommandHandler(null, null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithValidData_ShouldCreateTipType()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Landscape Photography",
                I8n = "en-US"
            };

            var createdTipType = new Domain.Entities.TipType(command.Name);
            createdTipType.SetLocalization(command.I8n);

            _tipTypeRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTipType);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be(command.Name);
            result.Data.I8n.Should().Be(command.I8n);

            _tipTypeRepositoryMock.Verify(x => x.AddAsync(
                It.Is<Domain.Entities.TipType>(t => t.Name == command.Name),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithExistingName_ShouldStillCreateTipType()
        {
            // Arrange - The repository level should handle uniqueness constraints
            var command = new CreateTipTypeCommand
            {
                Name = "Existing Type",
                I8n = "en-US"
            };

            var createdTipType = new Domain.Entities.TipType(command.Name);
            createdTipType.SetLocalization(command.I8n);

            _tipTypeRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTipType);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Name.Should().Be(command.Name);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Test Type",
                I8n = "en-US"
            };

            var createdTipType = new Domain.Entities.TipType(command.Name);
            var cancellationToken = new CancellationToken();

            _tipTypeRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TipType>(), cancellationToken))
                .ReturnsAsync(createdTipType);

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _tipTypeRepositoryMock.Verify(x => x.AddAsync(
                It.Is<Domain.Entities.TipType>(t => t.Name == command.Name),
                cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Test Type",
                I8n = "en-US"
            };

            var exception = new Exception("Database error");

            _tipTypeRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TipType>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to create tip type");
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_ShouldSetLocalization()
        {
            // Arrange
            var command = new CreateTipTypeCommand
            {
                Name = "Test Type",
                I8n = "fr-FR"
            };

            Domain.Entities.TipType capturedTipType = null;

            _tipTypeRepositoryMock
                .Setup(x => x.AddAsync(It.IsAny<Domain.Entities.TipType>(), It.IsAny<CancellationToken>()))
                .Callback<Domain.Entities.TipType, CancellationToken>((t, _) => capturedTipType = t)
                .ReturnsAsync((Domain.Entities.TipType t, CancellationToken _) => t);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            capturedTipType.Should().NotBeNull();
            capturedTipType.I8n.Should().Be("fr-FR");
        }
    }
}