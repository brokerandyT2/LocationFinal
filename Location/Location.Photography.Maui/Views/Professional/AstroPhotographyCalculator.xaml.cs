using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using MediatR;
using System.Runtime.CompilerServices;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class AstroPhotographyCalculator : ContentPage
    {
        private readonly AstroPhotographyCalculatorViewModel _viewModel;
        private readonly IAlertService _alertService;
        private readonly IMediator _mediator;
        private bool _isPopupVisible = false;

        public AstroPhotographyCalculator(
            AstroPhotographyCalculatorViewModel viewModel,
            IAlertService alertService,
            IMediator mediator)
        {
            InitializeComponent();

            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            BindingContext = _viewModel;
            _viewModel.IsBusy = true;

            // Initialize the page
            LoadInitialData();

            _viewModel.IsBusy = false;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private async void LoadInitialData()
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.ErrorOccurred += OnSystemError;

                    // Load locations and equipment in parallel
                    var locationTask = _viewModel.LoadLocationsAsync();
                    //var equipmentTask = _viewModel.LoadEquipmentAsync();

                    //await Task.WhenAll(locationTask, equipmentTask);
                    await Task.WhenAll(locationTask);
                    // Auto-calculate if we have valid selections
                    if (_viewModel.CanCalculate)
                    {
                        await _viewModel.CalculateAstroDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error initializing astrophotography calculator");
            }
        }

        private bool _isHydrated = false;
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            base.OnPropertyChanged(propertyName);
            if (!_isHydrated && nameof(IsVisible) == propertyName)
            {
                if (!IsVisible)
                    return;

                LoadInitialData();
                _isHydrated = true;
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            if (_viewModel != null)
            {
                _viewModel.ErrorOccurred -= OnSystemError;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel?.Dispose();
            }
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            // Handle specific property changes that might need UI updates
            switch (e.PropertyName)
            {
                case nameof(_viewModel.SelectedCamera):
                    MainThread.BeginInvokeOnMainThread(() => OnCameraSelectionChanged());
                    break;
                case nameof(_viewModel.SelectedLens):
                    MainThread.BeginInvokeOnMainThread(() => OnLensSelectionChanged());
                    break;
                case nameof(_viewModel.SelectedTarget):
                    MainThread.BeginInvokeOnMainThread(() => OnTargetSelectionChanged());
                    break;
                case nameof(_viewModel.SelectedLocation):
                    MainThread.BeginInvokeOnMainThread(() => OnLocationSelectionChanged());
                    break;
                case nameof(_viewModel.CurrentCalculations):
                    MainThread.BeginInvokeOnMainThread(() => OnCalculationsUpdated());
                    break;
            }
        }

        private void OnCameraSelectionChanged()
        {
            try
            {
                if (_viewModel?.SelectedCamera != null)
                {
                    // Update any camera-specific UI elements if needed
                    System.Diagnostics.Debug.WriteLine($"Camera selected: {_viewModel.SelectedCamera.Name}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling camera selection: {ex.Message}");
            }
        }

        private void OnLensSelectionChanged()
        {
            try
            {
                if (_viewModel?.SelectedLens != null)
                {
                    // Update any lens-specific UI elements if needed
                    System.Diagnostics.Debug.WriteLine($"Lens selected: {_viewModel.SelectedLens.NameForLens}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling lens selection: {ex.Message}");
            }
        }

        private void OnTargetSelectionChanged()
        {
            try
            {
                if (_viewModel != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Target selected: {_viewModel.SelectedTarget}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling target selection: {ex.Message}");
            }
        }

        private void OnLocationSelectionChanged()
        {
            try
            {
                if (_viewModel?.SelectedLocation != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Location selected: {_viewModel.SelectedLocation.Title}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling location selection: {ex.Message}");
            }
        }

        private void OnCalculationsUpdated()
        {
            try
            {
                if (_viewModel?.CurrentCalculations != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Calculations updated: {_viewModel.CurrentCalculations.Count} results");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling calculations update: {ex.Message}");
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            try
            {
                var errorMessage = e.Source switch
                {
                    OperationErrorSource.Network => "Network connection issue. Please check your internet connection and try again.",
                    OperationErrorSource.Database => "Database error occurred. Please restart the app if the problem persists.",
                    OperationErrorSource.Sensor => "Sensor access error. Please check app permissions.",
                    OperationErrorSource.Permission => "Permission required. Please grant necessary permissions in settings.",
                    OperationErrorSource.Validation => e.Message,
                    _ => $"An error occurred: {e.Message}"
                };

                var retry = await DisplayAlert(
                    "Astrophotography Calculator Error",
                    errorMessage,
                    "Retry",
                    "Cancel");

                if (retry && sender is AstroPhotographyCalculatorViewModel viewModel)
                {
                    await viewModel.RetryLastCommandAsync();
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Multiple errors occurred: {ex.Message}", "OK");
            }
        }

        private async Task HandleErrorAsync(Exception ex, string context)
        {
            var errorMessage = $"{context}: {ex.Message}";
            System.Diagnostics.Debug.WriteLine(errorMessage);

            try
            {
                if (_alertService != null)
                {
                    await _alertService.ShowErrorAlertAsync(errorMessage, "Astrophotography Calculator Error");
                }
                else
                {
                    await DisplayAlert("Astrophotography Calculator Error", errorMessage, "OK");
                }
            }
            catch
            {
                await DisplayAlert("Error", "Multiple errors occurred. Please restart the app.", "OK");
            }
        }

        // Event handlers for XAML controls
        private async void OnDateSelectionChanged(object sender, DateChangedEventArgs e)
        {
            try
            {
                if (_viewModel != null)
                {
                    // Cancel any ongoing operations before starting new ones
                    _viewModel.CancelAllOperations();

                    // Auto-recalculate if we have valid selections
                    if (_viewModel.CanCalculate)
                    {
                        await _viewModel.CalculateAstroDataAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating date selection");
            }
        }

        private async void OnLocationPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is Picker picker && picker.SelectedItem is LocationListItemViewModel selectedLocation)
                {
                    if (_viewModel != null && _viewModel.SelectedLocation != selectedLocation)
                    {
                        _viewModel.SelectedLocation = selectedLocation;

                        // Auto-recalculate if we have valid selections
                        if (_viewModel.CanCalculate)
                        {
                            await _viewModel.CalculateAstroDataAsync();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating location selection");
            }
        }

        private async void OnCameraPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is Picker picker && picker.SelectedItem is CameraBody selectedCamera)
                {
                    if (_viewModel != null)
                    {
                        await _viewModel.SelectCameraAsync(selectedCamera);
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating camera selection");
            }
        }

        private async void OnLensPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is Picker picker && picker.SelectedItem is Lens selectedLens)
                {
                    if (_viewModel != null)
                    {
                        await _viewModel.SelectLensAsync(selectedLens);
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating lens selection");
            }
        }

        private async void OnTargetPickerSelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                if (sender is Picker picker && picker.SelectedItem is AstroTarget selectedTarget)
                {
                    if (_viewModel != null)
                    {
                        await _viewModel.SelectTargetAsync(selectedTarget);
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error updating target selection");
            }
        }

        // Modal navigation helpers (if needed for future expansion)
        public async Task OpenEquipmentModal()
        {
            try
            {
                // Future implementation for equipment management modal
                await DisplayAlert("Equipment", "Equipment management coming soon!", "OK");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error opening equipment modal");
            }
        }

        public async Task OpenTargetDetailModal(AstroCalculationResult result)
        {
            try
            {
                // Future implementation for detailed target information modal
                var message = $"Detailed information for {result.Description}\n\n" +
                             $"Position: {result.Azimuth:F1}° Az, {result.Altitude:F1}° Alt\n" +
                             $"Visibility: {(result.IsVisible ? "Visible" : "Not visible")}\n\n" +
                             $"{result.PhotographyNotes}";

                await DisplayAlert("Target Details", message, "OK");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error opening target details");
            }
        }

        public async Task ShareCalculationResults()
        {
            try
            {
                if (_viewModel?.CurrentCalculations?.Any() != true)
                {
                    await DisplayAlert("Share Results", "No calculation results to share.", "OK");
                    return;
                }

                var shareText = $"Astrophotography calculations for {_viewModel.SelectedLocation?.Title} on {_viewModel.SelectedDate:yyyy-MM-dd}\n\n";
                shareText += $"Target: {_viewModel.SelectedTargetDisplay}\n";
                shareText += $"Equipment: {_viewModel.SelectedCameraDisplay} + {_viewModel.SelectedLensDisplay}\n\n";

                foreach (var result in _viewModel.CurrentCalculations.Take(3)) // Limit for readability
                {
                    shareText += $"• {result.Description}: {(result.IsVisible ? "Visible" : "Not visible")}\n";
                    if (result.OptimalTime.HasValue)
                    {
                        shareText += $"  Best time: {result.OptimalTime.Value:HH:mm}\n";
                    }
                }

                shareText += $"\n{_viewModel.ExposureRecommendation}";

                await Share.Default.RequestAsync(new ShareTextRequest
                {
                    Text = shareText,
                    Title = "Astrophotography Calculations"
                });
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error sharing results");
            }
        }

        // Manual refresh method for external access
        public async Task RefreshCalculations()
        {
            try
            {
                if (_viewModel != null)
                {
                    await _viewModel.RefreshCalculationsAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(ex, "Error refreshing calculations");
            }
        }

        // Helper method to get current viewmodel for external access
        public AstroPhotographyCalculatorViewModel GetViewModel()
        {
            return _viewModel;
        }
    }
}