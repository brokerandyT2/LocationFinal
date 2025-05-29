// Location.Photography.Maui/Controls/TintDial.cs
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace Location.Photography.Maui.Controls
{
    public class TintDial : SKCanvasView
    {
        public static readonly BindableProperty ValueProperty =
            BindableProperty.Create(nameof(Value), typeof(double), typeof(TintDial), 0.0,
                propertyChanged: OnValuePropertyChanged);

        public static readonly BindableProperty MinValueProperty =
            BindableProperty.Create(nameof(MinValue), typeof(double), typeof(TintDial), -1.0,
                propertyChanged: OnPropertyInvalidate);

        public static readonly BindableProperty MaxValueProperty =
            BindableProperty.Create(nameof(MaxValue), typeof(double), typeof(TintDial), 1.0,
                propertyChanged: OnPropertyInvalidate);

        public static readonly BindableProperty TitleProperty =
            BindableProperty.Create(nameof(Title), typeof(string), typeof(TintDial), "White Balance Tint",
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

        // Colors for the tint gradient
        private static readonly SKColor GreenTint = new SKColor(76, 175, 80);    // Green
        private static readonly SKColor NeutralTint = new SKColor(220, 220, 220); // Neutral
        private static readonly SKColor MagentaTint = new SKColor(216, 27, 96);   // Magenta

        public TintDial()
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
                    new SKColor[] { MagentaTint, NeutralTint, GreenTint, NeutralTint, MagentaTint },
                    new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f }))
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
                    canvas.DrawText(Title, center, size - 10, textPaint);
                }

                // Draw markers for Magenta, Neutral, Green
                using (var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = size / 16,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    // Calculate marker positions
                    float leftAngle = 150 * (float)(Math.PI / 180); // Magenta
                    float midAngle = 270 * (float)(Math.PI / 180);  // Neutral
                    float rightAngle = 30 * (float)(Math.PI / 180); // Green

                    float textRadius = radius + 10;

                    // Draw marker texts
                    float leftX = center + textRadius * (float)Math.Cos(leftAngle);
                    float leftY = center + textRadius * (float)Math.Sin(leftAngle);
                    canvas.DrawText("M", leftX, leftY, textPaint);

                    float midX = center + textRadius * (float)Math.Cos(midAngle);
                    float midY = center + textRadius * (float)Math.Sin(midAngle);
                    canvas.DrawText("N", midX, midY - 10, textPaint);

                    float rightX = center + textRadius * (float)Math.Cos(rightAngle);
                    float rightY = center + textRadius * (float)Math.Sin(rightAngle);
                    canvas.DrawText("G", rightX, rightY, textPaint);
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

                // Show current value
                string valueText;
                if (Value < 0)
                    valueText = $"M{Math.Abs(Value):F2}";
                else if (Value > 0)
                    valueText = $"G{Value:F2}";
                else
                    valueText = "Neutral";

                using (var textPaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = size / 14,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.DrawText(valueText, center, center + 40, textPaint);
                }
            }
        }

        private static void OnValuePropertyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            var control = bindable as TintDial;
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
            var control = bindable as TintDial;
            control?.InvalidateSurface();
        }
    }
}