using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
using Location.Photography.Infrastructure.Extensions;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Infrastructure.Test.Extensions
{
    [TestFixture]
    public class RepositoryExtensionsTests
    {
        private Mock<ITipTypeRepository> _mockTipTypeRepository;
        private Mock<ITipRepository> _mockTipRepository;
        private Mock<ILocationRepository> _mockLocationRepository;

        [SetUp]
        public void SetUp()
        {
            _mockTipTypeRepository = new Mock<ITipTypeRepository>();
            _mockTipRepository = new Mock<ITipRepository>();
            _mockLocationRepository = new Mock<ILocationRepository>();
        }

        #region TipType Repository Extension Tests

        [Test]
        public async Task CreateAsync_ForTipTypeRepository_WithValidEntity_ShouldReturnSuccess()
        {
            // Arrange
            var tipType = new TipType("Test Tip Type");
            var expectedResult = Result<TipType>.Success(tipType);

            _mockTipTypeRepository
                .Setup(x => x.CreateAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockTipTypeRepository.Object.CreateAsync(tipType, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(tipType);

            _mockTipTypeRepository.Verify(
                x => x.CreateAsync(tipType, CancellationToken.None),
                Times.Once);
        }

        [Test]
        public async Task CreateAsync_ForTipTypeRepository_WithNullEntity_ShouldReturnFailure()
        {
            // Arrange
            TipType nullTipType = null;
            var expectedResult = Result<TipType>.Failure("Entity cannot be null");

            _mockTipTypeRepository
                .Setup(x => x.CreateAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockTipTypeRepository.Object.CreateAsync(nullTipType, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Entity cannot be null");
        }

        [Test]
        public async Task CreateAsync_ForTipTypeRepository_WithCancellationToken_ShouldPassToken()
        {
            // Arrange
            var tipType = new TipType("Test Tip Type");
            var expectedResult = Result<TipType>.Success(tipType);
            var cancellationToken = new CancellationToken(true);

            _mockTipTypeRepository
                .Setup(x => x.CreateAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act & Assert - Should throw OperationCanceledException due to cancelled token
            await FluentActions.Invoking(async () =>
                await _mockTipTypeRepository.Object.CreateAsync(tipType, cancellationToken))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region Tip Repository Extension Tests

        [Test]
        public async Task CreateAsync_ForTipRepository_WithValidEntity_ShouldReturnSuccess()
        {
            // Arrange
            var tip = new Tip(1, "Test Title", "Test Content"); // int id, string title, string content
            var expectedResult = Result<Tip>.Success(tip);

            _mockTipRepository
                .Setup(x => x.CreateAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockTipRepository.Object.CreateAsync(tip, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(tip);

            _mockTipRepository.Verify(
                x => x.CreateAsync(tip, CancellationToken.None),
                Times.Once);
        }

        [Test]
        public async Task CreateAsync_ForTipRepository_WithRepositoryFailure_ShouldReturnFailure()
        {
            // Arrange
            var tip = new Tip(1, "Test Title", "Test Content");
            var expectedResult = Result<Tip>.Failure("Database error occurred");

            _mockTipRepository
                .Setup(x => x.CreateAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockTipRepository.Object.CreateAsync(tip, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Database error occurred");
        }

        [Test]
        public async Task CreateAsync_ForTipRepository_WithException_ShouldPropagateException()
        {
            // Arrange
            var tip = new Tip(1, "Test Title", "Test Content");
            var expectedException = new InvalidOperationException("Database connection failed");

            _mockTipRepository
                .Setup(x => x.CreateAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(expectedException);

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _mockTipRepository.Object.CreateAsync(tip, CancellationToken.None))
                .Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Database connection failed");
        }

        #endregion

        #region Location Repository Extension Tests

        [Test]
        public async Task CreateAsync_ForLocationRepository_WithValidEntity_ShouldReturnSuccess()
        {
            // Arrange
            var coordinate = new Location.Core.Domain.ValueObjects.Coordinate(47.6062, -86.1580);
            var address = new Location.Core.Domain.ValueObjects.Address("", "");
            var location = new Location.Core.Domain.Entities.Location("Test Location", "Test Description", coordinate, address);
            var expectedResult = Result<Location.Core.Domain.Entities.Location>.Success(location);

            _mockLocationRepository
                .Setup(x => x.CreateAsync(It.IsAny<Location.Core.Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockLocationRepository.Object.CreateAsync(location, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(location);

            _mockLocationRepository.Verify(
                x => x.CreateAsync(location, CancellationToken.None),
                Times.Once);
        }

        [Test]
        public async Task CreateAsync_ForLocationRepository_WithInvalidCoordinates_ShouldReturnFailure()
        {
            // Arrange
            var coordinate = new Location.Core.Domain.ValueObjects.Coordinate(999, 999);
            var address = new Location.Core.Domain.ValueObjects.Address("", "");
            var location = new Location.Core.Domain.Entities.Location("Invalid Location", "Description", coordinate, address);
            var expectedResult = Result<Location.Core.Domain.Entities.Location>.Failure("Invalid coordinates provided");

            _mockLocationRepository
                .Setup(x => x.CreateAsync(It.IsAny<Location.Core.Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockLocationRepository.Object.CreateAsync(location, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Invalid coordinates provided");
        }

        [Test]
        public async Task CreateAsync_ForLocationRepository_WithDuplicateName_ShouldReturnFailure()
        {
            // Arrange
            var coordinate = new Location.Core.Domain.ValueObjects.Coordinate(47.6062, -122.3321);
            var address = new Location.Core.Domain.ValueObjects.Address("", "");
            var location = new Location.Core.Domain.Entities.Location("Existing Location", "Description", coordinate, address);
            var expectedResult = Result<Location.Core.Domain.Entities.Location>.Failure("Location with this name already exists");

            _mockLocationRepository
                .Setup(x => x.CreateAsync(It.IsAny<Location.Core.Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _mockLocationRepository.Object.CreateAsync(location, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Location with this name already exists");
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task AllRepositoryExtensions_WithValidEntities_ShouldWorkTogether()
        {
            // Arrange
            var tipType = new TipType("Integration Test Tip Type");
            var tip = new Tip(1, "Integration Test Title", "Integration Test Content");
            var coordinate = new Location.Core.Domain.ValueObjects.Coordinate(47.6062, -122.3321);
            var address = new Location.Core.Domain.ValueObjects.Address("", "");
            var location = new Location.Core.Domain.Entities.Location("Integration Test Location", "Test Description", coordinate, address);

            _mockTipTypeRepository
                .Setup(x => x.CreateAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<TipType>.Success(tipType));

            _mockTipRepository
                .Setup(x => x.CreateAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Tip>.Success(tip));

            _mockLocationRepository
                .Setup(x => x.CreateAsync(It.IsAny<Location.Core.Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Location>.Success(location));

            // Act
            var tipTypeResult = await _mockTipTypeRepository.Object.CreateAsync(tipType, CancellationToken.None);
            var tipResult = await _mockTipRepository.Object.CreateAsync(tip, CancellationToken.None);
            var locationResult = await _mockLocationRepository.Object.CreateAsync(location, CancellationToken.None);

            // Assert
            tipTypeResult.IsSuccess.Should().BeTrue();
            tipResult.IsSuccess.Should().BeTrue();
            locationResult.IsSuccess.Should().BeTrue();

            // Verify all repositories were called
            _mockTipTypeRepository.Verify(x => x.CreateAsync(tipType, CancellationToken.None), Times.Once);
            _mockTipRepository.Verify(x => x.CreateAsync(tip, CancellationToken.None), Times.Once);
            _mockLocationRepository.Verify(x => x.CreateAsync(location, CancellationToken.None), Times.Once);
        }

        [Test]
        public async Task AllRepositoryExtensions_WithCancellationRequested_ShouldHonorCancellation()
        {
            // Arrange
            var tipType = new TipType("Cancellation Test Tip Type");
            var tip = new Tip(1, "Cancellation Test Title", "Cancellation Test Content");
            var coordinate = new Location.Core.Domain.ValueObjects.Coordinate(47.6062, -122.3321);
            var address = new Location.Core.Domain.ValueObjects.Address("", "");
            var location = new Location.Core.Domain.Entities.Location("Cancellation Test Location", "Test Description", coordinate, address);

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _mockTipTypeRepository.Object.CreateAsync(tipType, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();

            await FluentActions.Invoking(async () =>
                await _mockTipRepository.Object.CreateAsync(tip, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();

            await FluentActions.Invoking(async () =>
                await _mockLocationRepository.Object.CreateAsync(location, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async Task CreateAsync_WhenRepositoryReturnsNull_ShouldHandleGracefully()
        {
            // Arrange
            var tipType = new TipType("Null Test Tip Type");

            _mockTipTypeRepository
                .Setup(x => x.CreateAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Result<TipType>)null);

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _mockTipTypeRepository.Object.CreateAsync(tipType, CancellationToken.None))
                .Should().ThrowAsync<NullReferenceException>();
        }

        [Test]
        public async Task CreateAsync_WithTimeoutException_ShouldPropagateException()
        {
            // Arrange
            var tip = new Tip(1, "Timeout Test Title", "Timeout Test Content");
            var timeoutException = new TimeoutException("Database operation timed out");

            _mockTipRepository
                .Setup(x => x.CreateAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(timeoutException);

            // Act & Assert
            await FluentActions.Invoking(async () =>
                await _mockTipRepository.Object.CreateAsync(tip, CancellationToken.None))
                .Should().ThrowAsync<TimeoutException>()
                .WithMessage("Database operation timed out");
        }

        #endregion
    }
}