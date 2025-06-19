// Location.Photography.Maui/Views/UserOnboarding.xaml.cs

using Location.Core.Application.Services;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Photography.Infrastructure;
using Location.Photography.Maui.Resources;
using Location.Photography.ViewModels;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Maui.Views
{
    public partial class UserOnboarding : ContentPage
    {
        private readonly IAlertService _alertService;
        private readonly DatabaseInitializer _databaseInitializer;
        private readonly ILogger<UserOnboarding> _logger;
        private readonly IServiceProvider _serviceProvider;
        private string _guid;
        private bool _saveAttempted = false;

        public UserOnboarding(
            IAlertService alertService,
            DatabaseInitializer databaseInitializer,
            ILogger<UserOnboarding> logger,
            IServiceProvider serviceProvider)
        {
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;

            try
            {
                _logger.LogInformation("UserOnboarding constructor starting");

                // Clean up database if it doesn't exist (reset state)
                CleanupIfDatabaseMissing();

                // Initialize the page first
                InitializeComponent();
                _logger.LogInformation("UserOnboarding InitializeComponent completed");

                // Initialize page UI
                GetSetting();

                // Check for existing email and navigate if found (before any DB operations)
                CheckExistingEmailAndNavigate();

                // Initialize database with static data in background - non-blocking, thread-safe
                _ = Task.Run(async () =>
                {
                    try
                    {
                        _logger.LogInformation("Starting background database initialization check");

                        // Check if already initialized first (fast path)
                        if (await _databaseInitializer.IsDatabaseInitializedAsync())
                        {
                            _logger.LogInformation("Database already initialized - skipping initialization");
                            return;
                        }

                        _logger.LogInformation("Database not initialized - starting initialization");

                        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5)); // Increased timeout
                        await _databaseInitializer.InitializeDatabaseWithStaticDataAsync(cts.Token);

                        _logger.LogInformation("Background database initialization completed successfully");
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogWarning("Database initialization was cancelled");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error during background database initialization");

                        // Show error on UI thread
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await _alertService.ShowErrorAlertAsync(
                                "Failed to initialize application data. Please restart the app.",
                                "Initialization Error");
                        });
                    }
                });

                _logger.LogInformation("UserOnboarding constructor completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UserOnboarding constructor");
                throw;
            }
        }

        private void CleanupIfDatabaseMissing()
        {
            try
            {
                var path = Path.Combine(FileSystem.AppDataDirectory, "locations.db");

                if (!File.Exists(path))
                {
                    _logger.LogInformation("Database file does not exist, clearing SecureStorage");
                    SecureStorage.Remove(MagicStrings.Email);

                    // Reset the database initialization state
                    DatabaseInitializer.ResetInitializationState();
                }
                else
                {
                    _logger.LogDebug("Database file exists at: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database cleanup check");
            }
        }

        private async void CheckExistingEmailAndNavigate()
        {
            try
            {
                // Check SecureStorage first (source of truth for user state)
                var email = await SecureStorage.Default.GetAsync(MagicStrings.Email);

                if (!string.IsNullOrEmpty(email))
                {
                    _logger.LogInformation("Existing email found in SecureStorage, navigating to CameraEvaluation: {Email}", email);

                    // Ensure database is initialized before navigation
                    await EnsureDatabaseInitializedAsync();

                    await Navigation.PushAsync((ContentPage)_serviceProvider.GetRequiredService<CameraEvaluation>());
                }
                else
                {
                    _logger.LogInformation("No existing email found in SecureStorage, staying on onboarding page");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error checking existing email, continuing with onboarding");
            }
        }

        private async Task EnsureDatabaseInitializedAsync()
        {
            try
            {
                // Quick check if already initialized
                if (await _databaseInitializer.IsDatabaseInitializedAsync())
                {
                    return;
                }

                _logger.LogInformation("Database not initialized, initializing now...");

                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(3));
                await _databaseInitializer.InitializeDatabaseWithStaticDataAsync(cts.Token);

                _logger.LogInformation("Database initialization completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure database initialization");
                throw;
            }
        }

        private void OnPageLoaded(object sender, EventArgs e)
        {
            // No longer needed - initialization moved to constructor
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
        }

        private void GetSetting()
        {
            if (emailAddress != null)
            {
                //emailAddress.TextChanged += EmailAddress_TextChanged;
            }

            SettingsViewModel svm = new SettingsViewModel();
            svm.Hemisphere = new SettingViewModel();
            svm.TimeFormat = new SettingViewModel();
            svm.DateFormat = new SettingViewModel();
            svm.Email = new SettingViewModel();
            svm.WindDirection = new SettingViewModel();
            svm.TemperatureFormat = new SettingViewModel();
            svm.Hemisphere.Value = MagicStrings.North;
            svm.TimeFormat.Value = MagicStrings.USTimeformat;
            svm.DateFormat.Value = MagicStrings.USDateFormat;
            svm.WindDirection.Value = MagicStrings.TowardsWind;
            svm.TemperatureFormat.Value = MagicStrings.Fahrenheit;
            BindingContext = svm;

            if (svm.WindDirection.Value == MagicStrings.TowardsWind)
            {
                WindDirection.Text = AppResources.TowardsWind.FirstCharToUpper();
            }
            else
            {
                WindDirection.Text = AppResources.WithWind.FirstCharToUpper();
            }
        }

        private void UpdateValidationMessageVisibility()
        {
            var validationBehavior = emailAddress.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.TextValidationBehavior>().FirstOrDefault();
            bool isValid = validationBehavior?.IsValid ?? false;
            bool hasText = !string.IsNullOrWhiteSpace(emailAddress.Text);

            emailValidationMessage.IsVisible = (hasText && !isValid) || _saveAttempted;

            if (string.IsNullOrWhiteSpace(emailAddress.Text) && _saveAttempted)
            {
                emailValidationMessage.Text = "Email is required";
            }
            else if (!isValid)
            {
                emailValidationMessage.Text = "Please enter a valid email address";
            }
        }

        private async void save_Pressed(object sender, EventArgs e)
        {
            _saveAttempted = true;

            var validationBehavior = emailAddress.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.TextValidationBehavior>().FirstOrDefault();
            bool isValid = validationBehavior?.IsValid ?? false;
            bool hasText = !string.IsNullOrWhiteSpace(emailAddress.Text);

            if (isValid && hasText)
            {
                _saveAttempted = false;

                string hemisphere = HemisphereSwitch.IsToggled ? MagicStrings.North : MagicStrings.South;
                string temperatureFormat = TempFormatSwitch.IsToggled ? MagicStrings.Fahrenheit : MagicStrings.Celsius;
                string dateFormat = DateFormat.IsToggled ? MagicStrings.USDateFormat : MagicStrings.InternationalFormat;
                string timeFormat = TimeSwitch.IsToggled ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
                string windDirection = WindDirectionSwitch.IsToggled ? MagicStrings.TowardsWind : MagicStrings.WithWind;
                string email = emailAddress.Text;

                await SaveToSecureStorageAsync(email);
                save.IsEnabled = false;
                processingOverlay.IsVisible = true;

                try
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        loadingIndicator.IsRunning = true;
                    });

                    // Ensure database is initialized with static data first
                    await EnsureDatabaseInitializedAsync();

                    _logger.LogInformation("Creating user-specific settings");

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

                    // Only save user-specific settings captured from the form
                    await _databaseInitializer.CreateUserSettingsAsync(
                        hemisphere, temperatureFormat, dateFormat,
                        timeFormat, windDirection, email, _guid, cts.Token);

                    _logger.LogInformation("User settings saved successfully, navigating to CameraEvaluation");
                    await Navigation.PushAsync(_serviceProvider.GetRequiredService<CameraEvaluation>());
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during save process");

                    string errorMessage = "Error processing data";
                    if (AppResources.ResourceManager.GetString("ErrorProcessingData") != null)
                    {
                        errorMessage = AppResources.ResourceManager.GetString("ErrorProcessingData");
                    }

                    if (_alertService != null)
                    {
                        await _alertService.ShowErrorAlertAsync($"{errorMessage}: {ex.Message}", "Error");
                    }
                    else
                    {
                        await DisplayAlert("Error", $"{errorMessage}: {ex.Message}", "OK");
                    }
                }
                finally
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        processingOverlay.IsVisible = false;
                        save.IsEnabled = true;
                    });
                }
            }
            else
            {
                UpdateValidationMessageVisibility();
            }
        }

        private async Task SaveToSecureStorageAsync(string email)
        {
            try
            {
                _guid = Guid.NewGuid().ToString();
                await SecureStorage.Default.SetAsync(MagicStrings.Email, email);
                await SecureStorage.Default.SetAsync(MagicStrings.UniqueID, _guid);
                _logger.LogDebug("Saved email and GUID to SecureStorage");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to save to SecureStorage");
                throw;
            }
        }

        private void HemisphereSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (((SettingsViewModel)BindingContext).Hemisphere == null)
            {
                return;
            }
            ((SettingsViewModel)BindingContext).Hemisphere.Value = e.Value ? MagicStrings.North : MagicStrings.South;
        }

        private void TimeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (((SettingsViewModel)BindingContext).TimeFormat == null)
            {
                return;
            }
            ((SettingsViewModel)BindingContext).TimeFormat.Value = e.Value ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
        }

        private void DateFormat_Toggled(object sender, ToggledEventArgs e)
        {
            if (((SettingsViewModel)BindingContext).DateFormat == null)
            {
                return;
            }
            ((SettingsViewModel)BindingContext).DateFormat.Value = e.Value ? MagicStrings.USDateFormat : MagicStrings.InternationalFormat;
        }

        private void WindDirectionSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            var vm = (SettingsViewModel)BindingContext;
            if (vm.WindDirection == null)
            {
                return;
            }
            vm.WindDirection.Value = e.Value ? MagicStrings.TowardsWind : MagicStrings.WithWind;

            WindDirection.Text = e.Value
                ? Maui.Resources.AppResources.TowardsWind.FirstCharToUpper()
                : AppResources.WithWind.FirstCharToUpper();
        }

        private void TempFormatSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (((SettingsViewModel)BindingContext).TemperatureFormat == null)
            {
                return;
            }
            ((SettingsViewModel)BindingContext).TemperatureFormat.Value = e.Value ? MagicStrings.Fahrenheit : MagicStrings.Celsius;
        }
    }

    public static class StringExtensions
    {
        public static string FirstCharToUpper(this string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            return input.First().ToString().ToUpper() + input.Substring(1);
        }
    }
}