using System;

namespace Location.Photography.ViewModels
{
    public class HourlyPredictionDisplayModel
    {
        public DateTime Time { get; set; }
        public string DeviceTimeDisplay { get; set; } = string.Empty;
        public string LocationTimeDisplay { get; set; } = string.Empty;
        public double PredictedEV { get; set; }
        public double EVConfidenceMargin { get; set; }
        public string SuggestedAperture { get; set; } = string.Empty;
        public string SuggestedShutterSpeed { get; set; } = string.Empty;
        public string SuggestedISO { get; set; } = string.Empty;
        public double ConfidenceLevel { get; set; }
        public string LightQuality { get; set; } = string.Empty;
        public double ColorTemperature { get; set; }
        public string Recommendations { get; set; } = string.Empty;
        public bool IsOptimalTime { get; set; }
        public string FormattedPrediction => $"EV {PredictedEV:F1} ±{EVConfidenceMargin:F1}";
        public string FormattedSettings => $"f/{SuggestedAperture} @ {SuggestedShutterSpeed} ISO {SuggestedISO}";
        public string ConfidenceDisplay => $"{ConfidenceLevel:P0} confidence";
    }
}