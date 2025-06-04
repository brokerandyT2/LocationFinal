using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Graphics;
using GSize = Microsoft.Maui.Graphics.Size;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class FieldOfView : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly ILogger<FieldOfView> _logger;
        private readonly IFOVCalculationService _fovCalculationService;
        private readonly IAlertService _alertService;

        private double _phoneFOV = 0;
        private double _selectedCameraFOV = 0;
        private bool _isCalculating = false;
        private string _currentImagePath = string.Empty;

        public FieldOfView(
            IMediator mediator,
            ILogger<FieldOfView> logger,
            IFOVCalculationService fovCalculationService,
            IAlertService alertService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fovCalculationService = fovCalculationService ?? throw new ArgumentNullException(nameof(fovCalculationService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));

            InitializeComponent();
            InitializeFieldOfView();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPhoneCameraProfileAsync();
        }

        private void InitializeFieldOfView()
        {
            try
            {
                // Set up the graphics view with our drawable
                OverlayGraphicsView.Drawable = new FOVOverlayDrawable();

                // Initialize camera picker
                CameraPicker.SelectedIndex = 0;

                _logger.LogInformation("Field of View page initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Field of View page");
            }
        }

        private async void OnCaptureButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if (MediaPicker.Default.IsCaptureSupported)
                {
                    ProcessingOverlay.IsVisible = true;

                    var photo = await MediaPicker.Default.CapturePhotoAsync();

                    if (photo != null)
                    {
                        await DisplayCapturedImageAsync(photo);
                    }
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync("Camera capture is not supported on this device", "Error");
                }
            }
            catch (FeatureNotSupportedException)
            {
                await _alertService.ShowErrorAlertAsync("Camera is not supported on this device", "Error");
            }
            catch (PermissionException)
            {
                await _alertService.ShowErrorAlertAsync("Camera permission is required to capture photos", "Permission Required");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error capturing photo");
                await _alertService.ShowErrorAlertAsync("Error capturing photo", "Error");
            }
            finally
            {
                ProcessingOverlay.IsVisible = false;
            }
        }

        private async Task DisplayCapturedImageAsync(FileResult photo)
        {
            try
            {
                // Copy photo to app directory for processing
                var newFile = Path.Combine(FileSystem.AppDataDirectory, $"fov_capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

                using var sourceStream = await photo.OpenReadAsync();
                using var localFileStream = File.OpenWrite(newFile);
                await sourceStream.CopyToAsync(localFileStream);

                _currentImagePath = newFile;

                // Display the image
                CapturedImage.Source = ImageSource.FromFile(newFile);

                // Hide placeholder and show overlay
                PlaceholderStack.IsVisible = false;
                OverlayGraphicsView.IsVisible = true;

                // Update FOV display and overlay
                UpdateFOVDisplay();
                UpdateOverlay();

                _logger.LogInformation("Photo captured and displayed: {ImagePath}", newFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying captured image");
                await _alertService.ShowErrorAlertAsync("Error processing captured photo", "Error");
            }
        }

        private async Task LoadPhoneCameraProfileAsync()
        {
            if (_isCalculating) return;

            try
            {
                _isCalculating = true;
                ProcessingOverlay.IsVisible = true;

                // Try to get existing phone camera profile
                var command = new GetPhoneCameraProfileQuery();
                var result = await _mediator.Send(command);

                if (result.IsSuccess && result.Data != null)
                {
                    _phoneFOV = result.Data.MainLensFOV;
                    UpdateFOVDisplay();
                    _logger.LogInformation("Loaded phone camera profile: {FOV}°", _phoneFOV);
                }
                else
                {
                    // Estimate based on device if no profile exists
                    await EstimatePhoneFOVAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading phone camera profile");
                await _alertService.ShowErrorAlertAsync("Error loading camera profile", "Error");
            }
            finally
            {
                _isCalculating = false;
                ProcessingOverlay.IsVisible = false;
            }
        }

        private async Task EstimatePhoneFOVAsync()
        {
            try
            {
                var phoneModel = DeviceInfo.Model;
                var sensorResult = await _fovCalculationService.EstimateSensorDimensionsAsync(phoneModel);

                if (sensorResult.IsSuccess)
                {
                    // Estimate typical phone camera focal length (usually around 26-28mm equivalent)
                    var estimatedFocalLength = 4.0; // Typical physical focal length for phones
                    _phoneFOV = _fovCalculationService.CalculateHorizontalFOV(estimatedFocalLength, sensorResult.Data.Width);
                }
                else
                {
                    // Fallback to typical smartphone FOV
                    _phoneFOV = 78.0; // Typical smartphone camera FOV
                }

                UpdateFOVDisplay();
                _logger.LogInformation("Estimated phone FOV: {FOV}°", _phoneFOV);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating phone FOV");
                _phoneFOV = 78.0; // Fallback
                UpdateFOVDisplay();
            }
        }

        private void OnCameraPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                var picker = (Picker)sender;
                var selectedIndex = picker.SelectedIndex;

                switch (selectedIndex)
                {
                    case 0: // Phone Camera
                        FocalLengthStack.IsVisible = false;
                        _selectedCameraFOV = _phoneFOV;
                        break;

                    case 1: // Custom Focal Length
                        FocalLengthStack.IsVisible = true;
                        if (!string.IsNullOrEmpty(FocalLengthEntry.Text) && double.TryParse(FocalLengthEntry.Text, out double focalLength))
                        {
                            CalculateCustomFOV(focalLength);
                        }
                        else
                        {
                            _selectedCameraFOV = 0;
                        }
                        break;
                }

                UpdateFOVDisplay();
                UpdateOverlay();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling camera picker selection");
            }
        }

        private void OnFocalLengthChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(e.NewTextValue))
                {
                    _selectedCameraFOV = 0;
                    UpdateFOVDisplay();
                    UpdateOverlay();
                    return;
                }

                if (double.TryParse(e.NewTextValue, out double focalLength) && focalLength > 0)
                {
                    CalculateCustomFOV(focalLength);
                    UpdateFOVDisplay();
                    UpdateOverlay();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating custom FOV");
            }
        }

        private void CalculateCustomFOV(double focalLength)
        {
            try
            {
                // Use full-frame sensor dimensions for focal length comparison (36mm x 24mm)
                var fullFrameWidth = 36.0;
                _selectedCameraFOV = _fovCalculationService.CalculateHorizontalFOV(focalLength, fullFrameWidth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating FOV for focal length {FocalLength}", focalLength);
                _selectedCameraFOV = 0;
            }
        }

        private void UpdateFOVDisplay()
        {
            try
            {
                PhoneFOVLabel.Text = $"Phone FOV: {_phoneFOV:F1}°";

                if (_selectedCameraFOV > 0)
                {
                    CameraFOVLabel.Text = $"Selected FOV: {_selectedCameraFOV:F1}°";

                    var ratio = _selectedCameraFOV / _phoneFOV;
                    if (ratio > 1.1)
                    {
                        ComparisonLabel.Text = $"Comparison: {ratio:F1}x wider view";
                    }
                    else if (ratio < 0.9)
                    {
                        ComparisonLabel.Text = $"Comparison: {(1 / ratio):F1}x more zoomed";
                    }
                    else
                    {
                        ComparisonLabel.Text = "Comparison: Similar field of view";
                    }
                }
                else
                {
                    CameraFOVLabel.Text = "Selected FOV: --";
                    ComparisonLabel.Text = "Comparison: --";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating FOV display");
            }
        }

        private void UpdateOverlay()
        {
            try
            {
                if (OverlayGraphicsView.Drawable is FOVOverlayDrawable drawable)
                {
                    drawable.PhoneFOV = _phoneFOV;
                    drawable.SelectedCameraFOV = _selectedCameraFOV;
                    OverlayGraphicsView.Invalidate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overlay");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            // Clean up temporary image file
            if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
            {
                try
                {
                    File.Delete(_currentImagePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete temporary image file: {ImagePath}", _currentImagePath);
                }
            }
        }
    }

    public class FOVOverlayDrawable : IDrawable
    {
        public double PhoneFOV { get; set; }
        public double SelectedCameraFOV { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (PhoneFOV <= 0) return;

            var centerX = dirtyRect.Width / 2;
            var centerY = dirtyRect.Height / 2;

            // Calculate phone FOV box (cyan)
            var phoneFOVBox = CalculateFOVBox(PhoneFOV, dirtyRect);
            DrawFOVBox(canvas, phoneFOVBox, Colors.Cyan, "Phone Camera");

            // Calculate selected camera FOV box (orange)
            if (SelectedCameraFOV > 0)
            {
                var cameraFOVBox = CalculateFOVBox(SelectedCameraFOV, dirtyRect);
                DrawFOVBox(canvas, cameraFOVBox, Colors.Orange, "Selected Camera");
            }
        }

        private RectF CalculateFOVBox(double fov, RectF containerRect)
        {
            // Normalize FOV to a scale factor (phone FOV = 1.0)
            var referenceFOV = Math.Max(PhoneFOV, 60.0); // Use phone FOV or minimum 60° as reference
            var scaleFactor = fov / referenceFOV;

            // Calculate box dimensions
            var maxWidth = containerRect.Width * 0.8f; // Leave some margin
            var maxHeight = containerRect.Height * 0.8f;

            var boxWidth = Math.Max(100, Math.Min(maxWidth, maxWidth * scaleFactor));
            var boxHeight = boxWidth * 0.6f; // Maintain 3:2 aspect ratio

            // Center the box
            var x = (containerRect.Width - boxWidth) / 2;
            var y = (containerRect.Height - boxHeight) / 2;

            return new RectF((float)x, (float)y, (float)boxWidth, (float)boxHeight);
        }

        private void DrawFOVBox(ICanvas canvas, RectF box, Color color, string label)
        {
            // Draw the FOV rectangle
            canvas.StrokeColor = color;
            canvas.StrokeSize = 3;
            canvas.DrawRectangle(box);

            // Draw corner markers
            var cornerSize = 20;
            DrawCorner(canvas, box.Left, box.Top, cornerSize, color); // Top-left
            DrawCorner(canvas, box.Right - cornerSize, box.Top, cornerSize, color); // Top-right
            DrawCorner(canvas, box.Left, box.Bottom - cornerSize, cornerSize, color); // Bottom-left
            DrawCorner(canvas, box.Right - cornerSize, box.Bottom - cornerSize, cornerSize, color); // Bottom-right

            // Draw label
            canvas.FontColor = color;
            canvas.FontSize = 16;
            var labelY = box.Top - 25;
            if (labelY < 0) labelY = box.Bottom + 25;

            canvas.DrawString(label, box.Left, labelY, HorizontalAlignment.Left);
        }

        private void DrawCorner(ICanvas canvas, float x, float y, float size, Color color)
        {
            canvas.StrokeColor = color;
            canvas.StrokeSize = 4;

            // Draw L-shaped corner
            canvas.DrawLine(x, y, x + size, y); // Horizontal line
            canvas.DrawLine(x, y, x, y + size); // Vertical line
        }
    }

    // Query class for getting phone camera profile
    public class GetPhoneCameraProfileQuery : IRequest<Result<PhoneCameraProfileDto>>
    {
    }
}