using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Constants;
using Location.Photography.Application.Services;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Services
{
    [TestFixture]
    public class SubscriptionFeatureGuardTests
    {
        private SubscriptionFeatureGuard _subscriptionFeatureGuard;
        private Mock<ISubscriptionStatusService> _subscriptionStatusServiceMock;

        [SetUp]
        public void SetUp()
        {
            _subscriptionStatusServiceMock = new Mock<ISubscriptionStatusService>();
            _subscriptionFeatureGuard = new SubscriptionFeatureGuard(_subscriptionStatusServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullSubscriptionStatusService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new SubscriptionFeatureGuard(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("subscriptionStatusService");
        }

        #region CheckPremiumFeatureAccessAsync Tests

        [Test]
        public async Task CheckPremiumFeatureAccessAsync_WithActiveSubscription_ShouldAllowAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessPremiumFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _subscriptionFeatureGuard.CheckPremiumFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeTrue();
            result.Data.Action.Should().Be(FeatureAccessAction.Allow);
        }

        [Test]
        public async Task CheckPremiumFeatureAccessAsync_WithoutSubscription_ShouldDenyAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessPremiumFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            _subscriptionStatusServiceMock
                .Setup(x => x.CheckSubscriptionStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                {
                    HasActiveSubscription = false,
                    IsInGracePeriod = false
                }));

            // Act
            var result = await _subscriptionFeatureGuard.CheckPremiumFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeFalse();
            result.Data.RequiredSubscription.Should().Be(SubscriptionConstants.Premium);
            result.Data.Action.Should().Be(FeatureAccessAction.ShowUpgradePrompt);
            result.Data.Message.Should().Be("Premium subscription required to access this feature");
        }

        [Test]
        public async Task CheckPremiumFeatureAccessAsync_WithGracePeriod_ShouldShowGracePeriodMessage()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessPremiumFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            _subscriptionStatusServiceMock
                .Setup(x => x.CheckSubscriptionStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                {
                    HasActiveSubscription = false,
                    IsInGracePeriod = true
                }));

            // Act
            var result = await _subscriptionFeatureGuard.CheckPremiumFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeFalse();
            result.Data.RequiredSubscription.Should().Be(SubscriptionConstants.Premium);
            result.Data.Action.Should().Be(FeatureAccessAction.ShowUpgradePrompt);
            result.Data.Message.Should().Be("Your premium subscription has expired. You have limited time remaining to renew.");
        }

        [Test]
        public async Task CheckPremiumFeatureAccessAsync_WhenServiceFails_ShouldReturnDeniedAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessPremiumFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Service error"));

            // Act
            var result = await _subscriptionFeatureGuard.CheckPremiumFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeFalse();
            result.Data.RequiredSubscription.Should().Be(SubscriptionConstants.Premium);
            result.Data.Action.Should().Be(FeatureAccessAction.ShowUpgradePrompt);
            result.Data.Message.Should().Be("Premium subscription required to access this feature");
        }

        #endregion

        #region CheckProFeatureAccessAsync Tests

        [Test]
        public async Task CheckProFeatureAccessAsync_WithActiveSubscription_ShouldAllowAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessProFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _subscriptionFeatureGuard.CheckProFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeTrue();
            result.Data.Action.Should().Be(FeatureAccessAction.Allow);
        }

        [Test]
        public async Task CheckProFeatureAccessAsync_WithoutSubscription_ShouldDenyAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessProFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            _subscriptionStatusServiceMock
                .Setup(x => x.CheckSubscriptionStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                {
                    HasActiveSubscription = false,
                    IsInGracePeriod = false
                }));

            // Act
            var result = await _subscriptionFeatureGuard.CheckProFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeFalse();
            result.Data.RequiredSubscription.Should().Be(SubscriptionConstants.Pro);
            result.Data.Action.Should().Be(FeatureAccessAction.ShowUpgradePrompt);
            result.Data.Message.Should().Be("Professional subscription required to access this feature");
        }

        [Test]
        public async Task CheckProFeatureAccessAsync_WithGracePeriod_ShouldShowGracePeriodMessage()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessProFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            _subscriptionStatusServiceMock
                .Setup(x => x.CheckSubscriptionStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                {
                    HasActiveSubscription = false,
                    IsInGracePeriod = true
                }));

            // Act
            var result = await _subscriptionFeatureGuard.CheckProFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeFalse();
            result.Data.RequiredSubscription.Should().Be(SubscriptionConstants.Pro);
            result.Data.Action.Should().Be(FeatureAccessAction.ShowUpgradePrompt);
            result.Data.Message.Should().Be("Your professional subscription has expired. You have limited time remaining to renew.");
        }

        [Test]
        public async Task CheckProFeatureAccessAsync_WhenServiceFails_ShouldReturnDeniedAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessProFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Failure("Service error"));

            // Act
            var result = await _subscriptionFeatureGuard.CheckProFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeFalse();
            result.Data.RequiredSubscription.Should().Be(SubscriptionConstants.Pro);
            result.Data.Action.Should().Be(FeatureAccessAction.ShowUpgradePrompt);
            result.Data.Message.Should().Be("Professional subscription required to access this feature");
        }

        #endregion

        #region CheckPaidFeatureAccessAsync Tests

        [Test]
        public async Task CheckPaidFeatureAccessAsync_WithPremiumSubscription_ShouldAllowAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessPremiumFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _subscriptionFeatureGuard.CheckPaidFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeTrue();
            result.Data.Action.Should().Be(FeatureAccessAction.Allow);
        }

        [Test]
        public async Task CheckPaidFeatureAccessAsync_WithProSubscription_ShouldAllowAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessPremiumFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            _subscriptionStatusServiceMock
                .Setup(x => x.CheckSubscriptionStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                {
                    HasActiveSubscription = false,
                    IsInGracePeriod = false
                }));

            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessProFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            var result = await _subscriptionFeatureGuard.CheckPaidFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeTrue();
            result.Data.Action.Should().Be(FeatureAccessAction.Allow);
        }

        [Test]
        public async Task CheckPaidFeatureAccessAsync_WithoutAnySubscription_ShouldDenyAccess()
        {
            // Arrange
            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessPremiumFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            _subscriptionStatusServiceMock
                .Setup(x => x.CheckSubscriptionStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SubscriptionStatusResult>.Success(new SubscriptionStatusResult
                {
                    HasActiveSubscription = false,
                    IsInGracePeriod = false
                }));

            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessProFeaturesAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            // Act
            var result = await _subscriptionFeatureGuard.CheckPaidFeatureAccessAsync(CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.HasAccess.Should().BeFalse();
            result.Data.RequiredSubscription.Should().Be(SubscriptionConstants.Premium);
            result.Data.Action.Should().Be(FeatureAccessAction.ShowUpgradePrompt);
            result.Data.Message.Should().Be("Premium or Professional subscription required to access this feature");
        }

        #endregion

        #region Cancellation Token Tests

        [Test]
        public async Task CheckPremiumFeatureAccessAsync_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();

            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessPremiumFeaturesAsync(cancellationToken))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            await _subscriptionFeatureGuard.CheckPremiumFeatureAccessAsync(cancellationToken);

            // Assert
            _subscriptionStatusServiceMock.Verify(x => x.CanAccessPremiumFeaturesAsync(cancellationToken), Times.Once);
        }

        [Test]
        public async Task CheckProFeatureAccessAsync_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var cancellationToken = new CancellationToken();

            _subscriptionStatusServiceMock
                .Setup(x => x.CanAccessProFeaturesAsync(cancellationToken))
                .ReturnsAsync(Result<bool>.Success(true));

            // Act
            await _subscriptionFeatureGuard.CheckProFeatureAccessAsync(cancellationToken);

            // Assert
            _subscriptionStatusServiceMock.Verify(x => x.CanAccessProFeaturesAsync(cancellationToken), Times.Once);
        }

        #endregion
    }
}