using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.ComponentModel;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class AddCameraModal : ContentPage, INotifyPropertyChanged
    {
        private readonly ICameraDataService _cameraDataService;
        private readonly IAlertService _alertService;
        private readonly ILogger<AddCameraModal> _logger;

        private List<MountTypeDto> _mountTypes = new();
        private MountTypeDto? _selectedMountType;
        private bool _isValidating = false;
        private bool _isDuplicateWarningShown = false;

        public List<MountTypeDto> MountTypes
        {
            get => _mountTypes;
            set
            {
                _mountTypes = value;
                OnPropertyChanged();
            }
        }

        public MountTypeDto? SelectedMountType
        {
            get => _selectedMountType;
            set
            {
                _selectedMountType = value;
                OnPropertyChanged();
                ValidateForm();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AddCameraModal(
            ICameraDataService cameraDataService,
            IAlertService alertService,
            ILogger<AddCameraModal> logger)
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
            await LoadMountTypesAsync();
        }

        private async Task LoadMountTypesAsync()
        {
            try
            {
                var result = await _cameraDataService.GetMountTypesAsync();
                if (result.IsSuccess)
                {
                    MountTypes = result.Data.Cast<MountTypeDto>().ToList();
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync("Failed to load mount types", "Error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading mount types");
                await _alertService.ShowErrorAlertAsync("Error loading mount types", "Error");
            }
        }

        private async void OnCameraNameChanged(object sender, TextChangedEventArgs e)
        {
            if (_isValidating || string.IsNullOrWhiteSpace(e.NewTextValue))
            {
                DuplicateWarningFrame.IsVisible = false;
                _isDuplicateWarningShown = false;
                ValidateForm();
                return;
            }

            await CheckForDuplicatesAsync(e.NewTextValue);
            ValidateForm();
        }

        private async Task CheckForDuplicatesAsync(string cameraName)
        {
            try
            {
                _isValidating = true;

                var result = await _cameraDataService.CheckDuplicateCameraAsync(cameraName);
                if (result.IsSuccess && result.Data.Any())
                {
                    var duplicateNames = string.Join(", ", result.Data.Take(3).Select(c => c.DisplayName));
                    DuplicateWarningText.Text = $"Similar cameras found: {duplicateNames}";
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
                _logger.LogError(ex, "Error checking for duplicate cameras");
            }
            finally
            {
                _isValidating = false;
            }
        }

        private void ValidateForm()
        {
            ClearErrors();

            bool isValid = true;

            // Validate camera name
            if (string.IsNullOrWhiteSpace(CameraNameEntry.Text))
            {
                ShowError(CameraNameError, "Camera name is required");
                isValid = false;
            }

            // Validate sensor type
            if (string.IsNullOrWhiteSpace(SensorTypeEntry.Text))
            {
                ShowError(SensorTypeError, "Sensor type is required");
                isValid = false;
            }

            // Validate sensor width
            if (!double.TryParse(SensorWidthEntry.Text, out double width) || width <= 0)
            {
                ShowError(SensorWidthError, "Valid sensor width is required");
                isValid = false;
            }

            // Validate sensor height
            if (!double.TryParse(SensorHeightEntry.Text, out double height) || height <= 0)
            {
                ShowError(SensorHeightError, "Valid sensor height is required");
                isValid = false;
            }

            // Validate mount type
            if (SelectedMountType == null)
            {
                ShowError(MountTypeError, "Mount type is required");
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
            CameraNameError.IsVisible = false;
            SensorTypeError.IsVisible = false;
            SensorWidthError.IsVisible = false;
            SensorHeightError.IsVisible = false;
            MountTypeError.IsVisible = false;
        }

        private void OnCancelDuplicateClicked(object sender, EventArgs e)
        {
            DuplicateWarningFrame.IsVisible = false;
            _isDuplicateWarningShown = false;
            CameraNameEntry.Focus();
        }

        private async void OnSaveAnywayClicked(object sender, EventArgs e)
        {
            DuplicateWarningFrame.IsVisible = false;
            _isDuplicateWarningShown = false;
            await SaveCameraAsync();
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (_isDuplicateWarningShown)
            {
                return; // Let user handle duplicate warning first
            }

            await SaveCameraAsync();
        }

        private async Task SaveCameraAsync()
        {
            try
            {
                ProcessingOverlay.IsVisible = true;

                var result = await _cameraDataService.CreateCameraBodyAsync(
                    CameraNameEntry.Text.Trim(),
                    SensorTypeEntry.Text.Trim(),
                    double.Parse(SensorWidthEntry.Text),
                    double.Parse(SensorHeightEntry.Text),
                    SelectedMountType!.Value);

                if (result.IsSuccess)
                {
                    await _alertService.ShowSuccessAlertAsync($"Camera '{result.Data.DisplayName}' has been added successfully!", "Success");
                    await Shell.Current.GoToAsync("..");
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync(result.ErrorMessage ?? "Failed to save camera", "Error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving camera");
                await _alertService.ShowErrorAlertAsync("An error occurred while saving the camera", "Error");
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

    
}