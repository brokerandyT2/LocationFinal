using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.Queries.GetTipById;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Queries.GetTipById
{
    [TestFixture]
    public class GetTipByIdQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ITipRepository> _tipRepositoryMock;
        private GetTipByIdQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tipRepositoryMock = new Mock<ITipRepository>();

            _unitOfWorkMock.Setup(u => u.Tips).Returns(_tipRepositoryMock.Object);

            _handler = new GetTipByIdQueryHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetTipByIdQueryHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithExistingId_ShouldReturnTip()
        {
            // Arrange
            var query = new GetTipByIdQuery { Id = 1 };
            var tip = TestDataBuilder.CreateValidTip(1);

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(tip.Id);
            result.Data.TipTypeId.Should().Be(tip.TipTypeId);
            result.Data.Title.Should().Be(tip.Title);
            result.Data.Content.Should().Be(tip.Content);

            _tipRepositoryMock.Verify(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetTipByIdQuery { Id = 999 };

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Failure("Tip not found"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Tip not found");

            _tipRepositoryMock.Verify(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithZeroId_ShouldTryRepository()
        {
            // Arrange
            var query = new GetTipByIdQuery { Id = 0 };

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Failure("Invalid ID"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid ID");

            _tipRepositoryMock.Verify(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetTipByIdQuery { Id = 1 };
            var tip = TestDataBuilder.CreateValidTip(1);
            var cancellationToken = new CancellationToken();

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _tipRepositoryMock.Verify(x => x.GetByIdAsync(query.Id, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetTipByIdQuery { Id = 1 };
            var exception = new Exception("Database error");

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve tip");
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_ShouldIncludeAllFieldsInResponse()
        {
            // Arrange
            var query = new GetTipByIdQuery { Id = 1 };
            var tip = new Domain.Entities.Tip(2, "Photography Tip", "Use proper exposure");
            tip.UpdatePhotographySettings("f/8", "1/125", "ISO 100");
            tip.SetLocalization("en-US");

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(tip));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.TipTypeId.Should().Be(2);
            result.Data.Title.Should().Be("Photography Tip");
            result.Data.Content.Should().Be("Use proper exposure");
            result.Data.Fstop.Should().Be("f/8");
            result.Data.ShutterSpeed.Should().Be("1/125");
            result.Data.Iso.Should().Be("ISO 100");
            result.Data.I8n.Should().Be("en-US");
        }
    }
}