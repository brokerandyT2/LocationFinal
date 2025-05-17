// Location.Photography.ViewModels/SettingsViewModel.cs
using Location.Core.ViewModels;

namespace Location.Photography.ViewModels
{
    public class SettingViewModel : BaseViewModel
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
    }

    public class SettingsViewModel : BaseViewModel
    {
        private SettingViewModel _hemisphere;
        private SettingViewModel _timeFormat;
        private SettingViewModel _dateFormat;
        private SettingViewModel _email;
        private SettingViewModel _windDirection;
        private SettingViewModel _temperatureFormat;
        private bool _hemisphereNorth = true;
        private bool _timeFormatToggle = true;
        private bool _dateFormatToggle = true;
        private bool _windDirectionBoolean = true;
        private bool _temperatureFormatToggle = true;

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
    }
}