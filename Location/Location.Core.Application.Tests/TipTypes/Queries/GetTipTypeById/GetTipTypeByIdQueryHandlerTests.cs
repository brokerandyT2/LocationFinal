using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Queries.TipTypes;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.TipTypes.Queries.GetTipTypeById
{
    [TestFixture]
    public class GetTipTypeByIdQueryHandlerTests
    {
        private Mock<ITipTypeRepository> _tipTypeRepositoryMock;
        private GetTipTypeByIdQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _tipTypeRepositoryMock = new Mock<ITipTypeRepository>();
            _handler = new GetTipTypeByIdQueryHandler(_tipTypeRepositoryMock.Object);
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetTipTypeByIdQueryHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithExistingId_ShouldReturnTipType()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 1 };
            var tipType = TestDataBuilder.CreateValidTipType();

            _tipTypeRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipType);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Name.Should().Be(tipType.Name);
            result.Data.I8n.Should().Be(tipType.I8n);

            _tipTypeRepositoryMock.Verify(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentId_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 999 };

            _tipTypeRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.TipType)null);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Tip type with ID 999 not found");

            _tipTypeRepositoryMock.Verify(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 1 };
            var tipType = TestDataBuilder.CreateValidTipType();
            var cancellationToken = new CancellationToken();

            _tipTypeRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, cancellationToken))
                .ReturnsAsync(tipType);

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _tipTypeRepositoryMock.Verify(x => x.GetByIdAsync(query.Id, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 1 };
            var exception = new Exception("Database error");

            _tipTypeRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve tip type");
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_WithValidTipType_ShouldMapCorrectly()
        {
            // Arrange
            var query = new GetTipTypeByIdQuery { Id = 1 };
            var tipType = new Domain.Entities.TipType("Landscape Photography");
            tipType.SetLocalization("en-US");

            _tipTypeRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipType);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Name.Should().Be("Landscape Photography");
            result.Data.I8n.Should().Be("en-US");
        }
    }
}