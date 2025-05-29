// Location.Photography.Maui/Controls/ColorTemperatureDial.cs
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Location.Photography.Maui.Controls
{
    public class ColorTemperatureDial : SKCanvasView
    {
        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(double), typeof(ColorTemperatureDial), 5500.0,
                propertyChanged: OnValuePropertyChanged);

        public static readonly BindableProperty MinValueProperty =
            BindableProperty.Create(nameof(MinValue), typeof(double), typeof(ColorTemperatureDial), 2700.0,
                propertyChanged: OnPropertyInvalidate);

        public static readonly BindableProperty MaxValueProperty =
            BindableProperty.Create(nameof(MaxValue), typeof(double), typeof(ColorTemperatureDial), 9000.0,
                propertyChanged: OnPropertyInvalidate);

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(ColorTemperatureDial), "Color Temperature",
                propertyChanged: OnPropertyInvalidate);

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public double MinValue
        {
            get => (double)GetValue(MinValueProperty);
            set => SetValue(MinValueProperty, value);
        }

        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        // Colors for the temperature gradient
        private static readonly SKColor WarmColor = new SKColor(255, 147, 41);   // Orange/warm (2700K)
        private static readonly SKColor NeutralColor = new SKColor(255, 255, 255); // White/neutral (5500K)
        private static readonly SKColor CoolColor = new SKColor(167, 199, 255);   // Blue/cool (9000K)

        public ColorTemperatureDial()
        {
            // Set default size
            HeightRequest = 200;
            WidthRequest = 200;
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();

            var info = e.Info;
            float width = info.Width;
            float height = info.Height;
            float size = Math.Min(width, height);
            float center = size / 2;
            float strokeWidth = size / 20;

            // Calculate position
            float radius = (size / 2) - (strokeWidth / 2);
            float dialRadius = radius - (strokeWidth / 2) - 5;
            float needleLength = dialRadius - 10;

            using (var paint = new SKPaint())
            {
                // Create the gradient background
                using (var shader = SKShader.CreateSweepGradient(
                    new SKPoint(center, center),
                    new SKColor[] { WarmColor, NeutralColor, CoolColor, WarmColor },
                    new float[] { 0, 0.33f, 0.67f, 1.0f }))
                {
                    paint.Style = SKPaintStyle.Stroke;
                    paint.StrokeWidth = strokeWidth;
                    paint.IsAntialias = true;
                    paint.Shader = shader;

                    // Draw arc from 150 to 390 degrees (240-degree span)
                    canvas.DrawArc(
                        new SKRect(strokeWidth / 2, strokeWidth / 2, size - strokeWidth / 2, size - strokeWidth / 2),
                        150, 240, false, paint);
                }

                // Draw title
                using (var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = size / 12,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.DrawText(Title, center, size- 10, textPaint);
                }

                // Draw value text
                using (var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = size / 14,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.DrawText($"{Value:F0}K", center, center +40, textPaint);
                }

                // Calculate needle angle based on value
                float angleRange = 240;
                float normalizedValue = (float)((Value - MinValue) / (MaxValue - MinValue));
                float needleAngle = 150 + (normalizedValue * angleRange);
                float rads = (float)(needleAngle * Math.PI / 180);

                // Draw needle
                using (var needlePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    StrokeWidth = 3,
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke
                })
                {
                    float needleX = center + needleLength * (float)Math.Cos(rads);
                    float needleY = center + needleLength * (float)Math.Sin(rads);
                    canvas.DrawLine(center, center, needleX, needleY, needlePaint);

                    // Draw needle circle
                    needlePaint.Style = SKPaintStyle.Fill;
                    canvas.DrawCircle(center, center, 5, needlePaint);
                }
            }
        }

        private static void OnValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as ColorTemperatureDial;
            if (control == null) return;

            // Clamp value within range
            double value = (double)newValue;
            if (value < control.MinValue)
                control.Value = control.MinValue;
            else if (value > control.MaxValue)
                control.Value = control.MaxValue;
            else
                control.InvalidateSurface();
        }

        private static void OnPropertyInvalidate(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as ColorTemperatureDial;
            control?.InvalidateSurface();
        }
    }
}