// Location.Photography.Maui/Views/Settings.xaml.cs
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.Queries.GetAllSettings;
using Location.Photography.Infrastructure;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using MediatR;

namespace Location.Photography.Maui.Views
{
    public partial class Settings : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly ISettingRepository _settingRepository;
        private SettingsViewModel _viewModel;

        // Store response objects from query
        private GetAllSettingsQueryResponse _hemisphereSetting;
        private GetAllSettingsQueryResponse _timeFormatSetting;
        private GetAllSettingsQueryResponse _dateFormatSetting;
        private GetAllSettingsQueryResponse _windDirectionSetting;
        private GetAllSettingsQueryResponse _temperatureFormatSetting;
        private GetAllSettingsQueryResponse _subscriptionTypeSetting;
        private GetAllSettingsQueryResponse _subscriptionExpirationSetting;
        private GetAllSettingsQueryResponse _adSupportSetting;

        public Settings()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();

            BindingContext = _viewModel;
            InitializeViewModel();
        }

        public Settings(
            IMediator mediator,
            IAlertService alertService,
            ISettingRepository settingRepository)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _settingRepository = settingRepository ?? throw new ArgumentNullException(nameof(settingRepository));

            InitializeComponent();
            InitializeViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            InitializeViewModel();
        }

        private async void InitializeViewModel()
        {
            _viewModel = new SettingsViewModel()
            {
                IsBusy = true
            };

            BindingContext = _viewModel;
            _viewModel.ErrorOccurred -= OnSystemError;
            _viewModel.ErrorOccurred += OnSystemError;

            try
            {
                var allSettingsQuery = new GetAllSettingsQuery();
                var settingsResult = await _mediator.Send(allSettingsQuery);

                if (settingsResult.IsSuccess && settingsResult.Data != null)
                {
                    foreach (var setting in settingsResult.Data)
                    {
                        switch (setting.Key)
                        {
                            case var key when key == MagicStrings.Hemisphere:
                                _hemisphereSetting = setting;
                                _viewModel.Hemisphere = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = setting.Timestamp
                                };
                                _viewModel.HemisphereNorth = setting.Value == MagicStrings.North;
                                break;

                            case var key when key == MagicStrings.TimeFormat:
                                _timeFormatSetting = setting;
                                _viewModel.TimeFormat = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = setting.Timestamp
                                };
                                _viewModel.TimeFormatToggle = setting.Value == MagicStrings.USTimeformat;
                                TimeFormatPattern.Text = setting.Value;

                                GetFormattedExpiration();
                                break;

                            case var key when key == MagicStrings.DateFormat:
                                _dateFormatSetting = setting;
                                _viewModel.DateFormat = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = setting.Timestamp
                                };
                                _viewModel.DateFormatToggle = setting.Value == MagicStrings.USDateFormat;
                                break;

                            case var key when key == MagicStrings.WindDirection:
                                _windDirectionSetting = setting;
                                _viewModel.WindDirection = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = setting.Timestamp
                                };
                                _viewModel.WindDirectionBoolean = setting.Value == MagicStrings.TowardsWind;
                                WindDirection.Text = setting.Value == MagicStrings.TowardsWind ? "Towards Wind" : "With Wind";
                                break;

                            case var key when key == MagicStrings.TemperatureType:
                                _temperatureFormatSetting = setting;
                                _viewModel.TemperatureFormat = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = setting.Timestamp
                                };
                                _viewModel.TemperatureFormatToggle = setting.Value == MagicStrings.Fahrenheit;
                                break;

                            case var key when key == MagicStrings.SubscriptionType:
                                _subscriptionTypeSetting = setting;
                                _viewModel.Subscription = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = setting.Timestamp
                                };
                                break;

                            case var key when key == MagicStrings.SubscriptionExpiration:
                                _subscriptionExpirationSetting = setting;
                                _viewModel.SubscriptionExpiration = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = setting.Timestamp
                                };
                                GetFormattedExpiration();
                                break;

                            case var key when key == MagicStrings.FreePremiumAdSupported:
                                _adSupportSetting = setting;
                                _viewModel.AdSupportboolean = setting.Value == MagicStrings.True_string;
                                break;
                        }
                    }

                    HemisphereSwitch.Toggled += HemisphereSwitch_Toggled;
                    TimeSwitch.Toggled += TimeSwitch_Toggled;
                    DateFormat.Toggled += DateFormat_Toggled;
                    WindDirectionSwitch.Toggled += WindDirectionSwitch_Toggled;
                    TempFormatSwitch.Toggled += TempFormatSwitch_Toggled;
                    adsupport.Toggled += AdSupport_Toggled;
                }
                else
                {
                    _viewModel.ErrorMessage = settingsResult.ErrorMessage ?? "Failed to load settings";
                    _viewModel.IsError = true;
                }
            }
            catch (Exception ex)
            {
                _viewModel.ErrorMessage = $"Error loading settings: {ex.Message}";
                _viewModel.IsError = true;
                System.Diagnostics.Debug.WriteLine($"Settings initialization error: {ex}");
            }
            finally
            {
                _viewModel.IsBusy = false;
                BindingContext = _viewModel;
            }
        }

        private void GetFormattedExpiration()
        {
            if (_viewModel.DateFormat != null && _viewModel.TimeFormat != null)
            {
                if (!string.IsNullOrEmpty(_viewModel.DateFormat.Value) && !string.IsNullOrEmpty(_viewModel.TimeFormat.Value))
                {
                    var date = _viewModel.DateFormat.Value.ToString();
                    var time = _viewModel.TimeFormat.Value.ToString();
                    var format = date + ' ' + time;
                    Failure.Text = DateTime.Parse(_viewModel.SubscriptionExpiration.Value).ToString(format);
                }
            }
        }

        private async Task<bool> UpdateSettingAsync(string key, string value, string description = "")
        {
            try
            {
                var command = new UpdateSettingCommand()
                {
                    Key = key,
                    Value = value,
                    Description = description
                };

                var result = await _mediator.Send(command).ConfigureAwait(false);

                if (!result.IsSuccess)
                {
                    await _alertService.ShowErrorAlertAsync(
                        result.ErrorMessage ?? "Failed to update setting",
                        "Error");
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                await _alertService.ShowErrorAlertAsync($"Error updating setting: {ex.Message}", "Error");
                return false;
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
            if (retry && sender is SettingsViewModel viewModel)
            {
                await viewModel.RetryLastCommandAsync();
            }
        }

        private async void HemisphereSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (_hemisphereSetting != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.North : MagicStrings.South;
                    bool success = await UpdateSettingAsync(_hemisphereSetting.Key, newValue, _hemisphereSetting.Description);

                    if (success)
                    {
                        _hemisphereSetting.Value = newValue;
                        _viewModel.Hemisphere.Value = newValue;
                        _viewModel.HemisphereNorth = e.Value;
                    }
                    else
                    {
                        HemisphereSwitch.Toggled -= HemisphereSwitch_Toggled;
                        HemisphereSwitch.IsToggled = !e.Value;
                        HemisphereSwitch.Toggled += HemisphereSwitch_Toggled;
                    }
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error updating hemisphere: {ex.Message}", "Error");
                }
            }
        }

        private async void TimeSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (_timeFormatSetting != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
                    bool success = await UpdateSettingAsync(_timeFormatSetting.Key, newValue, _timeFormatSetting.Description);

                    if (success)
                    {
                        _timeFormatSetting.Value = newValue;
                        _viewModel.TimeFormat.Value = newValue;
                        _viewModel.TimeFormatToggle = e.Value;
                        GetFormattedExpiration();
                    }
                    else
                    {
                        TimeSwitch.Toggled -= TimeSwitch_Toggled;
                        TimeSwitch.IsToggled = !e.Value;
                        TimeSwitch.Toggled += TimeSwitch_Toggled;
                    }
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error updating time format: {ex.Message}", "Error");
                }
            }
        }

        private async void DateFormat_Toggled(object sender, ToggledEventArgs e)
        {
            if (_dateFormatSetting != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.USDateFormat : MagicStrings.InternationalFormat;
                    bool success = await UpdateSettingAsync(_dateFormatSetting.Key, newValue, _dateFormatSetting.Description);

                    if (success)
                    {
                        _dateFormatSetting.Value = newValue;
                        _viewModel.DateFormat.Value = newValue;
                        _viewModel.DateFormatToggle = e.Value;
                        GetFormattedExpiration();
                    }
                    else
                    {
                        DateFormat.Toggled -= DateFormat_Toggled;
                        DateFormat.IsToggled = !e.Value;
                        DateFormat.Toggled += DateFormat_Toggled;
                    }
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error updating date format: {ex.Message}", "Error");
                }
            }
        }

        private async void WindDirectionSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (_windDirectionSetting != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.TowardsWind : MagicStrings.WithWind;
                    bool success = await UpdateSettingAsync(_windDirectionSetting.Key, newValue, _windDirectionSetting.Description);

                    if (success)
                    {
                        _windDirectionSetting.Value = newValue;
                        _viewModel.WindDirection.Value = newValue;
                        _viewModel.WindDirectionBoolean = e.Value;
                        WindDirection.Text = e.Value ? "Towards Wind" : "With Wind";
                    }
                    else
                    {
                        WindDirectionSwitch.Toggled -= WindDirectionSwitch_Toggled;
                        WindDirectionSwitch.IsToggled = !e.Value;
                        WindDirectionSwitch.Toggled += WindDirectionSwitch_Toggled;
                    }
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error updating wind direction: {ex.Message}", "Error");
                }
            }
        }

        private async void TempFormatSwitch_Toggled(object sender, ToggledEventArgs e)
        {
            if (_temperatureFormatSetting != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.Fahrenheit : MagicStrings.Celsius;
                    bool success = await UpdateSettingAsync(_temperatureFormatSetting.Key, newValue, _temperatureFormatSetting.Description);

                    if (success)
                    {
                        _temperatureFormatSetting.Value = newValue;
                        _viewModel.TemperatureFormat.Value = newValue;
                        _viewModel.TemperatureFormatToggle = e.Value;
                    }
                    else
                    {
                        TempFormatSwitch.Toggled -= TempFormatSwitch_Toggled;
                        TempFormatSwitch.IsToggled = !e.Value;
                        TempFormatSwitch.Toggled += TempFormatSwitch_Toggled;
                    }
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error updating temperature format: {ex.Message}", "Error");
                }
            }
        }

        private async void AdSupport_Toggled(object sender, ToggledEventArgs e)
        {
            if (_adSupportSetting != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.True_string : MagicStrings.False_string;
                    bool success = await UpdateSettingAsync(_adSupportSetting.Key, newValue, _adSupportSetting.Description);

                    if (success)
                    {
                        _adSupportSetting.Value = newValue;
                        _viewModel.AdSupportboolean = e.Value;
                    }
                    else
                    {
                        adsupport.Toggled -= AdSupport_Toggled;
                        adsupport.IsToggled = !e.Value;
                        adsupport.Toggled += AdSupport_Toggled;
                    }
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error updating ad support: {ex.Message}", "Error");
                }
            }
            else
            {
                // Fallback to original method if setting not found
                try
                {
                    string newValue = e.Value ? MagicStrings.True_string : MagicStrings.False_string;
                    bool success = await UpdateSettingAsync(MagicStrings.FreePremiumAdSupported, newValue, "Whether the app is running in ad-supported mode");

                    if (!success)
                    {
                        adsupport.Toggled -= AdSupport_Toggled;
                        adsupport.IsToggled = !e.Value;
                        adsupport.Toggled += AdSupport_Toggled;
                    }
                }
                catch (Exception ex)
                {
                    await _alertService.ShowErrorAlertAsync($"Error updating ad support: {ex.Message}", "Error");
                }
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= OnSystemError;
            }
        }

        private async void Button_Pressed(object sender, EventArgs e)
        {
            await Shell.Current.Navigation.PushAsync(new SubscriptionSignUpPage(), true);
        }
    }
}