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

        // Colors for the tint gradient (Green to Magenta)
        private static readonly SKColor GreenColor = new SKColor(0, 255, 0);     // Pure Green
        private static readonly SKColor NeutralColor = new SKColor(255, 255, 255); // White/neutral
        private static readonly SKColor MagentaColor = new SKColor(255, 0, 255);   // Pure Magenta

        public TintDial()
        {
            // Set default size for horizontal layout
            HeightRequest = 120;
            WidthRequest = 380;
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear();

            var info = e.Info;
            float width = info.Width;
            float height = info.Height;

            // Horizontal layout calculations
            float margin = 20;
            float sliderHeight = 40; // Double the height to match ColorTemperatureDial
            float titleHeight = 35; // More space for title
            float sliderY = titleHeight + 15; // Move slider down more to accommodate title
            float sliderStartX = margin + 60; // More space for min value label
            float sliderEndX = width - margin - 60; // More space for max value label
            float sliderWidth = sliderEndX - sliderStartX;

            using (var paint = new SKPaint())
            {
                // Draw title
                using (var titlePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = height / 8,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.DrawText(Title, width / 2, 30, titlePaint);
                }

                // Create horizontal gradient for slider track (Green to Magenta)
                using (var shader = SKShader.CreateLinearGradient(
                    new SKPoint(sliderStartX, sliderY),
                    new SKPoint(sliderEndX, sliderY),
                    new SKColor[] { GreenColor, NeutralColor, MagentaColor },
                    new float[] { 0, 0.5f, 1.0f },
                    SKShaderTileMode.Clamp))
                {
                    paint.Style = SKPaintStyle.Fill;
                    paint.IsAntialias = true;
                    paint.Shader = shader;

                    // Draw slider track with rounded ends
                    var sliderRect = new SKRect(sliderStartX, sliderY, sliderEndX, sliderY + sliderHeight);
                    canvas.DrawRoundRect(sliderRect, sliderHeight / 2, sliderHeight / 2, paint);
                }

                // Draw min/max value labels
                using (var labelPaint = new SKPaint
                {
                    Color = SKColors.DarkGray,
                    TextSize = height / 10,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center
                })
                {
                    canvas.DrawText("Green", margin + 30, sliderY + sliderHeight + 20, labelPaint);
                    canvas.DrawText("Magenta", width - margin - 30, sliderY + sliderHeight + 20, labelPaint);
                }

                // Calculate thumb position based on value
                float normalizedValue = (float)((Value - MinValue) / (MaxValue - MinValue));
                float thumbX = sliderStartX + (normalizedValue * sliderWidth);
                float thumbY = sliderY + (sliderHeight / 2);

                // Draw thumb (slider handle)
                using (var thumbPaint = new SKPaint
                {
                    Color = SKColors.White,
                    Style = SKPaintStyle.Fill,
                    IsAntialias = true
                })
                {
                    // Draw white circle with border - bigger for double height bar
                    float thumbRadius = sliderHeight / 2 + 4;
                    canvas.DrawCircle(thumbX, thumbY, thumbRadius, thumbPaint);

                    thumbPaint.Color = SKColors.DarkGray;
                    thumbPaint.Style = SKPaintStyle.Stroke;
                    thumbPaint.StrokeWidth = 3;
                    canvas.DrawCircle(thumbX, thumbY, thumbRadius, thumbPaint);
                }

                // Draw current value text
                using (var valuePaint = new SKPaint
                {
                    Color = SKColors.Black,
                    TextSize = height / 9,
                    IsAntialias = true,
                    TextAlign = SKTextAlign.Center,
                    FakeBoldText = true
                })
                {
                    // Format value to show decimal places for tint
                    canvas.DrawText($"{Value:F1}", width / 2, height - 10, valuePaint);
                }

                // Draw tick marks
                using (var tickPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    StrokeWidth = 1,
                    IsAntialias = true
                })
                {
                    // Draw major ticks at -1, -0.5, 0, 0.5, 1
                    for (double tint = MinValue; tint <= MaxValue; tint += 0.5)
                    {
                        float tickNormalized = (float)((tint - MinValue) / (MaxValue - MinValue));
                        float tickX = sliderStartX + (tickNormalized * sliderWidth);

                        canvas.DrawLine(tickX, sliderY - 3, tickX, sliderY + sliderHeight + 3, tickPaint);
                    }
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