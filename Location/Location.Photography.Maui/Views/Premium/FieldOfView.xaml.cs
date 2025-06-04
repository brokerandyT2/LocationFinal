// Location.Photography.Maui/Views/Premium/FieldOfView.xaml.cs

using Camera.MAUI;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class FieldOfView : ContentPage
    {
        private readonly IPhoneCameraProfileRepository _profileRepository;
        private readonly IFOVCalculationService _fovCalculationService;
        private readonly ILogger<FieldOfView> _logger;
        private readonly IServiceProvider _serviceProvider;

        private PhoneCameraProfile _phoneProfile;
        private CameraBody _selectedCamera;
        private UserLens _selectedLens;
        private bool _overlayVisible = true;
        private double _currentDistance = 5.0;
        private bool _hasValidProfile = false;

        public FieldOfView()
        {
            InitializeComponent();
        }

        public FieldOfView(
            IPhoneCameraProfileRepository profileRepository,
            IFOVCalculationService fovCalculationService,
            ILogger<FieldOfView> logger,
            IServiceProvider serviceProvider)
        {
            _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
            _fovCalculationService = fovCalculationService ?? throw new ArgumentNullException(nameof(fovCalculationService));
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
                        "Camera access is required for FOV preview.",
                        "OK");
                    return;
                }

                // Load phone camera profile
                await LoadPhoneCameraProfileAsync();

                // Initialize camera
                if (_hasValidProfile && CameraView != null)
                {
                    CameraView.AutoStartPreview = true;
                    ModeLabel.Text = "Live FOV Preview";
                }
                else
                {
                    ShowFallbackMode();
                }

                // Update initial overlay
                UpdateFOVOverlay();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error initializing FOV preview");
                ShowFallbackMode();
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                if (CameraView != null)
                {
                    CameraView.AutoStartPreview = false;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Error stopping camera preview");
            }
        }

        private async Task LoadPhoneCameraProfileAsync()
        {
            try
            {
                var profileResult = await _profileRepository.GetActiveProfileAsync();

                if (profileResult.IsSuccess && profileResult.Data != null)
                {
                    _phoneProfile = profileResult.Data;
                    _hasValidProfile = true;

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        var phoneInfo = $"Phone: {_phoneProfile.MainLensFocalLength:F0}mm ({_phoneProfile.MainLensFOV:F0}°)";
                        FOVInfoLabel.Text = phoneInfo + " | Camera: Select equipment";
                    });

                    _logger?.LogInformation("Loaded phone profile: {PhoneModel}, {FocalLength}mm, {FOV}°",
                        _phoneProfile.PhoneModel, _phoneProfile.MainLensFocalLength, _phoneProfile.MainLensFOV);
                }
                else
                {
                    _hasValidProfile = false;
                    _logger?.LogWarning("No active phone camera profile found");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error loading phone camera profile");
                _hasValidProfile = false;
            }
        }

        private void ShowFallbackMode()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FallbackOverlay.IsVisible = true;
                ModeLabel.Text = "Educational Mode";

                if (CameraView != null)
                {
                    CameraView.AutoStartPreview = false;
                }
            });
        }

        private void UpdateFOVOverlay()
        {
            if (!_hasValidProfile || _phoneProfile == null || _selectedCamera == null || _selectedLens == null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    FOVOverlayBox.IsVisible = false;
                    NearFocusFrame.IsVisible = false;
                    FarFocusFrame.IsVisible = false;
                });
                return;
            }

            try
            {
                // Calculate camera FOV for current lens setting
                var cameraFOV = _fovCalculationService.CalculateHorizontalFOV(
                    _selectedLens.MinFocalLength,
                    _selectedCamera.SensorWidth);

                // Calculate overlay dimensions
                var screenSize = new Size(
                    (int)DeviceDisplay.MainDisplayInfo.Width,
                    (int)DeviceDisplay.MainDisplayInfo.Height);

                var overlayBox = _fovCalculationService.CalculateOverlayBox(
                    _phoneProfile.MainLensFOV,
                    cameraFOV,
                    screenSize);

                // Calculate depth of field markers
                var dofCalculation = CalculateDepthOfField(_selectedLens, _selectedCamera, _currentDistance);

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    if (_overlayVisible)
                    {
                        // Update FOV overlay box
                        Canvas.SetLeft(FOVOverlayBox, overlayBox.X);
                        Canvas.SetTop(FOVOverlayBox, overlayBox.Y);
                        FOVOverlayBox.WidthRequest = overlayBox.Width;
                        FOVOverlayBox.HeightRequest = overlayBox.Height;
                        FOVOverlayBox.IsVisible = true;

                        // Update focus markers
                        UpdateFocusMarkers(dofCalculation, overlayBox);
                    }
                    else
                    {
                        FOVOverlayBox.IsVisible = false;
                        NearFocusFrame.IsVisible = false;
                        FarFocusFrame.IsVisible = false;
                    }

                    // Update info display
                    var cameraInfo = $"Camera: {_selectedLens.MinFocalLength}mm f/{_selectedLens.MinAperture} ({cameraFOV:F0}°)";
                    var phoneInfo = $"Phone: {_phoneProfile.MainLensFocalLength:F0}mm ({_phoneProfile.MainLensFOV:F0}°)";
                    FOVInfoLabel.Text = phoneInfo + " | " + cameraInfo;
                });
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error updating FOV overlay");
            }
        }

        private void UpdateFocusMarkers(DepthOfFieldResult dof, OverlayBox overlayBox)
        {
            if (dof != null)
            {
                // Position near focus marker (top-left of overlay)
                Canvas.SetLeft(NearFocusFrame, overlayBox.X + 10);
                Canvas.SetTop(NearFocusFrame, overlayBox.Y + 10);
                NearFocusLabel.Text = $"Near: {dof.NearDistance:F1}m";
                NearFocusFrame.IsVisible = true;

                // Position far focus marker (bottom-right of overlay)
                Canvas.SetLeft(FarFocusFrame, overlayBox.X + overlayBox.Width - 100);
                Canvas.SetTop(FarFocusFrame, overlayBox.Y + overlayBox.Height - 40);
                FarFocusLabel.Text = $"Far: {dof.FarDistance:F1}m";
                FarFocusFrame.IsVisible = true;
            }
            else
            {
                NearFocusFrame.IsVisible = false;
                FarFocusFrame.IsVisible = false;
            }
        }

        private DepthOfFieldResult CalculateDepthOfField(UserLens lens, CameraBody camera, double distance)
        {
            try
            {
                // Simplified DOF calculation
                var focalLength = lens.MinFocalLength;
                var aperture = lens.MinAperture;
                var sensorWidth = camera.SensorWidth;

                // Circle of confusion for sensor size
                var coc = sensorWidth / 1500.0; // Simplified COC calculation

                // Hyperfocal distance
                var hyperfocal = (focalLength * focalLength) / (aperture * coc * 1000);

                // Near and far distances
                var nearDistance = (distance * hyperfocal) / (hyperfocal + distance);
                var farDistance = distance >= hyperfocal ? double.PositiveInfinity : (distance * hyperfocal) / (hyperfocal - distance);

                return new DepthOfFieldResult
                {
                    NearDistance = Math.Max(0.1, nearDistance),
                    FarDistance = Math.Min(50, farDistance),
                    SubjectDistance = distance,
                    HyperfocalDistance = hyperfocal
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error calculating depth of field");
                return null;
            }
        }

        private async void OnCameraButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Show camera selection (would typically open a picker)
                var cameras = GetSampleCameras();
                var cameraNames = cameras.Select(c => $"{c.Manufacturer} {c.Model}").ToArray();

                var action = await DisplayActionSheet("Select Camera", "Cancel", null, cameraNames);

                if (action != "Cancel" && action != null)
                {
                    var selectedIndex = Array.IndexOf(cameraNames, action);
                    if (selectedIndex >= 0)
                    {
                        _selectedCamera = cameras[selectedIndex];
                        UpdateFOVOverlay();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error selecting camera");
                await DisplayAlert("Error", "Unable to select camera", "OK");
            }
        }

        private async void OnLensButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Show lens selection (would typically open a picker)
                var lenses = GetSampleLenses();
                var lensNames = lenses.Select(l => l.Name).ToArray();

                var action = await DisplayActionSheet("Select Lens", "Cancel", null, lensNames);

                if (action != "Cancel" && action != null)
                {
                    var selectedIndex = Array.IndexOf(lensNames, action);
                    if (selectedIndex >= 0)
                    {
                        _selectedLens = lenses[selectedIndex];
                        UpdateFOVOverlay();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error selecting lens");
                await DisplayAlert("Error", "Unable to select lens", "OK");
            }
        }

        private void OnToggleOverlayClicked(object sender, EventArgs e)
        {
            _overlayVisible = !_overlayVisible;
            ToggleButton.Text = _overlayVisible ? "👁️ Hide" : "👁️ Show";
            UpdateFOVOverlay();
        }

        private void OnDistanceChanged(object sender, ValueChangedEventArgs e)
        {
            _currentDistance = e.NewValue;
            DistanceLabel.Text = $"{_currentDistance:F1}m";
            UpdateFOVOverlay();
        }

        private async void OnSettingsButtonClicked(object sender, EventArgs e)
        {
            try
            {
                // Navigate to camera/lens management settings
                await DisplayAlert("Settings", "Camera and lens management coming soon", "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening settings");
            }
        }

        private async void OnOpenCalculatorClicked(object sender, EventArgs e)
        {
            try
            {
                // Navigate to DOF calculator
                await DisplayAlert("Calculator", "DOF calculator coming soon", "OK");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening calculator");
            }
        }

        private async void OnCalibrateButtonClicked(object sender, EventArgs e)
        {
            try
            {
                var cameraEvaluation = _serviceProvider?.GetService<CameraEvaluation>();
                if (cameraEvaluation != null)
                {
                    await Navigation.PushAsync(cameraEvaluation);
                }
                else
                {
                    await DisplayAlert("Calibration", "Camera calibration page not available", "OK");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error opening camera calibration");
                await DisplayAlert("Error", "Unable to open camera calibration", "OK");
            }
        }

        // Sample data - in production these would come from database
        private List<CameraBody> GetSampleCameras()
        {
            return new List<CameraBody>
            {
                new CameraBody { Manufacturer = "Canon", Model = "EOS R5", SensorWidth = 36.0, SensorHeight = 24.0 },
                new CameraBody { Manufacturer = "Nikon", Model = "D850", SensorWidth = 35.9, SensorHeight = 23.9 },
                new CameraBody { Manufacturer = "Sony", Model = "A7 IV", SensorWidth = 35.7, SensorHeight = 23.8 },
                new CameraBody { Manufacturer = "Canon", Model = "EOS 90D", SensorWidth = 22.3, SensorHeight = 14.8 }
            };
        }

        private List<UserLens> GetSampleLenses()
        {
            return new List<UserLens>
            {
                new UserLens { Name = "24-70mm f/2.8", MinFocalLength = 24, MaxFocalLength = 70, MinAperture = 2.8, MaxAperture = 16 },
                new UserLens { Name = "70-200mm f/2.8", MinFocalLength = 70, MaxFocalLength = 200, MinAperture = 2.8, MaxAperture = 16 },
                new UserLens { Name = "50mm f/1.4", MinFocalLength = 50, MaxFocalLength = 50, MinAperture = 1.4, MaxAperture = 16 },
                new UserLens { Name = "85mm f/1.8", MinFocalLength = 85, MaxFocalLength = 85, MinAperture = 1.8, MaxAperture = 16 }
            };
        }
    }

    // Helper classes for sample data
    public class CameraBody
    {
        public string Manufacturer { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public double SensorWidth { get; set; }
        public double SensorHeight { get; set; }
    }

    public class UserLens
    {
        public string Name { get; set; } = string.Empty;
        public int MinFocalLength { get; set; }
        public int MaxFocalLength { get; set; }
        public double MinAperture { get; set; }
        public double MaxAperture { get; set; }
    }

    public class DepthOfFieldResult
    {
        public double NearDistance { get; set; }
        public double FarDistance { get; set; }
        public double SubjectDistance { get; set; }
        public double HyperfocalDistance { get; set; }
    }
}