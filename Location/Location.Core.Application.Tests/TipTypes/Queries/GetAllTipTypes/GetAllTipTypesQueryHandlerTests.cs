using System;
using System.Collections.Generic;
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

namespace Location.Core.Application.Tests.TipTypes.Queries.GetAllTipTypes
{
    [Category("Tips")]
    [Category("Get All")]
    [TestFixture]
    public class GetAllTipTypesQueryHandlerTests
    {
        private Mock<ITipTypeRepository> _tipTypeRepositoryMock;
        private GetAllTipTypesQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _tipTypeRepositoryMock = new Mock<ITipTypeRepository>();
            _handler = new GetAllTipTypesQueryHandler(_tipTypeRepositoryMock.Object);
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetAllTipTypesQueryHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithTipTypes_ShouldReturnAllTipTypes()
        {
            // Arrange
            var query = new GetAllTipTypesQuery();
            var tipTypes = new List<Domain.Entities.TipType>
            {
                TestDataBuilder.CreateValidTipType(),
                new Domain.Entities.TipType("Portrait"),
                new Domain.Entities.TipType("Macro")
            };

            _tipTypeRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipTypes);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(3);

            _tipTypeRepositoryMock.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNoTipTypes_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetAllTipTypesQuery();
            var emptyList = new List<Domain.Entities.TipType>();

            _tipTypeRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(emptyList);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetAllTipTypesQuery();
            var tipTypes = new List<Domain.Entities.TipType>();
            var cancellationToken = new CancellationToken();

            _tipTypeRepositoryMock
                .Setup(x => x.GetAllAsync(cancellationToken))
                .ReturnsAsync(tipTypes);

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _tipTypeRepositoryMock.Verify(x => x.GetAllAsync(cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetAllTipTypesQuery();
            var exception = new Exception("Database error");

            _tipTypeRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve tip types");
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_ShouldMapTipTypeFieldsCorrectly()
        {
            // Arrange
            var query = new GetAllTipTypesQuery();

            var tipType1 = new Domain.Entities.TipType("Landscape");
            tipType1.SetLocalization("en-US");

            var tipType2 = new Domain.Entities.TipType("Portrait");
            tipType2.SetLocalization("fr-FR");

            var tipTypes = new List<Domain.Entities.TipType> { tipType1, tipType2 };

            _tipTypeRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipTypes);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);

            result.Data[0].Name.Should().Be("Landscape");
            result.Data[0].I8n.Should().Be("en-US");

            result.Data[1].Name.Should().Be("Portrait");
            result.Data[1].I8n.Should().Be("fr-FR");
        }
    }
}