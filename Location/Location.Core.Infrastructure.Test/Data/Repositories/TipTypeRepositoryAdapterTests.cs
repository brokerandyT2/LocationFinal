
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

        [SetUp]
        public void Setup()
        {
            _mockInnerRepository = new Mock<ITipTypeRepository>();
            _adapter = new TipTypeRepositoryAdapter(_mockInnerRepository.Object);
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
            Action act = () => new TipTypeRepositoryAdapter(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("innerRepository");
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

        [Test]
        public void Update_MultipleTimes_ShouldDelegateEachTime()
        {
            // Arrange
            var tipType1 = TestDataBuilder.CreateValidTipType(name: "Type 1");
            var tipType2 = TestDataBuilder.CreateValidTipType(name: "Type 2");
            _mockInnerRepository.Setup(x => x.Update(It.IsAny<TipType>()))
                .Verifiable();

            // Act
            _adapter.Update(tipType1);
            _adapter.Update(tipType2);

            // Assert
            _mockInnerRepository.Verify(x => x.Update(tipType1), Times.Once);
            _mockInnerRepository.Verify(x => x.Update(tipType2), Times.Once);
        }

        [Test]
        public void Delete_MultipleTimes_ShouldDelegateEachTime()
        {
            // Arrange
            var tipType1 = TestDataBuilder.CreateValidTipType(name: "Type 1");
            var tipType2 = TestDataBuilder.CreateValidTipType(name: "Type 2");
            _mockInnerRepository.Setup(x => x.Delete(It.IsAny<TipType>()))
                .Verifiable();

            // Act
            _adapter.Delete(tipType1);
            _adapter.Delete(tipType2);

            // Assert
            _mockInnerRepository.Verify(x => x.Delete(tipType1), Times.Once);
            _mockInnerRepository.Verify(x => x.Delete(tipType2), Times.Once);
        }

        [Test]
        public async Task GetAllAsync_WithException_ShouldPropagateException()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            Func<Task> act = async () => await _adapter.GetAllAsync();

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database error");
        }

        [Test]
        public async Task AddAsync_WithException_ShouldPropagateException()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<TipType>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            Func<Task> act = async () => await _adapter.AddAsync(tipType);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database error");
        }

        [Test]
        public void Update_WithException_ShouldPropagateException()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.Update(It.IsAny<TipType>()))
                .Throws(exception);

            // Act
            Action act = () => _adapter.Update(tipType);

            // Assert
            act.Should().Throw<Exception>()
                .WithMessage("Database error");
        }

        [Test]
        public void Delete_WithException_ShouldPropagateException()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.Delete(It.IsAny<TipType>()))
                .Throws(exception);

            // Act
            Action act = () => _adapter.Delete(tipType);

            // Assert
            act.Should().Throw<Exception>()
                .WithMessage("Database error");
        }
    }
}