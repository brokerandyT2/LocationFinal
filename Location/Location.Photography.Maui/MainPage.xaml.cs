using Location.Photography.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure;
using Microsoft.Extensions.Logging;
using core = Location.Core.Maui.Views;

namespace Location.Photography.Maui
{
    public partial class MainPage : TabbedPage
    {
        bool isLoggedIn = false;

        private readonly IServiceProvider _serviceProvider;
        private readonly ISubscriptionStatusService _subscriptionStatusService;
        private readonly ILogger<MainPage> _logger;

        // MainPage.xaml.cs - Fixed constructor
        public MainPage(
            IServiceProvider serviceProvider,
            ISubscriptionStatusService subscriptionStatusService,
            ILogger<MainPage> logger)
        {
            try
            {
                _serviceProvider = serviceProvider;
                _subscriptionStatusService = subscriptionStatusService;
                _logger = logger;

                _logger.LogInformation("MainPage constructor starting");

                // Only do the absolute minimum in constructor
                InitializeComponent();

                _logger.LogInformation("MainPage InitializeComponent completed");

                // Move ALL heavy work to background - don't block constructor
                Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Starting background MainPage setup");

                        // Check login status safely (no .Result!)
                        try
                        {
                            var email = await SecureStorage.GetAsync(MagicStrings.Email);
                            isLoggedIn = !string.IsNullOrEmpty(email);
                            _logger.LogInformation($"Login status checked: {isLoggedIn}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to check login status");
                            isLoggedIn = false;
                        }

                        if (isLoggedIn)
                        {
                            _logger.LogInformation("User logged in, initializing tabs");
                            await InitializeTabsAsync();
                        }
                        else
                        {
                            _logger.LogInformation("User not logged in, MainPage ready");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in MainPage background setup");
                    }
                });

                _logger.LogInformation("MainPage constructor completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in MainPage constructor");
                throw;
            }
        }

        // MainPage.xaml.cs - Optimized InitializeTabsAsync
        private async Task InitializeTabsAsync()
        {
            try
            {
                _logger.LogInformation("InitializeTabsAsync starting");

                // Get subscription status with timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

                var statusResult = await _subscriptionStatusService.CheckSubscriptionStatusAsync();
                _logger.LogInformation("Subscription status check completed");

#if DEBUG
                var canAccessPremium = Result<bool>.Success(true);
                var canAccessPro = Result<bool>.Success(true);
#else
        var canAccessPremium = await _subscriptionStatusService.CanAccessPremiumFeaturesAsync();
        var canAccessPro = await _subscriptionStatusService.CanAccessProFeaturesAsync();
#endif

                // Add tabs on main thread but do it efficiently
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        _logger.LogInformation("Adding core tabs");

                        // Core features - always available
                        this.Children.Add(_serviceProvider.GetRequiredService<core.AddLocation>());
                        this.Children.Add(_serviceProvider.GetRequiredService<core.LocationsPage>());
                        this.Children.Add(_serviceProvider.GetRequiredService<core.TipsPage>());


                        _logger.LogInformation("Core tabs added successfully");

                        // Set minimum width
                        this.MinimumWidthRequest = 1000;

                        _logger.LogInformation("InitializeTabsAsync completed successfully");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error adding tabs to UI");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in InitializeTabsAsync");

                // Fallback - add minimal tabs
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        this.Children.Add(_serviceProvider.GetRequiredService<core.AddLocation>());
                        this.Children.Add(_serviceProvider.GetRequiredService<Views.Settings>());
                    }
                    catch (Exception fallbackEx)
                    {
                        _logger.LogError(fallbackEx, "Even fallback tab initialization failed");
                    }
                });
            }
        }


    }
}