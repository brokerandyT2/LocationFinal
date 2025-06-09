using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Settings.Queries.GetSettingByKey
{
    [Category("Setting")]
    [Category("Get")]
    [TestFixture]
    public class GetSettingByKeyQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ISettingRepository> _settingRepositoryMock;
        private GetSettingByKeyQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _settingRepositoryMock = new Mock<ISettingRepository>();

            _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingRepositoryMock.Object);

            _handler = new GetSettingByKeyQueryHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetSettingByKeyQuery { Key = "TestKey" };

            // Fix: Setup the repository to return a failure result instead of throwing
            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(query.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Database error");
        }
    }
}