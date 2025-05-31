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
        private readonly Color _currentSunColor = Color.FromRgb(255, 165, 0); // Orange

        private List<SunEventPoint> _sunEvents = new();

        public SunPathDrawable(EnhancedSunCalculatorViewModel viewModel)
        {
            _viewModel = viewModel;
            CalculateSunEvents();
        }

        public void UpdateViewModel(EnhancedSunCalculatorViewModel viewModel)
        {
            _viewModel = viewModel;
            CalculateSunEvents();
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (_viewModel == null) return;

            var width = dirtyRect.Width;
            var height = dirtyRect.Height;
            var centerX = width / 2;
            var centerY = height * 0.7f; // Move up to make room for legend

            // Clear background to white
            canvas.FillColor = _backgroundColor;
            canvas.FillRectangle(dirtyRect);

            // Draw the cosine wave using y = cos((π/4)x) with clamping
            DrawCosineWave(canvas, centerX, centerY, width, height);

            // Draw legend at bottom
            DrawLegend(canvas, width, height);
        }

        private void DrawCosineWave(ICanvas canvas, float centerX, float centerY, float canvasWidth, float canvasHeight)
        {
            // Map canvas width to x range [-3, +3]
            var amplitude = canvasHeight * 0.3f; // Wave amplitude (30% of canvas height)

            var dayPath = new PathF(); // y >= 0 (blue)
            var nightPath = new PathF(); // y < 0 (black)

            bool dayPathStarted = false;
            bool nightPathStarted = false;

            // Generate wave points across full canvas width
            for (float canvasX = 0; canvasX <= canvasWidth; canvasX += 1f) // 1px increments for smooth curve
            {
                // Map canvas X (0 to canvasWidth) to mathematical X (-3 to +3)
                double mathX = ((canvasX / canvasWidth) * 6.0) - 3.0; // Maps 0→canvasWidth to -3→+3

                // Calculate y using the cosine equation with clamping
                double mathY = CosLimited(mathX);

                // Map mathematical Y to canvas Y (flip vertically and scale)
                float canvasY = centerY - (float)(mathY * amplitude); // Negative to flip upside down

                // Determine color based on y value
                if (mathY >= 0) // Day portion (above horizon)
                {
                    if (!dayPathStarted)
                    {
                        dayPath.MoveTo(canvasX, canvasY);
                        dayPathStarted = true;
                    }
                    else
                    {
                        dayPath.LineTo(canvasX, canvasY);
                    }

                    // If we were drawing night path, we need to restart it later
                    if (nightPathStarted)
                    {
                        nightPathStarted = false;
                    }
                }
                else // Night portion (below horizon)
                {
                    if (!nightPathStarted)
                    {
                        nightPath.MoveTo(canvasX, canvasY);
                        nightPathStarted = true;
                    }
                    else
                    {
                        nightPath.LineTo(canvasX, canvasY);
                    }

                    // If we were drawing day path, we need to restart it later
                    if (dayPathStarted)
                    {
                        dayPathStarted = false;
                    }
                }
            }

            // Draw horizon line at y = 0 (mathematical y = 0 maps to centerY on canvas)
            canvas.StrokeColor = Color.FromRgb(128, 128, 128); // Gray horizon line
            canvas.StrokeSize = 2;
            canvas.DrawLine(0, centerY, canvasWidth, centerY);

            // Draw night portion (y < 0) - light grey so blue hours are visible
            if (nightPath.Count > 0)
            {
                canvas.StrokeColor = Color.FromRgb(180, 180, 180); // Light grey instead of black
                canvas.StrokeSize = 4; // Half of previous 8px
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawPath(nightPath);
            }

            // Draw day portion (y >= 0) - blue with half stroke width
            if (dayPath.Count > 0)
            {
                canvas.StrokeColor = Color.FromRgb(100, 149, 237); // Cornflower blue
                canvas.StrokeSize = 4; // Half of previous 8px
                canvas.StrokeLineCap = LineCap.Round;
                canvas.DrawPath(dayPath);
            }

            // Draw current sun position on the arc
            DrawCurrentSunOnArc(canvas, centerX, centerY, canvasWidth, canvasHeight, amplitude);

            // Draw sun events on the arc
            DrawSunEvents(canvas, centerX, centerY, canvasWidth, canvasHeight, amplitude);
        }

        // Your provided clamping function
        private double CosLimited(double x)
        {
            if (x < -3 || x > 3)
                return x < -3 ? -3 : x > 3 ? 3 : x; // clamp to edge value
            return Math.Cos(Math.PI / 4 * x);
        }

        private void DrawCurrentSunOnArc(ICanvas canvas, float centerX, float centerY, float canvasWidth, float canvasHeight, float amplitude)
        {
            // Calculate current sun position using the SAME time-based method as sun events
            if (_viewModel == null)
                return;

            // Get actual sunrise/sunset times from ViewModel
            var sunriseTime = GetSunriseDateTime();
            var sunsetTime = GetSunsetDateTime();
            var solarNoonTime = GetSolarNoonDateTime();

            if (sunriseTime == null || sunsetTime == null || solarNoonTime == null)
                return;

            // Use current time in location timezone for positioning
            var currentTime = DateTime.Now;
            // Convert current time to location timezone if needed
            var currentLocationTime = TimeZoneInfo.ConvertTime(currentTime, _viewModel.LocationTimeZone);

            // Calculate position using SAME method as sun events
            var position = CalculateSunEventPosition(currentLocationTime, centerX, centerY, canvasWidth, amplitude);

            // Only draw if sun is above horizon (current elevation > 0) and position is valid
            if (_viewModel.CurrentElevation > 0 && position.HasValue)
            {
                // Ensure the position is above horizon (positive Y calculation should put it above centerY)
                if (position.Value.Y <= centerY) // Above horizon line
                {
                    // Draw current sun position as orange circle (half size)
                    canvas.FillColor = _currentSunColor; // Orange
                    canvas.FillCircle(position.Value.X, position.Value.Y, 5); // Half of previous 10px

                    // Add white border for visibility
                    canvas.StrokeColor = Colors.White;
                    canvas.StrokeSize = 1;
                    canvas.DrawCircle(position.Value.X, position.Value.Y, 5);
                }
            }
        }

        private void DrawSunEvents(ICanvas canvas, float centerX, float centerY, float canvasWidth, float canvasHeight, float amplitude)
        {
            if (_viewModel == null) return;

            // Get actual sunrise/sunset times from ViewModel
            var sunriseTime = GetSunriseDateTime();
            var sunsetTime = GetSunsetDateTime();

            if (sunriseTime == null || sunsetTime == null)
                return;

            // Calculate all sun events - only 6 unique events
            _sunEvents.Clear();

            // Blue Hour Morning Start (60 min before sunrise, on night arc)
            var blueHourMorningStart = sunriseTime.Value.AddMinutes(-60);

            // Golden Hour Morning End (60 min after sunrise, on day arc)
            var goldenHourMorningEnd = sunriseTime.Value.AddMinutes(60);

            // Golden Hour Evening Start (60 min before sunset, on day arc)
            var goldenHourEveningStart = sunsetTime.Value.AddMinutes(-60);

            // Blue Hour Evening End (60 min after sunset, on night arc)
            var blueHourEveningEnd = sunsetTime.Value.AddMinutes(60);

            // Add events to collection for drawing and touch detection
            AddSunEvent(SunEventType.BlueHourStart, Color.FromRgb(25, 25, 112), blueHourMorningStart, "Blue Hour Start", false);
            AddSunEvent(SunEventType.Sunrise, Colors.Black, sunriseTime.Value, "Sunrise", false);
            AddSunEvent(SunEventType.GoldenHourEnd, Color.FromRgb(255, 215, 0), goldenHourMorningEnd, "Golden Hour End", false);
            AddSunEvent(SunEventType.GoldenHourStart, Color.FromRgb(255, 215, 0), goldenHourEveningStart, "Golden Hour Start", false);
            AddSunEvent(SunEventType.Sunset, Colors.Black, sunsetTime.Value, "Sunset", false);
            AddSunEvent(SunEventType.BlueHourEnd, Color.FromRgb(25, 25, 112), blueHourEveningEnd, "Blue Hour End", false);

            // Draw each event on the arc
            foreach (var sunEvent in _sunEvents)
            {
                var position = CalculateSunEventPosition(sunEvent.DateTime, centerX, centerY, canvasWidth, amplitude);

                if (position.HasValue)
                {
                    if (sunEvent.EventType == SunEventType.Sunrise || sunEvent.EventType == SunEventType.Sunset)
                    {
                        // Draw hash mark instead of circle
                        canvas.StrokeColor = Colors.Black;
                        canvas.StrokeSize = 2; // Slightly thinner
                        canvas.DrawLine(position.Value.X, position.Value.Y - 6, position.Value.X, position.Value.Y + 6); // Shorter hash
                    }
                    else
                    {
                        // Filled colored circle (half size)
                        canvas.FillColor = sunEvent.Color;
                        canvas.FillCircle(position.Value.X, position.Value.Y, 5); // Half of previous 10px

                        // White border for visibility
                        canvas.StrokeColor = Colors.White;
                        canvas.StrokeSize = 1;
                        canvas.DrawCircle(position.Value.X, position.Value.Y, 5);
                    }
                }
            }

            // Draw time labels for each event
            DrawEventLabels(canvas, centerX, centerY, canvasWidth, amplitude);
        }

        private void AddSunEvent(SunEventType eventType, Color color, DateTime time, string displayName, bool isBelowHorizon)
        {
            _sunEvents.Add(new SunEventPoint
            {
                EventType = eventType,
                Color = color,
                Time = time.ToString(_viewModel?.TimeFormat ?? "HH:mm"),
                DisplayName = displayName,
                DateTime = time,
                Size = 5,
                IsVisible = true,
                IsBelowHorizon = isBelowHorizon
            });
        }

        private PointF? CalculateSunEventPosition(DateTime eventTime, float centerX, float centerY, float canvasWidth, float amplitude)
        {
            var sunriseTime = GetSunriseDateTime();
            var sunsetTime = GetSunsetDateTime();
            var solarNoonTime = GetSolarNoonDateTime();

            if (sunriseTime == null || sunsetTime == null || solarNoonTime == null)
                return null;

            // Calculate hours from solar noon (this gives us the time position on our curve)
            double hoursFromNoon = (eventTime - solarNoonTime.Value).TotalHours;

            // Map time to mathematical X coordinate
            // We need to map the day length to the curve range where cosine is meaningful
            double dayLength = (sunsetTime.Value - sunriseTime.Value).TotalHours;

            // Scale the time to fit our cosine curve range
            // Solar noon (0 hours from noon) should map to X = 0 (peak of cosine)
            // Sunrise/sunset should map to where cosine crosses zero
            double mathX = (hoursFromNoon / (dayLength / 2.0)) * (Math.PI / 2.0); // Map to ±π/2 range

            // Convert to our CosLimited input range (-3 to +3)
            mathX = mathX * (4.0 / Math.PI); // Scale π/2 range to match our function's meaningful range

            // Calculate Y position using the cosine equation - this puts events exactly on the curve
            double mathY = CosLimited(mathX);

            // For events outside daylight hours (Blue Hours), mirror to negative Y
            if (eventTime < sunriseTime.Value || eventTime > sunsetTime.Value)
            {
                mathY = -Math.Abs(mathY); // Force to negative Y (night portion)
            }

            // Map mathematical coordinates to canvas coordinates
            float canvasX = (float)((mathX + 3.0) / 6.0 * canvasWidth);
            float canvasY = centerY - (float)(mathY * amplitude);

            return new PointF(canvasX, canvasY);
        }

        private void DrawEventLabels(ICanvas canvas, float centerX, float centerY, float canvasWidth, float amplitude)
        {
            canvas.FontSize = 10;
            canvas.FontColor = Colors.Black;

            var solarNoonTime = GetSolarNoonDateTime();
            if (solarNoonTime == null) return;

            foreach (var sunEvent in _sunEvents)
            {
                var position = CalculateSunEventPosition(sunEvent.DateTime, centerX, centerY, canvasWidth, amplitude);

                if (position.HasValue)
                {
                    // Determine if morning or evening event
                    bool isMorningEvent = sunEvent.DateTime <= solarNoonTime.Value;

                    // Position label with offsets
                    float labelX, labelY;

                    if (isMorningEvent)
                    {
                        // Morning events: left 5px, up 5px
                        labelX = position.Value.X - 5;
                        labelY = position.Value.Y - 20;
                    }
                    else
                    {
                        // Evening events: right 2px (was -5, now -3), up 2px more (was -20, now -22)
                        labelX = position.Value.X - 3;
                        labelY = position.Value.Y - 22;
                    }

                    // Save canvas state for rotation
                    canvas.SaveState();

                    // Translate to label position, then rotate based on morning/evening
                    canvas.Translate(labelX, labelY);

                    if (isMorningEvent)
                    {
                        canvas.Rotate(45); // 45° clockwise for morning events
                    }
                    else
                    {
                        canvas.Rotate(-45); // 45° counter-clockwise for evening events
                    }

                    // Draw the time text (now rotated)
                    canvas.DrawString(sunEvent.Time, -20, -5, 40, 10, HorizontalAlignment.Center, VerticalAlignment.Center);

                    // Restore canvas state
                    canvas.RestoreState();
                }
            }
        }

        private void DrawLegend(ICanvas canvas, float width, float height)
        {
            var legendY = height * 0.85f;
            var legendItemWidth = width / 4f; // Changed to 4 items

            canvas.FontSize = 10;
            canvas.FontColor = Colors.Black;

            // Blue Hour
            canvas.FillColor = Color.FromRgb(25, 25, 112);
            canvas.FillCircle(legendItemWidth * 0.5f, legendY, 3); // Half size
            canvas.DrawString("Blue Hour", 0, legendY + 15, legendItemWidth, 20, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Golden Hour
            canvas.FillColor = Color.FromRgb(255, 215, 0);
            canvas.FillCircle(legendItemWidth * 1.5f, legendY, 3); // Half size
            canvas.DrawString("Golden Hour", legendItemWidth, legendY + 15, legendItemWidth, 20, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Sunrise/Sunset (hash marks)
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 2;
            canvas.DrawLine(legendItemWidth * 2.5f, legendY - 4, legendItemWidth * 2.5f, legendY + 4); // Vertical hash
            canvas.DrawString("Sunrise/Sunset", legendItemWidth * 2, legendY + 15, legendItemWidth, 20, HorizontalAlignment.Center, VerticalAlignment.Center);

            // Current Sun
            canvas.FillColor = _currentSunColor;
            canvas.FillCircle(legendItemWidth * 3.5f, legendY, 3); // Half size
            canvas.DrawString("Current Sun", legendItemWidth * 3, legendY + 15, legendItemWidth, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        private void CalculateSunEvents()
        {
            _sunEvents.Clear();
            // This method is called to initialize events but actual calculation happens in DrawSunEvents
        }

        // Helper methods to extract DateTime from ViewModel UTC properties
        private DateTime? GetSunriseDateTime()
        {
            // Use the raw UTC time and convert to location timezone
            if (_viewModel.SunriseUtc != default(DateTime))
            {
                var utcTime = DateTime.SpecifyKind(_viewModel.SunriseUtc, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _viewModel.LocationTimeZone);
            }
            return _viewModel.SelectedDate.AddHours(6); // Fallback: 6 AM
        }

        private DateTime? GetSunsetDateTime()
        {
            // Use the raw UTC time and convert to location timezone
            if (_viewModel.SunsetUtc != default(DateTime))
            {
                var utcTime = DateTime.SpecifyKind(_viewModel.SunsetUtc, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _viewModel.LocationTimeZone);
            }
            return _viewModel.SelectedDate.AddHours(18); // Fallback: 6 PM
        }

        private DateTime? GetSolarNoonDateTime()
        {
            // Use the raw UTC time and convert to location timezone
            if (_viewModel.SolarNoonUtc != default(DateTime))
            {
                var utcTime = DateTime.SpecifyKind(_viewModel.SolarNoonUtc, DateTimeKind.Utc);
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, _viewModel.LocationTimeZone);
            }
            return _viewModel.SelectedDate.AddHours(12); // Fallback: 12 PM
        }

        public SunEventPoint GetTouchedEvent(PointF touchPoint, float centerX, float centerY, float canvasWidth)
        {
            // Gestures removed as requested
            return null;
        }
    }

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