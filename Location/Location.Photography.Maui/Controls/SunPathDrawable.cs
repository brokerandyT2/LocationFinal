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
            if (_viewModel?.SelectedLocation == null) return;

            // Use the selected date with current time of day
            var selectedDate = _viewModel.SelectedDate.Date;
            var currentTimeOfDay = DateTime.Now.TimeOfDay;
            var targetDateTime = selectedDate.Add(currentTimeOfDay);

            // Calculate sun times for the selected date (not auto-advanced times)
            var coordinate = new CoordinateSharp.Coordinate(_viewModel.SelectedLocation.Latitude, _viewModel.SelectedLocation.Longitude, selectedDate);

            var sunriseLocal = coordinate.CelestialInfo.SunRise.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SunRise.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(6);
            var sunsetLocal = coordinate.CelestialInfo.SunSet.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SunSet.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(18);
            var solarNoon = coordinate.CelestialInfo.SolarNoon.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SolarNoon.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(12);

            // Calculate current sun position for the target date/time
            var currentCoordinate = new CoordinateSharp.Coordinate(_viewModel.SelectedLocation.Latitude, _viewModel.SelectedLocation.Longitude, targetDateTime);
            var currentElevation = currentCoordinate.CelestialInfo.SunAltitude;

            // Only draw if sun is above horizon
            if (currentElevation <= 0) return;

            // Calculate position on arc
            var position = CalculateSunEventPosition(targetDateTime, centerX, centerY, canvasWidth, amplitude, sunriseLocal, sunsetLocal, solarNoon);

            if (position.HasValue && position.Value.Y <= centerY)
            {
                // Draw current sun position as orange circle
                canvas.FillColor = _currentSunColor;
                canvas.FillCircle(position.Value.X, position.Value.Y, 5);

                // Add white border for visibility
                canvas.StrokeColor = Colors.White;
                canvas.StrokeSize = 1;
                canvas.DrawCircle(position.Value.X, position.Value.Y, 5);
            }
        }

        private void DrawSunEvents(ICanvas canvas, float centerX, float centerY, float canvasWidth, float canvasHeight, float amplitude)
        {
            if (_viewModel == null || !_sunEvents.Any()) return;

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
            if (_viewModel?.SelectedLocation == null) return null;

            // Calculate sun times for the selected date
            var selectedDate = _viewModel.SelectedDate.Date;
            var coordinate = new CoordinateSharp.Coordinate(_viewModel.SelectedLocation.Latitude, _viewModel.SelectedLocation.Longitude, selectedDate);

            var sunriseTime = coordinate.CelestialInfo.SunRise.HasValue
                ? DateTime.SpecifyKind(coordinate.CelestialInfo.SunRise.Value, DateTimeKind.Local)
                : selectedDate.AddHours(6);
            var sunsetTime = coordinate.CelestialInfo.SunSet.HasValue
                ? DateTime.SpecifyKind(coordinate.CelestialInfo.SunSet.Value, DateTimeKind.Local)
                : selectedDate.AddHours(18);
            var solarNoonTime = coordinate.CelestialInfo.SolarNoon.HasValue
                ? DateTime.SpecifyKind(coordinate.CelestialInfo.SolarNoon.Value, DateTimeKind.Local)
                : selectedDate.AddHours(12);

            return CalculateSunEventPosition(eventTime, centerX, centerY, canvasWidth, amplitude, sunriseTime, sunsetTime, solarNoonTime);
        }

        private PointF? CalculateSunEventPosition(DateTime eventTime, float centerX, float centerY, float canvasWidth, float amplitude, DateTime sunriseTime, DateTime sunsetTime, DateTime solarNoonTime)
        {
            // Calculate hours from solar noon (this gives us the time position on our curve)
            double hoursFromNoon = (eventTime - solarNoonTime).TotalHours;

            // Map time to mathematical X coordinate
            // We need to map the day length to the curve range where cosine is meaningful
            double dayLength = (sunsetTime - sunriseTime).TotalHours;

            // Scale the time to fit our cosine curve range
            // Solar noon (0 hours from noon) should map to X = 0 (peak of cosine)
            // Sunrise/sunset should map to where cosine crosses zero
            double mathX = (hoursFromNoon / (dayLength / 2.0)) * (Math.PI / 2.0); // Map to ±π/2 range

            // Convert to our CosLimited input range (-3 to +3)
            mathX = mathX * (4.0 / Math.PI); // Scale π/2 range to match our function's meaningful range

            // Calculate Y position using the cosine equation - this puts events exactly on the curve
            double mathY = CosLimited(mathX);

            // For events outside daylight hours (Blue Hours), mirror to negative Y
            if (eventTime < sunriseTime || eventTime > sunsetTime)
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

            if (_viewModel?.SelectedLocation == null) return;

            // Calculate solar noon for the selected date
            var selectedDate = _viewModel.SelectedDate.Date;
            var coordinate = new CoordinateSharp.Coordinate(_viewModel.SelectedLocation.Latitude, _viewModel.SelectedLocation.Longitude, selectedDate);
            var sunriseLocal = coordinate.CelestialInfo.SunRise.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SunRise.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(6);
            var sunsetLocal = coordinate.CelestialInfo.SunSet.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SunSet.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(18);
            var solarNoonTime = coordinate.CelestialInfo.SolarNoon.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SolarNoon.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(12);

            foreach (var sunEvent in _sunEvents)
            {
                var position = CalculateSunEventPosition(sunEvent.DateTime, centerX, centerY, canvasWidth, amplitude);

                if (position.HasValue)
                {
                    // Determine if morning or evening event
                    bool isMorningEvent = sunEvent.DateTime <= solarNoonTime;

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

            if (_viewModel?.SelectedLocation == null) return;

            // Calculate sun times for the selected date
            var selectedDate = _viewModel.SelectedDate.Date;
            var coordinate = new CoordinateSharp.Coordinate(_viewModel.SelectedLocation.Latitude, _viewModel.SelectedLocation.Longitude, selectedDate);

            var sunriseLocal = coordinate.CelestialInfo.SunRise.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SunRise.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(6);
            var sunsetLocal = coordinate.CelestialInfo.SunSet.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SunSet.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(18);
            var solarNoon = coordinate.CelestialInfo.SolarNoon.HasValue
               ? TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(coordinate.CelestialInfo.SolarNoon.Value, DateTimeKind.Utc), TimeZoneInfo.Local)
               : selectedDate.AddHours(12);

            // Calculate sun events using calculated data
            var blueHourMorningStart = sunriseLocal.AddMinutes(-60);
            var goldenHourMorningEnd = sunriseLocal.AddMinutes(60);
            var goldenHourEveningStart = sunsetLocal.AddMinutes(-60);
            var blueHourEveningEnd = sunsetLocal.AddMinutes(60);

            // Add events using the calculated data
            AddSunEvent(SunEventType.BlueHourStart, Color.FromRgb(25, 25, 112), blueHourMorningStart, "Blue Hour Start", false);
            AddSunEvent(SunEventType.Sunrise, Colors.Black, sunriseLocal, "Sunrise", false);
            AddSunEvent(SunEventType.GoldenHourEnd, Color.FromRgb(255, 215, 0), goldenHourMorningEnd, "Golden Hour End", false);
            AddSunEvent(SunEventType.GoldenHourStart, Color.FromRgb(255, 215, 0), goldenHourEveningStart, "Golden Hour Start", false);
            AddSunEvent(SunEventType.Sunset, Colors.Black, sunsetLocal, "Sunset", false);
            AddSunEvent(SunEventType.BlueHourEnd, Color.FromRgb(25, 25, 112), blueHourEveningEnd, "Blue Hour End", false);
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