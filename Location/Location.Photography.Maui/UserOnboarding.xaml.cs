// Location.Photography.Maui/Views/UserOnboarding.xaml.cs
using Location.Core.Application.Services;
using Location.Photography.Infrastructure;
using Location.Photography.Maui.Resources;
using Location.Photography.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Storage;
using System.Linq;

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

        public UserOnboarding()
        {
            InitializeComponent();
            GetSetting();
        }

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

            InitializeComponent();
            GetSetting();
        }

        protected override void OnNavigatedTo(NavigatedToEventArgs args)
        {
            base.OnNavigatedTo(args);
            GetSetting();
        }

        private void GetSetting()
        {
            if (emailAddress != null)
            {
                emailAddress.TextChanged += EmailAddress_TextChanged;
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

        private void EmailAddress_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (emailAddress.Text.Contains('.') && emailAddress.Text.Split('.')[1].Length >= 2)
            {
                UpdateValidationMessageVisibility();
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
                    var guid = Guid.NewGuid().ToString();
                    await SecureStorage.SetAsync(MagicStrings.UniqueID, guid);

                    CancellationTokenSource cts = new CancellationTokenSource();

                    await MainThread.InvokeOnMainThreadAsync(() => {
                        loadingIndicator.IsRunning = true;
                    });

                    await Task.Delay(50);

                    await Task.Run(async () =>
                    {
                        await _databaseInitializer.InitializeDatabaseAsync(
                            cts.Token, hemisphere, temperatureFormat, dateFormat,
                            timeFormat, windDirection, email, _guid);
                    });

                    await Navigation.PushAsync(_serviceProvider.GetRequiredService<SubscriptionSignUpPage>());
                }
                catch (Exception ex)
                {
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
                    await MainThread.InvokeOnMainThreadAsync(() => {
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
            }
            catch (Exception ex)
            {
                _logger?.LogWarning("Failed to save to SecureStorage, falling back to Preferences: {Message}", ex.Message);
            }
        }

        private void HemisphereSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            ((SettingsViewModel)BindingContext).Hemisphere.Value = e.Value ? MagicStrings.North : MagicStrings.South;
        }

        private void TimeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            ((SettingsViewModel)BindingContext).TimeFormat.Value = e.Value ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
        }

        private void DateFormat_Toggled(object sender, ToggledEventArgs e)
        {
            ((SettingsViewModel)BindingContext).DateFormat.Value = e.Value ? MagicStrings.USDateFormat : MagicStrings.InternationalFormat;
        }

        private void WindDirectionSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            var vm = (SettingsViewModel)BindingContext;

            vm.WindDirection.Value = e.Value ? MagicStrings.TowardsWind : MagicStrings.WithWind;

            WindDirection.Text = e.Value
                ? Maui.Resources.AppResources.TowardsWind.FirstCharToUpper()
                : AppResources.WithWind.FirstCharToUpper();
        }

        private void TempFormatSwitch_Toggled(object sender, ToggledEventArgs e)
        {
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