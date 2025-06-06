using Location.Core.Application.Services;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class AddLensModal : ContentPage, INotifyPropertyChanged
    {
        private readonly ICameraDataService _cameraDataService;
        private readonly IAlertService _alertService;
        private readonly ILogger<AddLensModal> _logger;

        private ObservableCollection<CameraSelectionItem> _availableCameras = new();
        private bool _isLoadingCameras = false;
        private int _currentSkip = 0;
        private const int _pageSize = 20;
        private bool _hasMoreCameras = true;
        private bool _isUserEditingName = false;

        public ObservableCollection<CameraSelectionItem> AvailableCameras
        {
            get => _availableCameras;
            set
            {
                _availableCameras = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddLensModal(
            ICameraDataService cameraDataService,
            IAlertService alertService,
            ILogger<AddLensModal> logger)
        {
            _cameraDataService = cameraDataService ?? throw new ArgumentNullException(nameof(cameraDataService));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            InitializeComponent();
            BindingContext = this;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCamerasAsync();
        }

        private async Task LoadCamerasAsync(bool loadMore = false)
        {
            if (_isLoadingCameras) return;

            try
            {
                _isLoadingCameras = true;

                if (!loadMore)
                {
                    _currentSkip = 0;
                    AvailableCameras.Clear();
                }

                // Get current user ID
                var currentUserId = await SecureStorage.GetAsync("Email") ?? "default_user";

                var result = await _cameraDataService.GetUserCameraBodiesAsync(currentUserId, _currentSkip, _pageSize);
                if (result.IsSuccess)
                {
                    foreach (var camera in result.Data.CameraBodies)
                    {
                        AvailableCameras.Add(new CameraSelectionItem
                        {
                            Camera = camera,
                            IsSelected = false
                        });
                    }

                    _hasMoreCameras = result.Data.HasMore;
                    LoadMoreCamerasButton.IsVisible = _hasMoreCameras;
                    _currentSkip += _pageSize;
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
            }
        }

        private async void OnLoadMoreCamerasClicked(object sender, EventArgs e)
        {
            await LoadCamerasAsync(true);
        }

        private void OnFocalLengthChanged(object sender, TextChangedEventArgs e)
        {
            CheckPrimeLensStatus();
            GenerateLensName();
            ValidateForm();
        }

        private void OnFStopChanged(object sender, TextChangedEventArgs e)
        {
            var entry = sender as Entry;
            if (entry != null && !string.IsNullOrEmpty(e.NewTextValue))
            {
                // Remove "f/" if user tries to enter it
                if (e.NewTextValue.StartsWith("f/", StringComparison.OrdinalIgnoreCase))
                {
                    var cleanValue = e.NewTextValue.Substring(2);
                    entry.Text = cleanValue;
                    return;
                }
            }

            GenerateLensName();
            ValidateForm();
        }

        private void OnLensNameChanged(object sender, TextChangedEventArgs e)
        {
            _isUserEditingName = !string.IsNullOrEmpty(e.NewTextValue);
            ValidateForm();
        }

        private void CheckPrimeLensStatus()
        {
            bool showPrimeInfo = !string.IsNullOrWhiteSpace(MinFocalLengthEntry.Text) &&
                                 string.IsNullOrWhiteSpace(MaxFocalLengthEntry.Text);
            PrimeLensInfoFrame.IsVisible = showPrimeInfo;
        }

        private void OnCameraSelectionChanged(object sender, CheckedChangedEventArgs e)
        {
            var selectedCameras = AvailableCameras.Where(c => c.IsSelected).ToList();

            // Show lens name field if at least one camera is selected
            LensNameStack.IsVisible = selectedCameras.Any();

            GenerateLensName();
            ValidateForm();
        }

        private void GenerateLensName()
        {
            if (_isUserEditingName) return; // Don't override user's custom name

            var selectedCameras = AvailableCameras.Where(c => c.IsSelected).ToList();
            if (!selectedCameras.Any() || string.IsNullOrWhiteSpace(MinFocalLengthEntry.Text))
            {
                LensNameEntry.Text = string.Empty;
                return;
            }

            try
            {
                if (!double.TryParse(MinFocalLengthEntry.Text, out double minMM))
                    return;
                // Get mount type from first selected camera
                var firstCamera = selectedCameras.First().Camera;
                var mountTypeName = GetMountTypeName(firstCamera.MountType);

                // Build focal length part
                var focalLengthPart = "";
                var isPrime = PrimeLensInfoFrame.IsVisible;

                if (isPrime)
                {
                    focalLengthPart = $"{minMM:G29}mm";
                }
                else if (double.TryParse(MaxFocalLengthEntry.Text, out double maxMM))
                {
                    focalLengthPart = $"{minMM:G29}-{maxMM:G29}mm";
                }
                else
                {
                    focalLengthPart = $"{minMM:G29}mm";
                }

                // Build aperture part
                var aperturePart = "";
                bool hasMinFStop = !string.IsNullOrWhiteSpace(MinFStopEntry.Text);
                bool hasMaxFStop = !string.IsNullOrWhiteSpace(MaxFStopEntry.Text);
                double minFStop = 0;
                double maxFStop = 0;

                if (hasMinFStop)
                    hasMinFStop = double.TryParse(MinFStopEntry.Text, out minFStop);
                if (hasMaxFStop)
                    hasMaxFStop = double.TryParse(MaxFStopEntry.Text, out maxFStop);

                if (hasMinFStop)
                {
                    if (isPrime || !hasMaxFStop || Math.Abs(maxFStop - minFStop) < 0.1)
                    {
                        aperturePart = $" f/{minFStop:G29}";
                    }
                    else
                    {
                        aperturePart = $" f/{minFStop:G29}-{maxFStop:G29}";
                    }
                }

                // Combine parts
                var generatedName = $"{mountTypeName} {focalLengthPart}{aperturePart}";
                LensNameEntry.Text = generatedName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating lens name");
            }
        }

        private string GetMountTypeName(MountType mountType)
        {
            return mountType switch
            {
                MountType.CanonEF => "Canon EF",
                MountType.CanonEFS => "Canon EF-S",
                MountType.CanonEFM => "Canon EF-M",
                MountType.CanonRF => "Canon RF",
                MountType.CanonFD => "Canon FD",
                MountType.NikonF => "Nikon F",
                MountType.NikonZ => "Nikon Z",
                MountType.Nikon1 => "Nikon 1",
                MountType.SonyE => "Sony E",
                MountType.SonyFE => "Sony FE",
                MountType.SonyA => "Sony A",
                MountType.FujifilmX => "Fujifilm X",
                MountType.FujifilmGFX => "Fujifilm GFX",
                MountType.PentaxK => "Pentax K",
                MountType.PentaxQ => "Pentax Q",
                MountType.MicroFourThirds => "M4/3",
                MountType.LeicaM => "Leica M",
                MountType.LeicaL => "Leica L",
                MountType.LeicaSL => "Leica SL",
                MountType.LeicaTL => "Leica TL",
                MountType.OlympusFourThirds => "4/3",
                MountType.PanasonicL => "Panasonic L",
                MountType.SigmaSA => "Sigma SA",
                MountType.TamronAdaptall => "Tamron",
                MountType.C => "C Mount",
                MountType.CS => "CS Mount",
                MountType.M42 => "M42",
                MountType.T2 => "T2",
                MountType.Other => "Generic",
                _ => "Unknown"
            };
        }

        private void ValidateForm()
        {
            ClearErrors();

            bool isValid = true;

            // Validate minimum focal length
            if (string.IsNullOrWhiteSpace(MinFocalLengthEntry.Text))
            {
                ShowError(MinFocalLengthError, "Minimum focal length is required");
                isValid = false;
            }
            else if (!double.TryParse(MinFocalLengthEntry.Text, out double minMM) || minMM <= 0)
            {
                ShowError(MinFocalLengthError, "Valid minimum focal length is required");
                isValid = false;
            }

            // Validate maximum focal length if provided
            if (!string.IsNullOrWhiteSpace(MaxFocalLengthEntry.Text))
            {
                if (!double.TryParse(MaxFocalLengthEntry.Text, out double maxMM) || maxMM <= 0)
                {
                    ShowError(MaxFocalLengthError, "Valid maximum focal length is required");
                    isValid = false;
                }
                else if (double.TryParse(MinFocalLengthEntry.Text, out double minMM) && maxMM <= minMM)
                {
                    ShowError(MaxFocalLengthError, "Maximum focal length must be greater than minimum");
                    isValid = false;
                }
            }

            // Validate f-stops if provided
            if (!string.IsNullOrWhiteSpace(MinFStopEntry.Text))
            {
                if (!double.TryParse(MinFStopEntry.Text, out double minFStop) || minFStop <= 0)
                {
                    ShowError(MinFStopError, "Valid minimum f-stop is required (numbers only, no f/)");
                    isValid = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(MaxFStopEntry.Text))
            {
                if (!double.TryParse(MaxFStopEntry.Text, out double maxFStop) || maxFStop <= 0)
                {
                    ShowError(MaxFStopError, "Valid maximum f-stop is required (numbers only, no f/)");
                    isValid = false;
                }
                else if (!string.IsNullOrWhiteSpace(MinFStopEntry.Text) &&
                         double.TryParse(MinFStopEntry.Text, out double minFStop) &&
                         maxFStop < minFStop)
                {
                    ShowError(MaxFStopError, "Maximum f-stop must be greater than or equal to minimum");
                    isValid = false;
                }
            }

            // Validate lens name
            if (LensNameStack.IsVisible && string.IsNullOrWhiteSpace(LensNameEntry.Text))
            {
                ShowError(LensNameError, "Lens name is required");
                isValid = false;
            }

            // Validate camera selection
            var selectedCameras = AvailableCameras.Where(c => c.IsSelected).ToList();
            if (!selectedCameras.Any())
            {
                ShowError(CameraSelectionError, "At least one compatible camera must be selected");
                isValid = false;
            }

            SaveButton.IsEnabled = isValid;
        }

        private void ShowError(Label errorLabel, string message)
        {
            errorLabel.Text = message;
            errorLabel.IsVisible = true;
        }

        private void ClearErrors()
        {
            MinFocalLengthError.IsVisible = false;
            MaxFocalLengthError.IsVisible = false;
            MinFStopError.IsVisible = false;
            MaxFStopError.IsVisible = false;
            LensNameError.IsVisible = false;
            CameraSelectionError.IsVisible = false;
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            await SaveLensAsync();
        }

        private async Task SaveLensAsync()
        {
            try
            {
                ProcessingOverlay.IsVisible = true;

                var minMM = double.Parse(MinFocalLengthEntry.Text);
                var maxMM = string.IsNullOrWhiteSpace(MaxFocalLengthEntry.Text) ? (double?)null : double.Parse(MaxFocalLengthEntry.Text);
                var minFStop = string.IsNullOrWhiteSpace(MinFStopEntry.Text) ? (double?)null : double.Parse(MinFStopEntry.Text);
                var maxFStop = string.IsNullOrWhiteSpace(MaxFStopEntry.Text) ? (double?)null : double.Parse(MaxFStopEntry.Text);
                var lensName = LensNameEntry.Text?.Trim();
                var selectedCameraIds = AvailableCameras.Where(c => c.IsSelected).Select(c => c.Camera.Id).ToList();

                var result = await _cameraDataService.CreateLensAsync(minMM, maxMM, minFStop, maxFStop, selectedCameraIds, lensName);

                if (result.IsSuccess)
                {
                    await _alertService.ShowSuccessAlertAsync($"Lens '{result.Data.Lens.DisplayName}' has been added successfully!", "Success");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync(result.ErrorMessage ?? "Failed to save lens", "Error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving lens");
                await _alertService.ShowErrorAlertAsync("An error occurred while saving the lens", "Error");
            }
            finally
            {
                ProcessingOverlay.IsVisible = false;
            }
        }

        private async void OnCancelClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CameraSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public CameraBodyDto Camera { get; set; } = new();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string DisplayName => Camera.DisplayName;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}