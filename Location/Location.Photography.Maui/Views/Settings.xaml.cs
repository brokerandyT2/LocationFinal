
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.Application.Services;
using Location.Photography.ViewModels;
using MediatR;
using ISettingRepository = Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository;
using Location.Photography.Infrastructure;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.Queries.GetAllSettings;

namespace Location.Photography.Maui.Views;

public partial class Settings : ContentPage
{
    private IServiceProvider serviceProvider;
    private IMediator _mediator;

    private IAlertService _alertService;
    private ISettingRepository _settings;

    public Settings()
    {
        InitializeComponent();
        BindingContext = new SettingsViewModel();
    }
    public Settings(IMediator mediator,
            IAlertService alertService, ISettingRepository settings)
    {
        _mediator = mediator;
        _alertService = alertService;
        _settings = settings;

        InitializeComponent();

        


    }
    protected override void OnAppearing()
    {
        base.OnAppearing();
        InitializeViewModel();
    }

    private async void InitializeViewModel()
    {
        var viewModel = new SettingsViewModel()
        {
            IsBusy = true
        };
        BindingContext = viewModel;
        viewModel.ErrorOccurred += ViewModel_ErrorOccurred;
        BindingContext = viewModel;

        var hemisphere = new GetSettingByKeyQuery() { Key = MagicStrings.Hemisphere };
        var timeFormat = new GetSettingByKeyQuery() { Key = MagicStrings.TimeFormat };
        var dateFormat = new GetSettingByKeyQuery() { Key = MagicStrings.DateFormat };
        var windDirection = new GetSettingByKeyQuery() { Key = MagicStrings.WindDirection };
        var temperatureFormat = new GetSettingByKeyQuery() { Key = MagicStrings.TemperatureType };
        var addLocationViewed = new GetSettingByKeyQuery() { Key = MagicStrings.AddLocationViewed };
        var listLocationsViewed = new GetSettingByKeyQuery() { Key = MagicStrings.LocationListViewed };
        var weatherViewed = new GetSettingByKeyQuery() { Key = MagicStrings.WeatherDisplayViewed };
        var exposureCalculationViewed = new GetSettingByKeyQuery() { Key = MagicStrings.ExposureCalcViewed };
        var sceneEvaluationViewed = new GetSettingByKeyQuery() { Key = MagicStrings.SceneEvaluationViewed };
        var sunLocationViewed = new GetSettingByKeyQuery() { Key = MagicStrings.SunLocationViewed };
        var sunCalculationViewed = new GetSettingByKeyQuery() { Key = MagicStrings.SunCalculatorViewed };
        var settingsViewed = new GetSettingByKeyQuery() { Key = MagicStrings.SettingsViewed };
        var subscription = new GetSettingByKeyQuery() { Key = MagicStrings.SubscriptionType };
        var subExpiration = new GetSettingByKeyQuery() { Key = MagicStrings.SubscriptionExpiration };


        var allSettings = new GetAllSettingsQuery();
        
            var y = await _mediator.Send(allSettings);



        foreach(var x in y.Data)
        {
            if (x.Key == MagicStrings.Hemisphere)
            {
                viewModel.Hemisphere = new SettingViewModel() { Id = x.Id, Key = x.Key, Value = x.Value, Timestamp = DateTime.Now };
                hemiValue.Text = x.Value.FirstCharToUpper();
                swHemisphere.IsToggled = viewModel.Hemisphere.Value == MagicStrings.North ? true : false;
            }
            else if (x.Key == MagicStrings.TimeFormat)
            {
                viewModel.TimeFormat = new SettingViewModel() { Id = x.Id, Key = x.Key, Value = x.Value, Timestamp = DateTime.Now };
                clockValue.Text = x.Value;
                swClock.IsToggled  = viewModel.TimeFormat.Value == MagicStrings.USTimeformat ? true : false;
            }
        }
        swHemisphere.Toggled += swHemisphere_Toggled;
        swClock.Toggled += swClock_Toggled;
        /*

        viewModel.Hemisphere = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };
        hemiValue.Text = viewModel.Hemisphere.Value.FirstCharToUpper();
        swHemisphere.IsToggled = viewModel.Hemisphere.Value == MagicStrings.North ? true : false;


        viewModel.TimeFormat = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };
        clockValue.Text = viewModel.TimeFormat.Value;
        swClock.IsToggled  = viewModel.TimeFormat.Value == MagicStrings.USTimeformat ? true : false;

        viewModel.DateFormat = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };


        viewModel.WindDirection = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };


        viewModel.TemperatureFormat = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };

        viewModel.AddLocationViewed = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };

        viewModel.ListLocationsViewed = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };

        viewModel.WeatherViewed = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };


        viewModel.ExposureCalculationViewed = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };


        viewModel.SceneEvaluationViewed = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };

        //x = await _mediator.Send(sunLocationViewed);
        viewModel.SunLocationViewed = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };

        viewModel.SunCalculationViewed = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };


        viewModel.SettingsViewed = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };

        viewModel.Subscription = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };

        viewModel.SubscriptionExpiration = new SettingViewModel() { Id = x.Data.Id, Key = x.Data.Key, Value = x.Data.Value, Timestamp = DateTime.Now };
        */
        BindingContext = viewModel;
        viewModel.IsBusy = false;


    }

    private void ViewModel_ErrorOccurred(object? sender, Core.ViewModels.OperationErrorEventArgs e)
    {
        throw new NotImplementedException();
    }

    private async void swHemisphere_Toggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel)
        {
            viewModel.Hemisphere.Value = e.Value ? MagicStrings.North : MagicStrings.South;
            hemiValue.Text = viewModel.Hemisphere.Value.FirstCharToUpper();
            var x = new UpdateSettingCommand() { Key = MagicStrings.Hemisphere, Value = viewModel.Hemisphere.Value, Description = viewModel.Hemisphere.Description };
            var y = await _mediator.Send(x);
            if (y.IsSuccess)
            {
                viewModel.Hemisphere.Value = e.Value ? MagicStrings.North : MagicStrings.South;
            }
            else
            {

            }

        }
    }

    private void swClock_Toggled(object sender, ToggledEventArgs e)
    {
        if (BindingContext is SettingsViewModel viewModel)
        {
            viewModel.TimeFormat.Value = e.Value ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
            clockValue.Text = viewModel.TimeFormat.Value.FirstCharToUpper();
            var x = new UpdateSettingCommand() { Key = MagicStrings.TimeFormat, Value = viewModel.TimeFormat.Value };
            var y = _mediator.Send(x);
            if (y.IsCompletedSuccessfully && y.Result.IsSuccess)
            {
                viewModel.TimeFormat.Value = e.Value ? MagicStrings.USTimeformat : MagicStrings.InternationalTimeFormat;
            }
            else
            {
            }
        }
    }
}