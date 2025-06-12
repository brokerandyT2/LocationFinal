using FluentAssertions;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Tests.Helpers;
using Moq;
using NUnit.Framework;

namespace Location.Core.Infrastructure.Tests.Data.Repositories
{
    [TestFixture]
    public class TipTypeRepositoryAdapterTests
    {
        private TipTypeRepositoryAdapter _adapter;
        private Mock<ITipTypeRepository> _mockInnerRepository;
        private Mock<ILocationRepository> _mockLocationRepository;
        private Mock<ITipRepository> _mockTipRepository;

        [SetUp]
        public void Setup()
        {
            _mockInnerRepository = new Mock<ITipTypeRepository>();
            _mockLocationRepository = new Mock<ILocationRepository>();
            _mockTipRepository = new Mock<ITipRepository>();

            _adapter = new TipTypeRepositoryAdapter(
                _mockInnerRepository.Object,
                _mockLocationRepository.Object,
                _mockTipRepository.Object);
        }

        [Test]
        public async Task GetByIdAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipType);

            // Act
            var result = await _adapter.GetByIdAsync(1);

            // Assert
            result.Should().BeSameAs(tipType);
            _mockInnerRepository.Verify(x => x.GetByIdAsync(1, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetByIdAsync_WithNullResult_ShouldReturnNull()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TipType?)null);

            // Act
            var result = await _adapter.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
            _mockInnerRepository.Verify(x => x.GetByIdAsync(999, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetAllAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var tipTypes = new List<TipType>
            {
                TestDataBuilder.CreateValidTipType(),
                TestDataBuilder.CreateValidTipType()
            };
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipTypes);

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.Should().BeSameAs(tipTypes);
            _mockInnerRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task AddAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipType);

            // Act
            var result = await _adapter.AddAsync(tipType);

            // Assert
            result.Should().BeSameAs(tipType);
            _mockInnerRepository.Verify(x => x.AddAsync(tipType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _adapter.UpdateAsync(tipType);

            // Assert
            _mockInnerRepository.Verify(x => x.UpdateAsync(tipType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            _mockInnerRepository.Setup(x => x.DeleteAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask)
                .Verifiable();

            // Act
            await _adapter.DeleteAsync(tipType);

            // Assert
            _mockInnerRepository.Verify(x => x.DeleteAsync(tipType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateEntityAsync_ForTipType_ShouldReturnSuccess()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            var createdTipType = TestDataBuilder.CreateValidTipType();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTipType);

            // Act
            var result = await _adapter.CreateEntityAsync(tipType);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(createdTipType);
            _mockInnerRepository.Verify(x => x.AddAsync(tipType, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateEntityAsync_ForTipType_WithException_ShouldReturnFailure()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.CreateEntityAsync(tipType);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create TipType: Database error");
        }

        [Test]
        public async Task CreateEntityAsync_ForTip_ShouldReturnSuccess()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            var createdTip = TestDataBuilder.CreateValidTip();
            _mockTipRepository.Setup(x => x.AddAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdTip);

            // Act
            var result = await _adapter.CreateEntityAsync(tip);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(createdTip);
            _mockTipRepository.Verify(x => x.AddAsync(tip, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateEntityAsync_ForTip_WithException_ShouldReturnFailure()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            var exception = new Exception("Database error");
            _mockTipRepository.Setup(x => x.AddAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.CreateEntityAsync(tip);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create Tip: Database error");
        }

        [Test]
        public async Task CreateEntityAsync_ForLocation_ShouldReturnSuccess()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            var createdLocation = TestDataBuilder.CreateValidLocation();
            _mockLocationRepository.Setup(x => x.AddAsync(It.IsAny<Location.Core.Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(createdLocation);

            // Act
            var result = await _adapter.CreateEntityAsync(location);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(createdLocation);
            _mockLocationRepository.Verify(x => x.AddAsync(location, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateEntityAsync_ForLocation_WithException_ShouldReturnFailure()
        {
            // Arrange
            var location = TestDataBuilder.CreateValidLocation();
            var exception = new Exception("Database error");
            _mockLocationRepository.Setup(x => x.AddAsync(It.IsAny<Location.Core.Domain.Entities.Location>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.CreateEntityAsync(location);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create Location: Database error");
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowException()
        {
            // Act
            Action act = () => new TipTypeRepositoryAdapter(null!, _mockLocationRepository.Object, _mockTipRepository.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("innerRepository");
        }

        [Test]
        public void Constructor_WithNullLocationRepository_ShouldThrowException()
        {
            // Act
            Action act = () => new TipTypeRepositoryAdapter(_mockInnerRepository.Object, null!, _mockTipRepository.Object);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("locationRepository");
        }

        [Test]
        public void Constructor_WithNullTipRepository_ShouldThrowException()
        {
            // Act
            Action act = () => new TipTypeRepositoryAdapter(_mockInnerRepository.Object, _mockLocationRepository.Object, null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("tipRepository");
        }

        [Test]
        public async Task GetByIdAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((TipType?)null);

            // Act
            await _adapter.GetByIdAsync(1, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.GetByIdAsync(1, cancellationToken), Times.Once);
        }

        [Test]
        public async Task GetAllAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();
            var tipTypes = new List<TipType>();
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipTypes);

            // Act
            await _adapter.GetAllAsync(cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.GetAllAsync(cancellationToken), Times.Once);
        }

        [Test]
        public async Task AddAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipType);

            // Act
            await _adapter.AddAsync(tipType, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.AddAsync(tipType, cancellationToken), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.UpdateAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _adapter.UpdateAsync(tipType, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.UpdateAsync(tipType, cancellationToken), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.DeleteAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            // Act
            await _adapter.DeleteAsync(tipType, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.DeleteAsync(tipType, cancellationToken), Times.Once);
        }

        [Test]
        public async Task GetAllAsync_WithEmptyResult_ShouldReturnEmptyList()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<TipType>());

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.Should().BeEmpty();
            _mockInnerRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}