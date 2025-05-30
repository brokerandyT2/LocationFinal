// Location.Photography.Maui/Controls/SunPathVisualization.xaml.cs
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;
using Location.Photography.ViewModels;

namespace Location.Photography.Maui.Controls
{
    public partial class SunPathVisualization : ContentView
    {
        public static readonly BindableProperty SunPathPointsProperty =
            BindableProperty.Create(nameof(SunPathPoints), typeof(ObservableCollection<SunPathPoint>), typeof(SunPathVisualization),
                new ObservableCollection<SunPathPoint>(), propertyChanged: OnSunPathPointsChanged);

        public static readonly BindableProperty CurrentAzimuthProperty =
            BindableProperty.Create(nameof(CurrentAzimuth), typeof(double), typeof(SunPathVisualization), 0.0,
                propertyChanged: OnCurrentPositionChanged);

        public static readonly BindableProperty CurrentElevationProperty =
            BindableProperty.Create(nameof(CurrentElevation), typeof(double), typeof(SunPathVisualization), 0.0,
                propertyChanged: OnCurrentPositionChanged);

        public static readonly BindableProperty SelectedTimeMinutesProperty =
            BindableProperty.Create(nameof(SelectedTimeMinutes), typeof(double), typeof(SunPathVisualization), 720.0,
                propertyChanged: OnSelectedTimeChanged);

        public static readonly BindableProperty ShowLegendProperty =
            BindableProperty.Create(nameof(ShowLegend), typeof(bool), typeof(SunPathVisualization), true);

        public ObservableCollection<SunPathPoint> SunPathPoints
        {
            get => (ObservableCollection<SunPathPoint>)GetValue(SunPathPointsProperty);
            set => SetValue(SunPathPointsProperty, value);
        }

        public double CurrentAzimuth
        {
            get => (double)GetValue(CurrentAzimuthProperty);
            set => SetValue(CurrentAzimuthProperty, value);
        }

        public double CurrentElevation
        {
            get => (double)GetValue(CurrentElevationProperty);
            set => SetValue(CurrentElevationProperty, value);
        }

        public double SelectedTimeMinutes
        {
            get => (double)GetValue(SelectedTimeMinutesProperty);
            set => SetValue(SelectedTimeMinutesProperty, value);
        }

        public bool ShowLegend
        {
            get => (bool)GetValue(ShowLegendProperty);
            set => SetValue(ShowLegendProperty, value);
        }

        public string SelectedTimeDisplay => TimeSpan.FromMinutes(SelectedTimeMinutes).ToString(@"hh\:mm");

        private SunPathDrawable _sunPathDrawable;

        public SunPathVisualization()
        {
            InitializeComponent();
            _sunPathDrawable = new SunPathDrawable(this);
            SunPathCanvas.Drawable = _sunPathDrawable;
        }

        private static void OnSunPathPointsChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SunPathVisualization control)
            {
                control.SunPathCanvas.Invalidate();
            }
        }

        private static void OnCurrentPositionChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SunPathVisualization control)
            {
                control.SunPathCanvas.Invalidate();
            }
        }

        private static void OnSelectedTimeChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is SunPathVisualization control)
            {
                control.OnPropertyChanged(nameof(SelectedTimeDisplay));
                control.SunPathCanvas.Invalidate();
            }
        }

        private void OnTimeSliderChanged(object sender, ValueChangedEventArgs e)
        {
            SelectedTimeMinutes = e.NewValue;
        }

        private void OnSunPathTapped(object sender, TappedEventArgs e)
        {
            if (sender is GraphicsView graphicsView)
            {
                var tapPoint = e.GetPosition(graphicsView);
                if (tapPoint.HasValue)
                {
                    var selectedPoint = FindNearestSunPathPoint(tapPoint.Value, graphicsView);
                    if (selectedPoint != null)
                    {
                        ShowPointInfo(selectedPoint, tapPoint.Value);
                    }
                }
            }
        }

        private SunPathPoint FindNearestSunPathPoint(Point tapPoint, GraphicsView canvas)
        {
            if (SunPathPoints?.Any() != true) return null;

            var canvasSize = Math.Min(canvas.Width, canvas.Height);
            var center = new PointF((float)(canvas.Width / 2), (float)(canvas.Height / 2));
            var radius = (float)(canvasSize * 0.4);

            SunPathPoint nearestPoint = null;
            double minDistance = double.MaxValue;

            foreach (var point in SunPathPoints.Where(p => p.IsVisible))
            {
                var screenPoint = ConvertSunPositionToScreen(point.Azimuth, point.Elevation, center, radius);
                var distance = Math.Sqrt(Math.Pow(tapPoint.X - screenPoint.X, 2) + Math.Pow(tapPoint.Y - screenPoint.Y, 2));

                if (distance < minDistance && distance < 20) // 20 pixel tolerance
                {
                    minDistance = distance;
                    nearestPoint = point;
                }
            }

            return nearestPoint;
        }

        private PointF ConvertSunPositionToScreen(double azimuth, double elevation, PointF center, float radius)
        {
            var azimuthRad = (azimuth - 90) * Math.PI / 180; // Adjust so 0° is North
            var elevationFactor = Math.Max(0, elevation) / 90.0; // 0 at horizon, 1 at zenith
            var distance = radius * (1 - elevationFactor);

            var x = center.X + (float)(distance * Math.Cos(azimuthRad));
            var y = center.Y + (float)(distance * Math.Sin(azimuthRad));

            return new PointF(x, y);
        }

        private void ShowPointInfo(SunPathPoint point, Point tapPosition)
        {
            PopupTimeLabel.Text = point.GetFormattedTime("HH:mm");
            PopupAzimuthLabel.Text = $"Azimuth: {point.Azimuth:F1}°";
            PopupElevationLabel.Text = $"Elevation: {point.Elevation:F1}°";

            var lightQuality = DetermineLightQuality(point);
            PopupLightQualityLabel.Text = $"Light: {lightQuality}";

            PointInfoPopup.IsVisible = true;

            // Auto-hide after 5 seconds
            Device.StartTimer(TimeSpan.FromSeconds(5), () =>
            {
                PointInfoPopup.IsVisible = false;
                return false;
            });
        }

        private string DetermineLightQuality(SunPathPoint point)
        {
            if (!point.IsVisible) return "Dark";

            return point.Elevation switch
            {
                < 0 => "Dark",
                < 6 => "Blue Hour",
                < 12 => "Golden Hour",
                < 25 => "Soft Light",
                < 45 => "Good Light",
                _ => "Harsh Light"
            };
        }

        private void OnClosePopup(object sender, EventArgs e)
        {
            PointInfoPopup.IsVisible = false;
        }
    }

    public class SunPathDrawable : IDrawable
    {
        private readonly SunPathVisualization _control;

        public SunPathDrawable(SunPathVisualization control)
        {
            _control = control;
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
            var center = new PointF(dirtyRect.Width / 2, dirtyRect.Height / 2);
            var radius = size * 0.4f;

            // Clear background
            canvas.FillColor = Colors.White;
            canvas.FillRectangle(dirtyRect);

            // Draw compass circles (elevation rings)
            DrawCompassCircles(canvas, center, radius);

            // Draw cardinal direction lines
            DrawCardinalLines(canvas, center, radius);

            // Draw sun path
            DrawSunPath(canvas, center, radius);

            // Draw hourly points
            DrawHourlyPoints(canvas, center, radius);

            // Draw current sun position
            DrawCurrentPosition(canvas, center, radius);

            // Draw selected time position
            DrawSelectedTimePosition(canvas, center, radius);
        }

        private void DrawCompassCircles(ICanvas canvas, PointF center, float radius)
        {
            canvas.StrokeColor = Colors.LightGray;
            canvas.StrokeSize = 1;

            // Draw elevation circles (30°, 60°, 90°)
            for (int elevation = 30; elevation <= 90; elevation += 30)
            {
                var elevationRadius = radius * (1 - elevation / 90f);
                canvas.DrawCircle(center, elevationRadius);
            }

            // Draw horizon circle
            canvas.StrokeColor = Colors.Gray;
            canvas.StrokeSize = 2;
            canvas.DrawCircle(center, radius);
        }

        private void DrawCardinalLines(ICanvas canvas, PointF center, float radius)
        {
            canvas.StrokeColor = Colors.LightGray;
            canvas.StrokeSize = 1;

            // Draw N-S and E-W lines
            canvas.DrawLine(center.X, center.Y - radius, center.X, center.Y + radius); // N-S
            canvas.DrawLine(center.X - radius, center.Y, center.X + radius, center.Y); // E-W
        }

        private void DrawSunPath(ICanvas canvas, PointF center, float radius)
        {
            if (_control.SunPathPoints?.Any() != true) return;

            var visiblePoints = _control.SunPathPoints.Where(p => p.IsVisible).OrderBy(p => p.Time).ToList();
            if (visiblePoints.Count < 2) return;

            canvas.StrokeColor = Colors.Orange;
            canvas.StrokeSize = 3;

            // Draw path as connected line segments
            for (int i = 0; i < visiblePoints.Count - 1; i++)
            {
                var point1 = ConvertSunPositionToScreen(visiblePoints[i].Azimuth, visiblePoints[i].Elevation, center, radius);
                var point2 = ConvertSunPositionToScreen(visiblePoints[i + 1].Azimuth, visiblePoints[i + 1].Elevation, center, radius);
                canvas.DrawLine(point1, point2);
            }

            // Highlight golden hour sections
            DrawGoldenHourSections(canvas, center, radius, visiblePoints);
        }

        private void DrawGoldenHourSections(ICanvas canvas, PointF center, float radius, List<SunPathPoint> points)
        {
            canvas.StrokeColor = Colors.Gold;
            canvas.StrokeSize = 5;

            var goldenHourPoints = points.Where(p => p.Elevation > 0 && p.Elevation < 12).ToList();

            if (goldenHourPoints.Count >= 2)
            {
                // Draw golden hour segments as thick lines
                for (int i = 0; i < goldenHourPoints.Count - 1; i++)
                {
                    var point1 = ConvertSunPositionToScreen(goldenHourPoints[i].Azimuth, goldenHourPoints[i].Elevation, center, radius);
                    var point2 = ConvertSunPositionToScreen(goldenHourPoints[i + 1].Azimuth, goldenHourPoints[i + 1].Elevation, center, radius);
                    canvas.DrawLine(point1, point2);
                }
            }
        }

        private void DrawHourlyPoints(ICanvas canvas, PointF center, float radius)
        {
            if (_control.SunPathPoints?.Any() != true) return;

            canvas.FillColor = Colors.LightBlue;

            foreach (var point in _control.SunPathPoints.Where(p => p.IsVisible && p.Time.Minute == 0))
            {
                var screenPoint = ConvertSunPositionToScreen(point.Azimuth, point.Elevation, center, radius);
                canvas.FillCircle(screenPoint, 3);
            }
        }

        private void DrawCurrentPosition(ICanvas canvas, PointF center, float radius)
        {
            if (_control.CurrentElevation <= 0) return; // Don't draw if sun is below horizon

            var screenPoint = ConvertSunPositionToScreen(_control.CurrentAzimuth, _control.CurrentElevation, center, radius);

            // Draw current position with pulsing effect
            canvas.FillColor = Colors.Red;
            canvas.FillCircle(screenPoint, 8);

            canvas.StrokeColor = Colors.DarkRed;
            canvas.StrokeSize = 2;
            canvas.DrawCircle(screenPoint, 12);
        }

        private void DrawSelectedTimePosition(ICanvas canvas, PointF center, float radius)
        {
            if (_control.SunPathPoints?.Any() != true) return;

            var selectedTime = TimeSpan.FromMinutes(_control.SelectedTimeMinutes);
            var targetTime = DateTime.Today.Add(selectedTime);

            var nearestPoint = _control.SunPathPoints
                .Where(p => p.IsVisible)
                .OrderBy(p => Math.Abs((p.Time - targetTime).TotalMinutes))
                .FirstOrDefault();

            if (nearestPoint != null)
            {
                var screenPoint = ConvertSunPositionToScreen(nearestPoint.Azimuth, nearestPoint.Elevation, center, radius);

                canvas.StrokeColor = Colors.Blue;
                canvas.StrokeSize = 3;
                canvas.DrawCircle(screenPoint, 10);

                canvas.FillColor = Colors.LightBlue;
                canvas.FillCircle(screenPoint, 5);
            }
        }

        private PointF ConvertSunPositionToScreen(double azimuth, double elevation, PointF center, float radius)
        {
            var azimuthRad = (azimuth - 90) * Math.PI / 180; // Adjust so 0° is North
            var elevationFactor = Math.Max(0, elevation) / 90.0; // 0 at horizon, 1 at zenith
            var distance = radius * (1 - elevationFactor);

            var x = center.X + (float)(distance * Math.Cos(azimuthRad));
            var y = center.Y + (float)(distance * Math.Sin(azimuthRad));

            return new PointF(x, y);
        }
    }
}