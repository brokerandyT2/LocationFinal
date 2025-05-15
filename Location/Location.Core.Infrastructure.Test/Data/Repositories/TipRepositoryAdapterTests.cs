using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
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
    public class TipRepositoryAdapterTests
    {
        private TipRepositoryAdapter _adapter;
        private Mock<ITipRepository> _mockInnerRepository;

        [SetUp]
        public void Setup()
        {
            _mockInnerRepository = new Mock<ITipRepository>();
            _adapter = new TipRepositoryAdapter(_mockInnerRepository.Object);
        }

        [Test]
        public async Task GetByIdAsync_WithExistingTip_ShouldReturnSuccess()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tip);

            // Act
            var result = await _adapter.GetByIdAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(tip);
            result.ErrorMessage.Should().BeNull();
        }

        [Test]
        public async Task GetByIdAsync_WithNonExistingTip_ShouldReturnFailure()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tip)null);

            // Act
            var result = await _adapter.GetByIdAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeNull();
            result.ErrorMessage.Should().Be("Tip with ID 999 not found");
        }

        [Test]
        public async Task GetByIdAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetByIdAsync(1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve tip: Database error");
        }

        [Test]
        public async Task GetAllAsync_WithMultipleTips_ShouldReturnSuccess()
        {
            // Arrange
            var tips = new[]
            {
                TestDataBuilder.CreateValidTip(title: "Tip 1"),
                TestDataBuilder.CreateValidTip(title: "Tip 2"),
                TestDataBuilder.CreateValidTip(title: "Tip 3")
            };
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(tips);

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(3);
            result.Data.Should().BeEquivalentTo(tips);
        }

        [Test]
        public async Task GetAllAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetAllAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve tips: Database error");
        }

        [Test]
        public async Task GetByTypeAsync_WithMatchingTips_ShouldReturnSuccess()
        {
            // Arrange
            var tips = new[]
            {
                TestDataBuilder.CreateValidTip(tipTypeId: 1, title: "Type 1 Tip 1"),
                TestDataBuilder.CreateValidTip(tipTypeId: 1, title: "Type 1 Tip 2")
            };
            _mockInnerRepository.Setup(x => x.GetByTipTypeIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tips);

            // Act
            var result = await _adapter.GetByTypeAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().HaveCount(2);
            result.Data.All(t => t.TipTypeId == 1).Should().BeTrue();
        }

        [Test]
        public async Task GetByTypeAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetByTipTypeIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetByTypeAsync(1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve tips by type: Database error");
        }

        [Test]
        public async Task CreateAsync_WithValidTip_ShouldReturnSuccess()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tip);

            // Act
            var result = await _adapter.CreateAsync(tip);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(tip);
            _mockInnerRepository.Verify(x => x.AddAsync(tip, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task CreateAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.CreateAsync(tip);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to create tip: Database error");
        }

        [Test]
        public async Task UpdateAsync_WithValidTip_ShouldReturnSuccess()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            _mockInnerRepository.Setup(x => x.Update(It.IsAny<Tip>()))
                .Verifiable();

            // Act
            var result = await _adapter.UpdateAsync(tip);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(tip);
            _mockInnerRepository.Verify(x => x.Update(tip), Times.Once);
        }

        [Test]
        public async Task UpdateAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.Update(It.IsAny<Tip>()))
                .Throws(exception);

            // Act
            var result = await _adapter.UpdateAsync(tip);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to update tip: Database error");
        }

        [Test]
        public async Task DeleteAsync_WithExistingTip_ShouldReturnSuccess()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tip);
            _mockInnerRepository.Setup(x => x.Delete(It.IsAny<Tip>()))
                .Verifiable();

            // Act
            var result = await _adapter.DeleteAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();
            _mockInnerRepository.Verify(x => x.Delete(tip), Times.Once);
        }

        [Test]
        public async Task DeleteAsync_WithNonExistingTip_ShouldReturnFailure()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tip)null);

            // Act
            var result = await _adapter.DeleteAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Tip with ID 999 not found");
            _mockInnerRepository.Verify(x => x.Delete(It.IsAny<Tip>()), Times.Never);
        }

        [Test]
        public async Task DeleteAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.DeleteAsync(1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to delete tip: Database error");
        }

        [Test]
        public async Task GetRandomByTypeAsync_WithExistingTip_ShouldReturnSuccess()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip(tipTypeId: 1);
            _mockInnerRepository.Setup(x => x.GetRandomByTypeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tip);

            // Act
            var result = await _adapter.GetRandomByTypeAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeSameAs(tip);
        }

        [Test]
        public async Task GetRandomByTypeAsync_WithNoTips_ShouldReturnFailure()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetRandomByTypeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Tip)null);

            // Act
            var result = await _adapter.GetRandomByTypeAsync(999);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("No tips found for type ID 999");
        }

        [Test]
        public async Task GetRandomByTypeAsync_WithException_ShouldReturnFailure()
        {
            // Arrange
            var exception = new Exception("Database error");
            _mockInnerRepository.Setup(x => x.GetRandomByTypeAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _adapter.GetRandomByTypeAsync(1);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Failed to retrieve random tip: Database error");
        }

        [Test]
        public async Task GetTipTypesAsync_ShouldReturnNotImplemented()
        {
            // Act
            var result = await _adapter.GetTipTypesAsync();

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("GetTipTypes not implemented in persistence layer");
        }

        [Test]
        public async Task CreateTipTypeAsync_ShouldReturnNotImplemented()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType();

            // Act
            var result = await _adapter.CreateTipTypeAsync(tipType);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("CreateTipType not implemented in persistence layer");
        }

        [Test]
        public void Constructor_WithNullRepository_ShouldThrowException()
        {
            // Act
            Action act = () => new TipRepositoryAdapter(null!);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("innerRepository");
        }

        [Test]
        public async Task GetByTypeAsync_WithEmptyResult_ShouldReturnEmptyList()
        {
            // Arrange
            _mockInnerRepository.Setup(x => x.GetByTipTypeIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Enumerable.Empty<Tip>());

            // Act
            var result = await _adapter.GetByTypeAsync(1);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task CreateAsync_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var tip = TestDataBuilder.CreateValidTip();
            var cancellationToken = new CancellationToken();
            _mockInnerRepository.Setup(x => x.AddAsync(It.IsAny<Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(tip);

            // Act
            await _adapter.CreateAsync(tip, cancellationToken);

            // Assert
            _mockInnerRepository.Verify(x => x.AddAsync(tip, cancellationToken), Times.Once);
        }
    }
}