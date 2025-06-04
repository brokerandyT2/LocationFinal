using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Maui.Views.Premium;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Location.Photography.Maui.Views
{
    public partial class CameraEvaluation : ContentPage
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CameraEvaluation> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _isProcessing = false;
        private string _tempImagePath = string.Empty;

        public CameraEvaluation()
        {
            InitializeComponent();
        }

        public CameraEvaluation(
            IMediator mediator,
            ILogger<CameraEvaluation> logger,
            IServiceProvider serviceProvider)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
           
            try
            {
                // Request camera permissions
                var status = await Permissions.RequestAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    await DisplayAlert("Camera Permission",
                        "Camera access is required for calibration. Please enable it in settings.",
                        "OK");
                    await NavigateToSubscriptionPage();
                    return;
                }

                _logger?.LogInformation("Camera permissions granted");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error requesting camera permissions");
                await DisplayAlert("Permission Error",
                    "Unable to request camera permissions. You can skip calibration for now.",
                    "OK");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                // Cleanup temp files
                CleanupTempFiles();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error during cleanup");
            }
        }

        private async void OnCaptureButtonClicked(object sender, EventArgs e)
        {
            if (_isProcessing)
                return;

            try
            {
                _isProcessing = true;
                await SetProcessingState(true, "Opening camera...");

                // Check if camera capture is supported
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    await ShowError("Camera capture is not supported on this device");
                    return;
                }

                // Capture photo using MediaPicker
                var photo = await MediaPicker.Default.CapturePhotoAsync();
                if (photo == null)
                {
                    await ShowError("Photo capture was cancelled");
                    return;
                }

                await SetProcessingState(true, "Processing image...");

                // Save photo to temp file
                _tempImagePath = Path.Combine(FileSystem.AppDataDirectory, $"calibration_{DateTime.Now:yyyyMMddHHmmss}.jpg");

                // Read image data into memory first
                byte[] imageData;
                using (var sourceStream = await photo.OpenReadAsync())
                {
                    using var memoryStream = new MemoryStream();
                    await sourceStream.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                }

                // Save to file
                await File.WriteAllBytesAsync(_tempImagePath, imageData);

                // Create ImageSource from byte array to avoid file lock
                CapturedImage.Source = ImageSource.FromStream(() => new MemoryStream(imageData));
                CapturedImage.IsVisible = true;
                PlaceholderStack.IsVisible = false;

                await SetProcessingState(true, "Analyzing EXIF data...");

                // Process the image
                await ProcessCapturedImage();
            }
            catch (FeatureNotSupportedException)
            {
                await ShowError("Camera is not supported on this device");
            }
            catch (PermissionException)
            {
                await ShowError("Camera permission is required. Please enable it in settings.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error capturing photo");
                await ShowError("Error capturing photo. Please try again.");
            }
            finally
            {
                _isProcessing = false;
                await SetProcessingState(false);
            }
        }

        private async Task ProcessCapturedImage()
        {
            try
            {
                if (string.IsNullOrEmpty(_tempImagePath) || !File.Exists(_tempImagePath))
                {
                    await ShowError("Image file not found");
                    return;
                }

                var command = new CreatePhoneCameraProfileCommand
                {
                    ImagePath = _tempImagePath,
                    DeleteImageAfterProcessing = true
                };

                var result = await _mediator.Send(command);

                if (!result.IsSuccess)
                {
                    await ShowError($"Processing failed: {result.ErrorMessage}");
                    return;
                }

                var profile = result.Data;

                if (!profile.IsCalibrationSuccessful)
                {
                    await ShowError(profile.ErrorMessage);
                    return;
                }

                // Mark calibration as completed
                await SecureStorage.SetAsync("CameraProfileCompleted", "true");

                // Show success
                await ShowSuccess(profile);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error processing captured image");
                await ShowError("Error processing image. Please try again.");
            }
        }

        private async Task ShowSuccess(PhoneCameraProfileDto profile)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                CalibrationResultLabel.Text = $"Phone: {profile.PhoneModel}\n" +
                                            $"Focal Length: {profile.MainLensFocalLength:F1}mm\n" +
                                            $"Field of View: {profile.MainLensFOV:F1}°";

                SuccessOverlay.IsVisible = true;
                ErrorOverlay.IsVisible = false;
            });
        }

        private async Task ShowError(string message)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ErrorMessageLabel.Text = message;
                ErrorOverlay.IsVisible = true;
                SuccessOverlay.IsVisible = false;
            });
        }

        private async Task SetProcessingState(bool isProcessing, string message = "")
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ProcessingStack.IsVisible = isProcessing;
                ProcessingIndicator.IsRunning = isProcessing;
                ProcessingLabel.Text = message;

                CaptureButton.IsEnabled = !isProcessing;
                SkipButton.IsEnabled = !isProcessing;

                if (isProcessing)
                {
                    StatusLabel.Text = message;
                    StatusLabel.IsVisible = true;
                }
                else
                {
                    StatusLabel.IsVisible = false;
                }
            });
        }

        private async void OnContinueButtonClicked(object sender, EventArgs e)
        {
            await NavigateToSubscriptionPage();
        }

        private async void OnSkipButtonClicked(object sender, EventArgs e)
        {
            var result = await DisplayAlert("Skip Calibration",
                "Are you sure you want to skip camera calibration? You can set this up later in settings.",
                "Skip", "Cancel");

            if (result)
            {
                // Mark calibration as skipped
                await SecureStorage.SetAsync("CameraSetupSkipped", "true");
                await NavigateToSubscriptionPage();
            }
        }

        private async void OnRetryButtonClicked(object sender, EventArgs e)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                ErrorOverlay.IsVisible = false;
                SuccessOverlay.IsVisible = false;

                // Reset image display
                CapturedImage.IsVisible = false;
                PlaceholderStack.IsVisible = true;
                CapturedImage.Source = null;
            });
        }

        private async Task NavigateToSubscriptionPage()
        {
            try
            {
                var subscriptionPage = _serviceProvider?.GetService<SubscriptionSignUpPage>();
                if (subscriptionPage != null)
                {
                    await Navigation.PushAsync(subscriptionPage);
                }
                else
                {
                    // Fallback navigation
                    await Shell.Current.GoToAsync("//subscription");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error navigating to subscription page");
                await DisplayAlert("Navigation Error",
                    "Unable to proceed to next step. Please restart the app.",
                    "OK");
            }
        }

        private void CleanupTempFiles()
        {
            try
            {
                if (!string.IsNullOrEmpty(_tempImagePath) && File.Exists(_tempImagePath))
                {
                    File.Delete(_tempImagePath);
                    _logger?.LogDebug("Cleaned up temp image file: {TempImagePath}", _tempImagePath);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to cleanup temp files");
            }
        }
    }
}