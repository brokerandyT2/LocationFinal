namespace Location.Photography.ViewModels
{
    public class SunPathPoint
    {
        public DateTime Time { get; set; }
        public double Azimuth { get; set; }
        public double Elevation { get; set; }
        public bool IsCurrentPosition { get; set; }
        public bool IsVisible => Elevation > 0;
        public double X => Math.Sin(Azimuth * Math.PI / 180.0) * (90 - Elevation);
        public double Y => Math.Cos(Azimuth * Math.PI / 180.0) * (90 - Elevation);

        public string GetFormattedTime(string timeFormat)
        {
            return Time.ToString(timeFormat);
        }

        public string GetFormattedDate(string dateFormat)
        {
            return Time.ToString(dateFormat);
        }
    }
}