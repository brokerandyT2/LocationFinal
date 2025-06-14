// Location.Photography.Maui/App.xaml.cs - FIXED VERSION
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Photography.Infrastructure;
using Location.Photography.Maui.Views;
using MediatR;
using Microsoft.Extensions.Logging;

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
            // Set a loading page IMMEDIATELY - no heavy work here
            MainPage = new ContentPage
            {
                BackgroundColor = Colors.White,
                Content = new Label
                {
                    Text = "Configuring your Application for the first time",
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center,
                    FontSize = 16
                }
            };

            var window = base.CreateWindow(activationState);

            // Start initialization in background - NEVER block UI thread
            if (!_isInitializing)
            {
                _isInitializing = true;
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Starting background initialization");

                        // Do ALL heavy work in background
                        await InitializeAppInBackgroundAsync();

                        _initializationTcs.SetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during application initialization");
                        _initializationTcs.SetException(ex);

                        // Show error UI on main thread
                        MainThread.BeginInvokeOnMainThread(() =>
                        {
                            MainPage = new ContentPage
                            {
                                Content = new Label
                                {
                                    Text = "An error occurred during startup. Please restart the app.",
                                    HorizontalOptions = LayoutOptions.Center,
                                    VerticalOptions = LayoutOptions.Center
                                }
                            };
                        });
                    }
                });
            }

            return window;
        }

        private async Task InitializeAppInBackgroundAsync()
        {
            try
            {
                // Step 1: Initialize database (this can take time)
                _logger.LogInformation("Initializing database...");
                await DatabaseSetup.EnsureDatabaseInitialized(_serviceProvider);
                _logger.LogInformation("Database initialized");

                // Step 2: Check onboarding status quickly
                bool isOnboarded = await HasCompletedOnboardingAsync();
                _logger.LogInformation($"Onboarding status: {isOnboarded}");

                // Step 3: Switch to main UI on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        if (!isOnboarded)
                        {
                            _logger.LogInformation("Showing onboarding");
                            var onboardingPage = _serviceProvider.GetRequiredService<UserOnboarding>();
                            MainPage = new NavigationPage(onboardingPage);
                        }
                        else
                        {
                            _logger.LogInformation("Showing main app");
                            MainPage = _serviceProvider.GetRequiredService<AppShell>();
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error creating main UI");
                        MainPage = new ContentPage
                        {
                            Content = new Label
                            {
                                Text = "Error loading app. Please restart.",
                                HorizontalOptions = LayoutOptions.Center,
                                VerticalOptions = LayoutOptions.Center
                            }
                        };
                    }
                });

                // Step 4: Do post-initialization work (app counter, etc.)
                if (isOnboarded)
                {
                    await IncrementAppOpenCounterAsync();
                }

                _logger.LogInformation("App initialization completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Critical error during background initialization");
                throw;
            }
        }

        // Location.Photography.Maui/App.xaml.cs
        // CHANGE: HasCompletedOnboardingAsync method (lines 88-99)

        private async Task<bool> HasCompletedOnboardingAsync()
        {
            try
            {
                // Step 1: Check if email exists in SecureStorage (primary requirement)
                var email = await SecureStorage.Default.GetAsync(MagicStrings.Email);

                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("No email in SecureStorage - onboarding not completed");
                    return false;
                }

                // Step 2: Check if user settings exist in database (secondary requirement)
                // This ensures the user completed the full onboarding process
                var hasUserSettings = await CheckUserSettingsExistAsync();

                if (!hasUserSettings)
                {
                    _logger.LogInformation("Email exists but user settings not in database - onboarding not completed");
                    return false;
                }

                // Step 3: Check camera profile completion (optional)
                var hasProfile = await CheckForCameraProfileAsync();
                var hasSkippedCamera = await HasSkippedCameraSetupAsync();

                bool onboardingComplete = hasProfile || hasSkippedCamera;

                _logger.LogInformation("Onboarding status: Email={HasEmail}, Settings={HasSettings}, Camera={CameraStatus}",
                    !string.IsNullOrEmpty(email), hasUserSettings,
                    hasProfile ? "Completed" : (hasSkippedCamera ? "Skipped" : "Pending"));

                return onboardingComplete;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking onboarding status");
                // Fallback to Preferences if SecureStorage fails
                return Preferences.Default.ContainsKey(MagicStrings.Email) &&
                       !string.IsNullOrEmpty(Preferences.Default.Get(MagicStrings.Email, string.Empty));
            }
        }

        private async Task<bool> CheckUserSettingsExistAsync()
        {
            try
            {
                // Check if essential user settings exist in database
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var emailQuery = new GetSettingByKeyQuery { Key = MagicStrings.Email };
                var emailResult = await mediator.Send(emailQuery);

                // If email setting exists in database, assume other settings were also saved
                return emailResult?.Data?.Value != null && !string.IsNullOrEmpty(emailResult.Data.Value);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking user settings in database");
                return false;
            }
        }

        private async Task<bool> CheckForCameraProfileAsync()
        {
            try
            {
                // Check if user has completed camera calibration
                var hasProfile = await SecureStorage.Default.GetAsync("CameraProfileCompleted");
                return !string.IsNullOrEmpty(hasProfile) && hasProfile == "true";
            }
            catch
            {
                return false;
            }
        }

        private async Task<bool> HasSkippedCameraSetupAsync()
        {
            try
            {
                // Check if user explicitly skipped camera setup
                var skipped = await SecureStorage.Default.GetAsync("CameraSetupSkipped");
                return !string.IsNullOrEmpty(skipped) && skipped == "true";
            }
            catch
            {
                return false;
            }
        }

        private async Task IncrementAppOpenCounterAsync()
        {
            try
            {
                var countStr = await SecureStorage.Default.GetAsync(MagicStrings.AppOpenCounter);
                if (!int.TryParse(countStr, out int currentCount))
                    currentCount = 0;

                currentCount++;
                await SecureStorage.Default.SetAsync(MagicStrings.AppOpenCounter, currentCount.ToString());
                _logger.LogInformation("App opened {Count} times", currentCount);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating app open counter");
                var currentCount = Preferences.Default.Get(MagicStrings.AppOpenCounter, 0);
                currentCount++;
                Preferences.Default.Set(MagicStrings.AppOpenCounter, currentCount);
            }
        }
    }
}