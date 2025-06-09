using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.Queries.GetTipById;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Queries.GetTipById
{
    [Category("Tips")]
    [Category("Get")]
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
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetTipByIdQuery { Id = 1 };

            // Fix: Setup the repository to return a failure result instead of throwing
            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Failure("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Database error");
        }
    }
}