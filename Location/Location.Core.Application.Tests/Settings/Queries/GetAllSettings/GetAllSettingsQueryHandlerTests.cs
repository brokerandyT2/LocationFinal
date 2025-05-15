using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Settings.Queries.GetAllSettings;
using Location.Core.Application.Tests.Utilities;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Settings.Queries.GetAllSettings
{
    [TestFixture]
    public class GetAllSettingsQueryHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ISettingRepository> _settingRepositoryMock;
        private GetAllSettingsQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _settingRepositoryMock = new Mock<ISettingRepository>();

            _unitOfWorkMock.Setup(u => u.Settings).Returns(_settingRepositoryMock.Object);

            _handler = new GetAllSettingsQueryHandler(_unitOfWorkMock.Object);
        }

        [Test]
        public void Constructor_WithNullUnitOfWork_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetAllSettingsQueryHandler(null))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WithSettings_ShouldReturnAllSettings()
        {
            // Arrange
            var query = new GetAllSettingsQuery();
            var settings = new List<Domain.Entities.Setting>
            {
                TestDataBuilder.CreateValidSetting(),
                new Domain.Entities.Setting("api_key", "value2", "API Key"),
                new Domain.Entities.Setting("theme", "dark", "UI Theme")
            };

            _settingRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Setting>>.Success(settings));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Count.Should().Be(3);

            _settingRepositoryMock.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNoSettings_ShouldReturnEmptyList()
        {
            // Arrange
            var query = new GetAllSettingsQuery();
            var emptyList = new List<Domain.Entities.Setting>();

            _settingRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Setting>>.Success(emptyList));

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
            var query = new GetAllSettingsQuery();

            _settingRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Setting>>.Failure("Database error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Database error");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new GetAllSettingsQuery();
            var settings = new List<Domain.Entities.Setting>();
            var cancellationToken = new CancellationToken();

            _settingRepositoryMock
                .Setup(x => x.GetAllAsync(cancellationToken))
                .ReturnsAsync(Result<List<Domain.Entities.Setting>>.Success(settings));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _settingRepositoryMock.Verify(x => x.GetAllAsync(cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenRepositoryThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetAllSettingsQuery();
            var exception = new Exception("Unexpected error");

            _settingRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to retrieve settings");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        [Test]
        public async Task Handle_ShouldMapSettingsToDTOs()
        {
            // Arrange
            var query = new GetAllSettingsQuery();
            var settings = new List<Domain.Entities.Setting>
            {
                new Domain.Entities.Setting("key1", "value1", "Description 1"),
                new Domain.Entities.Setting("key2", "value2", "Description 2")
            };

            _settingRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Setting>>.Success(settings));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);

            result.Data[0].Key.Should().Be("key1");
            result.Data[0].Value.Should().Be("value1");
            result.Data[0].Description.Should().Be("Description 1");

            result.Data[1].Key.Should().Be("key2");
            result.Data[1].Value.Should().Be("value2");
            result.Data[1].Description.Should().Be("Description 2");
        }
    }
}