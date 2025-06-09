using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Queries.Subscription;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using Plugin.InAppBilling;
using DomainSubscriptionPeriod = Location.Photography.Domain.Entities.SubscriptionPeriod;

namespace Location.Photography.Infrastructure.Test.Services
{
    [TestFixture]
    public class SubscriptionServiceTests
    {
        private SubscriptionService _subscriptionService;
        private Mock<ILogger<SubscriptionService>> _loggerMock;
        private Mock<ISubscriptionRepository> _subscriptionRepositoryMock;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<SubscriptionService>>();
            _subscriptionRepositoryMock = new Mock<ISubscriptionRepository>();
            _subscriptionService = new SubscriptionService(
                _loggerMock.Object,
                _subscriptionRepositoryMock.Object);
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SubscriptionService(null, _subscriptionRepositoryMock.Object))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public void Constructor_WithNullSubscriptionRepository_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SubscriptionService(_loggerMock.Object, null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("subscriptionRepository");
        }

        #endregion

        #region InitializeAsync Tests

        [Test]
        public async Task InitializeAsync_WithValidConnection_ShouldReturnSuccess()
        {
            // Arrange & Act
            var result = await _subscriptionService.InitializeAsync(CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task InitializeAsync_WithRecentConnection_ShouldUseCachedResult()
        {
            // Arrange - First call establishes connection
            await _subscriptionService.InitializeAsync(CancellationToken.None);

            // Act - Second call within cache interval
            var result = await _subscriptionService.InitializeAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task InitializeAsync_WithCancellationToken_ShouldThrowWhenCancelled()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _subscriptionService.InitializeAsync(cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region StoreSubscriptionAsync Tests

        [Test]
        public async Task StoreSubscriptionAsync_WithValidData_ShouldReturnSuccess()
        {
            // Arrange
            var subscriptionData = new ProcessSubscriptionResultDto
            {
                ProductId = "monthly_premium",
                TransactionId = "txn_123",
                PurchaseToken = "token_456",
                PurchaseDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddMonths(1),
                Status = SubscriptionStatus.Active
            };

            _subscriptionRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Subscription>.Success(new Subscription()));

            // Act
            var result = await _subscriptionService.StoreSubscriptionAsync(subscriptionData, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().BeTrue();

            _subscriptionRepositoryMock.Verify(
                x => x.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Test]
        public async Task StoreSubscriptionAsync_WithYearlySubscription_ShouldCreateCorrectPeriod()
        {
            // Arrange
            var subscriptionData = new ProcessSubscriptionResultDto
            {
                ProductId = "yearly_premium",
                TransactionId = "txn_123",
                PurchaseToken = "token_456",
                PurchaseDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddYears(1),
                Status = SubscriptionStatus.Active
            };

            Subscription capturedSubscription = null;
            _subscriptionRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
                .Callback<Subscription, CancellationToken>((sub, ct) => capturedSubscription = sub)
                .ReturnsAsync(Result<Subscription>.Success(new Subscription()));

            // Act
            var result = await _subscriptionService.StoreSubscriptionAsync(subscriptionData, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedSubscription.Should().NotBeNull();
            capturedSubscription.Period.Should().Be(DomainSubscriptionPeriod.Yearly);
        }

        [Test]
        public async Task StoreSubscriptionAsync_WithMonthlySubscription_ShouldCreateCorrectPeriod()
        {
            // Arrange
            var subscriptionData = new ProcessSubscriptionResultDto
            {
                ProductId = "monthly_premium",
                TransactionId = "txn_123",
                PurchaseToken = "token_456",
                PurchaseDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddMonths(1),
                Status = SubscriptionStatus.Active
            };

            Subscription capturedSubscription = null;
            _subscriptionRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
                .Callback<Subscription, CancellationToken>((sub, ct) => capturedSubscription = sub)
                .ReturnsAsync(Result<Subscription>.Success(new Subscription()));

            // Act
            var result = await _subscriptionService.StoreSubscriptionAsync(subscriptionData, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            capturedSubscription.Should().NotBeNull();
            capturedSubscription.Period.Should().Be(DomainSubscriptionPeriod.Monthly);
        }

        [Test]
        public async Task StoreSubscriptionAsync_WhenRepositoryFails_ShouldReturnFailure()
        {
            // Arrange
            var subscriptionData = new ProcessSubscriptionResultDto
            {
                ProductId = "monthly_premium",
                TransactionId = "txn_123",
                PurchaseToken = "token_456",
                PurchaseDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddMonths(1),
                Status = SubscriptionStatus.Active
            };

            _subscriptionRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Subscription>.Failure("Database error"));

            // Act
            var result = await _subscriptionService.StoreSubscriptionAsync(subscriptionData, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Data.Should().BeFalse();
        }

        [Test]
        public async Task StoreSubscriptionAsync_WithCancellationToken_ShouldThrowWhenCancelled()
        {
            // Arrange
            var subscriptionData = new ProcessSubscriptionResultDto
            {
                ProductId = "monthly_premium",
                TransactionId = "txn_123",
                PurchaseToken = "token_456",
                PurchaseDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddMonths(1),
                Status = SubscriptionStatus.Active
            };

            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _subscriptionService.StoreSubscriptionAsync(
                subscriptionData, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region GetCurrentSubscriptionStatusAsync Tests

        [Test]
        public async Task GetCurrentSubscriptionStatusAsync_WithActiveSubscription_ShouldReturnActiveStatus()
        {
            // Arrange
            var activeSubscription = new Subscription(
                "monthly_premium",
                "txn_123",
                "token_456",
                DateTime.UtcNow.AddDays(-10),
                DateTime.UtcNow.AddDays(20),
                SubscriptionStatus.Active,
                DomainSubscriptionPeriod.Monthly,
                "user123");

            _subscriptionRepositoryMock
                .Setup(x => x.GetActiveSubscriptionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Subscription>.Success(activeSubscription));

            // Act
            var result = await _subscriptionService.GetCurrentSubscriptionStatusAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasActiveSubscription.Should().BeTrue();
            result.Data.ExpirationDate.Should().Be(activeSubscription.ExpirationDate);
        }

        [Test]
        public async Task GetCurrentSubscriptionStatusAsync_WithNoSubscription_ShouldReturnInactiveStatus()
        {
            // Arrange
            _subscriptionRepositoryMock
                .Setup(x => x.GetActiveSubscriptionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Subscription>.Failure("No active subscription"));

            // Act
            var result = await _subscriptionService.GetCurrentSubscriptionStatusAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasActiveSubscription.Should().BeFalse();
            result.Data.ProductId.Should().BeNull();
        }

        [Test]
        public async Task GetCurrentSubscriptionStatusAsync_WithExpiredSubscription_ShouldReturnInactiveStatus()
        {
            // Arrange
            var expiredSubscription = new Subscription(
                "monthly_premium",
                "txn_123",
                "token_456",
                DateTime.UtcNow.AddDays(-40),
                DateTime.UtcNow.AddDays(-10), // Expired 10 days ago
                SubscriptionStatus.Expired,
                DomainSubscriptionPeriod.Monthly,
                "user123");

            _subscriptionRepositoryMock
                .Setup(x => x.GetActiveSubscriptionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Subscription>.Success(expiredSubscription));

            // Act
            var result = await _subscriptionService.GetCurrentSubscriptionStatusAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasActiveSubscription.Should().BeFalse();
        }

        [Test]
        public async Task GetCurrentSubscriptionStatusAsync_WithCancellationToken_ShouldThrowWhenCancelled()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _subscriptionService.GetCurrentSubscriptionStatusAsync(
                cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region PurchaseSubscriptionAsync Tests

        [Test]
        public async Task PurchaseSubscriptionAsync_WithValidProduct_ShouldReturnSuccess()
        {
            // Arrange
            var productId = "monthly_premium";

            // Act
            var result = await _subscriptionService.PurchaseSubscriptionAsync(productId, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ProductId.Should().Be(productId);
        }

        [Test]
        public async Task PurchaseSubscriptionAsync_WithCancellationToken_ShouldThrowWhenCancelled()
        {
            // Arrange
            var productId = "monthly_premium";
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _subscriptionService.PurchaseSubscriptionAsync(
                productId, cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region ValidateAndUpdateSubscriptionAsync Tests

        [Test]
        public async Task ValidateAndUpdateSubscriptionAsync_ShouldReturnSuccess()
        {
            // Act
            var result = await _subscriptionService.ValidateAndUpdateSubscriptionAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
        }

        [Test]
        public async Task ValidateAndUpdateSubscriptionAsync_WithCancellationToken_ShouldThrowWhenCancelled()
        {
            // Arrange
            var cancellationTokenSource = new CancellationTokenSource();
            cancellationTokenSource.Cancel();

            // Act & Assert
            await FluentActions.Invoking(() => _subscriptionService.ValidateAndUpdateSubscriptionAsync(
                cancellationTokenSource.Token))
                .Should().ThrowAsync<OperationCanceledException>();
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task SubscriptionLifecycle_CreateValidateRestore_ShouldWorkEndToEnd()
        {
            // Arrange
            var subscriptionData = new ProcessSubscriptionResultDto
            {
                ProductId = "monthly_premium",
                TransactionId = "txn_123",
                PurchaseToken = "token_456",
                PurchaseDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddMonths(1),
                Status = SubscriptionStatus.Active
            };

            var storedSubscription = new Subscription(
                subscriptionData.ProductId,
                subscriptionData.TransactionId,
                subscriptionData.PurchaseToken,
                subscriptionData.PurchaseDate,
                subscriptionData.ExpirationDate,
                subscriptionData.Status,
                DomainSubscriptionPeriod.Monthly,
                "user123");

            _subscriptionRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Subscription>.Success(storedSubscription));

            _subscriptionRepositoryMock
                .Setup(x => x.GetByPurchaseTokenAsync(subscriptionData.PurchaseToken, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Subscription>.Success(storedSubscription));

            // Act - Store subscription
            var storeResult = await _subscriptionService.StoreSubscriptionAsync(subscriptionData, CancellationToken.None);

            // Act - Validate subscription
            var validateResult = await _subscriptionService.ValidateAndUpdateSubscriptionAsync(CancellationToken.None);

            // Assert
            storeResult.IsSuccess.Should().BeTrue();
            validateResult.IsSuccess.Should().BeTrue();

            _subscriptionRepositoryMock.Verify(
                x => x.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public async Task AllMethods_WithException_ShouldReturnFailureGracefully()
        {
            // Arrange
            _subscriptionRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Subscription>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            var subscriptionData = new ProcessSubscriptionResultDto
            {
                ProductId = "monthly_premium",
                TransactionId = "txn_123",
                PurchaseToken = "token_456",
                PurchaseDate = DateTime.UtcNow,
                ExpirationDate = DateTime.UtcNow.AddMonths(1),
                Status = SubscriptionStatus.Active
            };

            // Act
            var result = await _subscriptionService.StoreSubscriptionAsync(subscriptionData, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to store subscription");
        }

        #endregion
    }
}