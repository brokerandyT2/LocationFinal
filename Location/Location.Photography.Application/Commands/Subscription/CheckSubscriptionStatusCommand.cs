using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Application.Resources;
using MediatR;

namespace Location.Photography.Application.Commands.Subscription
{
    public class CheckSubscriptionStatusCommand : IRequest<Result<SubscriptionStatusResult>>
    {
        // No parameters needed
    }

    public class CheckSubscriptionStatusCommandHandler : IRequestHandler<CheckSubscriptionStatusCommand, Result<SubscriptionStatusResult>>
    {
        private readonly ISubscriptionStatusService _subscriptionStatusService;

        public CheckSubscriptionStatusCommandHandler(ISubscriptionStatusService subscriptionStatusService)
        {
            _subscriptionStatusService = subscriptionStatusService ?? throw new ArgumentNullException(nameof(subscriptionStatusService));
        }

        public async Task<Result<SubscriptionStatusResult>> Handle(CheckSubscriptionStatusCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await _subscriptionStatusService.CheckSubscriptionStatusAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<SubscriptionStatusResult>.Failure(string.Format(AppResources.Subscription_Error_StatusCheckFailed + ": {0}", ex.Message));
            }
        }
    }
}