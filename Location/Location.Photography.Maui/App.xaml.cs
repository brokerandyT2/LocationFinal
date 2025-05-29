// Location.Photography.Maui/App.xaml.cs
using Location.Photography.Infrastructure;
using Location.Photography.Maui.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using System;
using System.Threading.Tasks;

namespace Location.Photography.Maui
{
    public partial class App : Microsoft.Maui.Controls.Application
    {
        public IServiceProvider ServiceProvider => _serviceProvider;
        private readonly ILogger<App> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _isInitializing = false;
        private TaskCompletionSource _initializationTcs = new TaskCompletionSource();

        public App(IServiceProvider serviceProvider, ILogger<App> logger)
        {
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.InvariantCulture;
            InitializeComponent();

            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Set a loading page first
            MainPage = new ContentPage
            {
                BackgroundColor = Colors.White
            };

            var window = base.CreateWindow(activationState);

            // Add loading text after a tiny delay
            Task.Run(async () =>
            {
                await Task.Delay(50);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (MainPage is ContentPage loadingPage)
                    {
                        loadingPage.Content = new Label
                        {
                            Text = "Loading...",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            FontSize = 16
                        };
                    }
                });
            });

            //var window = base.CreateWindow(activationState);

            // Trigger initialization once (if not already happening)
            if (!_isInitializing)
            {
                _isInitializing = true;
                Task.Run(async () =>
                {
                    try
                    {
                        // First initialize the database
                        _logger.LogInformation("Starting database initialization");
                        await DatabaseSetup.EnsureDatabaseInitialized(_serviceProvider);
                        _logger.LogInformation("Database initialization completed");

                        // Then initialize the app UI
                        await InitializeAppAsync();
                        _initializationTcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during application initialization");
                        _initializationTcs.SetException(ex);

                        // Show error UI or fallback UI on main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            MainPage = new Microsoft.Maui.Controls.ContentPage
                            {
                                Content = new Microsoft.Maui.Controls.Label
                                {
                                    Text = "An error occurred during startup. Please restart the app.",
                                    HorizontalOptions = Microsoft.Maui.Controls.LayoutOptions.Center,
                                    VerticalOptions = Microsoft.Maui.Controls.LayoutOptions.Center
                                }
                            };
                        });
                    }
                });
            }

            return window;
        }

        protected override void OnStart()
        {
            base.OnStart();
            // No database initialization here - moved to the CreateWindow sequence
        }

        // In App.xaml.cs - Optimize the InitializeAppAsync method
        // App.xaml.cs - Update to use AppShell instead of MainPage
        private async Task InitializeAppAsync()
        {
            try
            {
                // Check if email setting exists as a proxy for completed onboarding
                bool isOnboarded = await HasCompletedOnboardingAsync();

                // UI updates must happen on the main thread
                await MainThread.InvokeOnMainThreadAsync(() => {
                    if (!isOnboarded)
                    {
                        // User hasn't completed onboarding, show the onboarding page
                        _logger.LogInformation("Onboarding not completed, showing UserOnboarding page");

                        // Resolve UserOnboarding from DI container
                        var onboardingPage = _serviceProvider.GetRequiredService<UserOnboarding>();
                        MainPage = new NavigationPage(onboardingPage);
                    }
                    else
                    {
                        // User has completed onboarding, show the main Shell app
                        _logger.LogInformation("Onboarding already completed, showing AppShell");

                        try
                        {
                            MainPage = _serviceProvider.GetRequiredService<AppShell>();
                            _logger.LogInformation("AppShell created successfully");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to create AppShell");

                            // Fallback to simple page
                            MainPage = new ContentPage
                            {
                                Content = new Label
                                {
                                    Text = "Error loading main app. Please restart.",
                                    HorizontalOptions = LayoutOptions.Center,
                                    VerticalOptions = LayoutOptions.Center
                                }
                            };
                        }
                    }
                });

                // Increment app open counter after UI is set
                if (isOnboarded)
                {
                    await IncrementAppOpenCounterAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during app UI initialization");

                // Show fallback UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() => {
                    MainPage = new ContentPage
                    {
                        Content = new Label
                        {
                            Text = "Startup error. Please restart the app.",
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center
                        }
                    };
                });

                // Re-throw to be caught by the outer handler
                throw;
            }
        }

        private async Task<bool> HasCompletedOnboardingAsync()
        {
            try
            {
                // Check if email has been set as a proxy for completed onboarding
                var email = await SecureStorage.Default.GetAsync(MagicStrings.Email);
                return !string.IsNullOrEmpty(email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking onboarding status from SecureStorage, falling back to Preferences");

                // Fall back to Preferences if SecureStorage fails
                return Preferences.Default.ContainsKey(MagicStrings.Email) &&
                        !string.IsNullOrEmpty(Preferences.Default.Get(MagicStrings.Email, string.Empty));
            }
        }

        private async Task IncrementAppOpenCounterAsync()
        {
            try
            {
                // Get current app open count from SecureStorage
                var countStr = await SecureStorage.Default.GetAsync(MagicStrings.AppOpenCounter);

                // Parse the current count or default to 0
                if (!int.TryParse(countStr, out int currentCount))
                {
                    currentCount = 0;
                }

                // Increment and save back
                currentCount++;
                await SecureStorage.Default.SetAsync(MagicStrings.AppOpenCounter, currentCount.ToString());

                _logger.LogInformation("App opened {Count} times", currentCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating app open counter in SecureStorage, falling back to Preferences");

                // Fall back to Preferences if SecureStorage fails
                var currentCount = Preferences.Default.Get(MagicStrings.AppOpenCounter, 0);
                currentCount++;
                Preferences.Default.Set(MagicStrings.AppOpenCounter, currentCount);
            }
        }
    }
}