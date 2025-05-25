// Location.Photography.Application/Queries/Subscription/GetSubscriptionStatusQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Commands.Subscription;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Queries.Subscription
{
    public class GetSubscriptionStatusQuery : IRequest<Result<SubscriptionStatusDto>>
    {
        // No parameters needed - gets current user's subscription status
    }

    public class SubscriptionStatusDto
    {
        public bool HasActiveSubscription { get; set; }
        public string ProductId { get; set; } = string.Empty;
        public SubscriptionStatus Status { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public SubscriptionPeriod Period { get; set; }
        public bool IsExpiringSoon { get; set; }
        public int DaysUntilExpiration { get; set; }
    }

    public class GetSubscriptionStatusQueryHandler : IRequestHandler<GetSubscriptionStatusQuery, Result<SubscriptionStatusDto>>
    {
        private readonly ISubscriptionService _subscriptionService;

        public GetSubscriptionStatusQueryHandler(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService ?? throw new ArgumentNullException(nameof(subscriptionService));
        }

        public async Task<Result<SubscriptionStatusDto>> Handle(GetSubscriptionStatusQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var result = await _subscriptionService.GetCurrentSubscriptionStatusAsync(cancellationToken);

                if (!result.IsSuccess)
                {
                    return Result<SubscriptionStatusDto>.Failure(result.ErrorMessage ?? "Failed to retrieve subscription status");
                }

                return Result<SubscriptionStatusDto>.Success(result.Data);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<SubscriptionStatusDto>.Failure($"Error retrieving subscription status: {ex.Message}");
            }
        }
    }
}