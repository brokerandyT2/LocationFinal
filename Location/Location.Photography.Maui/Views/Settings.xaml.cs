// Location.Photography.Maui/Views/Settings.xaml.cs
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.Queries.GetAllSettings;
using Location.Photography.Infrastructure;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using MediatR;
using System.Threading.Tasks;

namespace Location.Photography.Maui.Views
{
    public partial class Settings : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly IAlertService _alertService;
        private readonly ISettingRepository _settingRepository;
        private SettingsViewModel _viewModel;

        public Settings()
        {
            InitializeComponent();
            _viewModel = new SettingsViewModel();
            BindingContext = _viewModel;
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
                                _viewModel.Hemisphere = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = DateTime.Now
                                };
                                _viewModel.HemisphereNorth = setting.Value == MagicStrings.North;
                                break;

                            case var key when key == MagicStrings.TimeFormat:
                                _viewModel.TimeFormat = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = DateTime.Now
                                };
                                _viewModel.TimeFormatToggle = setting.Value == MagicStrings.USTimeformat;
                                break;

                            case var key when key == MagicStrings.DateFormat:
                                _viewModel.DateFormat = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = DateTime.Now
                                };
                                _viewModel.DateFormatToggle = setting.Value == MagicStrings.USDateFormat;
                                break;

                            case var key when key == MagicStrings.WindDirection:
                                _viewModel.WindDirection = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = DateTime.Now
                                };
                                _viewModel.WindDirectionBoolean = setting.Value == MagicStrings.TowardsWind;
                                WindDirection.Text = setting.Value == MagicStrings.TowardsWind ? "Towards Wind" : "With Wind";
                                break;

                            case var key when key == MagicStrings.TemperatureType:
                                _viewModel.TemperatureFormat = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = DateTime.Now
                                };
                                _viewModel.TemperatureFormatToggle = setting.Value == MagicStrings.Fahrenheit;
                                break;

                            case var key when key == MagicStrings.SubscriptionType:
                                _viewModel.Subscription = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = DateTime.Now
                                };
                                break;

                            case var key when key == MagicStrings.SubscriptionExpiration:
                                _viewModel.SubscriptionExpiration = new SettingViewModel()
                                {
                                    Id = setting.Id,
                                    Key = setting.Key,
                                    Value = setting.Value,
                                    Description = setting.Description,
                                    Timestamp = DateTime.Now
                                };
                                break;

                            case var key when key == MagicStrings.FreePremiumAdSupported:
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
            if (_viewModel?.Hemisphere != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.North : MagicStrings.South;
                    bool success = await UpdateSettingAsync(MagicStrings.Hemisphere, newValue, _viewModel.Hemisphere.Description);

                    if (success)
                    {
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
            if (_viewModel?.TimeFormat != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
                    bool success = await UpdateSettingAsync(MagicStrings.TimeFormat, newValue, _viewModel.TimeFormat.Description);

                    if (success)
                    {
                        _viewModel.TimeFormat.Value = newValue;
                        _viewModel.TimeFormatToggle = e.Value;
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
            if (_viewModel?.DateFormat != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.USDateFormat : MagicStrings.InternationalFormat;
                    bool success = await UpdateSettingAsync(MagicStrings.DateFormat, newValue, _viewModel.DateFormat.Description);

                    if (success)
                    {
                        _viewModel.DateFormat.Value = newValue;
                        _viewModel.DateFormatToggle = e.Value;
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
            if (_viewModel?.WindDirection != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.TowardsWind : MagicStrings.WithWind;
                    bool success = await UpdateSettingAsync(MagicStrings.WindDirection, newValue, _viewModel.WindDirection.Description);

                    if (success)
                    {
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
            if (_viewModel?.TemperatureFormat != null)
            {
                try
                {
                    string newValue = e.Value ? MagicStrings.Fahrenheit : MagicStrings.Celsius;
                    bool success = await UpdateSettingAsync(MagicStrings.TemperatureType, newValue, _viewModel.TemperatureFormat.Description);

                    if (success)
                    {
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
            if (_viewModel != null)
            {
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
            await Navigation.PushAsync(new NavigationPage( new SubscriptionSignUpPage()));
        }
    }
}