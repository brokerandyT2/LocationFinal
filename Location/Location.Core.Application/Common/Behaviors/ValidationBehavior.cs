using FluentValidation;
using MediatR;
using Location.Core.Application.Common.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;

namespace Location.Core.Application.Common.Behaviors
{
    /// <summary>
    /// Pipeline behavior for validating requests using FluentValidation
    /// </summary>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : IResult  // Removed the new() constraint
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;

        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }

        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            if (!_validators.Any())
            {
                return await next();
            }

            var context = new ValidationContext<TRequest>(request);

            try
            {
                var validationResults = await Task.WhenAll(
                    _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

                var failures = validationResults
                    .SelectMany(result => result.Errors)
                    .Where(f => f != null)
                    .ToList();

                if (failures.Count != 0)
                {
                    var errors = failures.Select(f => Error.Validation(f.PropertyName, f.ErrorMessage));

                    // Create appropriate failure response based on type
                    if (typeof(TResponse).IsGenericType &&
                        typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                    {
                        var genericArg = typeof(TResponse).GetGenericArguments()[0];
                        var resultType = typeof(Result<>).MakeGenericType(genericArg);
                        var failureMethod = resultType.GetMethod("Failure", new[] { typeof(IEnumerable<Error>) });
                        return (TResponse)failureMethod!.Invoke(null, new object[] { errors })!;
                    }
                    else if (typeof(TResponse) == typeof(Result))
                    {
                        return (TResponse)(object)Result.Failure(errors);
                    }

                    // If we reach here, we don't know how to create a failure result
                    throw new InvalidOperationException($"Cannot create failure result for type {typeof(TResponse).Name}");
                }
            }
            catch (ValidationException ex)
            {
                var error = Error.Validation("ValidationException", ex.Message);

                if (typeof(TResponse).IsGenericType &&
                    typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
                {
                    var genericArg = typeof(TResponse).GetGenericArguments()[0];
                    var resultType = typeof(Result<>).MakeGenericType(genericArg);
                    var failureMethod = resultType.GetMethod("Failure", new[] { typeof(Error) });
                    return (TResponse)failureMethod!.Invoke(null, new object[] { error })!;
                }
                else if (typeof(TResponse) == typeof(Result))
                {
                    return (TResponse)(object)Result.Failure(error);
                }

                // If we reach here, we don't know how to create a failure result
                throw new InvalidOperationException($"Cannot create failure result for type {typeof(TResponse).Name}");
            }

            return await next();
        }
    }
}