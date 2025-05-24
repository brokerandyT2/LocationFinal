using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Services;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.Queries.GetAllSettings;
using Location.Photography.Infrastructure;
using Location.Photography.ViewModels;
using MediatR;

namespace Location.Photography.Maui.Views;

public partial class Settings : ContentPage
{
    #region Services

    private readonly IServiceProvider _serviceProvider;
    private readonly IMediator _mediator;
    private readonly IAlertService _alertService;
    private readonly ISettingRepository _settingRepository;

    #endregion

    #region Constructors

    /// <summary>
    /// Default constructor for design-time and XAML preview
    /// </summary>
    public Settings()
    {
        InitializeComponent();
        BindingContext = new SettingsViewModel();
    }

    /// <summary>
    /// Main constructor with DI
    /// </summary>
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

    #endregion

    #region Lifecycle Methods

    /// <summary>
    /// Load settings when page appears
    /// </summary>
    protected override void OnAppearing()
    {
        base.OnAppearing();
        InitializeViewModel();
    }

    #endregion

    #region Data Loading

    /// <summary>
    /// Initialize the ViewModel with settings data
    /// </summary>
    private async void InitializeViewModel()
    {
        var viewModel = new SettingsViewModel()
        {
            IsBusy = true
        };

        BindingContext = viewModel;
        viewModel.ErrorOccurred += ViewModel_ErrorOccurred;

        try
        {
            // Load all settings
            var allSettingsQuery = new GetAllSettingsQuery();

            System.Diagnostics.Debug.WriteLine("Attempting to send GetAllSettingsQuery...");
            var settingsResult = await _mediator.Send(allSettingsQuery);
            System.Diagnostics.Debug.WriteLine($"GetAllSettingsQuery result: IsSuccess={settingsResult?.IsSuccess}, Data count={settingsResult?.Data?.Count}");

            if (settingsResult.IsSuccess && settingsResult.Data != null)
            {
                // Map settings to ViewModel
                foreach (var setting in settingsResult.Data)
                {
                    switch (setting.Key)
                    {
                        case var key when key == MagicStrings.Hemisphere:
                            viewModel.Hemisphere = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            viewModel.HemisphereNorth = setting.Value == MagicStrings.North;
                            break;

                        case var key when key == MagicStrings.TimeFormat:
                            viewModel.TimeFormat = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            viewModel.TimeFormatToggle = setting.Value == MagicStrings.USTimeformat;
                            break;

                        case var key when key == MagicStrings.DateFormat:
                            viewModel.DateFormat = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            viewModel.DateFormatToggle = setting.Value == MagicStrings.USDateFormat;
                            break;

                        case var key when key == MagicStrings.WindDirection:
                            viewModel.WindDirection = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            viewModel.WindDirectionBoolean = setting.Value == MagicStrings.TowardsWind;
                            // Set wind direction label text
                            WindDirection.Text = setting.Value == MagicStrings.TowardsWind ? "Towards Wind" : "With Wind";
                            break;

                        case var key when key == MagicStrings.TemperatureType:
                            viewModel.TemperatureFormat = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            viewModel.TemperatureFormatToggle = setting.Value == MagicStrings.Fahrenheit;
                            break;

                        case var key when key == MagicStrings.SubscriptionType:
                            viewModel.Subscription = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.SubscriptionExpiration:
                            viewModel.SubscriptionExpiration = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        // Page view settings
                        case var key when key == MagicStrings.AddLocationViewed:
                            viewModel.AddLocationViewed = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.LocationListViewed:
                            viewModel.ListLocationsViewed = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.SettingsViewed:
                            viewModel.SettingsViewed = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.SunCalculatorViewed:
                            viewModel.SunCalculationViewed = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.SceneEvaluationViewed:
                            viewModel.SceneEvaluationViewed = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.SunLocationViewed:
                            viewModel.SunLocationViewed = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.ExposureCalcViewed:
                            viewModel.ExposureCalculationViewed = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.WeatherDisplayViewed:
                            viewModel.WeatherViewed = new SettingViewModel()
                            {
                                Id = setting.Id,
                                Key = setting.Key,
                                Value = setting.Value,
                                Description = setting.Description,
                                Timestamp = DateTime.Now
                            };
                            break;

                        case var key when key == MagicStrings.FreePremiumAdSupported:
                            // Handle ad support setting and set the boolean property
                            viewModel.AdSupportboolean = setting.Value == MagicStrings.True_string;
                            break;
                    }
                }

                // Wire up event handlers
                HemisphereSwitch.Toggled += HemisphereSwitch_Toggled;
                TimeSwitch.Toggled += TimeSwitch_Toggled;
                DateFormat.Toggled += DateFormat_Toggled;
                WindDirectionSwitch.Toggled += WindDirectionSwitch_Toggled;
                TempFormatSwitch.Toggled += TempFormatSwitch_Toggled;
                adsupport.Toggled += AdSupport_Toggled;
            }
            else
            {
                viewModel.ErrorMessage = settingsResult.ErrorMessage ?? "Failed to load settings";
                viewModel.IsError = true;
            }
        }
        catch (Exception ex)
        {
            viewModel.ErrorMessage = $"Error loading settings: {ex.Message}";
            viewModel.IsError = true;

            // Also log to debug output for troubleshooting
            System.Diagnostics.Debug.WriteLine($"Settings initialization error: {ex}");
        }
        finally
        {
            viewModel.IsBusy = false;
        }
    }

    /// <summary>
    /// Update a setting with proper error handling
    /// </summary>
    /// <summary>
    /// Update a setting with proper error handling
    /// </summary>
    private async Task<bool> UpdateSettingAsync(string key, string value, string description = "")
    {
        try
        {
            System.Diagnostics.Debug.WriteLine($"UpdateSettingAsync called: Key={key}, Value={value}");

            var command = new UpdateSettingCommand()
            {
                Key = key,
                Value = value,
                Description = description
            };

            System.Diagnostics.Debug.WriteLine("About to send UpdateSettingCommand via MediatR...");

            // Use ConfigureAwait(false) to avoid deadlock
            var result = await _mediator.Send(command).ConfigureAwait(false);
            System.Diagnostics.Debug.WriteLine($"UpdateSettingCommand result: IsSuccess={result?.IsSuccess}");

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
            System.Diagnostics.Debug.WriteLine($"UpdateSettingAsync exception: {ex}");
            await _alertService.ShowErrorAlertAsync($"Error updating setting: {ex.Message}", "Error");
            return false;
        }
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handle errors from the view model
    /// </summary>
    private void ViewModel_ErrorOccurred(object? sender, Core.ViewModels.OperationErrorEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await _alertService.ShowErrorAlertAsync(e.Message, "Error");
        });
    }

    /// <summary>
    /// Handle hemisphere switch toggle
    /// </summary>
    private async void HemisphereSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel && viewModel.Hemisphere != null)
        {
            try
            {
                string newValue = e.Value ? MagicStrings.North : MagicStrings.South;
                bool success = await UpdateSettingAsync(MagicStrings.Hemisphere, newValue, viewModel.Hemisphere.Description);

                if (success)
                {
                    viewModel.Hemisphere.Value = newValue;
                    viewModel.HemisphereNorth = e.Value;
                }
                else
                {
                    // Revert the switch if update failed
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

    /// <summary>
    /// Handle time format switch toggle
    /// </summary>
    private async void TimeSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel && viewModel.TimeFormat != null)
        {
            try
            {
                string newValue = e.Value ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
                bool success = await UpdateSettingAsync(MagicStrings.TimeFormat, newValue, viewModel.TimeFormat.Description);

                if (success)
                {
                    viewModel.TimeFormat.Value = newValue;
                    viewModel.TimeFormatToggle = e.Value;
                }
                else
                {
                    // Revert the switch if update failed
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

    /// <summary>
    /// Handle date format switch toggle
    /// </summary>
    private async void DateFormat_Toggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel && viewModel.DateFormat != null)
        {
            try
            {
                string newValue = e.Value ? MagicStrings.USDateFormat : MagicStrings.InternationalFormat;
                bool success = await UpdateSettingAsync(MagicStrings.DateFormat, newValue, viewModel.DateFormat.Description);

                if (success)
                {
                    viewModel.DateFormat.Value = newValue;
                    viewModel.DateFormatToggle = e.Value;
                }
                else
                {
                    // Revert the switch if update failed
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

    /// <summary>
    /// Handle wind direction switch toggle
    /// </summary>
    private async void WindDirectionSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel && viewModel.WindDirection != null)
        {
            try
            {
                string newValue = e.Value ? MagicStrings.TowardsWind : MagicStrings.WithWind;
                bool success = await UpdateSettingAsync(MagicStrings.WindDirection, newValue, viewModel.WindDirection.Description);

                if (success)
                {
                    viewModel.WindDirection.Value = newValue;
                    viewModel.WindDirectionBoolean = e.Value;
                    // Update the label text
                    WindDirection.Text = e.Value ? "Towards Wind" : "With Wind";
                }
                else
                {
                    // Revert the switch if update failed
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

    /// <summary>
    /// Handle temperature format switch toggle
    /// </summary>
    private async void TempFormatSwitch_Toggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel && viewModel.TemperatureFormat != null)
        {
            try
            {
                string newValue = e.Value ? MagicStrings.Fahrenheit : MagicStrings.Celsius;
                bool success = await UpdateSettingAsync(MagicStrings.TemperatureType, newValue, viewModel.TemperatureFormat.Description);

                if (success)
                {
                    viewModel.TemperatureFormat.Value = newValue;
                    viewModel.TemperatureFormatToggle = e.Value;
                }
                else
                {
                    // Revert the switch if update failed
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

    /// <summary>
    /// Handle ad support switch toggle
    /// </summary>
    private async void AdSupport_Toggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel)
        {
            try
            {
                string newValue = e.Value ? MagicStrings.True_string : MagicStrings.False_string;
                bool success = await UpdateSettingAsync(MagicStrings.FreePremiumAdSupported, newValue, "Whether the app is running in ad-supported mode");

                if (!success)
                {
                    // Revert the switch if update failed
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

    #endregion
}