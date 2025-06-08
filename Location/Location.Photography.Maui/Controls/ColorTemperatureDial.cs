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
            float sliderHeight = 40; // Double the height
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

                // Create horizontal gradient for slider track
                using (var shader = SKShader.CreateLinearGradient(
                    new SKPoint(sliderStartX, sliderY),
                    new SKPoint(sliderEndX, sliderY),
                    new SKColor[] { WarmColor, NeutralColor, CoolColor },
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
                    canvas.DrawText($"{MinValue:F0}K", margin + 30, sliderY + sliderHeight + 20, labelPaint);
                    canvas.DrawText($"{MaxValue:F0}K", width - margin - 30, sliderY + sliderHeight + 20, labelPaint);
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
                    canvas.DrawText($"{Value:F0}K", width / 2, height - 10, valuePaint);
                }

                // Draw tick marks
                using (var tickPaint = new SKPaint
                {
                    Color = SKColors.Gray,
                    StrokeWidth = 1,
                    IsAntialias = true
                })
                {
                    // Draw major ticks every 1000K
                    for (double temp = MinValue; temp <= MaxValue; temp += 1000)
                    {
                        float tickNormalized = (float)((temp - MinValue) / (MaxValue - MinValue));
                        float tickX = sliderStartX + (tickNormalized * sliderWidth);

                        canvas.DrawLine(tickX, sliderY - 3, tickX, sliderY + sliderHeight + 3, tickPaint);
                    }
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

        // Add touch handling for horizontal slider
        protected override void OnTouch(SKTouchEventArgs e)
        {
            if (e.ActionType == SKTouchAction.Pressed || e.ActionType == SKTouchAction.Moved)
            {
                var info = CanvasSize;
                float width = info.Width;
                float margin = 20;
                float sliderStartX = margin + 40;
                float sliderEndX = width - margin - 40;
                float sliderWidth = sliderEndX - sliderStartX;

                // Calculate new value based on touch position
                float touchX = Math.Max(sliderStartX, Math.Min(sliderEndX, e.Location.X));
                float normalizedPosition = (touchX - sliderStartX) / sliderWidth;
                double newValue = MinValue + (normalizedPosition * (MaxValue - MinValue));

                // Round to nearest 100K for better UX
                newValue = Math.Round(newValue / 100) * 100;

                Value = newValue;
                e.Handled = true;
            }
        }
    }
}