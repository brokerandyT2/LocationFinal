using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Application.Tips.Queries.GetAllTips;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Queries.GetAllTips
{
    [Category("Tips")]
    [Category("Get")]
    [TestFixture]
    public class GetAllTipsQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ITipRepository> _tipRepositoryMock;
        private GetAllTipsQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tipRepositoryMock = new Mock<ITipRepository>();

            _unitOfWorkMock.Setup(u => u.Tips).Returns(_tipRepositoryMock.Object);

            _handler = new GetAllTipsQueryHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetAllTipsQueryHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithTips_ShouldReturnAllTips()
        {
            // Arrange
            var query = new GetAllTipsQuery();
            var tips = new List<Domain.Entities.Tip>
            {
                TestDataBuilder.CreateValidTip(1),
                TestDataBuilder.CreateValidTip(2),
                TestDataBuilder.CreateValidTip(3)
            };

            _tipRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(tips));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);

            _tipRepositoryMock.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNoTips_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetAllTipsQuery();
            var emptyList = new List<Domain.Entities.Tip>();

            _tipRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(emptyList));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WhenRepositoryFails_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetAllTipsQuery();

            _tipRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Failure("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve tips");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetAllTipsQuery();
            var tips = new List<Domain.Entities.Tip>();
            var cancellationToken = new CancellationToken();

            _tipRepositoryMock
                .Setup(x => x.GetAllAsync(cancellationToken))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(tips));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _tipRepositoryMock.Verify(x => x.GetAllAsync(cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetAllTipsQuery();
            var exception = new Exception("Unexpected error");

            _tipRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve tips");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        [Test]
        public async Task Handle_ShouldMapTipsToDTOs()
        {
            // Arrange
            var query = new GetAllTipsQuery();

            var tip1 = new Domain.Entities.Tip(1, "Tip 1", "Content 1");
            tip1.UpdatePhotographySettings("f/2.8", "1/500", "ISO 400");

            var tip2 = new Domain.Entities.Tip(2, "Tip 2", "Content 2");
            tip2.UpdatePhotographySettings("f/8", "1/125", "ISO 100");

            var tips = new List<Domain.Entities.Tip> { tip1, tip2 };

            _tipRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(tips));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);

            result.Data[0].TipTypeId.Should().Be(1);
            result.Data[0].Title.Should().Be("Tip 1");
            result.Data[0].Content.Should().Be("Content 1");
            result.Data[0].Fstop.Should().Be("f/2.8");
            result.Data[0].ShutterSpeed.Should().Be("1/500");
            result.Data[0].Iso.Should().Be("ISO 400");

            result.Data[1].TipTypeId.Should().Be(2);
            result.Data[1].Title.Should().Be("Tip 2");
            result.Data[1].Content.Should().Be("Content 2");
            result.Data[1].Fstop.Should().Be("f/8");
            result.Data[1].ShutterSpeed.Should().Be("1/125");
            result.Data[1].Iso.Should().Be("ISO 100");
        }
    }
}