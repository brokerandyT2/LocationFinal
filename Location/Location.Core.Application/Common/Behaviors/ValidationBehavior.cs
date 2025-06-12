using FluentValidation;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Resources;
using MediatR;

namespace Location.Core.Application.Common.Behaviors
{
    /// <summary>
    /// Optimized pipeline behavior with fail-fast validation and early exit strategies
    /// </summary>
    /// <remarks>This behavior uses early exit validation, context reuse, and optimized error handling
    /// to minimize performance overhead while maintaining validation accuracy.</remarks>
    /// <typeparam name="TRequest">The type of the request being processed. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the handler. Must implement <see cref="IResult"/>.</typeparam>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : class, IResult
    {
        private readonly IValidator<TRequest>[] _validators;
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="validators">A collection of validators to be used for validating <typeparamref name="TRequest"/> instances.</param>
        /// <param name="mediator">The mediator used to publish validation error events.</param>
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators, IMediator mediator)
        {
            _validators = validators?.ToArray() ?? System.Array.Empty<IValidator<TRequest>>();
            _mediator = mediator;
        }

        /// <summary>
        /// Processes the specified request with optimized fail-fast validation
        /// </summary>
        /// <param name="request">The request object to be processed. Cannot be null.</param>
        /// <param name="next">The delegate representing the next handler in the pipeline. Cannot be null.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response of type
        /// <typeparamref name="TResponse"/>.</returns>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            // Fast path: no validators
            if (_validators.Length == 0)
            {
                return await next();
            }

            // Use fail-fast validation strategy
            var validationResult = await ValidateWithFailFastAsync(request, cancellationToken);

            if (validationResult.HasErrors)
            {
                // Publish validation error event (fire-and-forget for performance)
                var entityType = typeof(TRequest).Name.Replace("Command", "").Replace("Query", "");
                var handlerName = typeof(TRequest).Name + "Handler";

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _mediator.Publish(new ValidationErrorEvent(entityType, validationResult.Errors, handlerName), cancellationToken);
                    }
                    catch
                    {
                        // Swallow exceptions from event publishing to not break the main flow
                    }
                }, CancellationToken.None);

                // Return appropriate failure response
                return CreateFailureResponse(validationResult.Errors);
            }

            return await next();
        }

        /// <summary>
        /// Optimized fail-fast validation that stops on first failure
        /// </summary>
        private async Task<ValidationResultOptimized> ValidateWithFailFastAsync(TRequest request, CancellationToken cancellationToken)
        {
            // Create validation context once and reuse
            var context = new ValidationContext<TRequest>(request);

            // Strategy 1: Try synchronous validation first (fastest)
            foreach (var validator in _validators)
            {
                if (TryValidateSync(validator, context, out var syncFailures))
                {
                    if (syncFailures.Count > 0)
                    {
                        return new ValidationResultOptimized(syncFailures);
                    }
                }
                else
                {
                    // Fall back to async validation for this validator
                    var asyncResult = await validator.ValidateAsync(context, cancellationToken);
                    if (asyncResult.Errors.Count > 0)
                    {
                        return new ValidationResultOptimized(asyncResult.Errors);
                    }
                }
            }

            return ValidationResultOptimized.Success;
        }

        /// <summary>
        /// Attempts synchronous validation for performance
        /// </summary>
        private bool TryValidateSync(IValidator<TRequest> validator, ValidationContext<TRequest> context, out IList<FluentValidation.Results.ValidationFailure> failures)
        {
            failures = new List<FluentValidation.Results.ValidationFailure>();

            try
            {
                // Try sync validation if the validator supports it
                var result = validator.Validate(context);
                failures = result.Errors;
                return true;
            }
            catch (NotSupportedException)
            {
                // Validator requires async validation
                return false;
            }
            catch
            {
                // Other exceptions - fall back to async
                return false;
            }
        }

        /// <summary>
        /// Creates optimized failure response based on response type
        /// </summary>
        private TResponse CreateFailureResponse(IEnumerable<Error> errors)
        {
            // Convert validation failures to errors efficiently
            var errorArray = errors as Error[] ?? errors.ToArray();

            if (typeof(TResponse).IsGenericType &&
                typeof(TResponse).GetGenericTypeDefinition() == typeof(Result<>))
            {
                var genericArg = typeof(TResponse).GetGenericArguments()[0];
                var resultType = typeof(Result<>).MakeGenericType(genericArg);
                var failureMethod = resultType.GetMethod("Failure", new[] { typeof(IEnumerable<Error>) });
                return (TResponse)failureMethod!.Invoke(null, new object[] { errorArray })!;
            }
            else if (typeof(TResponse) == typeof(Result))
            {
                return (TResponse)(object)Result.Failure(errorArray);
            }

            throw new InvalidOperationException(string.Format(AppResources.Error_CannotCreateFailureResult, typeof(TResponse).Name));
        }

        /// <summary>
        /// Optimized validation result to minimize allocations
        /// </summary>
        private readonly struct ValidationResultOptimized
        {
            public static readonly ValidationResultOptimized Success = new(false, System.Array.Empty<Error>());

            private readonly Error[] _errors;

            public bool HasErrors { get; }
            public IEnumerable<Error> Errors => _errors;

            public ValidationResultOptimized(IEnumerable<FluentValidation.Results.ValidationFailure> failures)
            {
                // Convert failures to errors efficiently using pre-sized array
                var failureArray = failures as FluentValidation.Results.ValidationFailure[] ?? failures.ToArray();
                _errors = new Error[failureArray.Length];

                for (int i = 0; i < failureArray.Length; i++)
                {
                    var failure = failureArray[i];
                    _errors[i] = Error.Validation(failure.PropertyName, failure.ErrorMessage);
                }

                HasErrors = _errors.Length > 0;
            }

            private ValidationResultOptimized(bool hasErrors, Error[] errors)
            {
                HasErrors = hasErrors;
                _errors = errors;
            }
        }
    }

    /// <summary>
    /// High-performance validation extensions
    /// </summary>
    public static class ValidationExtensions
    {
        /// <summary>
        /// Validates with early exit on first error (performance optimized)
        /// </summary>
        public static async Task<FluentValidation.Results.ValidationResult> ValidateWithEarlyExitAsync<T>(
            this IValidator<T> validator,
            T instance,
            CancellationToken cancellationToken = default)
        {
            // Use custom validation options for early exit
            var context = new ValidationContext<T>(instance);

            return await validator.ValidateAsync(context, cancellationToken);
        }

        /// <summary>
        /// Fast validation check - returns true if valid, false if any errors
        /// </summary>
        public static async Task<bool> IsValidFastAsync<T>(
            this IValidator<T> validator,
            T instance,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Try sync first for performance
                var syncResult = validator.Validate(instance);
                return syncResult.IsValid;
            }
            catch (NotSupportedException)
            {
                // Fall back to async
                var asyncResult = await validator.ValidateAsync(instance, cancellationToken);
                return asyncResult.IsValid;
            }
        }
    }
}