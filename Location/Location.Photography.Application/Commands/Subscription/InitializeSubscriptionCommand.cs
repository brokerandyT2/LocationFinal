using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Application.Resources;
using MediatR;

namespace Location.Photography.Application.Commands.Subscription
{
    public class InitializeSubscriptionCommand : IRequest<Result<InitializeSubscriptionResultDto>>
    {
        // No parameters needed for initialization
    }

    public class InitializeSubscriptionResultDto
    {
        public List<SubscriptionProductDto> Products { get; set; } = new List<SubscriptionProductDto>();
        public bool IsConnected { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class SubscriptionProductDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Price { get; set; } = string.Empty;
        public string PriceAmountMicros { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public SubscriptionPeriod Period { get; set; }
    }

    public class InitializeSubscriptionCommandHandler : IRequestHandler<InitializeSubscriptionCommand, Result<InitializeSubscriptionResultDto>>
    {
        private readonly ISubscriptionService _subscriptionService;

        public InitializeSubscriptionCommandHandler(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
        }

        public async Task<Result<InitializeSubscriptionResultDto>> Handle(InitializeSubscriptionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await _subscriptionService.InitializeAsync(cancellationToken);

                if (!result.IsSuccess)
                {
                    return Result<InitializeSubscriptionResultDto>.Failure(result.ErrorMessage ?? AppResources.Subscription_Error_InitializationFailed);
                }

                var products = await _subscriptionService.GetAvailableProductsAsync(cancellationToken);

                if (!products.IsSuccess)
                {
                    return Result<InitializeSubscriptionResultDto>.Failure(products.ErrorMessage ?? AppResources.Subscription_Error_ProductRetrievalFailed);
                }

                return Result<InitializeSubscriptionResultDto>.Success(new InitializeSubscriptionResultDto
                {
                    Products = products.Data ?? new List<SubscriptionProductDto>(),
                    IsConnected = result.Data,
                    ErrorMessage = string.Empty
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<InitializeSubscriptionResultDto>.Failure(AppResources.Subscription_Error_InitializationFailed);
            }
        }
    }
}