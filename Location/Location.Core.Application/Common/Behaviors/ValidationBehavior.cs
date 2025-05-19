using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Common.Behaviors
{
    /// <summary>
    /// Represents a pipeline behavior that performs validation on a request before passing it to the next handler in
    /// the pipeline.
    /// </summary>
    /// <remarks>This behavior uses a collection of validators implementing <see cref="IValidator{T}"/> to
    /// validate the incoming request. If validation errors are found, a failure result is returned without invoking the
    /// next handler in the pipeline. If no validators are registered or no validation errors are found, the request is
    /// passed to the next handler.</remarks>
    /// <typeparam name="TRequest">The type of the request being processed. Must implement <see cref="IRequest{TResponse}"/>.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the handler. Must implement <see cref="IResult"/>.</typeparam>
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : class, IResult
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationBehavior{TRequest}"/> class with the specified
        /// collection of validators.
        /// </summary>
        /// <remarks>The <paramref name="validators"/> parameter cannot be null. If no validators are
        /// provided, the behavior will not perform any validation.</remarks>
        /// <param name="validators">A collection of validators to be used for validating <typeparamref name="TRequest"/> instances. Each
        /// validator in the collection must implement the <see cref="IValidator{T}"/> interface.</param>
        public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        {
            _validators = validators;
        }
        /// <summary>
        /// Processes the specified request by validating it and invoking the next handler in the pipeline.
        /// </summary>
        /// <remarks>If no validators are registered, the request is passed directly to the next handler
        /// without validation. If validation errors are found, the method attempts to create a failure result of type
        /// <typeparamref name="TResponse"/>. The method supports responses of type <see cref="Result"/> or <see
        /// cref="Result{T}"/> for failure handling. If the response type does not support failure results, an <see
        /// cref="InvalidOperationException"/> is thrown.</remarks>
        /// <param name="request">The request object to be processed. Cannot be null.</param>
        /// <param name="next">The delegate representing the next handler in the pipeline. Cannot be null.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response of type
        /// <typeparamref name="TResponse"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if validation errors are found and the response type <typeparamref name="TResponse"/> does not
        /// support failure results.</exception>
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

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(result => result.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                var errors = failures.Select(f => Error.Validation(f.PropertyName, f.ErrorMessage));

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

                throw new InvalidOperationException($"Cannot create failure result for type {typeof(TResponse).Name}");
            }

            return await next();
        }
    }
}