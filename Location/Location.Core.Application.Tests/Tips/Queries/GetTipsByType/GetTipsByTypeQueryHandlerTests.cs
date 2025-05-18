using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.Queries.GetTipsByType;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Queries.GetTipsByType
{
    [Category("Tips")]
    [Category("Get")]
    [TestFixture]
    public class GetTipsByTypeQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ITipRepository> _tipRepositoryMock;
        private GetTipsByTypeQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tipRepositoryMock = new Mock<ITipRepository>();

            _unitOfWorkMock.Setup(u => u.Tips).Returns(_tipRepositoryMock.Object);

            _handler = new GetTipsByTypeQueryHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetTipsByTypeQueryHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithValidTipTypeId_ShouldReturnTips()
        {
            // Arrange
            var query = new GetTipsByTypeQuery { TipTypeId = 1 };
            var tips = new List<Domain.Entities.Tip>
           {
               TestDataBuilder.CreateValidTip(1),
               TestDataBuilder.CreateValidTip(1),
               TestDataBuilder.CreateValidTip(1)
           };

            _tipRepositoryMock
                .Setup(x => x.GetByTypeAsync(query.TipTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(tips));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);

            _tipRepositoryMock.Verify(x => x.GetByTypeAsync(query.TipTypeId, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNoTipsForType_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetTipsByTypeQuery { TipTypeId = 1 };
            var emptyList = new List<Domain.Entities.Tip>();

            _tipRepositoryMock
                .Setup(x => x.GetByTypeAsync(query.TipTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(emptyList));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WithNonExistentTipTypeId_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetTipsByTypeQuery { TipTypeId = 999 };

            _tipRepositoryMock
                .Setup(x => x.GetByTypeAsync(query.TipTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Failure("Tip type not found"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve tips by type");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetTipsByTypeQuery { TipTypeId = 1 };
            var tips = new List<Domain.Entities.Tip>();
            var cancellationToken = new CancellationToken();

            _tipRepositoryMock
                .Setup(x => x.GetByTypeAsync(query.TipTypeId, cancellationToken))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(tips));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _tipRepositoryMock.Verify(x => x.GetByTypeAsync(query.TipTypeId, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetTipsByTypeQuery { TipTypeId = 1 };
            var exception = new Exception("Database error");

            _tipRepositoryMock
                .Setup(x => x.GetByTypeAsync(query.TipTypeId, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve tips by type");
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_ShouldMapTipFieldsCorrectly()
        {
            // Arrange
            var query = new GetTipsByTypeQuery { TipTypeId = 1 };

            var tip1 = new Domain.Entities.Tip(1, "Landscape Tip", "Wide angle lens");
            tip1.UpdatePhotographySettings("f/11", "1/60", "ISO 100");
            tip1.SetLocalization("en-US");

            var tip2 = new Domain.Entities.Tip(1, "Night Sky Tip", "Use tripod");
            tip2.UpdatePhotographySettings("f/2.8", "15s", "ISO 3200");
            tip2.SetLocalization("en-US");

            var tips = new List<Domain.Entities.Tip> { tip1, tip2 };

            _tipRepositoryMock
                .Setup(x => x.GetByTypeAsync(query.TipTypeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(tips));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);

            result.Data[0].Title.Should().Be("Landscape Tip");
            result.Data[0].Content.Should().Be("Wide angle lens");
            result.Data[0].Fstop.Should().Be("f/11");
            result.Data[0].ShutterSpeed.Should().Be("1/60");
            result.Data[0].Iso.Should().Be("ISO 100");

            result.Data[1].Title.Should().Be("Night Sky Tip");
            result.Data[1].Content.Should().Be("Use tripod");
            result.Data[1].Fstop.Should().Be("f/2.8");
            result.Data[1].ShutterSpeed.Should().Be("15s");
            result.Data[1].Iso.Should().Be("ISO 3200");
        }
    }
}