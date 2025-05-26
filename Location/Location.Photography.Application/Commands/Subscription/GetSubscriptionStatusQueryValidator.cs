// Location.Photography.Application/Queries/Subscription/GetSubscriptionStatusQueryValidator.cs
using FluentValidation;

namespace Location.Photography.Application.Queries.Subscription
{
    public class GetSubscriptionStatusQueryValidator : AbstractValidator<GetSubscriptionStatusQuery>
    {
        public GetSubscriptionStatusQueryValidator()
        {
            // No validation rules needed for subscription status query
            // The query has no parameters to validate
        }
    }
}