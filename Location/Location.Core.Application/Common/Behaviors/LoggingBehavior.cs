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
    /// Optimized pipeline behavior that logs the execution of requests and their responses with minimal performance impact.
    /// </summary>
    /// <remarks>This behavior uses async serialization, size limits, and conditional formatting to minimize
    /// performance overhead while maintaining useful logging capabilities.</remarks>
    /// <typeparam name="TRequest">The type of the request being handled.</typeparam>
    /// <typeparam name="TResponse">The type of the response returned by the request handler.</typeparam>
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
        private readonly IMediator _mediator;

        // Optimized JSON options - reused for performance
        private static readonly JsonSerializerOptions _compactJsonOptions = new()
        {
            WriteIndented = false,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private static readonly JsonSerializerOptions _indentedJsonOptions = new()
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        private const int MaxSerializationLength = 2048; // Limit serialization size
        private const long SlowRequestThresholdMs = 500;

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
        /// Handles a request by invoking the next handler in the pipeline and logging relevant information with optimized performance.
        /// </summary>
        /// <remarks>This method uses async serialization, size limits, and conditional formatting to minimize
        /// performance impact while providing useful logging information.</remarks>
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

            // Pre-serialize request for logging (async and size-limited)
            var requestJson = await SerializeRequestAsync(request);

            _logger.LogInformation(
                "Starting request {RequestGuid} {RequestName}",
                requestGuid,
                requestName);

            // Only log request details if debug level is enabled (performance optimization)
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Request details {RequestGuid} {RequestName}: {RequestJson}",
                    requestGuid,
                    requestName,
                    requestJson);
            }

            TResponse response;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                response = await next();
                stopwatch.Stop();

                var elapsedMs = stopwatch.ElapsedMilliseconds;

                // Log based on performance threshold
                if (elapsedMs > SlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow request {RequestGuid} {RequestName} completed in {ElapsedMilliseconds}ms",
                        requestGuid,
                        requestName,
                        elapsedMs);

                    // Only include request details for slow requests if debug enabled
                    if (_logger.IsEnabled(LogLevel.Debug))
                    {
                        _logger.LogDebug(
                            "Slow request details {RequestGuid}: {RequestJson}",
                            requestGuid,
                            requestJson);
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "Completed request {RequestGuid} {RequestName} in {ElapsedMilliseconds}ms",
                        requestGuid,
                        requestName,
                        elapsedMs);
                }

                // Check for failure results efficiently
                if (response is IResult result && !result.IsSuccess)
                {
                    var errorCount = result.Errors?.Count() ?? 0;
                    _logger.LogWarning(
                        "Request completed with failure {RequestGuid} {RequestName} - {ErrorCount} errors",
                        requestGuid,
                        requestName,
                        errorCount);

                    // Only log error details if debug level is enabled
                    if (_logger.IsEnabled(LogLevel.Debug) && result.Errors != null)
                    {
                        foreach (var error in result.Errors)
                        {
                            _logger.LogDebug(
                                "Request error {RequestGuid} {ErrorCode}: {ErrorMessage}",
                                requestGuid,
                                error.Code,
                                error.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(
                    ex,
                    "Request failed {RequestGuid} {RequestName} after {ElapsedMilliseconds}ms",
                    requestGuid,
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                // Publish error event for unexpected exceptions (async fire-and-forget)
                var entityType = requestName.Replace("Command", "").Replace("Query", "");
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _mediator.Publish(new ValidationErrorEvent(entityType, new[] { Error.Domain(ex.Message) }, $"{requestName}Handler"), cancellationToken);
                    }
                    catch (Exception publishEx)
                    {
                        _logger.LogError(publishEx, "Failed to publish error event for request {RequestGuid}", requestGuid);
                    }
                }, CancellationToken.None);

                throw;
            }

            return response;
        }

        /// <summary>
        /// Efficiently serializes the request with size limits and async operation
        /// </summary>
        private async Task<string> SerializeRequestAsync(TRequest request)
        {
            try
            {
                // Use compact serialization by default for performance
                var json = await Task.Run(() => JsonSerializer.Serialize(request, _compactJsonOptions));

                // Truncate if too long to prevent memory issues
                if (json.Length > MaxSerializationLength)
                {
                    return json.Substring(0, MaxSerializationLength) + "... (truncated)";
                }

                return json;
            }
            catch (Exception ex)
            {
                // Fallback to type name if serialization fails
                _logger.LogDebug(ex, "Failed to serialize request {RequestType}", typeof(TRequest).Name);
                return $"<{typeof(TRequest).Name}> (serialization failed)";
            }
        }
    }
}