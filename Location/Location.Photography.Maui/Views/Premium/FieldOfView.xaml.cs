using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Queries.CameraEvaluation;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class FieldOfView : ContentPage, INotifyPropertyChanged, INavigationAware
    {
        private readonly IMediator _mediator;
        private readonly ILogger<FieldOfView> _logger;
        private readonly IFOVCalculationService _fovCalculationService;
        private readonly IAlertService _alertService;
        private readonly ICameraDataService _cameraDataService;

        private double _phoneFOV = 0;
        private double _selectedCameraFOV = 0;
        private bool _isCalculating = false;
        private string _currentImagePath = string.Empty;

        // Collections for camera and lens data
        private ObservableCollection<CameraDisplayItem> _availableCameras = new();
        private ObservableCollection<LensDisplayItem> _availableLenses = new();
        private CameraDisplayItem? _selectedCamera;
        private LensDisplayItem? _selectedLens;

        // Paging support
        private int _currentCameraSkip = 0;
        private int _currentLensSkip = 0;
        private const int _pageSize = 20;
        private bool _hasMoreCameras = true;
        private bool _hasMoreLenses = true;
        private bool _isLoadingCameras = false;
        private bool _isLoadingLenses = false;

        public ObservableCollection<CameraDisplayItem> AvailableCameras
        {
            get => _availableCameras;
            set
            {
                _availableCameras = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LensDisplayItem> AvailableLenses
        {
            get => _availableLenses;
            set
            {
                _availableLenses = value;
                OnPropertyChanged();
            }
        }

        public CameraDisplayItem? SelectedCamera
        {
            get => _selectedCamera;
            set
            {
                _selectedCamera = value;
                OnPropertyChanged();
                OnCameraSelectionChanged();
            }
        }

        public LensDisplayItem? SelectedLens
        {
            get => _selectedLens;
            set
            {
                _selectedLens = value;
                OnPropertyChanged();
                OnLensSelectionChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public FieldOfView(
            IMediator mediator,
            ILogger<FieldOfView> logger,
            IFOVCalculationService fovCalculationService,
            IAlertService alertService,
            ICameraDataService cameraDataService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fovCalculationService = fovCalculationService ?? throw new ArgumentNullException(nameof(fovCalculationService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _cameraDataService = cameraDataService ?? throw new ArgumentNullException(nameof(cameraDataService));

            InitializeComponent();
            BindingContext = this;
            InitializeFieldOfView();
             LoadPhoneCameraProfileAsync();
             LoadCamerasAsync();
        }

        // INavigationAware implementation
        public async void OnNavigatedToAsync()
        {
            await LoadPhoneCameraProfileAsync();
            await LoadCamerasAsync();
        }

        public void OnNavigatedFromAsync()
        {
            // Cleanup if needed
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Remove the async calls from here since we're using INavigationAware
        }

        private void InitializeFieldOfView()
        {
            try
            {
                OverlayGraphicsView.Drawable = new FOVOverlayDrawable();
                _logger.LogInformation("Field of View page initialized");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing Field of View page");
            }
        }

        private async Task LoadCamerasAsync(bool loadMore = false)
        {
            if (_isLoadingCameras) return;

            try
            {
                _isLoadingCameras = true;
                ProcessingOverlay.IsVisible = true;

                if (!loadMore)
                {
                    _currentCameraSkip = 0;
                    AvailableCameras.Clear();

                    // Add "Missing Camera?" option
                    AvailableCameras.Add(new CameraDisplayItem
                    {
                        Camera = null,
                        DisplayName = "Missing Camera?",
                        IsMissingOption = true
                    });
                }

                var result = await _cameraDataService.GetCameraBodiesAsync(_currentCameraSkip, _pageSize);
                if (result.IsSuccess)
                {
                    foreach (var camera in result.Data.CameraBodies)
                    {
                        AvailableCameras.Add(new CameraDisplayItem
                        {
                            Camera = camera,
                            DisplayName = camera.DisplayName,
                            IsMissingOption = false
                        });
                    }

                    _hasMoreCameras = result.Data.HasMore;
                    LoadMoreCamerasButton.IsVisible = _hasMoreCameras;
                    _currentCameraSkip += _pageSize;
                    CameraPicker.SelectedIndex = -1;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cameras");
                await _alertService.ShowErrorAlertAsync("Error loading cameras", "Error");
            }
            finally
            {
                _isLoadingCameras = false;
                ProcessingOverlay.IsVisible = false;
            }
        }

        private async Task LoadLensesAsync(bool loadMore = false)
        {
            if (_isLoadingLenses || SelectedCamera?.Camera == null) return;

            try
            {
                _isLoadingLenses = true;
                ProcessingOverlay.IsVisible = true;

                if (!loadMore)
                {
                    _currentLensSkip = 0;
                    AvailableLenses.Clear();

                    // Add "Missing Lens?" option
                    AvailableLenses.Add(new LensDisplayItem
                    {
                        Lens = null,
                        DisplayName = "Missing Lens?",
                        IsMissingOption = true
                    });
                }

                var result = await _cameraDataService.GetLensesAsync(_currentLensSkip, _pageSize, false, SelectedCamera.Camera.Id);
                if (result.IsSuccess)
                {
                    foreach (var lens in result.Data.Lenses)
                    {
                        AvailableLenses.Add(new LensDisplayItem
                        {
                            Lens = lens,
                            DisplayName = lens.DisplayName,
                            IsMissingOption = false
                        });
                    }

                    _hasMoreLenses = result.Data.HasMore;
                    LoadMoreLensesButton.IsVisible = _hasMoreLenses;
                    _currentLensSkip += _pageSize;
                }

                LensPicker.IsEnabled = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lenses");
                await _alertService.ShowErrorAlertAsync("Error loading lenses", "Error");
            }
            finally
            {
                _isLoadingLenses = false;
                ProcessingOverlay.IsVisible = false;
            }
        }

        private async void OnLoadMoreCamerasClicked(object sender, EventArgs e)
        {
            await LoadCamerasAsync(true);
        }

        private async void OnLoadMoreLensesClicked(object sender, EventArgs e)
        {
            await LoadLensesAsync(true);
        }

        private async void OnCameraSelectionChanged(object sender, EventArgs e)
        {
            await OnCameraSelectionChanged();
        }

        private async Task OnCameraSelectionChanged()
        {
            try
            {
                if (SelectedCamera?.IsMissingOption == true)
                {
                    await Shell.Current.GoToAsync("//AddCameraModal");
                    return;
                }

                // Reset lens selection
                SelectedLens = null;
                AvailableLenses.Clear();
                LensPicker.IsEnabled = false;
                LoadMoreLensesButton.IsVisible = false;

                if (SelectedCamera?.Camera != null)
                {
                    await LoadLensesAsync();
                }

                UpdateFOVDisplay();
                UpdateOverlay();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling camera selection");
            }
        }

        private async void OnLensSelectionChanged(object sender, EventArgs e)
        {
            await OnLensSelectionChanged();
        }

        private async Task OnLensSelectionChanged()
        {
            try
            {
                if (SelectedLens?.IsMissingOption == true)
                {
                    await Shell.Current.GoToAsync("//AddLensModal");
                    return;
                }

                if (SelectedLens?.Lens != null)
                {
                    CalculateSelectedCameraFOV();
                }
                else
                {
                    _selectedCameraFOV = 0;
                }

                UpdateFOVDisplay();
                UpdateOverlay();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling lens selection");
            }
        }

        private void CalculateSelectedCameraFOV()
        {
            try
            {
                if (SelectedCamera?.Camera == null || SelectedLens?.Lens == null) return;

                var focalLength = SelectedLens.Lens.MinMM; // Use minimum focal length for calculation
                var sensorWidth = SelectedCamera.Camera.SensorWidth;

                _selectedCameraFOV = _fovCalculationService.CalculateHorizontalFOV(focalLength, sensorWidth);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating selected camera FOV");
                _selectedCameraFOV = 0;
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
                var newFile = Path.Combine(FileSystem.AppDataDirectory, $"fov_capture_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

                using var sourceStream = await photo.OpenReadAsync();
                using var localFileStream = File.OpenWrite(newFile);
                await sourceStream.CopyToAsync(localFileStream);

                _currentImagePath = newFile;
                CapturedImage.Source = ImageSource.FromFile(newFile);
                PlaceholderStack.IsVisible = false;
                OverlayGraphicsView.IsVisible = true;

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
                    var estimatedFocalLength = 4.0;
                    _phoneFOV = _fovCalculationService.CalculateHorizontalFOV(estimatedFocalLength, sensorResult.Data.Width);
                }
                else
                {
                    _phoneFOV = 78.0;
                }

                UpdateFOVDisplay();
                _logger.LogInformation("Estimated phone FOV: {FOV}°", _phoneFOV);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error estimating phone FOV");
                _phoneFOV = 78.0;
                UpdateFOVDisplay();
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

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CameraDisplayItem
    {
        public CameraBodyDto? Camera { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsMissingOption { get; set; }
    }

    public class LensDisplayItem
    {
        public LensDto? Lens { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public bool IsMissingOption { get; set; }
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

            var phoneFOVBox = CalculateFOVBox(PhoneFOV, dirtyRect);
            DrawFOVBox(canvas, phoneFOVBox, Colors.Cyan, "Phone Camera");

            if (SelectedCameraFOV > 0)
            {
                var cameraFOVBox = CalculateFOVBox(SelectedCameraFOV, dirtyRect);
                DrawFOVBox(canvas, cameraFOVBox, Colors.Orange, "Selected Camera");
            }
        }

        private RectF CalculateFOVBox(double fov, RectF containerRect)
        {
            var referenceFOV = Math.Max(PhoneFOV, 60.0);
            var scaleFactor = fov / referenceFOV;

            var maxWidth = containerRect.Width * 0.8f;
            var maxHeight = containerRect.Height * 0.8f;

            var boxWidth = Math.Max(100, Math.Min(maxWidth, maxWidth * scaleFactor));
            var boxHeight = boxWidth * 0.6f;

            var x = (containerRect.Width - boxWidth) / 2;
            var y = (containerRect.Height - boxHeight) / 2;

            return new RectF((float)x, (float)y, (float)boxWidth, (float)boxHeight);
        }

        private void DrawFOVBox(ICanvas canvas, RectF box, Color color, string label)
        {
            canvas.StrokeColor = color;
            canvas.StrokeSize = 3;
            canvas.DrawRectangle(box);

            var cornerSize = 20;
            DrawCorner(canvas, box.Left, box.Top, cornerSize, color);
            DrawCorner(canvas, box.Right - cornerSize, box.Top, cornerSize, color);
            DrawCorner(canvas, box.Left, box.Bottom - cornerSize, cornerSize, color);
            DrawCorner(canvas, box.Right - cornerSize, box.Bottom - cornerSize, cornerSize, color);

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
            canvas.DrawLine(x, y, x + size, y);
            canvas.DrawLine(x, y, x, y + size);
        }
    }

}