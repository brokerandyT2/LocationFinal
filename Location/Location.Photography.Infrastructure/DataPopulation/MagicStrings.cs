// Location.Photography.Infrastructure/MagicStrings.cs
using System;

namespace Location.Photography.Infrastructure
{
    public static class MagicStrings
    {
        // Hemisphere settings
        public static readonly string Hemisphere = "Hemisphere";
        public static readonly string North = "north";
        public static readonly string South = "south";

        // Date and time format settings
        public static readonly string DateFormat = "DateFormat";
        public static readonly string USDateFormat = "MMM/dd/yyyy";
        public static readonly string InternationalFormat = "dd/MMM/yyyy";
        public static readonly string TimeFormat = "TimeFormat";
        public static readonly string USTimeformat = "hh:mm tt";
        public static readonly string InternationalTimeFormat = "HH:mm";
        // Adding the pattern formats to align with Core
        public static readonly string USTimeformat_Pattern = "hh:mm tt";
        public static readonly string InternationalTimeFormat_Pattern = "HH:mm";

        // Temperature settings
        public static readonly string TemperatureType = "TemperatureType";
        public static readonly string Fahrenheit = "F";
        public static readonly string Celsius = "C";

        // Wind direction settings
        public static readonly string WindDirection = "WindDirection";
        public static readonly string TowardsWind = "towardsWind";
        public static readonly string WithWind = "withWind";

        // Camera settings
        public static readonly string CameraRefresh = "CameraRefresh";

        // User identification
        public static readonly string Email = "Email";
        public static readonly string FirstName = "FirstName";
        public static readonly string LastName = "LastName";
        public static readonly string UniqueID = "UniqueID";
        public static readonly string DeviceInfo = "DeviceInfo";
        public static readonly string NoEmailEntered = "no_email_entered";
        public static readonly string RegEx_Email = "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$";

        // Weather API settings
        public static readonly string Weather_API_Key = "WeatherApiKey";
        public static readonly string WeatherURL = "WeatherURL";
        public static readonly string LastBulkWeatherUpdate = "LastBulkWeatherUpdate";

        // Language settings
        public static readonly string DefaultLanguage = "DefaultLanguage";
        public static readonly string English_for_i8n = "en-US";

        // Boolean string values
        public static readonly string False_string = "false";
        public static readonly string True_string = "true";

        // App usage tracking
        public static readonly string AppOpenCounter = "AppOpenCounter";

        // Subscription settings
        public static readonly string SubscriptionType = "SubscriptionType";
        public static readonly string SubscriptionExpiration = "SubscriptionExpiration";
        public static readonly string Free = "Free";
        public static readonly string Pro = "Pro";
        public static readonly string Premium = "Premium";
        public static readonly string FreePremiumAdSupported = "FreePremiumAdSupported";
        public static readonly string AdGivesHours = "AdGivesHours";

        // Feature view tracking
        public static readonly string HomePageViewed = "HomePageViewed";
        public static readonly string SettingsViewed = "SettingsViewed";
        public static readonly string LocationListViewed = "LocationListViewed";
        public static readonly string TipsViewed = "TipsViewed";
        public static readonly string ExposureCalcViewed = "ExposureCalcViewed";
        public static readonly string LightMeterViewed = "LightMeterViewed";
        public static readonly string SceneEvaluationViewed = "SceneEvaluationViewed";
        public static readonly string AddLocationViewed = "AddLocationViewed";
        public static readonly string WeatherDisplayViewed = "WeatherDisplayViewed";
        public static readonly string SunCalculatorViewed = "SunCalculatorViewed";
        public static readonly string SunLocationViewed = "SunLocationViewed";

        // Ad view timestamps
        public static readonly string ExposureCalcAdViewed_TimeStamp = "ExposureCalcAdViewedTimeStamp";
        public static readonly string LightMeterAdViewed_TimeStamp = "LightMeterAdViewedTimeStamp";
        public static readonly string SceneEvaluationAdViewed_TimeStamp = "SceneEvaluationAdViewedTimeStamp";
        public static readonly string SunCalculatorViewed_TimeStamp = "SunCalculatorAdViewedTimeStamp";
        public static readonly string SunLocationAdViewed_TimeStamp = "SunLocationAdViewedTimeStamp";
        public static readonly string WeatherDisplayAdViewed_TimeStamp = "WeatherDisplayAdViewedTimeStamp";
        public static readonly string Dismiss_LightMeter_Alert = "DismissLightMeterAlert";

        // Feature names
        public static readonly string ExposureCalculator = "ExposureCalculator";
    }
}