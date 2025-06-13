using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Events.Errors;
using Location.Core.Application.Resources;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text.Json;

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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger), AppResources.Validation_CannotBeNull);
            _mediator = mediator;
        }

        /// <summary>
        /// Handles a request by invoking the next handler in the pipeline and logging relevant information with optimized performance.
        /// </summary>
        /// <param name="request">The request object being processed.</param>
        /// <param name="next">The next handler in the pipeline.</param>
        /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
        /// <returns>A task representing the asynchronous operation that returns the response from the next handler.</returns>
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            var requestGuid = Guid.NewGuid();
            var stopwatch = Stopwatch.StartNew();

            // Log request start with compact info
            _logger.LogInformation(
                "Request started {RequestGuid} {RequestName}",
                requestGuid,
                requestName);

            // Only serialize and log request details if debug logging is enabled
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                var serializedRequest = await SerializeRequestAsync(request);
                _logger.LogDebug(
                    "Request details {RequestGuid} {RequestName}: {RequestData}",
                    requestGuid,
                    requestName,
                    serializedRequest);
            }

            TResponse response;

            try
            {
                response = await next();
                stopwatch.Stop();

                // Log successful completion
                _logger.LogInformation(
                    "Request completed successfully {RequestGuid} {RequestName} in {ElapsedMilliseconds}ms",
                    requestGuid,
                    requestName,
                    stopwatch.ElapsedMilliseconds);

                // Check for slow operations
                if (stopwatch.ElapsedMilliseconds > SlowRequestThresholdMs)
                {
                    _logger.LogWarning(
                        "Slow operation detected {RequestGuid} {RequestName} took {ElapsedMilliseconds}ms",
                        requestGuid,
                        requestName,
                        stopwatch.ElapsedMilliseconds);
                }

                // Check if response indicates failure
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
                        _logger.LogError(publishEx, AppResources.Log_OperationFailed + " for request {RequestGuid}", requestGuid);
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