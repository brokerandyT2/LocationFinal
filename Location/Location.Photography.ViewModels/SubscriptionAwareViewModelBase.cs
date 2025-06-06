// Location.Photography.ViewModels/Base/SubscriptionAwareViewModelBase.cs
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;

namespace Location.Photography.ViewModels
{
    public partial class SubscriptionAwareViewModelBase : ViewModelBase, INavigationAware
    {
        #region Fields
        protected readonly ISubscriptionFeatureGuard _featureGuard;
        private readonly IErrorDisplayService _errorDisplayService;

        // PERFORMANCE: Threading and caching
        private readonly SemaphoreSlim _subscriptionCheckLock = new(1, 1);
        private readonly Dictionary<string, CachedFeatureAccessResult> _accessCache = new();
        private DateTime _lastCacheCleanup = DateTime.MinValue;
        private const int CACHE_DURATION_MINUTES = 5;
        private const int CACHE_CLEANUP_INTERVAL_MINUTES = 10;

        private bool _hasSubscriptionError;
        private string _subscriptionErrorMessage = string.Empty;
        #endregion

        #region Properties
        [ObservableProperty]
        private bool _hasSubscriptionErrorProp;

        [ObservableProperty]
        private string _subscriptionErrorMessageProp = string.Empty;

        // Legacy property mappings for compatibility
        public bool HasSubscriptionError
        {
            get => _hasSubscriptionError;
            set => SetProperty(ref _hasSubscriptionError, value);
        }

        public string SubscriptionErrorMessage
        {
            get => _subscriptionErrorMessage;
            set => SetProperty(ref _subscriptionErrorMessage, value);
        }
        #endregion

        #region Events
        public event EventHandler<SubscriptionUpgradeRequestedEventArgs> SubscriptionUpgradeRequested;
        #endregion

        #region Constructor
        protected SubscriptionAwareViewModelBase(
            ISubscriptionFeatureGuard featureGuard,
            IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _featureGuard = featureGuard ?? throw new ArgumentNullException(nameof(featureGuard));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
        }
        #endregion

        #region PERFORMANCE OPTIMIZED METHODS

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Cached premium feature access check
        /// </summary>
        protected async Task<bool> CheckPremiumFeatureAccessAsync()
        {
            return await CheckFeatureAccessOptimizedAsync("Premium", async () => await _featureGuard.CheckPremiumFeatureAccessAsync());
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Cached professional feature access check
        /// </summary>
        protected async Task<bool> CheckProFeatureAccessAsync()
        {
            return await CheckFeatureAccessOptimizedAsync("Pro", async () => await _featureGuard.CheckProFeatureAccessAsync());
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Cached paid feature access check
        /// </summary>
        protected async Task<bool> CheckPaidFeatureAccessOptimizedAsync()
        {
            return await CheckFeatureAccessOptimizedAsync("Paid", async () => await _featureGuard.CheckPaidFeatureAccessAsync());
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Generic feature access check with caching
        /// </summary>
        private async Task<bool> CheckFeatureAccessOptimizedAsync(string featureType, Func<Task<Location.Core.Application.Common.Models.Result<FeatureAccessResult>>> checkFunction)
        {
            if (!await _subscriptionCheckLock.WaitAsync(100))
            {
                // If we can't get the lock quickly, return cached result or false
                return GetCachedAccessResult(featureType)?.HasAccess ?? false;
            }

            try
            {
                // Cleanup old cache entries periodically
                await CleanupCacheIfNeededAsync();

                // Check cache first
                var cachedResult = GetCachedAccessResult(featureType);
                if (cachedResult != null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ApplyAccessResultOptimized(cachedResult.AccessResult);
                    });
                    return cachedResult.HasAccess;
                }

                // Perform actual check on background thread
                var result = await Task.Run(async () =>
                {
                    try
                    {
                        return await checkFunction();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Feature access check failed: {ex.Message}", ex);
                    }
                });

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (!result.IsSuccess)
                    {
                        HasSubscriptionError = true;
                        SubscriptionErrorMessage = "Unable to verify subscription status";
                        return;
                    }

                    var accessResult = result.Data;

                    // Cache the result
                    _accessCache[featureType] = new CachedFeatureAccessResult
                    {
                        AccessResult = accessResult,
                        HasAccess = accessResult.HasAccess,
                        Timestamp = DateTime.Now
                    };

                    ApplyAccessResultOptimized(accessResult);
                });

                return result.IsSuccess && result.Data.HasAccess;
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HasSubscriptionError = true;
                    SubscriptionErrorMessage = "Error checking subscription status";
                    OnSystemError($"There was an error processing your request, please try again: {ex.Message}");
                });
                return false;
            }
            finally
            {
                _subscriptionCheckLock.Release();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Apply access result with batch updates
        /// </summary>
        private void ApplyAccessResultOptimized(FeatureAccessResult accessResult)
        {
            BeginPropertyChangeBatch();

            if (accessResult.HasAccess)
            {
                HasSubscriptionError = false;
                SubscriptionErrorMessage = string.Empty;
            }
            else
            {
                _ = HandleFeatureAccessDeniedAsync(accessResult);
            }

            _ = EndPropertyChangeBatchAsync();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized feature access denial handling
        /// </summary>
        private async Task HandleFeatureAccessDeniedAsync(FeatureAccessResult accessResult)
        {
            try
            {
                switch (accessResult.Action)
                {
                    case FeatureAccessAction.ShowUpgradePrompt:
                        OnSubscriptionUpgradeRequested(new SubscriptionUpgradeRequestedEventArgs
                        {
                            RequiredSubscription = accessResult.RequiredSubscription,
                            Message = accessResult.Message
                        });
                        break;

                    case FeatureAccessAction.ShowError:
                        HasSubscriptionError = true;
                        SubscriptionErrorMessage = accessResult.Message;
                        SetValidationError(accessResult.Message);
                        break;
                }
            }
            catch (Exception ex)
            {
                OnSystemError($"Error handling feature access denial: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Get cached access result if valid
        /// </summary>
        private CachedFeatureAccessResult GetCachedAccessResult(string featureType)
        {
            if (_accessCache.TryGetValue(featureType, out var cachedResult))
            {
                var age = DateTime.Now - cachedResult.Timestamp;
                if (age.TotalMinutes < CACHE_DURATION_MINUTES)
                {
                    return cachedResult;
                }
                else
                {
                    // Remove expired cache entry
                    _accessCache.Remove(featureType);
                }
            }
            return null;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Periodic cache cleanup
        /// </summary>
        private async Task CleanupCacheIfNeededAsync()
        {
            var now = DateTime.Now;
            if ((now - _lastCacheCleanup).TotalMinutes >= CACHE_CLEANUP_INTERVAL_MINUTES)
            {
                _lastCacheCleanup = now;

                await Task.Run(() =>
                {
                    var expiredKeys = new List<string>();
                    var cutoffTime = now.AddMinutes(-CACHE_DURATION_MINUTES);

                    foreach (var kvp in _accessCache)
                    {
                        if (kvp.Value.Timestamp < cutoffTime)
                        {
                            expiredKeys.Add(kvp.Key);
                        }
                    }

                    foreach (var key in expiredKeys)
                    {
                        _accessCache.Remove(key);
                    }
                });
            }
        }

        #endregion

        #region Methods

        protected virtual void OnSubscriptionUpgradeRequested(SubscriptionUpgradeRequestedEventArgs e)
        {
            SubscriptionUpgradeRequested?.Invoke(this, e);
        }

        [RelayCommand]
        protected virtual async Task RequestUpgradeAsync()
        {
            OnSubscriptionUpgradeRequested(new SubscriptionUpgradeRequestedEventArgs
            {
                RequiredSubscription = "Premium",
                Message = "Upgrade to Premium to access this feature"
            });
            await Task.CompletedTask;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Clear subscription cache
        /// </summary>
        protected void ClearSubscriptionCache()
        {
            _accessCache.Clear();
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Clear specific feature cache
        /// </summary>
        protected void ClearFeatureCache(string featureType)
        {
            _accessCache.Remove(featureType);
        }

        public virtual void OnNavigatedToAsync()
        {
            // Default implementation - can be overridden by derived classes
        }

        public virtual void OnNavigatedFromAsync()
        {
            // Default implementation - can be overridden by derived classes
        }

        public override void Dispose()
        {
            _subscriptionCheckLock?.Dispose();
            _accessCache.Clear();
            base.Dispose();
        }

        #endregion

        #region Helper Classes

        private class CachedFeatureAccessResult
        {
            public FeatureAccessResult AccessResult { get; set; }
            public bool HasAccess { get; set; }
            public DateTime Timestamp { get; set; }
        }

        #endregion
    }
}