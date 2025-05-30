namespace Location.Photography.ViewModels
{
    public class OptimalWindowDisplayModel
    {
        public string WindowType { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string StartTimeDisplay { get; set; } = string.Empty;
        public string EndTimeDisplay { get; set; } = string.Empty;
        public string LightQuality { get; set; } = string.Empty;
        public string OptimalFor { get; set; } = string.Empty;
        public bool IsCurrentlyActive { get; set; }
        public double ConfidenceLevel { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public string DurationDisplay => $"{Duration.Hours}h {Duration.Minutes}m";
        public string ConfidenceDisplay => $"{ConfidenceLevel:P0} confidence";

        public string GetFormattedStartTime(string timeFormat)
        {
            return StartTime.ToString(timeFormat);
        }

        public string GetFormattedEndTime(string timeFormat)
        {
            return EndTime.ToString(timeFormat);
        }

        public string GetFormattedTimeRange(string timeFormat)
        {
            return $"{StartTime.ToString(timeFormat)} - {EndTime.ToString(timeFormat)}";
        }
    }
}