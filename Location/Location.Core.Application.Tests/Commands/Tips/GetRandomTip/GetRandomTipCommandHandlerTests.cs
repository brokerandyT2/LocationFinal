using FluentAssertions;
using Location.Core.Application.Commands.Tips;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Commands.Tips.GetRandomTip
{
    [Category("Tips")]
    [TestFixture]
    public class GetRandomTipCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ITipRepository> _tipRepositoryMock;
        private GetRandomTipCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tipRepositoryMock = new Mock<ITipRepository>();

            _unitOfWorkMock.Setup(u => u.Tips).Returns(_tipRepositoryMock.Object);

            _handler = new GetRandomTipCommandHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetRandomTipCommandHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithValidTipTypeId_ShouldReturnRandomTip()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = 1 };
            var tip = TestDataBuilder.CreateValidTip(1);

            _tipRepositoryMock
                .Setup(x => x.GetRandomByTypeAsync(command.TipTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.TipTypeId.Should().Be(tip.TipTypeId);
            result.Data.Title.Should().Be(tip.Title);
            result.Data.Content.Should().Be(tip.Content);

            _tipRepositoryMock.Verify(x => x.GetRandomByTypeAsync(command.TipTypeId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNoTipsFound_ShouldReturnFailure()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = 999 };

            _tipRepositoryMock
                .Setup(x => x.GetRandomByTypeAsync(command.TipTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Failure("No tips found for the specified type"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("No tips found for the specified type");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = 1 };
            var tip = TestDataBuilder.CreateValidTip(1);
            var cancellationToken = new CancellationToken();

            _tipRepositoryMock
                .Setup(x => x.GetRandomByTypeAsync(command.TipTypeId, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            // Act
            await _handler.Handle(command, cancellationToken);

            // Assert
            _tipRepositoryMock.Verify(x => x.GetRandomByTypeAsync(command.TipTypeId, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = 1 };
            var exception = new Exception("Unexpected error");

            _tipRepositoryMock
                .Setup(x => x.GetRandomByTypeAsync(command.TipTypeId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve random tip");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        [Test]
        public async Task Handle_WithTipWithPhotographySettings_ShouldIncludeSettingsInResponse()
        {
            // Arrange
            var command = new GetRandomTipCommand { TipTypeId = 1 };

            var tip = new Domain.Entities.Tip(1, "Photography Tip", "Use proper exposure");
            tip.UpdatePhotographySettings("f/8", "1/125", "ISO 100");
            tip.SetLocalization("en-US");

            _tipRepositoryMock
                .Setup(x => x.GetRandomByTypeAsync(command.TipTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Fstop.Should().Be("f/8");
            result.Data.ShutterSpeed.Should().Be("1/125");
            result.Data.Iso.Should().Be("ISO 100");
            result.Data.I8n.Should().Be("en-US");
        }
    }
}