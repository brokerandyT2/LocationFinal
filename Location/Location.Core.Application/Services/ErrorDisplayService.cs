using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Channels;
using Location.Core.Application.Events.Errors;
using MediatR;

namespace Location.Core.Application.Services
{
    /// <summary>
    /// High-performance error display service using lock-free collections and background processing
    /// </summary>
    public class ErrorDisplayService : IErrorDisplayService,
        INotificationHandler<LocationSaveErrorEvent>,
        INotificationHandler<WeatherUpdateErrorEvent>,
        INotificationHandler<ValidationErrorEvent>,
        INotificationHandler<TipValidationErrorEvent>,
        INotificationHandler<SettingErrorEvent>,
        INotificationHandler<TipTypeErrorEvent>,
        IDisposable
    {
        private readonly Channel<DomainErrorEvent> _errorChannel;
        private readonly ChannelWriter<DomainErrorEvent> _errorWriter;
        private readonly ChannelReader<DomainErrorEvent> _errorReader;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly Task _backgroundProcessor;

        // Configuration
        private const int ERROR_AGGREGATION_WINDOW_MS = 500;
        private const int MAX_ERRORS_PER_BATCH = 10;
        private const int CHANNEL_CAPACITY = 1000;

        /// <summary>
        /// Event raised when aggregated errors are ready to be displayed
        /// </summary>
        public event EventHandler<ErrorDisplayEventArgs>? ErrorsReady;

        public ErrorDisplayService()
        {
            // Create bounded channel for better memory management
            var options = new BoundedChannelOptions(CHANNEL_CAPACITY)
            {
                FullMode = BoundedChannelFullMode.DropOldest, // Drop old errors if channel is full
                SingleReader = true,
                SingleWriter = false,
                AllowSynchronousContinuations = false
            };

            _errorChannel = Channel.CreateBounded<DomainErrorEvent>(options);
            _errorWriter = _errorChannel.Writer;
            _errorReader = _errorChannel.Reader;
            _cancellationTokenSource = new CancellationTokenSource();

            // Start background processing
            _backgroundProcessor = Task.Run(ProcessErrorsAsync, _cancellationTokenSource.Token);
        }

        #region Domain Error Event Handlers (Lock-Free)

        public Task Handle(LocationSaveErrorEvent notification, CancellationToken cancellationToken)
        {
            return TryEnqueueErrorAsync(notification);
        }

        public Task Handle(WeatherUpdateErrorEvent notification, CancellationToken cancellationToken)
        {
            return TryEnqueueErrorAsync(notification);
        }

        public Task Handle(ValidationErrorEvent notification, CancellationToken cancellationToken)
        {
            return TryEnqueueErrorAsync(notification);
        }

        public Task Handle(TipValidationErrorEvent notification, CancellationToken cancellationToken)
        {
            return TryEnqueueErrorAsync(notification);
        }

        public Task Handle(SettingErrorEvent notification, CancellationToken cancellationToken)
        {
            return TryEnqueueErrorAsync(notification);
        }

        public Task Handle(TipTypeErrorEvent notification, CancellationToken cancellationToken)
        {
            return TryEnqueueErrorAsync(notification);
        }

        #endregion

        /// <summary>
        /// High-performance lock-free error enqueuing
        /// </summary>
        private Task TryEnqueueErrorAsync(DomainErrorEvent errorEvent)
        {
            // Non-blocking enqueue - fails fast if channel is full
            if (_errorWriter.TryWrite(errorEvent))
            {
                return Task.CompletedTask;
            }

            // Channel is full - try async write with timeout for important errors
            return TryWriteWithTimeoutAsync(errorEvent);
        }

        /// <summary>
        /// Attempts async write with timeout for critical errors
        /// </summary>
        private async Task TryWriteWithTimeoutAsync(DomainErrorEvent errorEvent)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, timeoutCts.Token);
                
                await _errorWriter.WriteAsync(errorEvent, combinedCts.Token);
            }
            catch (OperationCanceledException)
            {
                // Error dropped due to timeout or shutdown - acceptable for performance
            }
        }

        /// <summary>
        /// High-performance background error processing with batching
        /// </summary>
        private async Task ProcessErrorsAsync()
        {
            var errorBatch = new List<DomainErrorEvent>(MAX_ERRORS_PER_BATCH);
            var batchTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(ERROR_AGGREGATION_WINDOW_MS));

            try
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    errorBatch.Clear();

                    // Collect errors for aggregation window
                    var deadline = DateTime.UtcNow.AddMilliseconds(ERROR_AGGREGATION_WINDOW_MS);
                    
                    while (DateTime.UtcNow < deadline && errorBatch.Count < MAX_ERRORS_PER_BATCH)
                    {
                        var remainingTime = deadline - DateTime.UtcNow;
                        if (remainingTime <= TimeSpan.Zero)
                            break;

                        using var timeoutCts = new CancellationTokenSource(remainingTime);
                        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token, timeoutCts.Token);

                        try
                        {
                            var errorEvent = await _errorReader.ReadAsync(combinedCts.Token);
                            errorBatch.Add(errorEvent);
                        }
                        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                        {
                            // Timeout reached - process current batch
                            break;
                        }
                    }

                    // Process batch if we have errors
                    if (errorBatch.Count > 0)
                    {
                        await ProcessErrorBatchAsync(errorBatch);
                    }

                    // Wait for next processing cycle if no errors were found
                    if (errorBatch.Count == 0)
                    {
                        try
                        {
                            await batchTimer.WaitForNextTickAsync(_cancellationTokenSource.Token);
                        }
                        catch (OperationCanceledException)
                        {
                            break;
                        }
                    }
                }
            }
            finally
            {
                batchTimer.Dispose();
            }
        }

        /// <summary>
        /// Processes a batch of errors with optimized aggregation
        /// </summary>
        private async Task ProcessErrorBatchAsync(List<DomainErrorEvent> errors)
        {
            try
            {
                var displayMessage = GenerateOptimizedDisplayMessage(errors);
                var eventArgs = new ErrorDisplayEventArgs(new List<DomainErrorEvent>(errors), displayMessage);

                // Fire event on thread pool to avoid blocking background processor
                _ = Task.Run(() => ErrorsReady?.Invoke(this, eventArgs));
                
                await Task.CompletedTask;
            }
            catch (Exception)
            {
                // Swallow exceptions to keep background processor running
            }
        }

        /// <summary>
        /// Optimized display message generation with caching
        /// </summary>
        private string GenerateOptimizedDisplayMessage(List<DomainErrorEvent> errors)
        {
            if (errors.Count == 1)
            {
                return GetCachedLocalizedErrorMessage(errors[0]);
            }
            else
            {
                // Group similar errors for better user experience
                var errorGroups = GroupSimilarErrors(errors);

                if (errorGroups.Count == 1)
                {
                    var group = errorGroups[0];
                    return $"{group.Count()} similar errors occurred: {GetCachedLocalizedErrorMessage(group.First())}";
                }
                else
                {
                    return $"Multiple errors occurred ({errors.Count} total), please retry";
                }
            }
        }

        /// <summary>
        /// Groups similar errors to reduce noise
        /// </summary>
        private List<IGrouping<string, DomainErrorEvent>> GroupSimilarErrors(List<DomainErrorEvent> errors)
        {
            return errors
                .GroupBy(e => e.GetResourceKey())
                .ToList();
        }

        /// <summary>
        /// Cached localized error message retrieval
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _messageCache = new();

        private string GetCachedLocalizedErrorMessage(DomainErrorEvent errorEvent)
        {
            var resourceKey = errorEvent.GetResourceKey();
            
            return _messageCache.GetOrAdd(resourceKey, key =>
            {
                var parameters = errorEvent.GetParameters();
                
                return key switch
                {
                    "Location_Error_DuplicateTitle" => $"Location '{parameters.GetValueOrDefault("LocationTitle", "")}' already exists",
                    "Location_Error_InvalidCoordinates" => "Invalid coordinates provided",
                    "Location_Error_NetworkError" => "Network error occurred",
                    "Location_Error_DatabaseError" => "Database error occurred",
                    "Weather_Error_ApiUnavailable" => "Weather service is unavailable",
                    "Weather_Error_NetworkTimeout" => "Weather service timeout",
                    "Weather_Error_InvalidApiKey" => "Weather service authentication failed",
                    "Validation_Error_Single" => $"Validation error: {parameters.GetValueOrDefault("ErrorMessage", "Invalid input")}",
                    "Validation_Error_Multiple" => $"Multiple validation errors occurred ({parameters.GetValueOrDefault("ErrorCount", 0)} errors)",
                    "Tip_Validation_Error_Single" => $"Tip validation error: {parameters.GetValueOrDefault("ErrorMessage", "Invalid tip data")}",
                    "Setting_Error_DuplicateKey" => $"Setting '{parameters.GetValueOrDefault("SettingKey", "")}' already exists",
                    "TipType_Error_DuplicateName" => $"Tip type '{parameters.GetValueOrDefault("TipTypeName", "")}' already exists",
                    _ => "An error occurred, please try again"
                };
            });
        }

        /// <summary>
        /// Manual trigger for testing purposes with high performance
        /// </summary>
        public async Task TriggerErrorDisplayAsync(List<DomainErrorEvent> errors)
        {
            if (errors == null || errors.Count == 0)
                return;

            await ProcessErrorBatchAsync(errors);
        }

        #region Disposal

        private bool _disposed = false;

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            try
            {
                // Signal shutdown
                _cancellationTokenSource.Cancel();
                
                // Complete the writer to signal no more errors
                _errorWriter.TryComplete();

                // Wait for background processor to finish (with timeout)
                if (!_backgroundProcessor.Wait(TimeSpan.FromSeconds(5)))
                {
                    // Force disposal if processor doesn't complete gracefully
                }
            }
            finally
            {
                _cancellationTokenSource.Dispose();
                _backgroundProcessor.Dispose();
            }
        }

        #endregion
    }
}