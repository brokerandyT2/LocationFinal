// Location.Photography.ViewModels/SettingsViewModel.cs
using Location.Core.Application.Services;
using Location.Photography.ViewModels.Interfaces;

namespace Location.Photography.ViewModels
{
    public class SettingViewModel : ViewModelBase, INavigationAware
    {
        #region Fields
        private int _id;
        private string _key;
        private string _value;
        private string _description;
        private DateTime _timestamp;
        #endregion

        #region Properties
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
        #endregion

        #region Constructors
        public SettingViewModel() : base(null, null)
        {
        }

        public SettingViewModel(IErrorDisplayService errorDisplayService) : base(null, errorDisplayService)
        {
        }
        #endregion

        #region INavigationAware
        public void OnNavigatedToAsync()
        {
            // Implementation not required for this use case
        }

        public void OnNavigatedFromAsync()
        {
            // Implementation not required for this use case
        }
        #endregion
    }

    public class SettingsViewModel : ViewModelBase
    {
        #region Fields
        // Setting ViewModels
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
        private SettingViewModel _subscriptionExpiration;

        // Boolean toggles
        private bool _hemisphereNorth = true;
        private bool _timeFormatToggle = true;
        private bool _dateFormatToggle = true;
        private bool _windDirectionBoolean = true;
        private bool _temperatureFormatToggle = true;
        private bool _adSupportboolean;
        #endregion

        #region Properties
        // Setting ViewModels
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

        public SettingViewModel SubscriptionExpiration
        {
            get => _subscriptionExpiration;
            set => SetProperty(ref _subscriptionExpiration, value);
        }

        // Boolean toggles
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

        public bool AdSupportboolean
        {
            get => _adSupportboolean;
            set => SetProperty(ref _adSupportboolean, value);
        }
        #endregion

        #region Constructors
        public SettingsViewModel() : base(null, null)
        {
            InitializeSettings();
        }

        public SettingsViewModel(IErrorDisplayService errorDisplayService) : base(null, errorDisplayService)
        {
            InitializeSettings();
        }
        #endregion

        #region Methods
        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Initialize all settings in one batch operation
        /// </summary>
        private void InitializeSettings()
        {
            try
            {
                BeginPropertyChangeBatch();

                // Initialize all setting ViewModels with default values
                Hemisphere = new SettingViewModel { Key = "Hemisphere", Value = "North" };
                TimeFormat = new SettingViewModel { Key = "TimeFormat", Value = "12" };
                DateFormat = new SettingViewModel { Key = "DateFormat", Value = "MM/dd/yyyy" };
                Email = new SettingViewModel { Key = "Email", Value = string.Empty };
                WindDirection = new SettingViewModel { Key = "WindDirection", Value = "Degrees" };
                TemperatureFormat = new SettingViewModel { Key = "TemperatureFormat", Value = "Fahrenheit" };
                Subscription = new SettingViewModel { Key = "Subscription", Value = "Free" };

                // Initialize viewed flags
                AddLocationViewed = new SettingViewModel { Key = "AddLocationViewed", Value = "false" };
                ListLocationsViewed = new SettingViewModel { Key = "ListLocationsViewed", Value = "false" };
                EditLocationViewed = new SettingViewModel { Key = "EditLocationViewed", Value = "false" };
                WeatherViewed = new SettingViewModel { Key = "WeatherViewed", Value = "false" };
                SettingsViewed = new SettingViewModel { Key = "SettingsViewed", Value = "false" };
                SunLocationViewed = new SettingViewModel { Key = "SunLocationViewed", Value = "false" };
                SunCalculationViewed = new SettingViewModel { Key = "SunCalculationViewed", Value = "false" };
                ExposureCalculationViewed = new SettingViewModel { Key = "ExposureCalculationViewed", Value = "false" };
                SceneEvaluationViewed = new SettingViewModel { Key = "SceneEvaluationViewed", Value = "false" };
                SubscriptionExpiration = new SettingViewModel { Key = "SubscriptionExpiration", Value = DateTime.MinValue.ToString() };

                _ = EndPropertyChangeBatchAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error initializing settings: {ex.Message}");
                _ = EndPropertyChangeBatchAsync();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update multiple settings in one batch
        /// </summary>
        public void UpdateSettingsBatch(Dictionary<string, string> settings)
        {
            try
            {
                BeginPropertyChangeBatch();

                foreach (var setting in settings)
                {
                    UpdateSettingByKey(setting.Key, setting.Value);
                }

                _ = EndPropertyChangeBatchAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating settings batch: {ex.Message}");
                _ = EndPropertyChangeBatchAsync();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update individual setting without triggering immediate UI updates
        /// </summary>
        private void UpdateSettingByKey(string key, string value)
        {
            var settingToUpdate = key switch
            {
                "Hemisphere" => Hemisphere,
                "TimeFormat" => TimeFormat,
                "DateFormat" => DateFormat,
                "Email" => Email,
                "WindDirection" => WindDirection,
                "TemperatureFormat" => TemperatureFormat,
                "Subscription" => Subscription,
                "AddLocationViewed" => AddLocationViewed,
                "ListLocationsViewed" => ListLocationsViewed,
                "EditLocationViewed" => EditLocationViewed,
                "WeatherViewed" => WeatherViewed,
                "SettingsViewed" => SettingsViewed,
                "SunLocationViewed" => SunLocationViewed,
                "SunCalculationViewed" => SunCalculationViewed,
                "ExposureCalculationViewed" => ExposureCalculationViewed,
                "SceneEvaluationViewed" => SceneEvaluationViewed,
                "SubscriptionExpiration" => SubscriptionExpiration,
                _ => null
            };

            if (settingToUpdate != null)
            {
                settingToUpdate.Value = value;
                settingToUpdate.Timestamp = DateTime.Now;

                // Update corresponding boolean toggles
                UpdateBooleanToggles(key, value);
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update boolean toggles based on setting values
        /// </summary>
        private void UpdateBooleanToggles(string key, string value)
        {
            switch (key)
            {
                case "Hemisphere":
                    HemisphereNorth = value.Equals("North", StringComparison.OrdinalIgnoreCase);
                    break;
                case "TimeFormat":
                    TimeFormatToggle = value.Equals("12", StringComparison.OrdinalIgnoreCase);
                    break;
                case "DateFormat":
                    DateFormatToggle = value.Equals("MM/dd/yyyy", StringComparison.OrdinalIgnoreCase);
                    break;
                case "WindDirection":
                    WindDirectionBoolean = value.Equals("Degrees", StringComparison.OrdinalIgnoreCase);
                    break;
                case "TemperatureFormat":
                    TemperatureFormatToggle = value.Equals("Fahrenheit", StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Reset all settings to defaults in one batch
        /// </summary>
        public void ResetToDefaults()
        {
            try
            {
                BeginPropertyChangeBatch();

                var defaultSettings = new Dictionary<string, string>
                {
                    { "Hemisphere", "North" },
                    { "TimeFormat", "12" },
                    { "DateFormat", "MM/dd/yyyy" },
                    { "Email", string.Empty },
                    { "WindDirection", "Degrees" },
                    { "TemperatureFormat", "Fahrenheit" },
                    { "Subscription", "Free" },
                    { "AddLocationViewed", "false" },
                    { "ListLocationsViewed", "false" },
                    { "EditLocationViewed", "false" },
                    { "WeatherViewed", "false" },
                    { "SettingsViewed", "false" },
                    { "SunLocationViewed", "false" },
                    { "SunCalculationViewed", "false" },
                    { "ExposureCalculationViewed", "false" },
                    { "SceneEvaluationViewed", "false" },
                    { "SubscriptionExpiration", DateTime.MinValue.ToString() }
                };

                foreach (var setting in defaultSettings)
                {
                    UpdateSettingByKey(setting.Key, setting.Value);
                }

                _ = EndPropertyChangeBatchAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error resetting settings: {ex.Message}");
                _ = EndPropertyChangeBatchAsync();
            }
        }

        /// <summary>
        /// Get all settings as a dictionary for serialization/persistence
        /// </summary>
        public Dictionary<string, string> GetAllSettings()
        {
            return new Dictionary<string, string>
            {
                { "Hemisphere", Hemisphere?.Value ?? "North" },
                { "TimeFormat", TimeFormat?.Value ?? "12" },
                { "DateFormat", DateFormat?.Value ?? "MM/dd/yyyy" },
                { "Email", Email?.Value ?? string.Empty },
                { "WindDirection", WindDirection?.Value ?? "Degrees" },
                { "TemperatureFormat", TemperatureFormat?.Value ?? "Fahrenheit" },
                { "Subscription", Subscription?.Value ?? "Free" },
                { "AddLocationViewed", AddLocationViewed?.Value ?? "false" },
                { "ListLocationsViewed", ListLocationsViewed?.Value ?? "false" },
                { "EditLocationViewed", EditLocationViewed?.Value ?? "false" },
                { "WeatherViewed", WeatherViewed?.Value ?? "false" },
                { "SettingsViewed", SettingsViewed?.Value ?? "false" },
                { "SunLocationViewed", SunLocationViewed?.Value ?? "false" },
                { "SunCalculationViewed", SunCalculationViewed?.Value ?? "false" },
                { "ExposureCalculationViewed", ExposureCalculationViewed?.Value ?? "false" },
                { "SceneEvaluationViewed", SceneEvaluationViewed?.Value ?? "false" },
                { "SubscriptionExpiration", SubscriptionExpiration?.Value ?? DateTime.MinValue.ToString() }
            };
        }
        #endregion
    }
}