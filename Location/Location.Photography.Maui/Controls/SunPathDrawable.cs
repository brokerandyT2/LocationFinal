// Location.Photography.Maui/Controls/SunPathDrawable.cs
using Microsoft.Maui.Graphics;
using Location.Photography.ViewModels;
using Location.Photography.Domain.Models;

namespace Location.Photography.Maui.Controls
{
    public class SunPathDrawable : IDrawable
    {
        private EnhancedSunCalculatorViewModel _viewModel;
        private readonly Color _backgroundColor = Colors.White;
        private readonly Color _daylightColor = Color.FromRgb(135, 206, 235); // Sky blue
        private readonly Color _nightColor = Color.FromRgb(25, 25, 112); // Dark blue
        private readonly Color _sunColor = Color.FromRgb(255, 215, 0); // Gold
        private readonly Color _textColor = Colors.Black;

        public SunPathDrawable(EnhancedSunCalculatorViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void UpdateViewModel(EnhancedSunCalculatorViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_viewModel?.SelectedLocation == null) return;

            var width = dirtyRect.Width;
            var height = dirtyRect.Height;
            var centerX = width / 2;
            var centerY = height * 0.55f; // Move up to 55% to make room for labels below
            var amplitude = height * 0.25f; // Adjust amplitude proportionally

            // Clear background
            canvas.FillColor = _backgroundColor;
            canvas.FillRectangle(dirtyRect);

            // Get sun times for the selected date
            var sunTimes = GetSunTimesForSelectedDate();
            if (sunTimes == null) return;

            // Draw the cosine wave
            DrawCosineWave(canvas, centerX, centerY, width, height, amplitude);

            // Draw time labels using trigonometry
            DrawTimeLabelsWithTrigonometry(canvas, centerX, centerY, width, amplitude, sunTimes);

            // Draw current sun position using trigonometry
            DrawCurrentSunPositionWithTrigonometry(canvas, centerX, centerY, width, amplitude, sunTimes);
        }

        private void DrawCosineWave(ICanvas canvas, float centerX, float centerY, float canvasWidth, float canvasHeight, float amplitude)
        {
            var daylightPath = new PathF();
            var nightPath = new PathF();

            bool daylightStarted = false;
            bool nightStarted = false;

            // Generate wave points across full canvas width
            for (float canvasX = 0; canvasX <= canvasWidth; canvasX += 1f)
            {
                // Map canvas X (0 to canvasWidth) to mathematical X (-3 to +3)
                double mathX = ((canvasX / canvasWidth) * 6.0) - 3.0;

                // Calculate y using cosine with clamping
                double mathY = CosLimited(mathX);

                // Map mathematical Y to canvas Y (flip vertically and scale)
                float canvasY = centerY - (float)(mathY * amplitude);

                // Separate daylight (y >= 0) and night (y < 0) portions
                if (mathY >= 0) // Daylight portion
                {
                    if (!daylightStarted)
                    {
                        daylightPath.MoveTo(canvasX, canvasY);
                        daylightStarted = true;
                    }
                    else
                    {
                        daylightPath.LineTo(canvasX, canvasY);
                    }
                    nightStarted = false; // Reset night path
                }
                else // Night portion (y < 0)
                {
                    if (!nightStarted)
                    {
                        nightPath.MoveTo(canvasX, canvasY);
                        nightStarted = true;
                    }
                    else
                    {
                        nightPath.LineTo(canvasX, canvasY);
                    }
                    daylightStarted = false; // Reset daylight path
                }
            }

            // Draw horizon line at y = 0
            canvas.StrokeColor = Color.FromRgb(128, 128, 128);
            canvas.StrokeSize = 1;
            canvas.DrawLine(0, centerY, canvasWidth, centerY);

            // Draw night portion (y < 0) in dark blue
            if (nightPath.Count > 0)
            {
                canvas.StrokeColor = _nightColor;
                canvas.StrokeSize = 4;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawPath(nightPath);
            }

            // Draw daylight portion (y >= 0) in sky blue
            if (daylightPath.Count > 0)
            {
                canvas.StrokeColor = _daylightColor;
                canvas.StrokeSize = 4;
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawPath(daylightPath);
            }
        }

        private double CosLimited(double x)
        {
            if (x < -3 || x > 3)
                return x < -3 ? -3 : x > 3 ? 3 : x; // clamp to edge value
            return Math.Cos(Math.PI / 4 * x);
        }

        private SunTimesData? GetSunTimesForSelectedDate()
        {
            if (_viewModel?.SelectedLocation == null) return null;

            try
            {
                var selectedDate = _viewModel.SelectedDateProp.Date;
                var coordinate = new CoordinateSharp.Coordinate(_viewModel.SelectedLocation.Latitude, _viewModel.SelectedLocation.Longitude, selectedDate);

                var sunTimes = new SunTimesData
                {
                    Dawn = TimeZoneInfo.ConvertTimeFromUtc(coordinate.CelestialInfo.AdditionalSolarTimes.CivilDawn.Value, TimeZoneInfo.Local),
                    Sunrise = TimeZoneInfo.ConvertTimeFromUtc(coordinate.CelestialInfo.SunRise.Value, TimeZoneInfo.Local),
                    SolarNoon = TimeZoneInfo.ConvertTimeFromUtc(coordinate.CelestialInfo.SolarNoon.Value, TimeZoneInfo.Local),
                    Sunset = TimeZoneInfo.ConvertTimeFromUtc(coordinate.CelestialInfo.SunSet.Value, TimeZoneInfo.Local),
                    Dusk = TimeZoneInfo.ConvertTimeFromUtc(coordinate.CelestialInfo.AdditionalSolarTimes.CivilDusk.Value, TimeZoneInfo.Local)
                };

                return sunTimes;
            }
            catch
            {
                return null;
            }
        }

        private (float x, float y) CalculatePositionFromTime(DateTime time, DateTime sunrise, DateTime sunset, float canvasWidth, float amplitude, float centerY)
        {
            // Calculate daylight duration in hours
            var daylightDuration = (sunset - sunrise).TotalHours;

            // Calculate hours since sunrise
            var hoursSinceSunrise = (time - sunrise).TotalHours;

            // Map to x coordinate: sunrise=-2, sunset=+2, span=4 units
            var mathX = -2.0 + (hoursSinceSunrise / daylightDuration) * 4.0;

            // Calculate y using cosine equation: y = cos(π/4 * x)
            var mathY = Math.Cos(Math.PI / 4.0 * mathX);

            // Convert to canvas coordinates
            var canvasX = Math.Abs((float)((mathX + 3.0) / 6.0 * canvasWidth));
            var canvasY = Math.Abs(centerY - (float)(mathY * amplitude));

            return (canvasX, canvasY);
        }

        private void DrawTimeLabelsWithTrigonometry(ICanvas canvas, float centerX, float centerY, float canvasWidth, float amplitude, SunTimesData sunTimes)
        {
            canvas.FontSize = 11;
            canvas.FontColor = _textColor;
            var timeFormat = "HH:mm";

            // Convert times to local
            var dawn = sunTimes.Dawn;
            var sunrise = sunTimes.Sunrise;
            var sunset = sunTimes.Sunset;
            var dusk = sunTimes.Dusk;
            var solarNoon = sunTimes.SolarNoon;

            // Calculate golden hour times
            var goldenHourMorningEnd = sunrise.AddHours(1);
            var goldenHourEveningStart = sunset.AddHours(-1);

            // Draw Sunrise label (at horizon)
            var sunrisePos = CalculatePositionFromTime(sunrise, sunrise, sunset, canvasWidth, amplitude, centerY);
            canvas.DrawString("Sunrise", sunrisePos.x - 30, centerY + 10, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString(sunrise.ToString(timeFormat), sunrisePos.x - 30, centerY + 25, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Draw Sunset label (at horizon)
            var sunsetPos = CalculatePositionFromTime(sunset, sunrise, sunset, canvasWidth, amplitude, centerY);
            canvas.DrawString("Sunset", sunsetPos.x - 30, centerY + 10, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString(sunset.ToString(timeFormat), sunsetPos.x - 30, centerY + 25, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Draw Golden Hour Morning End
            var goldenMorningPos = CalculatePositionFromTime(goldenHourMorningEnd, sunrise, sunset, canvasWidth, amplitude, centerY);
            canvas.DrawString("Golden Hour End", goldenMorningPos.x - 40, goldenMorningPos.y - 35, 80, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString(goldenHourMorningEnd.ToString(timeFormat), goldenMorningPos.x - 40, goldenMorningPos.y - 20, 80, 15, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Draw Solar Noon
            var solarNoonPos = CalculatePositionFromTime(solarNoon, sunrise, sunset, canvasWidth, amplitude, centerY);
            canvas.DrawString("Solar Noon", solarNoonPos.x - 30, solarNoonPos.y - 25, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString(solarNoon.ToString(timeFormat), solarNoonPos.x - 30, solarNoonPos.y - 10, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Draw Golden Hour Evening Start
            var goldenEveningPos = CalculatePositionFromTime(goldenHourEveningStart, sunrise, sunset, canvasWidth, amplitude, centerY);
            canvas.DrawString("Golden Hour Start", goldenEveningPos.x - 40, goldenEveningPos.y - 25, 80, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString(goldenHourEveningStart.ToString(timeFormat), goldenEveningPos.x - 40, goldenEveningPos.y - 10, 80, 15, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Dawn - positioned in negative y region (left side)
            var dawnYPos = centerY + amplitude * 0.7f; // Below horizon
            canvas.DrawString("Dawn", 20, dawnYPos + 10, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString(dawn.ToString(timeFormat), 50, dawnYPos + 25, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Dusk - positioned in negative y region (right side)
            var duskYPos = centerY + amplitude * 0.7f; // Below horizon
            canvas.DrawString("Dusk", canvasWidth - 50, duskYPos + 10, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString(dusk.ToString(timeFormat), canvasWidth - 50, duskYPos + 25, 60, 15, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Draw markers
            DrawMarkersWithTrigonometry(canvas, centerX, centerY, canvasWidth, amplitude, sunTimes, sunrise, sunset, solarNoon, goldenHourMorningEnd, goldenHourEveningStart);
        }

        private void DrawMarkersWithTrigonometry(ICanvas canvas, float centerX, float centerY, float canvasWidth, float amplitude, SunTimesData sunTimes, DateTime sunrise, DateTime sunset, DateTime solarNoon, DateTime goldenHourMorningEnd, DateTime goldenHourEveningStart)
        {
            // Solar Noon marker (dark orange)
            var solarNoonPos = CalculatePositionFromTime(solarNoon, sunrise, sunset, canvasWidth, amplitude, centerY);
            DrawMarker(canvas, solarNoonPos.x, solarNoonPos.y, Color.FromRgb(255, 140, 0));

            // Golden Hour Morning End marker (gold)
            var goldenMorningPos = CalculatePositionFromTime(goldenHourMorningEnd, sunrise, sunset, canvasWidth, amplitude, centerY);
            DrawMarker(canvas, goldenMorningPos.x, goldenMorningPos.y, Color.FromRgb(255, 215, 0));

            // Golden Hour Evening Start marker (gold)
            var goldenEveningPos = CalculatePositionFromTime(goldenHourEveningStart, sunrise, sunset, canvasWidth, amplitude, centerY);
            DrawMarker(canvas, goldenEveningPos.x, goldenEveningPos.y, Color.FromRgb(255, 215, 0));
        }

        private void DrawMarker(ICanvas canvas, float x, float y, Color color)
        {
            // Draw filled circle marker
            canvas.FillColor = color;
            canvas.FillCircle(x, y, 4);

            // Add white border
            canvas.StrokeColor = Colors.White;
            canvas.StrokeSize = 1;
            canvas.DrawCircle(x, y, 4);
        }

        private void DrawCurrentSunPositionWithTrigonometry(ICanvas canvas, float centerX, float centerY, float canvasWidth, float amplitude, SunTimesData sunTimes)
        {
            try
            {
                var now = DateTime.Now;
                var selectedDate = _viewModel?.SelectedDateProp.Date ?? DateTime.Today;

                // Convert sun times to local for calculation
                var sunrise = sunTimes.Sunrise;
                var sunset = sunTimes.Sunset;

                // Use current time with selected date
                var targetDateTime = selectedDate.Date.Add(now.TimeOfDay);

                // Only draw if current time is between sunrise and sunset

                var currentPos = CalculatePositionFromTime(targetDateTime, sunrise, sunset, canvasWidth, amplitude, centerY);

                // Draw sun circle
                canvas.FillColor = _sunColor;
                canvas.FillCircle(currentPos.x, currentPos.y, 8);

                // Add white border for visibility
                canvas.StrokeColor = Colors.White;
                canvas.StrokeSize = 2;
                canvas.DrawCircle(currentPos.x, currentPos.y, 8);

            }
            catch
            {
                // Silently handle any calculation errors
            }
        }

        // Keep existing interface methods
        public SunEventPoint GetTouchedEvent(PointF touchPoint, float centerX, float centerY, float canvasWidth)
        {
            return null;
        }

        private class SunTimesData
        {
            public DateTime Dawn { get; set; }
            public DateTime Sunrise { get; set; }
            public DateTime SolarNoon { get; set; }
            public DateTime Sunset { get; set; }
            public DateTime Dusk { get; set; }
        }
    }

    // Keep existing classes that other code depends on
    public class SunEventPoint
    {
        public SunEventType EventType { get; set; }
        public double Azimuth { get; set; }
        public double Elevation { get; set; }
        public Color Color { get; set; }
        public float Size { get; set; }
        public string Time { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public DateTime DateTime { get; set; }
        public bool IsVisible { get; set; }
        public bool IsBelowHorizon { get; set; }
    }

    public enum SunEventType
    {
        Sunrise,
        Sunset,
        SolarNoon,
        CivilDawn,
        CivilDusk,
        GoldenHourStart,
        GoldenHourEnd,
        BlueHourStart,
        BlueHourEnd,
        Current
    }
}