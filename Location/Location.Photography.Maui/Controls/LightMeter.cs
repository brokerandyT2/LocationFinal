// Single pan gesture handler - Both dials draggable (like version 42)
using Location.Photography.Application.Services;



namespace Location.Photography.Maui.Controls
{
    public class LunaProDrawable : IDrawable
    {
        private GraphicsView meter;

        // Events for interaction state
        public event EventHandler InteractionStarted;
        public event EventHandler InteractionEnded;

        // Current angle values for each dial
        private float outerDialAngle = 0f; // ISO dial
        private float middleDialAngle = 0f; // Shutter speed dial
        private float innerDialAngle = 0f; // F-stop dial

        // Currently selected value indices
        private int selectedIsoIndex = 0;
        private int selectedShutterSpeedIndex = 0;
        private int selectedFStopIndex = 0;

        // Value arrays - using ONLY Full scale from utility classes
        private readonly string[] isoValues = ISOs.Full;
        private readonly string[] shutterSpeeds = ShutterSpeeds.Full;
        private readonly string[] fStops = Apetures.Full;

        // Single gesture recognizer and state tracking
        private PanGestureRecognizer singlePanGesture;
        private int activeDial = -1; // Which dial is currently being dragged (-1 = none)
        private bool isDragging = false;
        private PointF lastDragPoint;

        // Colors for dials
        private readonly Color outerDialColor = Color.FromRgb(112, 112, 112);
        private readonly Color middleDialColor = Color.FromRgb(45, 90, 85);
        private readonly Color innerDialColor = Color.FromRgb(25, 80, 70);

        // Store dial dimensions
        private float dialCenterX;
        private float dialCenterY;
        private float outerDialRadius;
        private float middleDialRadius;
        private float innerDialRadius;

        public LunaProDrawable(GraphicsView meter)
        {
            this.meter = meter;
            SetupSingleGesture();
        }

        public LunaProDrawable() { }

        private void SetupSingleGesture()
        {
            singlePanGesture = new PanGestureRecognizer();
            singlePanGesture.PanUpdated += OnPanUpdated;
            meter.GestureRecognizers.Add(singlePanGesture);
        }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            // Set up dimensions - use portrait orientation
            float width = dirtyRect.Width;
            float height = dirtyRect.Height;
            float centerX = width / 2;

            // Create more portrait-like aspect ratio
            height = width * 1.6f;

            // Draw the outer case
            canvas.FillColor = Color.FromRgb(45, 45, 45);
            canvas.FillRoundedRectangle(width * 0.05f, height * 0.03f, width * 0.9f, height * 0.94f, 30);

            // Add inner border for depth
            canvas.StrokeColor = Color.FromRgb(30, 30, 30);
            canvas.StrokeSize = 3;
            canvas.DrawRoundedRectangle(width * 0.08f, height * 0.05f, width * 0.84f, height * 0.9f, 25);

            // Draw sections
            DrawMeterDisplay(canvas, width, height);
            DrawDialSystemWithZIndex(canvas, width, height);
            DrawBottomLabel(canvas, width, height);
        }

        private void DrawMeterDisplay(ICanvas canvas, float width, float height)
        {
            float centerX = width / 2;
            float meterTop = height * 0.08f;
            float meterHeight = height * 0.25f;
            float meterWidth = width * 0.8f;

            // Draw meter background
            canvas.FillColor = Color.FromRgb(240, 235, 220);
            canvas.FillRoundedRectangle(centerX - meterWidth / 2, meterTop, meterWidth, meterHeight, 10);

            // Draw meter border
            canvas.StrokeColor = Color.FromRgb(30, 30, 30);
            canvas.StrokeSize = 3;
            canvas.DrawRoundedRectangle(centerX - meterWidth / 2, meterTop, meterWidth, meterHeight, 10);

            // Draw the meter scale
            float meterCenterY = (meterTop + meterHeight / 2) + 70;
            float arcRadius = meterHeight;

            // Draw scale markings
            canvas.StrokeColor = Colors.Black;
            canvas.StrokeSize = 1.5f;
            canvas.FontSize = 16;
            canvas.FontColor = Colors.Black;

            float startAngle = 150 * (float)(Math.PI / 180);
            float endAngle = 30 * (float)(Math.PI / 180);

            string[] values = { "-5", "-4", "-3", "-2", "-1", "0", "1", "2", "3", "4", "5" };
            for (int i = 0; i < values.Length; i++)
            {
                float angle = startAngle + i * (endAngle - startAngle) / (values.Length - 1);

                // Draw tick marks
                float innerRadius = arcRadius - 2;
                float outerRadius = arcRadius;

                float innerX = centerX + innerRadius * (float)Math.Cos(angle);
                float innerY = meterCenterY - innerRadius * (float)Math.Sin(angle) + meterHeight * 0.1f;
                float outerX = centerX + outerRadius * (float)Math.Cos(angle);
                float outerY = meterCenterY - outerRadius * (float)Math.Sin(angle) + meterHeight * 0.1f;

                canvas.DrawLine(innerX, innerY, outerX, outerY);

                // Draw value labels
                float textRadius = arcRadius - 25;
                float textX = centerX + textRadius * (float)Math.Cos(angle);
                float textY = meterCenterY - textRadius * (float)Math.Sin(angle) + meterHeight * 0.1f;

                canvas.DrawString(values[i], textX, textY, HorizontalAlignment.Center);
            }

            // Draw "EV" label
            canvas.FontSize = 24;
            canvas.FontColor = Colors.Black;
            canvas.DrawString("EV", centerX, meterCenterY + (meterHeight - 110) * 0.15f, HorizontalAlignment.Center);

            // Draw meter needle
            canvas.StrokeColor = Colors.Red;
            canvas.StrokeSize = 2;
            float needleAngle = 30 * (float)(Math.PI / 180.0);
            float needleLength = arcRadius - 10;

            float needleEndX = centerX + needleLength * (float)Math.Cos(needleAngle);
            float needleEndY = meterCenterY - needleLength * (float)Math.Sin(needleAngle) + meterHeight * 0.1f;

            canvas.DrawLine(centerX, meterCenterY + (meterHeight - 110) * 0.1f, needleEndX, needleEndY);

            // Draw pivot point
            canvas.FillColor = Colors.Red;
            canvas.FillCircle(centerX, meterCenterY + meterHeight * 0.1f, 3);
        }

        private void DrawDialSystemWithZIndex(ICanvas canvas, float width, float height)
        {
            float centerX = width / 2;
            float dialY = height * 0.5f + 60;

            // Store dimensions
            dialCenterX = centerX;
            dialCenterY = dialY;
            outerDialRadius = width * 0.38f;
            middleDialRadius = outerDialRadius * 0.7f;
            innerDialRadius = middleDialRadius * 0.65f;

            // Z-INDEX DRAWING ORDER - All three dials
            DrawOuterDial(canvas, centerX, dialY);
            DrawMiddleDial(canvas, centerX, dialY);
            DrawInnerDial(canvas, centerX, dialY);
            DrawCenterHub(canvas, centerX, dialY);
        }

        private void DrawOuterDial(ICanvas canvas, float centerX, float centerY)
        {
            // Draw outer dial background
            canvas.FillColor = outerDialColor;
            canvas.FillCircle(centerX, centerY, outerDialRadius);

            // Apply rotation and draw ISO labels
            canvas.SaveState();
            canvas.Translate(centerX, centerY);
            canvas.Rotate(-outerDialAngle * 180 / (float)Math.PI);
            canvas.Translate(-centerX, -centerY);

            DrawDialLabels(canvas, centerX, centerY, isoValues, outerDialRadius, Color.FromRgb(200, 190, 150), 14);

            canvas.RestoreState();
        }

        private void DrawMiddleDial(ICanvas canvas, float centerX, float centerY)
        {
            // Draw middle dial background
            canvas.FillColor = middleDialColor;
            canvas.FillCircle(centerX, centerY, middleDialRadius);

            // Apply rotation and draw shutter speed labels
            canvas.SaveState();
            canvas.Translate(centerX, centerY);
            canvas.Rotate(-middleDialAngle * 180 / (float)Math.PI);
            canvas.Translate(-centerX, -centerY);

            // Draw highlighted sync speed section
            float highlightStartAngle = 240 * (float)(Math.PI / 180);
            float highlightEndAngle = 290 * (float)(Math.PI / 180);
            canvas.FillColor = Color.FromRgb(180, 120, 40);
            canvas.FillArc(centerX - middleDialRadius, centerY - middleDialRadius,
                          middleDialRadius * 2, middleDialRadius * 2,
                          highlightStartAngle, highlightEndAngle - highlightStartAngle, true);

            DrawDialLabels(canvas, centerX, centerY, shutterSpeeds, middleDialRadius, Color.FromRgb(200, 190, 150), 12);

            canvas.RestoreState();
        }

        private void DrawInnerDial(ICanvas canvas, float centerX, float centerY)
        {
            // Draw inner dial background
            canvas.FillColor = innerDialColor;
            canvas.FillCircle(centerX, centerY, innerDialRadius);

            // Apply rotation and draw f-stop labels
            canvas.SaveState();
            canvas.Translate(centerX, centerY);
            canvas.Rotate(-innerDialAngle * 180 / (float)Math.PI);
            canvas.Translate(-centerX, -centerY);

            DrawDialLabels(canvas, centerX, centerY, fStops, innerDialRadius, Color.FromRgb(200, 190, 150), 12);

            canvas.RestoreState();
        }

        private void DrawCenterHub(ICanvas canvas, float centerX, float centerY)
        {
            // Draw non-touchable center hub
            float hubRadius = innerDialRadius * 0.5f;
            canvas.FillColor = Color.FromRgb(30, 30, 30);
            canvas.FillCircle(centerX, centerY, hubRadius);

            // Draw stylized "f"
            canvas.FontSize = 28;
            canvas.FontColor = Color.FromRgb(200, 190, 150);
            canvas.DrawString("ƒ", centerX, centerY - 5, HorizontalAlignment.Center);
        }

        private void DrawDialLabels(ICanvas canvas, float centerX, float centerY, string[] values, float radius, Color color, int fontSize)
        {
            canvas.StrokeColor = color;
            canvas.FontColor = color;
            canvas.FontSize = fontSize;

            for (int i = 0; i < values.Length; i++)
            {
                float angle = i * (float)(2 * Math.PI / values.Length);
                float textRadius = radius - 25;
                float tickInnerRadius = radius - 15;
                float tickOuterRadius = radius - 5;

                // Draw tick mark
                float x1 = centerX + tickInnerRadius * (float)Math.Sin(angle);
                float y1 = centerY - tickInnerRadius * (float)Math.Cos(angle);
                float x2 = centerX + tickOuterRadius * (float)Math.Sin(angle);
                float y2 = centerY - tickOuterRadius * (float)Math.Cos(angle);

                canvas.DrawLine(x1, y1, x2, y2);

                // Draw text with correct rotation (90 degrees clockwise from perpendicular)
                float textX = centerX + textRadius * (float)Math.Sin(angle);
                float textY = centerY - textRadius * (float)Math.Cos(angle);

                canvas.SaveState();
                canvas.Translate(textX, textY);

                float radiusAngleDegrees = angle * (180f / (float)Math.PI);
                float textRotationAngle = radiusAngleDegrees;

                canvas.Rotate(textRotationAngle);

                // Clean the value string for display
                string displayValue = values[i];
                if (displayValue.StartsWith("f/"))
                {
                    displayValue = displayValue.Substring(2);
                }

                canvas.DrawString(displayValue, 0, 0, HorizontalAlignment.Center);
                canvas.RestoreState();
            }
        }

        private void DrawBottomLabel(ICanvas canvas, float width, float height)
        {
            float centerX = width / 2;
            float labelY = height * 0.87f;
            float labelWidth = width * 0.8f;
            float labelHeight = height * 0.08f;

            canvas.FillColor = Color.FromRgb(45, 45, 45);
            canvas.FillRoundedRectangle(centerX - labelWidth / 2, labelY, labelWidth, labelHeight, 10);

            canvas.FontSize = 18;
            canvas.FontColor = Color.FromRgb(200, 190, 150);
            canvas.DrawString("PixMap-PRO", centerX, labelY + labelHeight / 2 - 2, HorizontalAlignment.Center);
        }

        // Ring-based hit testing - checks if point is within the ring area
        private bool IsPointInRing(PointF point, float outerRadius, float innerRadius)
        {
            float dx = point.X - dialCenterX;
            float dy = point.Y - dialCenterY;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);

            return distance >= innerRadius && distance <= outerRadius;
        }

        // Single pan gesture handler with z-index priority logic
        private void OnPanUpdated(object sender, PanUpdatedEventArgs e)
        {
            // Calculate touch point
            PointF touchPoint = new PointF(dialCenterX + (float)e.TotalX, dialCenterY + (float)e.TotalY);

            // Check if touch is within biggest dial - throw away if not
            if (IsWithinBiggestDial(touchPoint))
            {
                // Determine which dial based on z-index priority (inner wins over middle wins over outer)
                if (IsWithinInnerDial(touchPoint))
                {
                    activeDial = 2; // Inner dial (center to inner radius)
                }
                else if (IsWithinMiddleDialRing(touchPoint))
                {
                    activeDial = 1; // Middle dial (inner radius to middle radius)
                }
                else
                {
                    activeDial = 0; // Outer dial (middle radius to outer radius)
                }

                // Handle the gesture state for the active dial
                HandleGestureForActiveDial(e, touchPoint);
            }
            // else: throw away touch (outside biggest dial)
        }

        private void HandleGestureForActiveDial(PanUpdatedEventArgs e, PointF touchPoint)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    isDragging = true;
                    InteractionStarted?.Invoke(this, EventArgs.Empty);
                    lastDragPoint = touchPoint;
                    break;

                case GestureStatus.Running:
                    if (!isDragging) return;

                    PointF currentPoint = new PointF(dialCenterX + (float)e.TotalX, dialCenterY + (float)e.TotalY);
                    float angleDelta = CalculateAngleDelta(lastDragPoint, currentPoint);

                    // Rotate the active dial
                    switch (activeDial)
                    {
                        case 0:
                            outerDialAngle += angleDelta;
                            break;
                        case 1:
                            middleDialAngle += angleDelta;
                            break;
                        case 2:
                            innerDialAngle += angleDelta;
                            break;
                    }

                    lastDragPoint = currentPoint;
                    meter?.Invalidate();
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    if (!isDragging) return;

                    // Snap the active dial to nearest value
                    switch (activeDial)
                    {
                        case 0:
                            SnapOuterDialToValue();
                            break;
                        case 1:
                            SnapMiddleDialToValue();
                            break;
                        case 2:
                            SnapInnerDialToValue();
                            break;
                    }

                    isDragging = false;
                    InteractionEnded?.Invoke(this, EventArgs.Empty);
                    meter?.Invalidate();
                    break;
            }
        }

        private bool IsWithinBiggestDial(PointF point)
        {
            float distance = GetDistanceFromCenter(point);
            return distance <= outerDialRadius;
        }

        private bool IsWithinInnerDial(PointF point)
        {
            float distance = GetDistanceFromCenter(point);
            float hubRadius = innerDialRadius * 0.5f;
            return distance >= hubRadius && distance <= innerDialRadius; // Ring from hub to inner radius
        }

        private bool IsWithinMiddleDialRing(PointF point)
        {
            float distance = GetDistanceFromCenter(point);
            return distance > innerDialRadius && distance <= middleDialRadius; // Ring from inner to middle radius
        }

        // Note: Outer dial ring is implicit - anything that passes IsWithinBiggestDial 
        // but not IsWithinInnerDial and not IsWithinMiddleDialRing

        private float GetDistanceFromCenter(PointF point)
        {
            return (float)Math.Sqrt(Math.Pow(point.X - dialCenterX, 2) + Math.Pow(point.Y - dialCenterY, 2));
        }

        private float CalculateAngleDelta(PointF point1, PointF point2)
        {
            // Calculate angles from dial center to both points
            float angle1 = (float)Math.Atan2(point1.Y - dialCenterY, point1.X - dialCenterX);
            float angle2 = (float)Math.Atan2(point2.Y - dialCenterY, point2.X - dialCenterX);

            // Calculate the difference
            float delta = angle2 - angle1;

            // Handle wraparound for shortest rotation path
            if (delta > Math.PI)
                delta -= (float)(2 * Math.PI);
            else if (delta < -Math.PI)
                delta += (float)(2 * Math.PI);

            return delta;
        }

        private void SnapOuterDialToValue()
        {
            // Calculate step angle for each ISO value
            float stepAngle = (float)(2 * Math.PI / isoValues.Length);

            // Normalize angle to 0-2π range
            float normalizedAngle = outerDialAngle;
            while (normalizedAngle < 0) normalizedAngle += (float)(2 * Math.PI);
            while (normalizedAngle >= 2 * Math.PI) normalizedAngle -= (float)(2 * Math.PI);

            // Find closest index
            int closestIndex = (int)Math.Round(normalizedAngle / stepAngle);

            // Ensure index is in valid range
            if (closestIndex >= isoValues.Length) closestIndex = 0;
            if (closestIndex < 0) closestIndex = isoValues.Length - 1;

            // Snap to the exact angle
            outerDialAngle = closestIndex * stepAngle;
            selectedIsoIndex = closestIndex;

            System.Diagnostics.Debug.WriteLine($"Snapped to ISO: {isoValues[selectedIsoIndex]} at angle: {outerDialAngle}");
        }

        private void SnapMiddleDialToValue()
        {
            // Calculate step angle for each shutter speed value
            float stepAngle = (float)(2 * Math.PI / shutterSpeeds.Length);

            // Normalize angle to 0-2π range
            float normalizedAngle = middleDialAngle;
            while (normalizedAngle < 0) normalizedAngle += (float)(2 * Math.PI);
            while (normalizedAngle >= 2 * Math.PI) normalizedAngle -= (float)(2 * Math.PI);

            // Find closest index
            int closestIndex = (int)Math.Round(normalizedAngle / stepAngle);

            // Ensure index is in valid range
            if (closestIndex >= shutterSpeeds.Length) closestIndex = 0;
            if (closestIndex < 0) closestIndex = shutterSpeeds.Length - 1;

            // Snap to the exact angle
            middleDialAngle = closestIndex * stepAngle;
            selectedShutterSpeedIndex = closestIndex;

            System.Diagnostics.Debug.WriteLine($"Snapped to Shutter Speed: {shutterSpeeds[selectedShutterSpeedIndex]} at angle: {middleDialAngle}");
        }

        private void SnapInnerDialToValue()
        {
            // Calculate step angle for each f-stop value
            float stepAngle = (float)(2 * Math.PI / fStops.Length);

            // Normalize angle to 0-2π range
            float normalizedAngle = innerDialAngle;
            while (normalizedAngle < 0) normalizedAngle += (float)(2 * Math.PI);
            while (normalizedAngle >= 2 * Math.PI) normalizedAngle -= (float)(2 * Math.PI);

            // Find closest index
            int closestIndex = (int)Math.Round(normalizedAngle / stepAngle);

            // Ensure index is in valid range
            if (closestIndex >= fStops.Length) closestIndex = 0;
            if (closestIndex < 0) closestIndex = fStops.Length - 1;

            // Snap to the exact angle
            innerDialAngle = closestIndex * stepAngle;
            selectedFStopIndex = closestIndex;

            System.Diagnostics.Debug.WriteLine($"Snapped to F-Stop: {fStops[selectedFStopIndex]} at angle: {innerDialAngle}");
        }

        public (string Asa, string ShutterSpeed, string FStop) SelectedValues
        {
            get
            {
                return (
                    isoValues[selectedIsoIndex],
                    shutterSpeeds[selectedShutterSpeedIndex],
                    fStops[selectedFStopIndex]
                );
            }
        }
    }
}