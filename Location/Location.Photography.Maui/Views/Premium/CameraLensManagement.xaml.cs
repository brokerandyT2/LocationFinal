using Location.Core.Application.Services;
using Location.Photography.Application.Commands.CameraEvaluation;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Notifications;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Location.Photography.Maui.Views.Premium
{
    public partial class CameraLensManagement : ContentPage, INotifyPropertyChanged,
        INotificationHandler<CameraCreatedNotification>,
        INotificationHandler<LensCreatedNotification>
    {
        private readonly ICameraDataService _cameraDataService;
        private readonly ILensCameraCompatibilityRepository _compatibilityRepository;
        private readonly IAlertService _alertService;
        private readonly ILogger<CameraLensManagement> _logger;
        private readonly IMediator _mediator;

        private ObservableCollection<CameraSelectionManagementItem> _availableCameras = new();
        private ObservableCollection<LensSelectionItem> _availableLenses = new();
        private CameraSelectionManagementItem? _selectedCamera;
        private string _currentUserId = string.Empty;
        private bool _isLoading = false;

        public ObservableCollection<CameraSelectionManagementItem> AvailableCameras
        {
            get => _availableCameras;
            set
            {
                _availableCameras = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<LensSelectionItem> AvailableLenses
        {
            get => _availableLenses;
            set
            {
                _availableLenses = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private readonly IServiceProvider _serviceProvider;
        public CameraLensManagement(
            ICameraDataService cameraDataService,
            ILensCameraCompatibilityRepository compatibilityRepository,
            IAlertService alertService,
            ILogger<CameraLensManagement> logger,
            IMediator mediator, IServiceProvider serviceProvider)
        {
            _cameraDataService = cameraDataService ?? throw new ArgumentNullException(nameof(cameraDataService));
            _compatibilityRepository = compatibilityRepository ?? throw new ArgumentNullException(nameof(compatibilityRepository));
            _alertService = alertService ?? throw new ArgumentNullException(nameof(alertService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            InitializeComponent();
            BindingContext = this;
            _serviceProvider = serviceProvider;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCurrentUserIdAsync();
            await LoadDataAsync();
        }

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

        private async Task LoadDataAsync()
        {
            if (_isLoading) return;

            try
            {
                _isLoading = true;
                ProcessingOverlay.IsVisible = true;

                await LoadCamerasAsync();
                await LoadLensesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading data");
                await _alertService.ShowErrorAlertAsync("Error loading camera and lens data", "Error");
            }
            finally
            {
                _isLoading = false;
                ProcessingOverlay.IsVisible = false;
            }
        }

        private async Task LoadCamerasAsync()
        {
            try
            {
                var result = await _cameraDataService.GetUserCameraBodiesAsync(_currentUserId, 0, int.MaxValue);
                if (result.IsSuccess)
                {
                    AvailableCameras.Clear();
                    foreach (var camera in result.Data.CameraBodies.OrderBy(c => c.DisplayName))
                    {
                        AvailableCameras.Add(new CameraSelectionManagementItem
                        {
                            Camera = camera,
                            IsSelected = false
                        });
                    }
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync("Failed to load cameras", "Error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading cameras");
                throw;
            }
        }

        private async Task LoadLensesAsync()
        {
            try
            {
                var result = await _cameraDataService.GetLensesAsync(0, int.MaxValue, true); // userLensesOnly = true
                if (result.IsSuccess)
                {
                    AvailableLenses.Clear();
                    foreach (var lens in result.Data.Lenses.OrderBy(l => l.DisplayName))
                    {
                        AvailableLenses.Add(new LensSelectionItem
                        {
                            Lens = lens,
                            IsSelected = false
                        });
                    }
                }
                else
                {
                    await _alertService.ShowErrorAlertAsync("Failed to load lenses", "Error");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading lenses");
                throw;
            }
        }

        private async void OnCameraSelectionChanged(object sender, CheckedChangedEventArgs e)
        {
            try
            {
                var radioButton = sender as RadioButton;
                if (radioButton?.BindingContext is CameraSelectionManagementItem cameraItem && e.Value)
                {
                    // Update selected camera
                    foreach (var camera in AvailableCameras)
                    {
                        camera.IsSelected = camera == cameraItem;
                    }

                    _selectedCamera = cameraItem;
                    await UpdateLensCompatibilityAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling camera selection");
                await _alertService.ShowErrorAlertAsync("Error selecting camera", "Error");
            }
        }

        private async Task UpdateLensCompatibilityAsync()
        {
            if (_selectedCamera?.Camera == null) return;

            try
            {
                var compatibilityResult = await _compatibilityRepository.GetByCameraIdAsync(_selectedCamera.Camera.Id);
                if (compatibilityResult.IsSuccess)
                {
                    var compatibleLensIds = compatibilityResult.Data.Select(c => c.LensId).ToHashSet();

                    foreach (var lens in AvailableLenses)
                    {
                        lens.IsSelected = compatibleLensIds.Contains(lens.Lens.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating lens compatibility for camera {CameraId}", _selectedCamera.Camera.Id);
            }
        }

        private async void OnLensSelectionChanged(object sender, CheckedChangedEventArgs e)
        {
            try
            {
                var checkBox = sender as CheckBox;
                if (checkBox?.BindingContext is LensSelectionItem lensItem && _selectedCamera?.Camera != null)
                {
                    lensItem.IsSelected = e.Value;

                    if (e.Value)
                    {
                        // Create compatibility
                        var compatibility = new LensCameraCompatibility(lensItem.Lens.Id, _selectedCamera.Camera.Id);
                        var createResult = await _compatibilityRepository.CreateAsync(compatibility);

                        if (!createResult.IsSuccess)
                        {
                            lensItem.IsSelected = false; // Revert on failure
                            await _alertService.ShowErrorAlertAsync("Failed to assign lens to camera", "Error");
                        }
                    }
                    else
                    {
                        // Delete compatibility
                        var deleteResult = await _compatibilityRepository.DeleteAsync(lensItem.Lens.Id, _selectedCamera.Camera.Id);

                        if (!deleteResult.IsSuccess)
                        {
                            lensItem.IsSelected = true; // Revert on failure
                            await _alertService.ShowErrorAlertAsync("Failed to remove lens from camera", "Error");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling lens selection");
                await _alertService.ShowErrorAlertAsync("Error updating lens assignment", "Error");
            }
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

        private async void OnCloseClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        // Notification Handlers
        public async Task Handle(CameraCreatedNotification notification, CancellationToken cancellationToken)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await LoadCamerasAsync();
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
                    await LoadLensesAsync();
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling lens created notification");
            }
        }

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class CameraSelectionManagementItem : INotifyPropertyChanged
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class LensSelectionItem : INotifyPropertyChanged
    {
        private bool _isSelected;

        public LensDto Lens { get; set; } = new();

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}