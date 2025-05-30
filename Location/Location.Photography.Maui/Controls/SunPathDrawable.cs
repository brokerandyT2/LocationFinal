// Location.Photography.Maui/Graphics/SunPathDrawable.cs
using Microsoft.Maui.Graphics;
using Location.Photography.ViewModels;
using Location.Photography.Domain.Models;

namespace Location.Photography.Maui.Controls
{
    public class SunPathDrawable : IDrawable
    {
        private EnhancedSunCalculatorViewModel _viewModel;
        private readonly Color _skyGradientTop = Color.FromRgb(135, 206, 235); // Light blue
        private readonly Color _skyGradientBottom = Color.FromRgb(176, 224, 230); // Powder blue
        private readonly Color _horizonColor = Color.FromRgb(70, 130, 180); // Steel blue
        private readonly Color _sunColor = Color.FromRgb(255, 215, 0); // Gold
        private readonly Color _currentSunColor = Color.FromRgb(255, 165, 0); // Orange
        private readonly Color _sunriseColor = Color.FromRgb(255, 140, 0); // Dark orange
        private readonly Color _civilColor = Color.FromRgb(173, 216, 230); // Light blue
        private readonly Color _goldenHourColor = Color.FromRgb(255, 215, 0); // Gold
        private readonly Color _noonColor = Color.FromRgb(255, 255, 0); // Yellow

        private List<SunEventPoint> _sunEvents = new();
        private SunEventPoint _touchedEvent = null;

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
            var groundY = height * 0.75f; // Horizon at 75% down
            var arcRadius = width * 0.4f;

            // Draw sky gradient background
            DrawSkyGradient(canvas, dirtyRect);

            // Draw horizon arc
            DrawHorizonArc(canvas, centerX, groundY, arcRadius);

            // Draw sun path arc
            DrawSunPathArc(canvas, centerX, groundY, arcRadius);

            // Draw sun events
            DrawSunEvents(canvas, centerX, groundY, arcRadius);

            // Draw current sun position
            DrawCurrentSunPosition(canvas, centerX, groundY, arcRadius);

            // Draw time labels
            DrawTimeLabels(canvas, centerX, groundY, arcRadius, width);

            // Draw compass directions
            DrawCompassDirections(canvas, centerX, groundY, width, height);
        }

        private void DrawSkyGradient(ICanvas canvas, RectF rect)
        {
            var paint = new LinearGradientPaint
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, rect.Height * 0.75f)
            };
            //paint.GradientStops
            paint.GradientStops.Append(new PaintGradientStop(0, _skyGradientTop));
            paint.GradientStops.Append(new PaintGradientStop(1, _skyGradientBottom));

            canvas.SetFillPaint(paint, rect);
            canvas.FillRectangle(rect);
        }

        private void DrawHorizonArc(ICanvas canvas, float centerX, float groundY, float radius)
        {
            // Draw ground (below horizon)
            canvas.FillColor = Color.FromRgb(139, 69, 19); // Brown ground
            canvas.FillRectangle(0, groundY, centerX * 2, groundY);

            // Draw horizon arc line
            canvas.StrokeColor = _horizonColor;
            canvas.StrokeSize = 3;
            canvas.DrawArc(centerX - radius, groundY - radius, radius * 2, radius * 2, 0, 180, false, false);
        }

        private void DrawSunPathArc(ICanvas canvas, float centerX, float groundY, float radius)
        {
            // Draw the sun's path arc (slightly above horizon arc)
            canvas.StrokeColor = Color.FromRgb(255, 215, 0); // Semi-transparent gold
            canvas.StrokeSize = 2;
            canvas.StrokeDashPattern = new float[] { 5, 3 };
            canvas.DrawArc(centerX - radius * 0.95f, groundY - radius * 0.95f,
                          radius * 2 * 0.95f, radius * 2 * 0.95f, 0, 180, false, false);
            canvas.StrokeDashPattern = null;
        }

        private void DrawSunEvents(ICanvas canvas, float centerX, float groundY, float radius)
        {
            foreach (var sunEvent in _sunEvents)
            {
                if (sunEvent.IsVisible)
                {
                    var position = CalculateArcPosition(centerX, groundY, radius * 0.95f, sunEvent.Azimuth, sunEvent.Elevation);

                    // Draw event dot
                    canvas.FillColor = sunEvent.Color;
                    canvas.FillCircle(position.X, position.Y, sunEvent.Size);

                    // Draw border for better visibility
                    canvas.StrokeColor = Colors.White;
                    canvas.StrokeSize = 1;
                    canvas.DrawCircle(position.X, position.Y, sunEvent.Size);
                }
            }
        }

        private void DrawCurrentSunPosition(ICanvas canvas, float centerX, float groundY, float radius)
        {
            if (_viewModel.IsSunUp && _viewModel.CurrentElevation > 0)
            {
                var position = CalculateArcPosition(centerX, groundY, radius * 0.95f,
                                                   _viewModel.CurrentAzimuth, _viewModel.CurrentElevation);

                // Draw sun with rays
                canvas.FillColor = _currentSunColor;
                canvas.FillCircle(position.X, position.Y, 8);

                // Draw sun rays
                canvas.StrokeColor = _currentSunColor;
                canvas.StrokeSize = 2;
                for (int i = 0; i < 8; i++)
                {
                    var rayAngle = (i * Math.PI * 2) / 8;
                    var rayStartX = position.X + (float)(12 * Math.Cos(rayAngle));
                    var rayStartY = position.Y + (float)(12 * Math.Sin(rayAngle));
                    var rayEndX = position.X + (float)(18 * Math.Cos(rayAngle));
                    var rayEndY = position.Y + (float)(18 * Math.Sin(rayAngle));
                    canvas.DrawLine(rayStartX, rayStartY, rayEndX, rayEndY);
                }
            }
        }

        private void DrawTimeLabels(ICanvas canvas, float centerX, float groundY, float radius, float width)
        {
            canvas.FontColor = Color.FromRgb(70, 70, 70);
            canvas.FontSize = 14;

            // Sunrise label (left side)
            var sunriseEvent = _sunEvents.FirstOrDefault(e => e.EventType == SunEventType.Sunrise);
            if (sunriseEvent != null)
            {
                canvas.DrawString("Sunrise", 20, groundY + 30, 80, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
                canvas.FontSize = 16;
                canvas.FontColor = _sunriseColor;
                canvas.DrawString(_viewModel.SunriseLocationTime, 20, groundY + 50, 80, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
            }

            // Sunset label (right side)
            var sunsetEvent = _sunEvents.FirstOrDefault(e => e.EventType == SunEventType.Sunset);
            if (sunsetEvent != null)
            {
                canvas.FontColor = Color.FromRgb(70, 70, 70);
                canvas.FontSize = 14;
                canvas.DrawString("Sunset", width - 100, groundY + 30, 80, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
                canvas.FontSize = 16;
                canvas.FontColor = _sunriseColor;
                canvas.DrawString(_viewModel.SunsetLocationTime, width - 100, groundY + 50, 80, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
            }
        }

        private void DrawCompassDirections(ICanvas canvas, float centerX, float groundY, float width, float height)
        {
            canvas.FontColor = Color.FromRgb(100, 100, 100);
            canvas.FontSize = 12;

            // Cardinal directions
            canvas.DrawString("E", 15, groundY - 10, 20, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString("W", width - 35, groundY - 10, 20, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
            canvas.DrawString("S", centerX - 10, groundY + 15, 20, 20, HorizontalAlignment.Center, VerticalAlignment.Center);
        }

        private PointF CalculateArcPosition(float centerX, float groundY, float radius, double azimuth, double elevation)
        {
            // Convert azimuth (0-360°) to arc position
            // Azimuth 90° = East (right), 270° = West (left), 180° = South (bottom)
            var normalizedAzimuth = (azimuth - 90) * Math.PI / 180; // Convert to radians, offset for East=0

            // Use elevation to determine height on arc (0° = horizon, 90° = zenith)
            var elevationFactor = Math.Max(0, Math.Min(1, elevation / 90.0));
            var adjustedRadius = radius * Math.Sin(elevationFactor * Math.PI / 2);

            var x = centerX + (float)(adjustedRadius * Math.Cos(normalizedAzimuth));
            var y = groundY - (float)(radius * elevationFactor * 0.8); // 0.8 factor to keep within arc bounds

            return new PointF(x, y);
        }

        private void CalculateSunEvents()
        {
            _sunEvents.Clear();

            if (_viewModel?.SunPathPoints == null || !_viewModel.SunPathPoints.Any())
                return;

            // Add key sun events with their times from ViewModel
            AddSunEvent(SunEventType.Sunrise, _sunriseColor, 6, GetSunriseData());
            AddSunEvent(SunEventType.Sunset, _sunriseColor, 6, GetSunsetData());
            AddSunEvent(SunEventType.SolarNoon, _noonColor, 6, GetSolarNoonData());

            // Add civil events (you'll need to add these to your ViewModel)
            AddCivilEvents();

            // Add golden hour events
            AddGoldenHourEvents();
        }

        private void AddSunEvent(SunEventType eventType, Color color, float size, (double azimuth, double elevation, string time)? data)
        {
            if (data.HasValue)
            {
                _sunEvents.Add(new SunEventPoint
                {
                    EventType = eventType,
                    Azimuth = data.Value.azimuth,
                    Elevation = data.Value.elevation,
                    Color = color,
                    Size = size,
                    Time = data.Value.time,
                    IsVisible = data.Value.elevation > 0
                });
            }
        }

        private (double azimuth, double elevation, string time)? GetSunriseData()
        {
            var sunrisePoint = _viewModel.SunPathPoints?.FirstOrDefault(p => p.Elevation > 0);
            if (sunrisePoint != null)
            {
                return (sunrisePoint.Azimuth, sunrisePoint.Elevation, _viewModel.SunriseLocationTime);
            }
            return null;
        }

        private (double azimuth, double elevation, string time)? GetSunsetData()
        {
            var sunsetPoint = _viewModel.SunPathPoints?.LastOrDefault(p => p.Elevation > 0);
            if (sunsetPoint != null)
            {
                return (sunsetPoint.Azimuth, sunsetPoint.Elevation, _viewModel.SunsetLocationTime);
            }
            return null;
        }

        private (double azimuth, double elevation, string time)? GetSolarNoonData()
        {
            var noonPoint = _viewModel.SunPathPoints?.OrderByDescending(p => p.Elevation).FirstOrDefault();
            if (noonPoint != null)
            {
                return (noonPoint.Azimuth, noonPoint.Elevation, _viewModel.SolarNoonLocationTime);
            }
            return null;
        }

        private void AddCivilEvents()
        {
            // Civil Dawn - approximately 30 minutes before sunrise
            var civilDawn = _viewModel.SunPathPoints?.FirstOrDefault(p => p.Elevation > -6 && p.Elevation < -3);
            if (civilDawn != null)
            {
                AddSunEvent(SunEventType.CivilDawn, _civilColor, 4, (civilDawn.Azimuth, civilDawn.Elevation, "Civil Dawn"));
            }

            // Civil Dusk - approximately 30 minutes after sunset  
            var civilDusk = _viewModel.SunPathPoints?.LastOrDefault(p => p.Elevation > -6 && p.Elevation < -3);
            if (civilDusk != null)
            {
                AddSunEvent(SunEventType.CivilDusk, _civilColor, 4, (civilDusk.Azimuth, civilDusk.Elevation, "Civil Dusk"));
            }
        }

        private void AddGoldenHourEvents()
        {
            // Golden Hour Start (sunrise + 1 hour)
            var goldenStart = _viewModel.SunPathPoints?.FirstOrDefault(p => p.Elevation > 5 && p.Elevation < 15);
            if (goldenStart != null)
            {
                AddSunEvent(SunEventType.GoldenHourStart, _goldenHourColor, 4, (goldenStart.Azimuth, goldenStart.Elevation, "Golden Hour"));
            }

            // Golden Hour End (sunset - 1 hour)
            var goldenEnd = _viewModel.SunPathPoints?.LastOrDefault(p => p.Elevation > 5 && p.Elevation < 15);
            if (goldenEnd != null)
            {
                AddSunEvent(SunEventType.GoldenHourEnd, _goldenHourColor, 4, (goldenEnd.Azimuth, goldenEnd.Elevation, "Golden Hour"));
            }
        }

        public SunEventPoint GetTouchedEvent(PointF touchPoint, float centerX, float groundY, float radius)
        {
            foreach (var sunEvent in _sunEvents)
            {
                if (sunEvent.IsVisible)
                {
                    var eventPosition = CalculateArcPosition(centerX, groundY, radius * 0.95f, sunEvent.Azimuth, sunEvent.Elevation);
                    var distance = Math.Sqrt(Math.Pow(touchPoint.X - eventPosition.X, 2) + Math.Pow(touchPoint.Y - eventPosition.Y, 2));

                    if (distance <= sunEvent.Size + 10) // 10px touch tolerance
                    {
                        return sunEvent;
                    }
                }
            }
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
        public bool IsVisible { get; set; }
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
        Current
    }
}