// Update the TipTypeRepositoryAdapterTests class to include the additional dependencies

using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Repositories;
using Location.Core.Infrastructure.Tests.Helpers;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
        }

        [Test]
        public async Task GetAllAsync_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var tipTypes = new[]
            {
                TestDataBuilder.CreateValidTipType(name: "Landscape"),
                TestDataBuilder.CreateValidTipType(name: "Portrait"),
                TestDataBuilder.CreateValidTipType(name: "Macro")
            };
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tipTypes);

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.Should().BeEquivalentTo(tipTypes);
            _mockInnerRepository.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task GetAllAsync_WithEmptyResult_ShouldReturnEmptyCollection()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<TipType>());

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.Should().BeEmpty();
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
        public void Update_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            _mockInnerRepository.Setup(x => x.Update(It.IsAny<TipType>()))
                .Verifiable();

            // Act
            _adapter.Update(tipType);

            // Assert
            _mockInnerRepository.Verify(x => x.Update(tipType), Times.Once);
        }

        [Test]
        public void Delete_ShouldDelegateToInnerRepository()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            _mockInnerRepository.Setup(x => x.Delete(It.IsAny<TipType>()))
                .Verifiable();

            // Act
            _adapter.Delete(tipType);

            // Assert
            _mockInnerRepository.Verify(x => x.Delete(tipType), Times.Once);
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
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<TipType>());

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
    }
}
