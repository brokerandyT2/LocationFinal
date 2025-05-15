using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using Location.Core.Application.Common.Interfaces;

namespace Location.Core.Application.Common.Behaviors
{
    public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

        public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

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

                throw;
            }

            return response;
        }
    }
}