using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Photography.Application.Notifications;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class AstroPhotographyCalculator : ContentPage, INotificationHandler<CameraCreatedNotification>,
        INotificationHandler<LensCreatedNotification>
    {
        private readonly AstroPhotographyCalculatorViewModel _viewModel;
        private readonly IAlertService _alertService;
        private readonly IMediator _mediator;
        private bool _isPopupVisible = false;
        private readonly ILogger<AstroPhotographyCalculator> _logger;
        private readonly IServiceProvider _serviceProvider;
        private string _currentUserId = string.Empty;

        public AstroPhotographyCalculator(
    AstroPhotographyCalculatorViewModel viewModel,
    ILogger<AstroPhotographyCalculator> logger,
    IServiceProvider serviceProvider, IAlertService alertService, IMediator mediator)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _alertService = alertService;
            _mediator = mediator;

            InitializeComponent();
            BindingContext = _viewModel;

            _ = LoadCurrentUserIdAsync();

        }

        #region User Management

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

        #endregion

        #region Notification Handlers

        public async Task Handle(CameraCreatedNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                if (notification.UserId == _currentUserId)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Refresh the camera list in the view model
                        await _viewModel.LoadEquipmentAsync();
                    });
                }
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
                if (notification.UserId == _currentUserId)
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        // Refresh the lens list in the view model
                        await _viewModel.LoadEquipmentAsync();// RefreshLensesAsync();
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling lens created notification");
            }
        }

        #endregion

        #region Button Click Handlers

        private async void OnAddCameraClicked(object sender, EventArgs e)
        {
            try
            {
                var addCameraModal = _serviceProvider.GetRequiredService<Premium.AddCameraModal>();
                await Shell.Current.Navigation.PushModalAsync(addCameraModal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to Add Camera modal");
                // You might want to show an alert here, depending on your alert service availability
                await DisplayAlert("Error", "Error opening Add Camera", "OK");
            }
        }

        private async void OnAddLensClicked(object sender, EventArgs e)
        {
            try
            {
                var addLensModal = _serviceProvider.GetRequiredService<Premium.AddLensModal>();
                await Shell.Current.Navigation.PushModalAsync(addLensModal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error navigating to Add Lens modal");
                // You might want to show an alert here, depending on your alert service availability
                await DisplayAlert("Error", "Error opening Add Lens", "OK");
            }
        }

        #endregion

        private async Task LoadInitialDataAsync()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync START ===");

                // Check what we actually need to do
                bool hasLocations = (_viewModel.Locations?.Count ?? 0) > 0;
                bool hasEquipment = (_viewModel.AvailableCameras?.Count ?? 0) > 0;
                bool hasSelectedLocation = _viewModel.SelectedLocation != null;
                bool hasValidDate = _viewModel.SelectedDate != default;
                bool hasPredictions = (_viewModel.HourlyAstroPredictions?.Count ?? 0) > 0;

                System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Current state:");
                System.Diagnostics.Debug.WriteLine($"    IsInitialized: {_viewModel.IsInitialized}");
                System.Diagnostics.Debug.WriteLine($"    HasLocations: {hasLocations} ({_viewModel.Locations?.Count ?? 0})");
                System.Diagnostics.Debug.WriteLine($"    HasEquipment: {hasEquipment} ({_viewModel.AvailableCameras?.Count ?? 0})");
                System.Diagnostics.Debug.WriteLine($"    HasSelectedLocation: {hasSelectedLocation} ({_viewModel.SelectedLocation?.Title ?? "null"})");
                System.Diagnostics.Debug.WriteLine($"    HasValidDate: {hasValidDate} ({_viewModel.SelectedDate:yyyy-MM-dd})");
                System.Diagnostics.Debug.WriteLine($"    HasPredictions: {hasPredictions} ({_viewModel.HourlyAstroPredictions?.Count ?? 0})");

                _viewModel.IsBusy = true;

                // Load data only if needed
                List<Task> loadingTasks = new List<Task>();

                if (!hasLocations)
                {
                    System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Loading locations");
                    loadingTasks.Add(_viewModel.LoadLocationsAsync());
                }

                if (!hasEquipment)
                {
                    System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Loading equipment");
                    loadingTasks.Add(_viewModel.LoadEquipmentAsync());
                }

                if (loadingTasks.Any())
                {
                    await Task.WhenAll(loadingTasks);
                    System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Loading tasks completed");

                    // Wait for data to populate (only if we just loaded it)
                    var timeout = DateTime.Now.AddSeconds(5);
                    while (DateTime.Now < timeout)
                    {
                        if ((_viewModel.Locations?.Any() == true || hasLocations) &&
                            (_viewModel.AvailableCameras?.Any() == true || hasEquipment))
                        {
                            break;
                        }
                        await Task.Delay(100);
                    }
                }

                // Set default date if not already set
                if (!hasValidDate)
                {
                    _viewModel.SelectedDate = DateTime.Today;
                    System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Set default date: {_viewModel.SelectedDate:yyyy-MM-dd}");
                }

                // Auto-select first location if none selected
                if (!hasSelectedLocation && _viewModel.Locations?.Any() == true)
                {
                    _viewModel.SelectedLocation = _viewModel.Locations.First();
                    System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Auto-selected location: {_viewModel.SelectedLocation?.Title}");
                }

                // Mark as initialized
                _viewModel.IsInitialized = true;
                System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Marked as initialized");

                // Debug calculation decision
                bool shouldCalculate = !hasPredictions ||
                                      (_viewModel.SelectedLocation != null && _viewModel.CanCalculate);

                System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Calculation Decision:");
                System.Diagnostics.Debug.WriteLine($"    !hasPredictions: {!hasPredictions}");
                System.Diagnostics.Debug.WriteLine($"    SelectedLocation != null: {_viewModel.SelectedLocation != null}");
                System.Diagnostics.Debug.WriteLine($"    CanCalculate: {_viewModel.CanCalculate}");
                System.Diagnostics.Debug.WriteLine($"    shouldCalculate: {shouldCalculate}");

                if (shouldCalculate && _viewModel.SelectedLocation != null && _viewModel.CanCalculate)
                {
                    System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - CONDITIONS MET, TRIGGERING CALCULATION");
                    await _viewModel.CalculateAstroDataAsync();
                    System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Calculation completed. Predictions count: {_viewModel.HourlyAstroPredictions?.Count ?? 0}");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - CONDITIONS NOT MET, SKIPPING CALCULATION");
                    if (_viewModel.SelectedLocation == null)
                        System.Diagnostics.Debug.WriteLine($"    Reason: No selected location");
                    if (!_viewModel.CanCalculate)
                        System.Diagnostics.Debug.WriteLine($"    Reason: CanCalculate is false");
                    if (!shouldCalculate)
                        System.Diagnostics.Debug.WriteLine($"    Reason: shouldCalculate is false");
                }

                
                System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync END ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync STACK: {ex.StackTrace}");
                await HandleErrorAsync(ex, "Error initializing astrophotography calculator");
            }
            finally
            {
                System.Diagnostics.Debug.WriteLine($"=== LoadInitialDataAsync - Setting IsBusy = false");
                _viewModel.IsBusy = false;
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

                LoadInitialDataAsync();
                _isHydrated = true;
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
                System.Diagnostics.Debug.WriteLine("Location picker selection changed");

                if (sender is Picker picker && picker.SelectedItem is LocationListItemViewModel selectedLocation)
                {
                    System.Diagnostics.Debug.WriteLine($"Selected location: {selectedLocation.Title}");

                    if (_viewModel != null && _viewModel.SelectedLocation != selectedLocation)
                    {
                        // Cancel any ongoing operations first
                        _viewModel.CancelAllOperations();

                        // Update the location
                        _viewModel.SelectedLocation = selectedLocation;

                        // Small delay to ensure UI updates
                        await Task.Delay(100);

                        // Auto-recalculate if we have valid selections
                        if (_viewModel.CanCalculate)
                        {
                            System.Diagnostics.Debug.WriteLine("Triggering calculation due to location change");
                            await _viewModel.CalculateAstroDataAsync();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Cannot calculate - CanCalculate: {_viewModel.CanCalculate}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in location selection: {ex.Message}");
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