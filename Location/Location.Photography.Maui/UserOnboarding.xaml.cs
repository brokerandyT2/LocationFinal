using Location.Core.Application.Services;
using Location.Photography.Infrastructure;
using Location.Photography.Maui.Resources;
using Location.Photography.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Maui.Views
{
    public partial class UserOnboarding : ContentPage
    {
        #region Services

        private readonly IAlertService _alertService;
        private readonly DatabaseInitializer _databaseInitializer;

        #endregion

        #region Fields
        private string _guid;
        private bool _saveAttempted = false;

        #endregion

        #region Constructors

        /// <summary>
        /// Default constructor for design-time and XAML preview
        /// </summary>
        public UserOnboarding()
        {
            InitializeComponent();
        }
        private readonly IServiceProvider _serviceProvider;
        /// <summary>
        /// Main constructor with DI
        /// </summary>
        public UserOnboarding(IAlertService alertService, DatabaseInitializer databaseInitializer, ILogger<UserOnboarding> logger, IServiceProvider serviceProvider)
        {
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _databaseInitializer = databaseInitializer ?? throw new ArgumentNullException(nameof(databaseInitializer));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider;
            InitializeComponent();
        }

        #endregion
        private readonly ILogger<UserOnboarding> _logger;

       
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
            // Update validation message visibility when text changes
            if (emailAddress.Text.Contains('.') && emailAddress.Text.Split('.')[1].Length >= 2)
            {
                // only execute validation if someone has entered at least 2 characters (minimum) after a period.  
                // This way we aren't showing an error until the user has had the chance to put in an actual address
                UpdateValidationMessageVisibility();
            }
        }

        private void UpdateValidationMessageVisibility()
        {
            var validationBehavior = emailAddress.Behaviors.OfType<CommunityToolkit.Maui.Behaviors.TextValidationBehavior>().FirstOrDefault();
            bool isValid = validationBehavior?.IsValid ?? false;
            bool hasText = !string.IsNullOrWhiteSpace(emailAddress.Text);

            // Show validation message only if there's text AND it's invalid
            // OR if save button was pressed (tracked by a separate flag)
            emailValidationMessage.IsVisible = (hasText && !isValid) || _saveAttempted;

            // You could customize the message based on the error
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

            UpdateValidationMessageVisibility();

            if (isValid && hasText)
            {
                // Reset the save attempted flag
                _saveAttempted = false;

                // Extract settings values for clarity
                string hemisphere = HemisphereSwitch.IsToggled ? MagicStrings.North : MagicStrings.South;
                string temperatureFormat = TempFormatSwitch.IsToggled ? MagicStrings.Fahrenheit : MagicStrings.Celsius;
                string dateFormat = DateFormat.IsToggled ? MagicStrings.USDateFormat : MagicStrings.InternationalFormat;
                string timeFormat = TimeSwitch.IsToggled ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
                string windDirection = WindDirectionSwitch.IsToggled ? MagicStrings.TowardsWind : MagicStrings.WithWind;
                string email = emailAddress.Text;

                // Save critical settings to secure storage for quick access
                await SaveToSecureStorageAsync(email);

                // Show processing indicator
                processingOverlay.IsVisible = true;

                try
                {
                    var guid = Guid.NewGuid().ToString();

                  await  SecureStorage.SetAsync(MagicStrings.UniqueID, guid);
                    // Initialize database with all settings on a background thread
                    CancellationTokenSource cts = new CancellationTokenSource();

                  

                    await Task.Run(async () =>
                    {
                         await _databaseInitializer.InitializeDatabaseAsync(cts.Token,hemisphere,temperatureFormat,dateFormat,timeFormat,
                            windDirection,email,_guid);
                    });

                    // Navigate to the main page on success
                    await Navigation.PushAsync(new MainPage(_serviceProvider));
                }
                catch (Exception ex)
                {
                    // Handle any errors
                    string errorMessage = "Error processing data";
                    if (AppResources.ResourceManager.GetString("ErrorProcessingData") != null)
                    {
                        errorMessage = AppResources.ResourceManager.GetString("ErrorProcessingData");
                    }

                    await _alertService.ShowErrorAlertAsync($"{errorMessage}: {ex.Message}", "Error");
                }
                finally
                {
                    // Hide processing indicator
                    processingOverlay.IsVisible = false;
                }
            }
        }

        private async Task SaveToSecureStorageAsync(string email)
        {
            try
            {
                _guid = Guid.NewGuid().ToString();
                // Save only sensitive user info to secure storage
                await SecureStorage.Default.SetAsync(MagicStrings.Email, email);
                await SecureStorage.Default.SetAsync(MagicStrings.UniqueID, _guid);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to save to SecureStorage, falling back to Preferences: {Message}", ex.Message);

                // Fall back to Preferences if SecureStorage fails
                
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

    // Extension method for string capitalization if needed
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