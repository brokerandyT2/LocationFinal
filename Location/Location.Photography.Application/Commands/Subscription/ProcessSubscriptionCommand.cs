// Location.Photography.Application/Commands/Subscription/ProcessSubscriptionCommand.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Commands.Subscription
{
    public class ProcessSubscriptionCommand : IRequest<Result<ProcessSubscriptionResultDto>>
    {
        public string ProductId { get; set; } = string.Empty;
        public SubscriptionPeriod Period { get; set; }
    }

    public class ProcessSubscriptionResultDto
    {
        public bool IsSuccessful { get; set; }
        public string TransactionId { get; set; } = string.Empty;
        public string PurchaseToken { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class ProcessSubscriptionCommandHandler : IRequestHandler<ProcessSubscriptionCommand, Result<ProcessSubscriptionResultDto>>
    {
        private readonly ISubscriptionService _subscriptionService;
        private readonly IMediator _mediator;

        public ProcessSubscriptionCommandHandler(ISubscriptionService subscriptionService, IMediator mediator)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        }

        public async Task<Result<ProcessSubscriptionResultDto>> Handle(ProcessSubscriptionCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await _subscriptionService.PurchaseSubscriptionAsync(request.ProductId, cancellationToken);

                if (!result.IsSuccess)
                {
                    return Result<ProcessSubscriptionResultDto>.Failure(result.ErrorMessage ?? "Failed to process subscription");
                }

                // Store subscription data in SQLite
                var storeResult = await _subscriptionService.StoreSubscriptionAsync(result.Data, cancellationToken);

                if (!storeResult.IsSuccess)
                {
                    return Result<ProcessSubscriptionResultDto>.Failure("Subscription processed but failed to store locally");
                }

                // Store subscription details in settings table
                var storeSettingsCommand = new StoreSubscriptionInSettingsCommand
                {
                    ProductId = result.Data.ProductId,
                    ExpirationDate = result.Data.ExpirationDate,
                    PurchaseDate = result.Data.PurchaseDate,
                    TransactionId = result.Data.TransactionId
                };

                var settingsResult = await _mediator.Send(storeSettingsCommand, cancellationToken);

                if (!settingsResult.IsSuccess)
                {
                    // Log warning but don't fail the entire operation
                    // The subscription is still valid, just the settings storage failed
                }

                return Result<ProcessSubscriptionResultDto>.Success(result.Data);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<ProcessSubscriptionResultDto>.Failure($"Error processing subscription: {ex.Message}");
            }
        }
    }
}