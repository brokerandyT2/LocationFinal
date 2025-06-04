using Location.Core.Application.Services;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Services;
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
        private bool _isValidating = false;
        private bool _isDuplicateWarningShown = false;
        private bool _isLoadingCameras = false;
        private int _currentSkip = 0;
        private const int _pageSize = 20;
        private bool _hasMoreCameras = true;

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

                var result = await _cameraDataService.GetCameraBodiesAsync(_currentSkip, _pageSize);
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

        private async void OnFocalLengthChanged(object sender, TextChangedEventArgs e)
        {
            CheckPrimeLensStatus();

            if (_isValidating || !ShouldCheckForDuplicates())
            {
                DuplicateWarningFrame.IsVisible = false;
                _isDuplicateWarningShown = false;
                ValidateForm();
                return;
            }

            await CheckForDuplicatesAsync();
            ValidateForm();
        }

        private void CheckPrimeLensStatus()
        {
            bool showPrimeInfo = !string.IsNullOrWhiteSpace(MinFocalLengthEntry.Text) &&
                                 string.IsNullOrWhiteSpace(MaxFocalLengthEntry.Text);
            PrimeLensInfoFrame.IsVisible = showPrimeInfo;
        }

        private bool ShouldCheckForDuplicates()
        {
            if (string.IsNullOrWhiteSpace(MinFocalLengthEntry.Text)) return false;
            if (!double.TryParse(MinFocalLengthEntry.Text, out _)) return false;
            return true;
        }

        private async Task CheckForDuplicatesAsync()
        {
            try
            {
                _isValidating = true;

                var focalLength = double.Parse(MinFocalLengthEntry.Text);
                var result = await _cameraDataService.CheckDuplicateLensAsync(focalLength);

                if (result.IsSuccess && result.Data.Any())
                {
                    var duplicateNames = string.Join(", ", result.Data.Take(3).Select(l => l.DisplayName));
                    DuplicateWarningText.Text = $"Similar lenses found: {duplicateNames}";
                    DuplicateWarningFrame.IsVisible = true;
                    _isDuplicateWarningShown = true;
                }
                else
                {
                    DuplicateWarningFrame.IsVisible = false;
                    _isDuplicateWarningShown = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking for duplicate lenses");
            }
            finally
            {
                _isValidating = false;
            }
        }

        private void OnCameraSelectionChanged(object sender, CheckedChangedEventArgs e)
        {
            ValidateForm();
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
                    ShowError(MinFStopError, "Valid minimum f-stop is required");
                    isValid = false;
                }
            }

            if (!string.IsNullOrWhiteSpace(MaxFStopEntry.Text))
            {
                if (!double.TryParse(MaxFStopEntry.Text, out double maxFStop) || maxFStop <= 0)
                {
                    ShowError(MaxFStopError, "Valid maximum f-stop is required");
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
            CameraSelectionError.IsVisible = false;
        }

        private void OnCancelDuplicateClicked(object sender, EventArgs e)
        {
            DuplicateWarningFrame.IsVisible = false;
            _isDuplicateWarningShown = false;
            MinFocalLengthEntry.Focus();
        }

        private async void OnSaveAnywayClicked(object sender, EventArgs e)
        {
            DuplicateWarningFrame.IsVisible = false;
            _isDuplicateWarningShown = false;
            await SaveLensAsync();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_isDuplicateWarningShown)
            {
                return; // Let user handle duplicate warning first
            }

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
                var maxFStop = string.IsNullOrWhiteSpace(MaxFStopEntry.Text) ? (double?)null : double.Parse(MaxFStopEntry.Text); var selectedCameraIds = AvailableCameras.Where(c => c.IsSelected).Select(c => c.Camera.Id).ToList();

                var result = await _cameraDataService.CreateLensAsync(minMM, maxMM, minFStop, maxFStop, selectedCameraIds);

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