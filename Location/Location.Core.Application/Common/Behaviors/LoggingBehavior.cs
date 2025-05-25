using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Common.Behaviors
{
    /// <summary>
    /// Implements a pipeline behavior that logs the execution of requests and their responses.
    /// </summary>
    /// <remarks>This behavior logs the start and completion of each request, including its execution time. If
    /// the request takes longer than 500 milliseconds, a warning is logged. If the response implements <see
    /// cref="IResult"/> and indicates a failure, the errors are logged as warnings. In the event of an exception, the
    /// behavior logs the error and rethrows the exception.</remarks>
    /// <typeparam name="TRequest">The type of the request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the request handler.</typeparam>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly IMediator _mediator;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoggingBehavior{TRequest, TResponse}"/> class.
        /// </summary>
        /// <param name="logger">The logger instance used to log information about the behavior.  Cannot be <see langword="null"/>.</param>
        /// <param name="mediator">The mediator used to publish error events.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="logger"/> is <see langword="null"/>.</exception>
        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger, IMediator mediator)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator;
        }
        /// <summary>
        /// Handles a request by invoking the next handler in the pipeline and logging relevant information.
        /// </summary>
        /// <remarks>This method logs the start, completion, and any errors encountered during the
        /// handling of the request.  It also logs warnings for long-running requests or requests that complete with a
        /// failure result.</remarks>
        /// <param name="request">The request object to be processed. Cannot be <see langword="null"/>.</param>
        /// <param name="next">The delegate representing the next handler in the pipeline.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the response of type
        /// <typeparamref name="TResponse"/>.</returns>
        public async Task<TResponse> Handle(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestGuid = Guid.NewGuid();

            var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            _logger.LogInformation(
                "Starting request {RequestGuid} {RequestName} {@Request}",
                requestGuid,
                requestName,
                requestJson);

            TResponse response;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                response = await next();
                stopwatch.Stop();

                if (stopwatch.ElapsedMilliseconds > 500)
                {
                    _logger.LogWarning(
                        "Long running request {RequestGuid} {RequestName} ({ElapsedMilliseconds} milliseconds) {@Request}",
                        requestGuid,
                        requestName,
                        stopwatch.ElapsedMilliseconds,
                        requestJson);
                }

                if (response is IResult result && !result.IsSuccess)
                {
                    _logger.LogWarning(
                        "Request completed with failure {RequestGuid} {RequestName} {@Errors}",
                        requestGuid,
                        requestName,
                        result.Errors);
                }
                else
                {
                    _logger.LogInformation(
                        "Completed request {RequestGuid} {RequestName} in {ElapsedMilliseconds}ms",
                        requestGuid,
                        requestName,
                        stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Request failed {RequestGuid} {RequestName} after {ElapsedMilliseconds}ms {@Request}",
                    requestGuid,
                    requestName,
                    stopwatch.ElapsedMilliseconds,
                    requestJson);

                // Publish error event for unexpected exceptions
                var entityType = requestName.Replace("Command", "").Replace("Query", "");
                await _mediator.Publish(new ValidationErrorEvent(entityType, new[] { Error.Domain(ex.Message) }, $"{requestName}Handler"), cancellationToken);

                throw;
            }

            return response;
        }
    }
}