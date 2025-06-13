using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
using Location.Photography.Application.Common.Constants;
using Location.Photography.Application.Resources;
using MediatR;

namespace Location.Photography.Application.Commands.Subscription
{
    public class StoreSubscriptionInSettingsCommand : IRequest<Result<bool>>
    {
        public string ProductId { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public DateTime PurchaseDate { get; set; }
        public string TransactionId { get; set; } = string.Empty;
    }

    public class StoreSubscriptionInSettingsCommandHandler : IRequestHandler<StoreSubscriptionInSettingsCommand, Result<bool>>
    {
        private readonly IUnitOfWork _unitOfWork;

        public StoreSubscriptionInSettingsCommandHandler(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<Result<bool>> Handle(StoreSubscriptionInSettingsCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Update subscription type based on product
                string subscriptionType;
                if (request.ProductId.Contains("premium"))
                {
                    subscriptionType = SubscriptionConstants.Premium;
                }
                else if (request.ProductId.Contains("professional") || request.ProductId.Contains("pro"))
                {
                    subscriptionType = SubscriptionConstants.Pro;
                }
                else
                {
                    subscriptionType = SubscriptionConstants.Premium; // Default fallback
                }

                await UpdateOrCreateSettingAsync(SubscriptionConstants.SubscriptionType, subscriptionType, cancellationToken);

                // Store expiration date
                await UpdateOrCreateSettingAsync(SubscriptionConstants.SubscriptionExpiration, request.ExpirationDate.ToString("yyyy-MM-dd HH:mm:ss"), cancellationToken);

                // Store additional subscription details
                await UpdateOrCreateSettingAsync(SubscriptionConstants.SubscriptionProductId, request.ProductId, cancellationToken);
                await UpdateOrCreateSettingAsync(SubscriptionConstants.SubscriptionPurchaseDate, request.PurchaseDate.ToString("yyyy-MM-dd HH:mm:ss"), cancellationToken);
                await UpdateOrCreateSettingAsync(SubscriptionConstants.SubscriptionTransactionId, request.TransactionId, cancellationToken);

                return Result<bool>.Success(true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure(string.Format(AppResources.Subscription_Error_StoringFailed + ": {0}", ex.Message));
            }
        }

        private async Task UpdateOrCreateSettingAsync(string key, string value, CancellationToken cancellationToken)
        {
            // Try to get existing setting
            var existingSettingResult = await _unitOfWork.Settings.GetByKeyAsync(key, cancellationToken);

            if (existingSettingResult.IsSuccess && existingSettingResult.Data != null)
            {
                // Update existing setting
                var setting = existingSettingResult.Data;
                setting.UpdateValue(value);
                await _unitOfWork.Settings.UpdateAsync(setting, cancellationToken);
            }
            else
            {
                // Create new setting
                var newSetting = new Setting(key, value, $"Subscription setting for {key}");
                await _unitOfWork.Settings.CreateAsync(newSetting, cancellationToken);
            }
        }
    }
}