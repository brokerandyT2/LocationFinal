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
        private readonly ILogger<App> _logger;
        private readonly IServiceProvider _serviceProvider;

        public App(IServiceProvider serviceProvider, ILogger<App> logger)
        {
            InitializeComponent();
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async void OnStart()
        {
            base.OnStart();
            await InitializeAppAsync();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Set a default page first
            MainPage = new Microsoft.Maui.Controls.ContentPage
            {
                Content = new Microsoft.Maui.Controls.StackLayout
                {
                    Children =
            {
                new Microsoft.Maui.Controls.ActivityIndicator
                {
                    IsRunning = true,
                    HorizontalOptions = Microsoft.Maui.Controls.LayoutOptions.Center,
                    VerticalOptions = Microsoft.Maui.Controls.LayoutOptions.Center
                }
            }
                }
            };

            var window = base.CreateWindow(activationState);

            // Initialize the app immediately after creating the window
            _ = InitializeAppAsync();

            return window;
        }

        private async Task InitializeAppAsync()
        {
            try
            {
                // Check if email setting exists as a proxy for completed onboarding
                bool isOnboarded = await HasCompletedOnboardingAsync();

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
                    // User has completed onboarding, show the main app
                    _logger.LogInformation("Onboarding already completed, showing MainPage");
                    MainPage = new MainPage(_serviceProvider);

                    // Increment app open counter
                    await IncrementAppOpenCounterAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during app initialization");

                // Fallback to main page in case of error
                MainPage = new MainPage(_serviceProvider);
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