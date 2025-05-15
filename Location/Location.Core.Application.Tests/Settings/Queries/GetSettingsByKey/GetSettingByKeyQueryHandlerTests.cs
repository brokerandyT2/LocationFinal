using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Settings.Queries.GetSettingByKey
{
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
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetSettingByKeyQueryHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithExistingKey_ShouldReturnSetting()
        {
            // Arrange
            var query = new GetSettingByKeyQuery { Key = "WeatherApiKey" };
            var setting = TestDataBuilder.CreateValidSetting();

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(query.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Key.Should().Be(setting.Key);
            result.Data.Value.Should().Be(setting.Value);
            result.Data.Description.Should().Be(setting.Description);

            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(query.Key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentKey_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetSettingByKeyQuery { Key = "NonExistentKey" };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(query.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Setting not found"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Setting not found");

            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(query.Key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithEmptyKey_ShouldStillTryRepository()
        {
            // Arrange
            var query = new GetSettingByKeyQuery { Key = string.Empty };

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(query.Key, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure("Invalid key"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().NotBeNull();

            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(query.Key, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetSettingByKeyQuery { Key = "TestKey" };
            var setting = TestDataBuilder.CreateValidSetting();
            var cancellationToken = new CancellationToken();

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(query.Key, cancellationToken))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(setting));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _settingRepositoryMock.Verify(x => x.GetByKeyAsync(query.Key, cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetSettingByKeyQuery { Key = "TestKey" };
            var exception = new Exception("Database error");

            _settingRepositoryMock
                .Setup(x => x.GetByKeyAsync(query.Key, It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve setting");
            result.ErrorMessage.Should().Contain("Database error");
        }
    }
}