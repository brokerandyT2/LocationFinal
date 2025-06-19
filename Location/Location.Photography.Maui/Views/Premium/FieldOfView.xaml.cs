using Camera.MAUI;
using Location.Core.Application.Services;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Notifications;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class FieldOfView : ContentPage, INotifyPropertyChanged, INotificationHandler<CameraCreatedNotification>,
       INotificationHandler<LensCreatedNotification>, IDisposable
    {
        private readonly IMediator _mediator;
        private readonly ILogger<FieldOfView> _logger;
        private readonly IFOVCalculationService _fovCalculationService;
        private readonly IAlertService _alertService;
        private readonly ICameraDataService _cameraDataService;
        private readonly ICameraSensorProfileService _cameraSensorProfileService;
        private readonly IUserCameraBodyRepository _userCameraBodyRepository;
        private readonly IServiceProvider _serviceProvider;

        private double _selectedCameraFOV = 0;
        private bool _isCalculating = false;
        private string _currentImagePath = string.Empty;
        private string _currentUserId = string.Empty;
        private double _imageWidth = 0;
        private double _imageHeight = 0;
        private bool _disposed = false;
        private bool _cameraInitialized = false;

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
                _ = Task.Run(async () => await OnCameraSelectionChanged());
            }
        }

        public LensDisplayItem? SelectedLens
        {
            get => _selectedLens;
            set
            {
                _selectedLens = value;
                OnPropertyChanged();
                _ = Task.Run(async () => await OnLensSelectionChanged());
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public FieldOfView(
            IMediator mediator,
            ILogger<FieldOfView> logger,
            IFOVCalculationService fovCalculationService,
            IAlertService alertService,
            ICameraDataService cameraDataService,
            ICameraSensorProfileService cameraSensorProfileService,
            IUserCameraBodyRepository userCameraBodyRepository,
            IServiceProvider serviceProvider)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fovCalculationService = fovCalculationService ?? throw new ArgumentNullException(nameof(fovCalculationService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _cameraDataService = cameraDataService ?? throw new ArgumentNullException(nameof(cameraDataService));
            _cameraSensorProfileService = cameraSensorProfileService;
            _userCameraBodyRepository = userCameraBodyRepository ?? throw new ArgumentNullException(nameof(userCameraBodyRepository));
            _serviceProvider = serviceProvider;

            InitializeComponent();
            BindingContext = this;
            InitializeFieldOfView();
            _ = LoadCurrentUserIdAsync();
            _ = LoadCamerasAsync();

            // Use weak event handler to prevent memory leaks
            CameraPreview.CamerasLoaded += CameraView_CamerasLoaded;
        }

        #region Camera Management Methods

        public void PauseCamera()
        {
            if (_disposed) return;

            _logger.LogInformation("Pausing camera");
            _cameraInitialized = false;

            // Fire and forget - don't block
            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
                    if (CameraPreview != null)
                    {
                        await CameraPreview.StopCameraAsync().WaitAsync(cts.Token);
                        _logger.LogInformation("Camera paused successfully");
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Camera pause timed out");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Camera pause failed (ignored)");
                }
            });
        }

        public async Task ResumeCameraAsync()
        {
            if (_disposed) return;

            try
            {
                _logger.LogInformation("Resuming camera");

                if (CameraPreview?.Camera != null && !_cameraInitialized)
                {
                    await CameraPreview.StartCameraAsync();
                    _cameraInitialized = true;
                    _logger.LogInformation("Camera resumed successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Camera resume failed");
            }
        }

        private async void CameraView_CamerasLoaded(object? sender, EventArgs e)
        {
            try
            {
                // Safety check - don't initialize if disposed
                if (_disposed)
                {
                    _logger.LogInformation("Page disposed, skipping camera initialization");
                    return;
                }

                foreach (var x in CameraPreview.Cameras)
                {
                    if (x.Position == CameraPosition.Back)
                    {
                        CameraPreview.Camera = x;

                        // Get actual camera resolution and calculate real aspect ratio
                        if (x.AvailableResolutions != null && x.AvailableResolutions.Any())
                        {
                            // Use the highest resolution available (usually the native resolution)
                            var maxResolution = x.AvailableResolutions.OrderByDescending(r => r.Width * r.Height).First();
                            var cameraAspectRatio = (double)maxResolution.Width / maxResolution.Height;

                            var containerWidth = 500.0; // From WidthRequest
                            var calculatedHeight = containerWidth / cameraAspectRatio;

                            _imageWidth = containerWidth;
                            _imageHeight = calculatedHeight;

                            _logger.LogInformation("Camera resolution: {Width}x{Height}, Aspect ratio: {Ratio:F2}, Calculated height: {Height:F1}",
                                maxResolution.Width, maxResolution.Height, cameraAspectRatio, calculatedHeight);
                        }
                        else
                        {
                            // Fallback to common mobile camera aspect ratio
                            var fallbackAspectRatio = 16.0 / 9.0;
                            _imageWidth = 500.0;
                            _imageHeight = 500.0 / fallbackAspectRatio;

                            _logger.LogWarning("No camera resolutions available, using fallback 16:9 aspect ratio");
                        }

                        break;
                    }
                }

                // Only start if not disposed
                if (!_disposed)
                {
                    await CameraPreview.StartCameraAsync();
                    _cameraInitialized = true;
                    _logger.LogInformation("Camera started successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CameraView_CamerasLoaded");
            }
        }

        #endregion

        #region Navigation Events (Custom Tab Implementation)

        // These methods should be called by your custom tab system
        public async void OnNavigatedToAsync()
        {
            try
            {
                if (_disposed) return;

                _logger.LogInformation("Navigated to Field of View page");
                await LoadCamerasAsync();

                // Camera will auto-start when CamerasLoaded event fires
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during navigation to page");
            }
        }

        public void OnNavigatedFromAsync()
        {
            try
            {
                _logger.LogInformation("Navigating away from Field of View page");
                PauseCamera(); // Non-blocking camera stop
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during navigation away from page");
            }
        }

        #endregion

        #region Event Handlers

        private async void OnCameraManagementClicked(object sender, EventArgs e)
        {
            try
            {
                var cameraManagementPage = _serviceProvider.GetRequiredService<CameraLensManagement>();
                await Shell.Current.Navigation.PushModalAsync(cameraManagementPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to Camera Management page");
                await _alertService.ShowErrorAlertAsync("Error opening Camera Management", "Error");
            }
        }

        public async Task Handle(CameraCreatedNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Reload cameras to include the new one
                    await LoadCamerasAsync(false);

                    // Automatically select the newly created camera
                    var newCamera = AvailableCameras.FirstOrDefault(c => c.Camera?.Id == notification.CreatedCamera.Id);
                    if (newCamera != null)
                    {
                        SelectedCamera = newCamera;
                        CameraPicker.SelectedItem = newCamera;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling camera created notification");
            }
        }

        public async Task Handle(LensCreatedNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    // Reload lenses to include the new one
                    await LoadLensesAsync(false);

                    // Automatically select the newly created lens
                    var newLens = AvailableLenses.FirstOrDefault(l => l.Lens?.Id == notification.CreatedLens.Id);
                    if (newLens != null)
                    {
                        SelectedLens = newLens;
                        LensPicker.SelectedItem = newLens;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling lens created notification");
            }
        }

        private async void OnCameraSelectionChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker && picker.SelectedItem is CameraDisplayItem selectedCamera)
            {
                SelectedCamera = selectedCamera; // This will trigger the property setter and async method
            }
        }

        private async Task OnCameraSelectionChanged()
        {
            try
            {
                if (_disposed) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    // Reset lens selection
                    SelectedLens = null;
                    AvailableLenses.Clear();
                    LensPicker.IsEnabled = false;
                });

                if (SelectedCamera?.Camera != null)
                {
                    await LoadLensesAsync();
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    UpdateFOVDisplay();
                    UpdateOverlay();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling camera selection");
            }
        }

        private async void OnLensSelectionChanged(object sender, EventArgs e)
        {
            if (sender is Picker picker && picker.SelectedItem is LensDisplayItem selectedLens)
            {
                SelectedLens = selectedLens; // This will trigger the property setter and async method
            }
        }

        private async Task OnLensSelectionChanged()
        {
            try
            {
                if (_disposed) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (SelectedLens?.Lens != null)
                    {
                        CalculateSelectedCameraFOV();
                        OverlayGraphicsView.IsVisible = true;
                    }
                    else
                    {
                        _selectedCameraFOV = 0;
                        OverlayGraphicsView.IsVisible = false;
                    }

                    UpdateFOVDisplay();
                    UpdateOverlay();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling lens selection");
            }
        }

        #endregion

        #region Data Loading Methods

        private async Task LoadCurrentUserIdAsync()
        {
            try
            {
                _currentUserId = await SecureStorage.GetAsync("Email") ?? "default_user";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading current user ID");
                _currentUserId = "default_user";
            }
        }

        private async Task LoadCamerasAsync(bool loadMore = false)
        {
            if (_isLoadingCameras || _disposed) return;

            try
            {
                _isLoadingCameras = true;
                await MainThread.InvokeOnMainThreadAsync(() => ProcessingOverlay.IsVisible = true);

                // Store current selection to restore after reload
                var currentSelectedCameraId = SelectedCamera?.Camera?.Id;

                if (!loadMore)
                {
                    _currentCameraSkip = 0;
                    await MainThread.InvokeOnMainThreadAsync(() => AvailableCameras.Clear());
                }

                // Load user cameras first (these will show with "*" prefix)
                var userResult = await _cameraDataService.GetUserCameraBodiesAsync(_currentUserId, 0, int.MaxValue);
                var userCameraIds = new HashSet<int>();

                if (userResult.IsSuccess)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        foreach (var camera in userResult.Data.CameraBodies.OrderBy(c => c.DisplayName))
                        {
                            userCameraIds.Add(camera.Id);
                            AvailableCameras.Add(new CameraDisplayItem
                            {
                                Camera = camera,
                                DisplayName = "* " + camera.DisplayName,
                                IsMissingOption = false
                            });
                        }
                    });
                }

                // Load all cameras (excluding user cameras to avoid duplicates)
                var allCamerasResult = await _cameraDataService.GetCameraBodiesAsync(0, int.MaxValue);
                if (allCamerasResult.IsSuccess)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        foreach (var camera in allCamerasResult.Data.CameraBodies.OrderBy(c => c.DisplayName))
                        {
                            // Skip if this camera is already in user cameras
                            if (!userCameraIds.Contains(camera.Id))
                            {
                                AvailableCameras.Add(new CameraDisplayItem
                                {
                                    Camera = camera,
                                    DisplayName = camera.DisplayName,
                                    IsMissingOption = false
                                });
                            }
                        }

                        // Restore selection if possible
                        if (currentSelectedCameraId.HasValue)
                        {
                            var cameraToSelect = AvailableCameras.FirstOrDefault(c => c.Camera?.Id == currentSelectedCameraId.Value);
                            if (cameraToSelect != null)
                            {
                                SelectedCamera = cameraToSelect;
                                CameraPicker.SelectedItem = cameraToSelect;
                            }
                        }
                        else
                        {
                            CameraPicker.SelectedIndex = -1;
                        }
                    });
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync("Failed to load cameras", "Error");
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
                await MainThread.InvokeOnMainThreadAsync(() => ProcessingOverlay.IsVisible = false);
            }
        }

        private async Task LoadLensesAsync(bool loadMore = false)
        {
            if (_isLoadingLenses || SelectedCamera?.Camera == null || _disposed) return;

            try
            {
                _isLoadingLenses = true;
                await MainThread.InvokeOnMainThreadAsync(() => ProcessingOverlay.IsVisible = true);

                // Store current selection to restore after reload
                var currentSelectedLensId = SelectedLens?.Lens?.Id;

                if (!loadMore)
                {
                    _currentLensSkip = 0;
                    await MainThread.InvokeOnMainThreadAsync(() => AvailableLenses.Clear());
                }

                // Load all lenses compatible with the selected camera
                var result = await _cameraDataService.GetLensesAsync(_currentLensSkip, _pageSize, false, SelectedCamera.Camera.Id);
                if (result.IsSuccess)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
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
                        _currentLensSkip += _pageSize;
                        LensPicker.IsEnabled = true;

                        // Restore selection if possible
                        if (currentSelectedLensId.HasValue)
                        {
                            var lensToSelect = AvailableLenses.FirstOrDefault(l => l.Lens?.Id == currentSelectedLensId.Value);
                            if (lensToSelect != null)
                            {
                                SelectedLens = lensToSelect;
                                LensPicker.SelectedItem = lensToSelect;
                            }
                        }
                    });
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync("Failed to load lenses", "Error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lenses");
                await _alertService.ShowErrorAlertAsync("Error loading lenses", "Error");
            }
            finally
            {
                _isLoadingLenses = false;
                await MainThread.InvokeOnMainThreadAsync(() => ProcessingOverlay.IsVisible = false);
            }
        }

        #endregion

        #region Button Click Handlers

        private async void OnLoadMoreCamerasClicked(object sender, EventArgs e)
        {
            await LoadCamerasAsync(true);
        }

        private async void OnLoadMoreLensesClicked(object sender, EventArgs e)
        {
            await LoadLensesAsync(true);
        }

        private async void OnAddCameraClicked(object sender, EventArgs e)
        {
            try
            {
                var addCameraModal = _serviceProvider.GetRequiredService<AddCameraModal>();
                await Shell.Current.Navigation.PushModalAsync(addCameraModal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to Add Camera modal");
                await _alertService.ShowErrorAlertAsync("Error opening Add Camera", "Error");
            }
        }

        private async void OnAddLensClicked(object sender, EventArgs e)
        {
            try
            {
                var addLensModal = _serviceProvider.GetRequiredService<AddLensModal>();
                await Shell.Current.Navigation.PushModalAsync(addLensModal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to Add Lens modal");
                await _alertService.ShowErrorAlertAsync("Error opening Add Lens", "Error");
            }
        }

        private async void OnSaveCameraClicked(object sender, EventArgs e)
        {
            try
            {
                if (SelectedCamera?.Camera == null || SelectedCamera.IsMissingOption)
                {
                    await _alertService.ShowErrorAlertAsync("Please select a camera to save", "No Camera Selected");
                    return;
                }

                if (string.IsNullOrEmpty(_currentUserId))
                {
                    await _alertService.ShowErrorAlertAsync("User not logged in", "Error");
                    return;
                }

                ProcessingOverlay.IsVisible = true;

                // Check if camera is already saved
                var existsResult = await _userCameraBodyRepository.ExistsAsync(_currentUserId, SelectedCamera.Camera.Id);
                if (existsResult.IsSuccess && existsResult.Data)
                {
                    await _alertService.ShowInfoAlertAsync("This camera is already in your saved list", "Camera Already Saved");
                    return;
                }

                // Save the camera
                var userCameraBody = new UserCameraBody(SelectedCamera.Camera.Id, _currentUserId);
                var saveResult = await _userCameraBodyRepository.CreateAsync(userCameraBody);

                if (saveResult.IsSuccess)
                {
                    await _alertService.ShowSuccessAlertAsync($"Camera '{SelectedCamera.Camera.DisplayName}' has been saved to your collection!", "Camera Saved");
                    _logger.LogInformation("User {UserId} saved camera {CameraId}", _currentUserId, SelectedCamera.Camera.Id);
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync(saveResult.ErrorMessage ?? "Failed to save camera", "Save Failed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving camera");
                await _alertService.ShowErrorAlertAsync("Error saving camera", "Error");
            }
            finally
            {
                ProcessingOverlay.IsVisible = false;
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

        #endregion

        #region Helper Methods

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

        private void CalculateSelectedCameraFOV()
        {
            try
            {
                if (SelectedCamera?.Camera == null || SelectedLens?.Lens == null) return;

                var sensorWidth = SelectedCamera.Camera.SensorWidth;

                // Calculate wide FOV (minimum focal length, maximum aperture)
                var wideFOV = _fovCalculationService.CalculateHorizontalFOV(SelectedLens.Lens.MinMM, sensorWidth);

                // Calculate telephoto FOV (maximum focal length, minimum aperture)
                var telephotoFOV = wideFOV; // Default for prime lenses
                if (SelectedLens.Lens.MaxMM.HasValue && SelectedLens.Lens.MaxMM.Value > SelectedLens.Lens.MinMM)
                {
                    telephotoFOV = _fovCalculationService.CalculateHorizontalFOV(SelectedLens.Lens.MaxMM.Value, sensorWidth);
                }

                // Update the drawable with both FOV values and image dimensions
                if (OverlayGraphicsView.Drawable is FOVOverlayDrawable drawable)
                {
                    drawable.WideFOV = wideFOV;
                    drawable.TelephotoFOV = telephotoFOV;
                    drawable.IsPrimeLens = !SelectedLens.Lens.MaxMM.HasValue || SelectedLens.Lens.MaxMM.Value <= SelectedLens.Lens.MinMM;
                }

                _selectedCameraFOV = wideFOV; // Use wide FOV for display
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating selected camera FOV");
                _selectedCameraFOV = 0;
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

                // Extract actual image dimensions from EXIF or file
                using var imageStream = await photo.OpenReadAsync();
                await GetImageDimensionsAsync(imageStream);

                _currentImagePath = newFile;
                OverlayGraphicsView.IsVisible = true;

                UpdateFOVDisplay();
                UpdateOverlay();

                _logger.LogInformation("Photo captured and displayed: {ImagePath}, Dimensions: {Width}x{Height}", newFile, _imageWidth, _imageHeight);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying captured image");
                await _alertService.ShowErrorAlertAsync("Error processing captured photo", "Error");
            }
        }

        private async Task GetImageDimensionsAsync(Stream imageStream)
        {
            try
            {
                // Use SkiaSharp to get actual image dimensions
                using var skiaStream = new SKManagedStream(imageStream);
                using var codec = SKCodec.Create(skiaStream);

                if (codec != null)
                {
                    _imageWidth = codec.Info.Width;
                    _imageHeight = codec.Info.Height;
                }
                else
                {
                    // Fallback to typical mobile camera dimensions
                    _imageWidth = 4000;
                    _imageHeight = 3000;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not extract image dimensions, using defaults");
                // Fallback to typical mobile camera dimensions
                _imageWidth = 4000;
                _imageHeight = 3000;
            }
        }

        private void UpdateFOVDisplay()
        {
            try
            {
                if (_selectedCameraFOV > 0)
                {
                    CameraFOVLabel.Text = $"Selected FOV: {_selectedCameraFOV:F1}°";
                    ComparisonLabel.Text = "Field of view calculated for selected lens";
                }
                else
                {
                    CameraFOVLabel.Text = "Selected FOV: --";
                    ComparisonLabel.Text = "Select a lens to see field of view";
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
                    OverlayGraphicsView.Invalidate();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating overlay");
            }
        }

        private void CleanupTempFiles()
        {
            try
            {
                if (!string.IsNullOrEmpty(_currentImagePath) && File.Exists(_currentImagePath))
                {
                    File.Delete(_currentImagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete temporary image file: {ImagePath}", _currentImagePath);
            }
        }

        #endregion

        #region Lifecycle and Disposal

        protected override void OnAppearing()
        {
            base.OnAppearing();
            // Don't put camera logic here since this is a custom tab implementation
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            CleanupTempFiles();
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true; // Set immediately to prevent re-entry

                if (disposing)
                {
                    try
                    {
                        // Immediately disconnect event handler to prevent new camera operations
                        CameraPreview.CamerasLoaded -= CameraView_CamerasLoaded;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error disconnecting camera event handler");
                    }

                    // Fire and forget camera cleanup - don't block disposal
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(500)); // Very short timeout
                            if (CameraPreview != null)
                            {
                                await CameraPreview.StopCameraAsync().WaitAsync(cts.Token);
                                _logger.LogInformation("Camera disposed successfully");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Camera disposal failed (ignored)");
                        }
                    });

                    // Clean up temp files synchronously
                    CleanupTempFiles();
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this); // Prevent finalizer from running
        }

        #endregion
    }

    #region Helper Classes

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
        public double WideFOV { get; set; }
        public double TelephotoFOV { get; set; }
        public bool IsPrimeLens { get; set; }
        public double ImageWidth { get; set; }
        public double ImageHeight { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (WideFOV <= 0) return;

            var imageRect = dirtyRect; // Simplified - no aspect ratio calculation needed

            var referenceFOV = 60.0;
            var outerBox = CalculateFOVBox(WideFOV, imageRect, referenceFOV);
            var innerBox = CalculateFOVBox(TelephotoFOV, imageRect, referenceFOV);

            DrawSolidRectangle(canvas, outerBox, Colors.Red);

            if (!IsPrimeLens && TelephotoFOV > 0 && TelephotoFOV != WideFOV)
            {
                FillAreaBetweenBoxes(canvas, outerBox, innerBox);
                DrawCornerBrackets(canvas, innerBox, Colors.Red);
            }
        }

        private void FillAreaBetweenBoxes(ICanvas canvas, RectF outerBox, RectF innerBox)
        {
            // Create 10% alpha gray color
            var fillColor = Color.FromRgba(128, 128, 128, 128);

            canvas.FillColor = fillColor;

            // Create a path that represents the area between the two rectangles
            var path = new PathF();

            // Add outer rectangle
            path.MoveTo(outerBox.Left, outerBox.Top);
            path.LineTo(outerBox.Right, outerBox.Top);
            path.LineTo(outerBox.Right, outerBox.Bottom);
            path.LineTo(outerBox.Left, outerBox.Bottom);
            path.Close();

            // Subtract inner rectangle (hole)
            path.MoveTo(innerBox.Left, innerBox.Top);
            path.LineTo(innerBox.Left, innerBox.Bottom);
            path.LineTo(innerBox.Right, innerBox.Bottom);
            path.LineTo(innerBox.Right, innerBox.Top);
            path.Close();

            canvas.FillPath(path);
        }

        private RectF CalculateFOVBox(double fov, RectF imageRect, double referenceFOV)
        {
            var scaleFactor = fov / referenceFOV;

            // Calculate box size as percentage of image area
            var boxWidthPercent = Math.Max(0.3, Math.Min(0.9, scaleFactor));
            var boxHeightPercent = boxWidthPercent; // Keep square-ish for now

            var boxWidth = imageRect.Width * (float)boxWidthPercent;
            var boxHeight = imageRect.Height * (float)boxHeightPercent;

            // Center the box within the image area
            var x = imageRect.Left + (imageRect.Width - boxWidth) / 2;
            var y = imageRect.Top + (imageRect.Height - boxHeight) / 2;

            return new RectF(x, y, boxWidth, boxHeight);
        }

        private void DrawSolidRectangle(ICanvas canvas, RectF box, Color color)
        {
            canvas.StrokeColor = color;
            canvas.StrokeSize = 3;
            canvas.DrawRectangle(box);
        }

        private void DrawCornerBrackets(ICanvas canvas, RectF box, Color color)
        {
            canvas.StrokeColor = color;
            canvas.StrokeSize = 3;

            var cornerSize = Math.Min(25f, Math.Min(box.Width, box.Height) * 0.2f);

            // Top-left corner
            canvas.DrawLine(box.Left, box.Top, box.Left + cornerSize, box.Top);
            canvas.DrawLine(box.Left, box.Top, box.Left, box.Top + cornerSize);

            // Top-right corner
            canvas.DrawLine(box.Right - cornerSize, box.Top, box.Right, box.Top);
            canvas.DrawLine(box.Right, box.Top, box.Right, box.Top + cornerSize);

            // Bottom-left corner
            canvas.DrawLine(box.Left, box.Bottom - cornerSize, box.Left, box.Bottom);
            canvas.DrawLine(box.Left, box.Bottom, box.Left + cornerSize, box.Bottom);

            // Bottom-right corner
            canvas.DrawLine(box.Right - cornerSize, box.Bottom, box.Right, box.Bottom);
            canvas.DrawLine(box.Right, box.Bottom - cornerSize, box.Right, box.Bottom);
        }
    }

    #endregion
}