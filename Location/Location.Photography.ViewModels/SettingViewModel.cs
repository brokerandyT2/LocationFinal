// Location.Photography.ViewModels/SettingsViewModel.cs
using Location.Core.Application.Services;
using Location.Photography.ViewModels.Interfaces;
using System;

namespace Location.Photography.ViewModels
{
    public class SettingViewModel : ViewModelBase, INavigationAware
    {
        private int _id;
        private string _key;
        private string _value;
        private string _description;
        private DateTime _timestamp;

        public int Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string Description
        {
            get => _description;
            set => SetProperty(ref _description, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        public SettingViewModel() : base(null, null)
        {
        }

        public SettingViewModel(IErrorDisplayService errorDisplayService) : base(null, errorDisplayService)
        {
        }

        public void OnNavigatedToAsync()
        {
            //throw new NotImplementedException();
        }

        public void OnNavigatedFromAsync()
        {
            //throw new NotImplementedException();
        }
    }

    public class SettingsViewModel : ViewModelBase
    {
        private SettingViewModel _hemisphere;
        private SettingViewModel _timeFormat;
        private SettingViewModel _dateFormat;
        private SettingViewModel _email;
        private SettingViewModel _windDirection;
        private SettingViewModel _temperatureFormat;
        private SettingViewModel _subscription;
        private SettingViewModel _addLocationViewed;
        private SettingViewModel _listLocationsViewed;
        private SettingViewModel _editLocationViewed;
        private SettingViewModel _weatherViewed;
        private SettingViewModel _settingsViewed;
        private SettingViewModel _sunLocationViewed;
        private SettingViewModel _sunCalculationViewed;
        private SettingViewModel _exposureCalculationViewed;
        private SettingViewModel _sceneEvaluationViewed;

        private bool _hemisphereNorth = true;
        private bool _timeFormatToggle = true;
        private bool _dateFormatToggle = true;
        private bool _windDirectionBoolean = true;
        private bool _temperatureFormatToggle = true;
        private bool _adSupportboolean;

        public bool AdSupportboolean
        {
            get => _adSupportboolean;
            set => SetProperty(ref _adSupportboolean, value);
        }

        public SettingViewModel Subscription
        {
            get => _subscription;
            set => SetProperty(ref _subscription, value);
        }

        public SettingViewModel AddLocationViewed
        {
            get => _addLocationViewed;
            set => SetProperty(ref _addLocationViewed, value);
        }

        public SettingViewModel ListLocationsViewed
        {
            get => _listLocationsViewed;
            set => SetProperty(ref _listLocationsViewed, value);
        }

        public SettingViewModel EditLocationViewed
        {
            get => _editLocationViewed;
            set => SetProperty(ref _editLocationViewed, value);
        }

        public SettingViewModel WeatherViewed
        {
            get => _weatherViewed;
            set => SetProperty(ref _weatherViewed, value);
        }

        public SettingViewModel SettingsViewed
        {
            get => _settingsViewed;
            set => SetProperty(ref _settingsViewed, value);
        }

        public SettingViewModel SunLocationViewed
        {
            get => _sunLocationViewed;
            set => SetProperty(ref _sunLocationViewed, value);
        }

        public SettingViewModel SunCalculationViewed
        {
            get => _sunCalculationViewed;
            set => SetProperty(ref _sunCalculationViewed, value);
        }

        public SettingViewModel ExposureCalculationViewed
        {
            get => _exposureCalculationViewed;
            set => SetProperty(ref _exposureCalculationViewed, value);
        }

        public SettingViewModel SceneEvaluationViewed
        {
            get => _sceneEvaluationViewed;
            set => SetProperty(ref _sceneEvaluationViewed, value);
        }

        public SettingViewModel Hemisphere
        {
            get => _hemisphere;
            set => SetProperty(ref _hemisphere, value);
        }

        public SettingViewModel TimeFormat
        {
            get => _timeFormat;
            set => SetProperty(ref _timeFormat, value);
        }

        public SettingViewModel DateFormat
        {
            get => _dateFormat;
            set => SetProperty(ref _dateFormat, value);
        }

        public SettingViewModel Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public SettingViewModel WindDirection
        {
            get => _windDirection;
            set => SetProperty(ref _windDirection, value);
        }

        public SettingViewModel TemperatureFormat
        {
            get => _temperatureFormat;
            set => SetProperty(ref _temperatureFormat, value);
        }

        public bool HemisphereNorth
        {
            get => _hemisphereNorth;
            set => SetProperty(ref _hemisphereNorth, value);
        }

        public bool TimeFormatToggle
        {
            get => _timeFormatToggle;
            set => SetProperty(ref _timeFormatToggle, value);
        }

        public bool DateFormatToggle
        {
            get => _dateFormatToggle;
            set => SetProperty(ref _dateFormatToggle, value);
        }

        public bool WindDirectionBoolean
        {
            get => _windDirectionBoolean;
            set => SetProperty(ref _windDirectionBoolean, value);
        }

        public bool TemperatureFormatToggle
        {
            get => _temperatureFormatToggle;
            set => SetProperty(ref _temperatureFormatToggle, value);
        }

        public SettingViewModel SubscriptionExpiration { get; set; }

        public SettingsViewModel() : base(null, null)
        {
        }

        public SettingsViewModel(IErrorDisplayService errorDisplayService) : base(null, errorDisplayService)
        {
        }
    }
}