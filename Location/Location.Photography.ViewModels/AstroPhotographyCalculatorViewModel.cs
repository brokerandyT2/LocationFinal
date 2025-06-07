// Location.Photography.ViewModels/AstroPhotographyCalculatorViewModel.cs - Part 1
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Locations.Queries.GetLocations;
using Location.Core.Application.Services;
using Location.Core.ViewModels;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Entities;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels.Interfaces;
using MediatR;
using System.Collections.ObjectModel;
using System.Diagnostics;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class AstroPhotographyCalculatorViewModel : ViewModelBase
    {
        #region Fields
        private readonly IMediator _mediator;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly IAstroCalculationService _astroCalculationService;
        private readonly ICameraBodyRepository _cameraBodyRepository;
        private readonly ILensRepository _lensRepository;
        private readonly IUserCameraBodyRepository _userCameraBodyRepository;

        // PERFORMANCE: Threading and caching
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private readonly Dictionary<string, CachedAstroCalculation> _calculationCache = new();
        private CancellationTokenSource _cancellationTokenSource = new();
        private DateTime _lastCalculationTime = DateTime.MinValue;
        private const int CALCULATION_THROTTLE_MS = 500;

        // Core properties backing fields
        private ObservableCollection<LocationListItemViewModel> _locations = new();
        private LocationListItemViewModel _selectedLocation;
        private DateTime _selectedDate = DateTime.Today;
        private string _locationPhoto = string.Empty;
        private bool _isInitialized;
        private bool _hasError;
        private string _errorMessage = string.Empty;

        // Equipment backing fields
        private ObservableCollection<CameraBody> _availableCameras = new();
        private ObservableCollection<Lens> _availableLenses = new();
        private CameraBody _selectedCamera;
        private Lens _selectedLens;
        private bool _isLoadingEquipment;

        // Astro target and calculation backing fields
        private AstroTarget _selectedTarget = AstroTarget.MilkyWayCore;
        private ObservableCollection<AstroTargetDisplayModel> _availableTargets = new();
        private ObservableCollection<AstroCalculationResult> _currentCalculations = new();
        private bool _isCalculating;
        private string _calculationStatus = string.Empty;

        // Results backing fields
        private string _exposureRecommendation = string.Empty;
        private string _equipmentRecommendation = string.Empty;
        private string _photographyNotes = string.Empty;
        private double _fieldOfViewWidth;
        private double _fieldOfViewHeight;
        private bool _targetFitsInFrame;
        #endregion
        // Add new properties for hourly predictions
        [ObservableProperty]
        private ObservableCollection<HourlyPredictionDisplayModel> _hourlyAstroPredictions = new();

        [ObservableProperty]
        private bool _isGeneratingHourlyPredictions;

        [ObservableProperty]
        private string _hourlyPredictionsStatus = string.Empty;

      
        #region Constructor


        private readonly IEquipmentRecommendationService _equipmentRecommendationService;
        private readonly IPredictiveLightService _predictiveLightService;

        public AstroPhotographyCalculatorViewModel(
            IMediator mediator,
            IErrorDisplayService errorDisplayService,
            IAstroCalculationService astroCalculationService,
            ICameraBodyRepository cameraBodyRepository,
            ILensRepository lensRepository,
            IUserCameraBodyRepository userCameraBodyRepository,
            IEquipmentRecommendationService equipmentRecommendationService, // NEW
            IPredictiveLightService predictiveLightService) // NEW
            : base(null, errorDisplayService)
        {
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            _astroCalculationService = astroCalculationService ?? throw new ArgumentNullException(nameof(astroCalculationService));
            _cameraBodyRepository = cameraBodyRepository ?? throw new ArgumentNullException(nameof(cameraBodyRepository));
            _lensRepository = lensRepository ?? throw new ArgumentNullException(nameof(lensRepository));
            _userCameraBodyRepository = userCameraBodyRepository ?? throw new ArgumentNullException(nameof(userCameraBodyRepository));
            _equipmentRecommendationService = equipmentRecommendationService ?? throw new ArgumentNullException(nameof(equipmentRecommendationService)); // NEW
            _predictiveLightService = predictiveLightService ?? throw new ArgumentNullException(nameof(predictiveLightService)); // NEW

            InitializeCommands();
            InitializeAstroTargets();
        }
        #endregion

        #region Initialization
        private void InitializeCommands()
        {
            LoadLocationsCommand = new AsyncRelayCommand(LoadLocationsAsync);
            LoadEquipmentCommand = new AsyncRelayCommand(LoadEquipmentAsync);
            CalculateAstroDataCommand = new AsyncRelayCommand(CalculateAstroDataAsync);
            RefreshCalculationsCommand = new AsyncRelayCommand(RefreshCalculationsAsync);
            SelectCameraCommand = new AsyncRelayCommand<CameraBody>(SelectCameraAsync);
            SelectLensCommand = new AsyncRelayCommand<Lens>(SelectLensAsync);
            SelectTargetCommand = new AsyncRelayCommand<AstroTarget>(SelectTargetAsync);
            RetryLastCommandCommand = new AsyncRelayCommand(RetryLastCommandAsync);
        }

        private void InitializeAstroTargets()
        {
            var targets = new List<AstroTargetDisplayModel>
            {
                new AstroTargetDisplayModel { Target = AstroTarget.MilkyWayCore, DisplayName = "Milky Way Core", Description = "Galactic center region - best summer months" },
                new AstroTargetDisplayModel { Target = AstroTarget.Moon, DisplayName = "Moon", Description = "Lunar photography - all phases" },
                new AstroTargetDisplayModel { Target = AstroTarget.Planets, DisplayName = "Planets", Description = "Planetary photography - visible planets" },
                new AstroTargetDisplayModel { Target = AstroTarget.DeepSkyObjects, DisplayName = "Deep Sky Objects", Description = "Nebulae, galaxies, and star clusters" },
                new AstroTargetDisplayModel { Target = AstroTarget.StarTrails, DisplayName = "Star Trails", Description = "Circular or linear star trail photography" },
                new AstroTargetDisplayModel { Target = AstroTarget.MeteorShowers, DisplayName = "Meteor Showers", Description = "Annual meteor shower events" },
                new AstroTargetDisplayModel { Target = AstroTarget.Constellations, DisplayName = "Constellations", Description = "Constellation photography and identification" },
                new AstroTargetDisplayModel { Target = AstroTarget.PolarAlignment, DisplayName = "Polar Alignment", Description = "Mount alignment for tracking" }
            };

            AvailableTargets = new ObservableCollection<AstroTargetDisplayModel>(targets);
            SelectedTarget = AstroTarget.MilkyWayCore;
        }

        private void InitializeDesignTimeData()
        {


            LocationPhoto = string.Empty;
            SelectedDate = DateTime.Today;
            CalculationStatus = "Ready for calculations";
            ExposureRecommendation = "ISO 3200, f/2.8, 20 seconds";
            EquipmentRecommendation = "Wide-angle lens recommended";
            PhotographyNotes = "Best visibility after astronomical twilight";
        }
        #endregion

        #region Cache Management
        private class CachedAstroCalculation
        {
            public DateTime Timestamp { get; set; }
            public LocationListItemViewModel Location { get; set; }
            public DateTime CalculationDate { get; set; }
            public AstroTarget Target { get; set; }
            public CameraBody Camera { get; set; }
            public Lens Lens { get; set; }
            public List<AstroCalculationResult> Results { get; set; }
            public TimeSpan CacheDuration => DateTime.Now - Timestamp;
            public bool IsExpired => CacheDuration.TotalMinutes > 30; // 30-minute cache
        }

        private string GenerateCacheKey(LocationListItemViewModel location, DateTime date, AstroTarget target, CameraBody camera, Lens lens)
        {
            return $"{location?.Id}_{date:yyyyMMdd}_{target}_{camera?.Id}_{lens?.Id}";
        }

        private void ClearExpiredCache()
        {
            var expiredKeys = _calculationCache
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _calculationCache.Remove(key);
            }
        }
        #endregion
        // Location.Photography.ViewModels/AstroPhotographyCalculatorViewModel.cs - Part 2
        #region Observable Properties

        // Core location and date properties
        [ObservableProperty]
        private ObservableCollection<LocationListItemViewModel> _locationsProp = new();

        [ObservableProperty]
        private LocationListItemViewModel _selectedLocationProp;

        [ObservableProperty]
        private DateTime _selectedDateProp = DateTime.Today;

        [ObservableProperty]
        private string _locationPhotoProp = string.Empty;

        [ObservableProperty]
        private bool _isInitializedProp;

        [ObservableProperty]
        private bool _hasErrorProp;

        [ObservableProperty]
        private string _errorMessageProp = string.Empty;

        // Equipment properties
        [ObservableProperty]
        private ObservableCollection<CameraBody> _availableCamerasProp = new();

        [ObservableProperty]
        private ObservableCollection<Lens> _availableLensesProp = new();

        [ObservableProperty]
        private CameraBody _selectedCameraProp;

        [ObservableProperty]
        private Lens _selectedLensProp;

        [ObservableProperty]
        private bool _isLoadingEquipmentProp;

        // Astro target and calculation properties
        [ObservableProperty]
        private AstroTarget _selectedTargetProp = AstroTarget.MilkyWayCore;

        [ObservableProperty]
        private ObservableCollection<AstroTargetDisplayModel> _availableTargetsProp = new();

        [ObservableProperty]
        private ObservableCollection<AstroCalculationResult> _currentCalculationsProp = new();

        [ObservableProperty]
        private bool _isCalculatingProp;

        [ObservableProperty]
        private string _calculationStatusProp = string.Empty;

        // Results properties
        [ObservableProperty]
        private string _exposureRecommendationProp = string.Empty;

        [ObservableProperty]
        private string _equipmentRecommendationProp = string.Empty;

        [ObservableProperty]
        private string _photographyNotesProp = string.Empty;

        [ObservableProperty]
        private double _fieldOfViewWidthProp;

        [ObservableProperty]
        private double _fieldOfViewHeightProp;

        [ObservableProperty]
        private bool _targetFitsInFrameProp;

        // Legacy property mappings for compatibility with existing XAML bindings
        public ObservableCollection<LocationListItemViewModel> Locations
        {
            get => _locations;
            set => SetProperty(ref _locations, value);
        }

        public LocationListItemViewModel SelectedLocation
        {
            get => _selectedLocation;
            set
            {
                if (SetProperty(ref _selectedLocation, value))
                {
                    // Update LocationPhoto when SelectedLocation changes
                    LocationPhoto = value?.Photo ?? string.Empty;

                    // Trigger recalculation if we have all required data
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (CanCalculate)
                                await CalculateAstroDataAsync();
                        }
                        catch (Exception ex)
                        {
                            await HandleErrorAsync(OperationErrorSource.Validation, $"Error updating calculations: {ex.Message}");
                        }
                    });
                }
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set => SetProperty(ref _selectedDate, value);
        }

        public string LocationPhoto
        {
            get => _locationPhoto;
            set => SetProperty(ref _locationPhoto, value);
        }

        public bool IsInitialized
        {
            get => _isInitialized;
            set => SetProperty(ref _isInitialized, value);
        }

        public bool HasError
        {
            get => _hasError;
            set => SetProperty(ref _hasError, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        // Equipment properties
        public ObservableCollection<CameraBody> AvailableCameras
        {
            get => _availableCameras;
            set => SetProperty(ref _availableCameras, value);
        }

        public ObservableCollection<Lens> AvailableLenses
        {
            get => _availableLenses;
            set => SetProperty(ref _availableLenses, value);
        }

        public CameraBody SelectedCamera
        {
            get => _selectedCamera;
            set => SetProperty(ref _selectedCamera, value);
        }

        public Lens SelectedLens
        {
            get => _selectedLens;
            set => SetProperty(ref _selectedLens, value);
        }

        public bool IsLoadingEquipment
        {
            get => _isLoadingEquipment;
            set => SetProperty(ref _isLoadingEquipment, value);
        }

        // Astro calculation properties
        public AstroTarget SelectedTarget
        {
            get => _selectedTarget;
            set => SetProperty(ref _selectedTarget, value);
        }

        public ObservableCollection<AstroTargetDisplayModel> AvailableTargets
        {
            get => _availableTargets;
            set => SetProperty(ref _availableTargets, value);
        }

        public ObservableCollection<AstroCalculationResult> CurrentCalculations
        {
            get => _currentCalculations;
            set => SetProperty(ref _currentCalculations, value);
        }

        public bool IsCalculating
        {
            get => _isCalculating;
            set => SetProperty(ref _isCalculating, value);
        }

        public string CalculationStatus
        {
            get => _calculationStatus;
            set => SetProperty(ref _calculationStatus, value);
        }

        // Results properties
        public string ExposureRecommendation
        {
            get => _exposureRecommendation;
            set => SetProperty(ref _exposureRecommendation, value);
        }

        public string EquipmentRecommendation
        {
            get => _equipmentRecommendation;
            set => SetProperty(ref _equipmentRecommendation, value);
        }

        public string PhotographyNotes
        {
            get => _photographyNotes;
            set => SetProperty(ref _photographyNotes, value);
        }

        public double FieldOfViewWidth
        {
            get => _fieldOfViewWidth;
            set => SetProperty(ref _fieldOfViewWidth, value);
        }

        public double FieldOfViewHeight
        {
            get => _fieldOfViewHeight;
            set => SetProperty(ref _fieldOfViewHeight, value);
        }

        public bool TargetFitsInFrame
        {
            get => _targetFitsInFrame;
            set => SetProperty(ref _targetFitsInFrame, value);
        }

        // Computed properties for UI display
        public string SelectedCameraDisplay => SelectedCamera?.Name ?? "No camera selected";
        public string SelectedLensDisplay => SelectedLens?.NameForLens ?? "No lens selected";
        public string SelectedTargetDisplay => AvailableTargets?.FirstOrDefault(t => t.Target == SelectedTarget)?.DisplayName ?? SelectedTarget.ToString();
        public string FieldOfViewDisplay => TargetFitsInFrame ?
            $"FOV: {FieldOfViewWidth:F1}° × {FieldOfViewHeight:F1}° - Target fits" :
            $"FOV: {FieldOfViewWidth:F1}° × {FieldOfViewHeight:F1}° - Target too large";

        // Status properties for UI feedback
        public bool CanCalculate => SelectedLocation != null && !IsCalculating;
        public bool HasEquipmentSelected => SelectedCamera != null && SelectedLens != null;
        public bool HasValidSelection => SelectedLocation != null && HasEquipmentSelected;
        public string StatusMessage => IsCalculating ? CalculationStatus :
                                     !HasValidSelection ? "Select location, camera, and lens to begin" :
                                     CurrentCalculations?.Any() == true ? $"Showing {CurrentCalculations.Count} calculations" :
                                     "Ready for calculations";
        #endregion

        #region Commands
        public IAsyncRelayCommand LoadLocationsCommand { get; private set; }
        public IAsyncRelayCommand LoadEquipmentCommand { get; private set; }
        public IAsyncRelayCommand CalculateAstroDataCommand { get; private set; }
        public IAsyncRelayCommand RefreshCalculationsCommand { get; private set; }
        public IAsyncRelayCommand<CameraBody> SelectCameraCommand { get; private set; }
        public IAsyncRelayCommand<Lens> SelectLensCommand { get; private set; }
        public IAsyncRelayCommand<AstroTarget> SelectTargetCommand { get; private set; }
        public IAsyncRelayCommand RetryLastCommandCommand { get; private set; }
        public object SetTime { get; private set; }
        public object OptimalTime { get; private set; }
        public string Description { get; private set; }
        #endregion

        #region Events
        public event EventHandler<OperationErrorEventArgs> ErrorOccurred;
        public event EventHandler CalculationCompleted;
        public event EventHandler<AstroCalculationResult> TargetCalculated;
        #endregion

        // Location.Photography.ViewModels/AstroPhotographyCalculatorViewModel.cs - Part 3
        #region Equipment Selection and Location/Date Commands

        public async Task LoadLocationsAsync()
        {
            if (!await _operationLock.WaitAsync(100))
                return;

            try
            {
                IsBusy = true;
                HasError = false;
                ErrorMessage = string.Empty;

                var query = new GetLocationsQuery();
                var result = await _mediator.Send(query, _cancellationTokenSource.Token);

                if (result.IsSuccess && result.Data?.Items?.Any() == true)
                {
                    var locationViewModels = result.Data.Items
                        .Where(location => location != null) // Filter out null locations
                        .Select(location => new LocationListItemViewModel
                        {
                            Id = location.Id,
                            Title = location.Title ?? "Unknown Location",
                            Latitude = location.Latitude,
                            Longitude = location.Longitude,
                            Photo = location.PhotoPath ?? string.Empty
                        }).ToList();

                    // Check if we have any valid locations
                    if (locationViewModels.Any())
                    {
                        Locations = new ObservableCollection<LocationListItemViewModel>(locationViewModels);

                        // Auto-select first location if none selected
                        if (SelectedLocation == null && Locations.Any())
                        {
                            SelectedLocation = Locations.First();
                            LocationPhoto = SelectedLocation?.Photo ?? string.Empty;
                        }

                        IsInitialized = true;
                    }
                    else
                    {
                        await HandleErrorAsync(OperationErrorSource.Database, "No valid locations found in the database");
                    }
                }
                else
                {
                    var errorMsg = result?.ErrorMessage ?? "Failed to retrieve locations from database";
                    await HandleErrorAsync(OperationErrorSource.Database, errorMsg);
                }
            }
            catch (OperationCanceledException)
            {
                // Operation was cancelled, this is normal
                CalculationStatus = "Location loading cancelled";
            }
            catch (ArgumentNullException ex)
            {
                await HandleErrorAsync(OperationErrorSource.Database, $"Database configuration error: {ex.ParamName}");
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Database, $"Error loading locations: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
                _operationLock.Release();
            }
        }

        public async Task LoadEquipmentAsync()
        {
            if (!await _operationLock.WaitAsync(100))
                return;

            try
            {
                IsLoadingEquipment = true;
                HasError = false;

                // Load user's cameras first, then all cameras if user has none
                var userCamerasResult = await _cameraBodyRepository.GetUserCamerasAsync(_cancellationTokenSource.Token);
                var userLensesResult = await _lensRepository.GetUserLensesAsync(_cancellationTokenSource.Token);

                var cameras = new List<CameraBody>();
                var lenses = new List<Lens>();

                if (userCamerasResult.IsSuccess && userCamerasResult.Data?.Any() == true)
                {
                    cameras.AddRange(userCamerasResult.Data);
                }
                else
                {
                    // Fallback to paged cameras if no user cameras
                    var allCamerasResult = await _cameraBodyRepository.GetPagedAsync(0, 50, _cancellationTokenSource.Token);
                    if (allCamerasResult.IsSuccess && allCamerasResult.Data?.Any() == true)
                    {
                        cameras.AddRange(allCamerasResult.Data);
                    }
                }

                if (userLensesResult.IsSuccess && userLensesResult.Data?.Any() == true)
                {
                    lenses.AddRange(userLensesResult.Data);
                }
                else
                {
                    // Fallback to paged lenses if no user lenses
                    var allLensesResult = await _lensRepository.GetPagedAsync(0, 50, _cancellationTokenSource.Token);
                    if (allLensesResult.IsSuccess && allLensesResult.Data?.Any() == true)
                    {
                        lenses.AddRange(allLensesResult.Data);
                    }
                }

                AvailableCameras = new ObservableCollection<CameraBody>(cameras);
                AvailableLenses = new ObservableCollection<Lens>(lenses);

                // Auto-select equipment suitable for current target
                await AutoSelectOptimalEquipmentAsync();
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Database, $"Error loading equipment: {ex.Message}");
            }
            finally
            {
                IsLoadingEquipment = false;
            }
        }

        public async Task SelectCameraAsync(CameraBody camera)
        {
            if (camera == null) return;

            try
            {
                SelectedCamera = camera;

                // Filter compatible lenses when camera changes
                await UpdateCompatibleLensesAsync();

                // Recalculate if we have all required selections
                if (CanCalculate)
                {
                    await CalculateAstroDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Validation, $"Error selecting camera: {ex.Message}");
            }
        }

        public async Task SelectLensAsync(Lens lens)
        {
            if (lens == null) return;

            try
            {
                SelectedLens = lens;

                // Recalculate if we have all required selections
                if (CanCalculate)
                {
                    await CalculateAstroDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Validation, $"Error selecting lens: {ex.Message}");
            }
        }

        public async Task SelectTargetAsync(AstroTarget target)
        {
            if (target == SelectedTarget) return;

            try
            {
                SelectedTarget = target;

                // Auto-select optimal equipment for new target
                await AutoSelectOptimalEquipmentAsync();

                // Recalculate for new target
                if (CanCalculate)
                {
                    await CalculateAstroDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Validation, $"Error selecting target: {ex.Message}");
            }
        }

        private async Task UpdateCompatibleLensesAsync()
        {
            if (SelectedCamera == null) return;

            try
            {
                var compatibleLensesResult = await _lensRepository.GetCompatibleLensesAsync(
                    SelectedCamera.Id, _cancellationTokenSource.Token);

                if (compatibleLensesResult.IsSuccess && compatibleLensesResult.Data?.Any() == true)
                {
                    var compatibleLenses = compatibleLensesResult.Data;

                    // Keep only compatible lenses in the available list
                    var filteredLenses = AvailableLenses.Where(l => compatibleLenses.Any(cl => cl.Id == l.Id)).ToList();
                    AvailableLenses = new ObservableCollection<Lens>(filteredLenses);

                    // Reselect lens if still compatible, otherwise clear selection
                    if (SelectedLens != null && !compatibleLenses.Any(l => l.Id == SelectedLens.Id))
                    {
                        SelectedLens = null;
                        await AutoSelectOptimalEquipmentAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error filtering compatible lenses: {ex.Message}");
            }
        }

        private async Task AutoSelectOptimalEquipmentAsync()
        {
            try
            {
                // Get optimal equipment specs for current target
                var optimalSpecs = GetOptimalEquipmentSpecs(SelectedTarget);

                // Select camera if none selected - prefer user cameras
                if (SelectedCamera == null && AvailableCameras.Any())
                {
                    var optimalCamera = AvailableCameras
                        .Where(c => c.IsUserCreated) // Prefer user cameras
                        .FirstOrDefault(c => IsOptimalCameraForTarget(c, SelectedTarget)) ??
                        AvailableCameras.FirstOrDefault(c => IsOptimalCameraForTarget(c, SelectedTarget)) ??
                        AvailableCameras.First(); // Fallback to first available

                    await SelectCameraAsync(optimalCamera);
                }

                // Select lens if none selected - prefer user lenses that match target requirements
                if (SelectedLens == null && AvailableLenses.Any())
                {
                    var optimalLens = FindOptimalUserLens(optimalSpecs) ??
                                    FindOptimalLens(optimalSpecs) ??
                                    AvailableLenses.First();

                    await SelectLensAsync(optimalLens);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error auto-selecting equipment: {ex.Message}");
            }
        }

        private Lens FindOptimalUserLens(OptimalEquipmentSpecs specs)
        {
            var userLenses = AvailableLenses.Where(l => l.IsUserCreated).ToList();
            if (!userLenses.Any()) return null;

            // Find exact focal length match first
            var exactMatch = userLenses.FirstOrDefault(l =>
                (l.IsPrime && Math.Abs(l.MinMM - specs.OptimalFocalLength) <= 5) ||
                (!l.IsPrime && l.MaxMM.HasValue && specs.OptimalFocalLength >= l.MinMM && specs.OptimalFocalLength <= l.MaxMM.Value));

            if (exactMatch != null) return exactMatch;

            // Find focal length range match
            var rangeMatch = userLenses.FirstOrDefault(l =>
                (!l.IsPrime && l.MaxMM.HasValue &&
                 specs.MinFocalLength >= l.MinMM && specs.MinFocalLength <= l.MaxMM.Value) ||
                (!l.IsPrime && l.MaxMM.HasValue &&
                 specs.MaxFocalLength >= l.MinMM && specs.MaxFocalLength <= l.MaxMM.Value));

            if (rangeMatch != null) return rangeMatch;

            // Find aperture match within broader focal length tolerance
            return userLenses.FirstOrDefault(l => l.MaxFStop <= specs.MaxAperture);
        }

        private Lens FindOptimalLens(OptimalEquipmentSpecs specs)
        {
            // Find best match from all available lenses using same logic as user lenses
            var exactMatch = AvailableLenses.FirstOrDefault(l =>
                (l.IsPrime && Math.Abs(l.MinMM - specs.OptimalFocalLength) <= 5) ||
                (!l.IsPrime && l.MaxMM.HasValue && specs.OptimalFocalLength >= l.MinMM && specs.OptimalFocalLength <= l.MaxMM.Value));

            return exactMatch ?? AvailableLenses.FirstOrDefault(l => l.MaxFStop <= specs.MaxAperture);
        }

        private bool IsOptimalCameraForTarget(CameraBody camera, AstroTarget target)
        {
            return true;
        }

        public void OnSelectedLocationChanged(LocationListItemViewModel value)
        {
            if (value != null)
            {
                LocationPhoto = value.Photo ?? string.Empty;
                _ = Task.Run(async () =>
                {
                    if (CanCalculate)
                        await CalculateAstroDataAsync();
                });
            }
        }

        public void OnSelectedDateChanged(DateTime value)
        {
            _ = Task.Run(async () =>
            {
                if (CanCalculate)
                    await CalculateAstroDataAsync();
            });
        }
        #endregion

        // Location.Photography.ViewModels/AstroPhotographyCalculatorViewModel.cs - Part 4
        #region Astro Calculation Commands and Core Methods

        public async Task CalculateAstroDataAsync()
        {
            if (!CanCalculate || !await _operationLock.WaitAsync(100))
                return;

            try
            {
                // Throttle calculations to avoid excessive API calls
                var timeSinceLastCalculation = DateTime.Now - _lastCalculationTime;
                if (timeSinceLastCalculation.TotalMilliseconds < CALCULATION_THROTTLE_MS)
                {
                    await Task.Delay(CALCULATION_THROTTLE_MS - (int)timeSinceLastCalculation.TotalMilliseconds);
                }

                IsCalculating = true;
                HasError = false;
                CalculationStatus = "Calculating astronomical data...";

                // Check cache first
                var cacheKey = GenerateCacheKey(SelectedLocation, SelectedDate, SelectedTarget, SelectedCamera, SelectedLens);
                if (_calculationCache.TryGetValue(cacheKey, out var cachedResult) && !cachedResult.IsExpired)
                {
                    await ApplyCachedResultsAsync(cachedResult);
                    return;
                }

                // Clear expired cache entries
                ClearExpiredCache();

                // Convert times for calculations (following EnhancedSunCalculatorViewModel pattern)
                var utcCalculationDateTime = ConvertToUtcForCalculation(SelectedDate, TimeZoneInfo.Local.ToString());
                var calculations = new List<AstroCalculationResult>();

                // Perform target-specific calculations
                await PerformTargetCalculationsAsync(calculations, utcCalculationDateTime);

                // Calculate equipment-specific recommendations
                await CalculateEquipmentRecommendationsAsync(calculations);

                // Calculate field of view data
                await CalculateFieldOfViewAsync();

                // Cache results
                var newCacheEntry = new CachedAstroCalculation
                {
                    Timestamp = DateTime.Now,
                    Location = SelectedLocation,
                    CalculationDate = SelectedDate,
                    Target = SelectedTarget,
                    Camera = SelectedCamera,
                    Lens = SelectedLens,
                    Results = calculations
                };
                _calculationCache[cacheKey] = newCacheEntry;

                CurrentCalculations = new ObservableCollection<AstroCalculationResult>(calculations);
                _lastCalculationTime = DateTime.Now;

                CalculationCompleted?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                CalculationStatus = "Calculation cancelled";
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Network, $"Calculation error: {ex.Message}");
            }
            finally
            {
                IsCalculating = false;
                if (string.IsNullOrEmpty(ErrorMessage))
                {
                    CalculationStatus = "Calculations complete";
                }
                _operationLock.Release();
            }
        }

        private async Task PerformTargetCalculationsAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            try
            {
                switch (SelectedTarget)
                {
                    case AstroTarget.MilkyWayCore:
                        await CalculateMilkyWayDataAsync(calculations, utcDateTime);
                        break;
                    case AstroTarget.Moon:
                        await CalculateMoonDataAsync(calculations, utcDateTime);
                        break;
                    case AstroTarget.Planets:
                        await CalculatePlanetDataAsync(calculations, utcDateTime);
                        break;
                    case AstroTarget.DeepSkyObjects:
                        await CalculateDeepSkyDataAsync(calculations, utcDateTime);
                        break;
                    case AstroTarget.StarTrails:
                        await CalculateStarTrailDataAsync(calculations, utcDateTime);
                        break;
                    case AstroTarget.MeteorShowers:
                        await CalculateMeteorShowerDataAsync(calculations, utcDateTime);
                        break;
                    case AstroTarget.Constellations:
                        await CalculateConstellationDataAsync(calculations, utcDateTime);
                        break;
                    case AstroTarget.PolarAlignment:
                        await CalculatePolarAlignmentDataAsync(calculations, utcDateTime);
                        break;
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Network, $"Error calculating {SelectedTarget} data: {ex.Message}");
            }
        }

        private async Task CalculateMilkyWayDataAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            CalculationStatus = "Calculating Milky Way visibility...";

            var milkyWayData = await _astroCalculationService.GetMilkyWayDataAsync(
                utcDateTime, SelectedLocation.Latitude, SelectedLocation.Longitude);

            var result = new AstroCalculationResult
            {
                Target = AstroTarget.MilkyWayCore,
                CalculationTime = utcDateTime,
                LocalTime = TimeZoneInfo.ConvertTime(utcDateTime, TimeZoneInfo.Local),
                IsVisible = milkyWayData.IsVisible,
                Azimuth = milkyWayData.GalacticCenterAzimuth,
                Altitude = milkyWayData.GalacticCenterAltitude,
                RiseTime = TimeZoneInfo.ConvertTime((DateTime)milkyWayData.Rise, TimeZoneInfo.Local),
                SetTime = TimeZoneInfo.ConvertTime((DateTime)milkyWayData.Set, TimeZoneInfo.Local),
                OptimalTime = TimeZoneInfo.ConvertTime((DateTime)milkyWayData.OptimalViewingTime, TimeZoneInfo.Local),
                Description = $"Galactic center visibility: {milkyWayData.Season}",
                PhotographyNotes = milkyWayData.PhotographyRecommendations
            };

            calculations.Add(result);
            TargetCalculated?.Invoke(this, result);
        }

        private async Task CalculateMoonDataAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            CalculationStatus = "Calculating lunar data...";

            var moonData = await _astroCalculationService.GetEnhancedMoonDataAsync(
                utcDateTime, SelectedLocation.Latitude, SelectedLocation.Longitude);

            var result = new AstroCalculationResult
            {
                Target = AstroTarget.Moon,
                CalculationTime = utcDateTime,
                LocalTime = TimeZoneInfo.ConvertTime(utcDateTime, TimeZoneInfo.Local),
                IsVisible = moonData.Altitude > 0,
                Azimuth = moonData.Azimuth,
                Altitude = moonData.Altitude,
                RiseTime = TimeZoneInfo.ConvertTime((DateTime)moonData.Rise, TimeZoneInfo.Local),
                    SetTime = TimeZoneInfo.ConvertTime((DateTime)moonData.Set, TimeZoneInfo.Local),
                    Description = $"Phase: {moonData.PhaseName} ({moonData.OpticalLibration:F0}%)",
                PhotographyNotes = $"Angular size: {moonData.AngularDiameter:F1}' | Distance: {moonData.Distance:F0} km"
            };

            calculations.Add(result);
            TargetCalculated?.Invoke(this, result);
        }

        private async Task CalculatePlanetDataAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            CalculationStatus = "Calculating visible planets...";

            var visiblePlanets = await _astroCalculationService.GetVisiblePlanetsAsync(
                utcDateTime, SelectedLocation.Latitude, SelectedLocation.Longitude);

            foreach (var planet in visiblePlanets.Take(3)) // Limit to top 3 for UI
            {
                var result = new AstroCalculationResult
                {
                    Target = AstroTarget.Planets,
                    CalculationTime = utcDateTime,
                    LocalTime = TimeZoneInfo.ConvertTime(utcDateTime, TimeZoneInfo.Local),
                    IsVisible = planet.IsVisible,
                    Azimuth = planet.Azimuth,
                    Altitude = planet.Altitude,
                    RiseTime = TimeZoneInfo.ConvertTime((DateTime)planet.Rise, TimeZoneInfo.Local),
                    SetTime = TimeZoneInfo.ConvertTime((DateTime)planet.Set, TimeZoneInfo.Local),
                    OptimalTime = TimeZoneInfo.ConvertTime((DateTime)planet.Transit, TimeZoneInfo.Local),
                    Description = $"{planet.Planet}: Magnitude {planet.ApparentMagnitude:F1}",
                    PhotographyNotes = planet.PhotographyNotes,
                    PlanetData = planet
                };

                calculations.Add(result);
                TargetCalculated?.Invoke(this, result);
            }
        }

        private async Task CalculateDeepSkyDataAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            CalculationStatus = "Calculating deep sky objects...";

            // Get popular deep sky objects for the current season
            var dsoData = await _astroCalculationService.GetDeepSkyObjectDataAsync(
                "M42", utcDateTime, SelectedLocation.Latitude, SelectedLocation.Longitude); // Example: Orion Nebula

            var result = new AstroCalculationResult
            {
                Target = AstroTarget.DeepSkyObjects,
                CalculationTime = utcDateTime,
                LocalTime = TimeZoneInfo.ConvertTime(utcDateTime, TimeZoneInfo.Local),
                IsVisible = dsoData.IsVisible,
                Azimuth = dsoData.Azimuth,
                Altitude = dsoData.Altitude,
                RiseTime = null,// TimeZoneInfo.ConvertTime(dsoData.Rise, TimeZoneInfo.Local),
                SetTime = null, //TimeZoneInfo.ConvertTime(dsoData.Set, TimeZoneInfo.Local),
                OptimalTime = TimeZoneInfo.ConvertTime((DateTime)dsoData.OptimalViewingTime, TimeZoneInfo.Local),
                Description = $"{dsoData.CommonName}: {dsoData.ObjectType}",
                PhotographyNotes = dsoData.ExposureGuidance,
                Equipment = dsoData.RecommendedEquipment
            };

            calculations.Add(result);
            TargetCalculated?.Invoke(this, result);
        }

        private async Task CalculateStarTrailDataAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            CalculationStatus = "Calculating star trail data...";

            var starTrailData = await _astroCalculationService.GetStarTrailDataAsync(
                utcDateTime, TimeSpan.FromHours(2), SelectedLocation.Latitude, SelectedLocation.Longitude);

            var result = new AstroCalculationResult
            {
                Target = AstroTarget.StarTrails,
                CalculationTime = utcDateTime,
                LocalTime = TimeZoneInfo.ConvertTime((DateTime)utcDateTime, TimeZoneInfo.Local),
                IsVisible = true, // Star trails always possible
                Description = $"Trail length: {starTrailData.StarTrailLength:F1}° over 2 hours",
                PhotographyNotes = starTrailData.OptimalComposition
            };

            calculations.Add(result);
            TargetCalculated?.Invoke(this, result);
        }

        private async Task CalculateMeteorShowerDataAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            CalculationStatus = "Calculating meteor shower data...";

            var meteorShowers = await _astroCalculationService.GetMeteorShowersAsync(
                utcDateTime.Date, utcDateTime.Date.AddDays(30), SelectedLocation.Latitude, SelectedLocation.Longitude);

            var activeShower = meteorShowers.FirstOrDefault(s => s.ActivityStart <= utcDateTime && s.ActivityEnd >= utcDateTime);

            if (activeShower != null)
            {
                var result = new AstroCalculationResult
                {
                    Target = AstroTarget.MeteorShowers,
                    CalculationTime = utcDateTime,
                    LocalTime = TimeZoneInfo.ConvertTime((DateTime)utcDateTime, TimeZoneInfo.Local),
                    IsVisible = activeShower.OptimalConditions,
                    Azimuth = activeShower.RadiantAzimuth,
                    Altitude = activeShower.RadiantAltitude,
                    Description = $"{activeShower.Name} - ZHR: {activeShower.ZenithHourlyRate}",
                    PhotographyNotes = activeShower.PhotographyStrategy
                };

                calculations.Add(result);
                TargetCalculated?.Invoke(this, result);
            }
        }

        private async Task CalculateConstellationDataAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            CalculationStatus = "Calculating constellation visibility...";

            var constellationData = await _astroCalculationService.GetConstellationDataAsync(
                ConstellationType.Orion, utcDateTime, SelectedLocation.Latitude, SelectedLocation.Longitude);

            var result = new AstroCalculationResult
            {
                Target = AstroTarget.Constellations,
                CalculationTime = utcDateTime,
                LocalTime = TimeZoneInfo.ConvertTime((DateTime)utcDateTime, TimeZoneInfo.Local),
                IsVisible = constellationData.CenterAltitude > 0,
                Azimuth = constellationData.CenterAzimuth,
                Altitude = constellationData.CenterAltitude,
                RiseTime = TimeZoneInfo.ConvertTime((DateTime)constellationData.Rise, TimeZoneInfo.Local),
                SetTime = TimeZoneInfo.ConvertTime((DateTime)constellationData.Set, TimeZoneInfo.Local),
                Description = $"{constellationData.Constellation} constellation",
                PhotographyNotes = constellationData.PhotographyNotes
            };

            calculations.Add(result);
            TargetCalculated?.Invoke(this, result);
        }

        private async Task CalculatePolarAlignmentDataAsync(List<AstroCalculationResult> calculations, DateTime utcDateTime)
        {
            CalculationStatus = "Calculating polar alignment data...";

            var polarData = await _astroCalculationService.GetPolarAlignmentDataAsync(
                utcDateTime, SelectedLocation.Latitude, SelectedLocation.Longitude);

            var result = new AstroCalculationResult
            {
                Target = AstroTarget.PolarAlignment,
                CalculationTime = utcDateTime,
                LocalTime = TimeZoneInfo.ConvertTime((DateTime)utcDateTime, TimeZoneInfo.Local),
                IsVisible = true,
                Azimuth = polarData.PolarisAzimuth,
                Altitude = polarData.PolarisAltitude,
                Description = $"Polar star at {polarData.PolarisAltitude:F1}° altitude",
                PhotographyNotes = polarData.AlignmentInstructions
            };

            calculations.Add(result);
            TargetCalculated?.Invoke(this, result);
        }

        public async Task RefreshCalculationsAsync()
        {
            // Clear cache and force recalculation
            _calculationCache.Clear();
            await CalculateAstroDataAsync();
        }

        public async Task RetryLastCommandAsync()
        {
            try
            {
                HasError = false;
                ErrorMessage = string.Empty;

                if (!IsInitialized)
                {
                    await LoadLocationsAsync();
                    await LoadEquipmentAsync();
                }
                else if (CanCalculate)
                {
                    await CalculateAstroDataAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Network, $"Retry failed: {ex.Message}");
            }
        }

        // Time conversion methods following EnhancedSunCalculatorViewModel pattern
        private DateTime ConvertToUtcForCalculation(DateTime localDate, string timezone)
        {
            try
            {
                // Convert local date to UTC for astronomical calculations
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return TimeZoneInfo.ConvertTimeToUtc(localDate, timeZoneInfo);
            }
            catch
            {
                // Fallback to UTC if timezone conversion fails
                return localDate.ToUniversalTime();
            }
        }

        private DateTime? ConvertUtcToLocalTime(DateTime utcTime, string timezone)
        {
            //if (!utcTime.) return null;

            try
            {
                var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(timezone);
                return TimeZoneInfo.ConvertTimeFromUtc(utcTime, timeZoneInfo);
            }
            catch
            {
                return utcTime.ToLocalTime();
            }
        }

        private async Task ApplyCachedResultsAsync(CachedAstroCalculation cachedResult)
        {
            CurrentCalculations = new ObservableCollection<AstroCalculationResult>(cachedResult.Results);
            CalculationStatus = $"Using cached data from {cachedResult.Timestamp:HH:mm}";

            // Still calculate equipment recommendations as they might have changed
            await CalculateEquipmentRecommendationsAsync(cachedResult.Results);
            await CalculateFieldOfViewAsync();
        }
        #endregion

        // Location.Photography.ViewModels/AstroPhotographyCalculatorViewModel.cs - Part 5
        #region Equipment Recommendations and Helper Methods

        private async Task CalculateEquipmentRecommendationsAsync(List<AstroCalculationResult> calculations)
        {
            try
            {
                if (SelectedCamera == null || SelectedLens == null)
                {
                    await GenerateGenericEquipmentRecommendationAsync();
                    return;
                }

                var optimalSpecs = GetOptimalEquipmentSpecs(SelectedTarget);
                var equipmentData = new CameraEquipmentData
                {
                    SensorWidth = SelectedCamera.SensorWidth,
                    SensorHeight = SelectedCamera.SensorHeight,
                   
                    FocalLength = SelectedLens.IsPrime ? SelectedLens.MinMM : (SelectedLens.MinMM + SelectedLens.MaxMM.GetValueOrDefault()) / 2,
                    HasTracker = false // Could be user preference
                };

                var astroConditions = new AstroConditions
                {
                    BortleScale = 4, // Average Bortle scale for suburban areas
                    //LightPollution = 3.0, // Could be calculated based on location
                    Seeing = 2.5, // Average seeing
                    CloudCover = 0.1, // Clear skies assumption
                    Humidity = 0.5
                };

                CalculationStatus = "Generating equipment recommendations...";

                var exposureRec = await _astroCalculationService.GetAstroExposureRecommendationAsync(
                    SelectedTarget, equipmentData, astroConditions);

                // Check if user's equipment is optimal
                var isUserEquipmentOptimal = IsUserEquipmentOptimal(optimalSpecs);

                if (isUserEquipmentOptimal)
                {
                    ExposureRecommendation = $"{exposureRec.RecommendedISO}, {exposureRec.RecommendedAperture}, {exposureRec.RecommendedShutterSpeed}";
                    EquipmentRecommendation = $"✓ Your {SelectedLens.NameForLens} on {SelectedCamera.Name} is excellent for {SelectedTargetDisplay}";
                    PhotographyNotes = $"{exposureRec.FocusingTechnique}\n\n{string.Join("\n", exposureRec.ProcessingNotes)}";
                }
                else
                {
                    // Generate recommendation for what user needs
                    var genericRecommendation = GenerateGenericEquipmentRecommendation(optimalSpecs);
                    ExposureRecommendation = $"{exposureRec.RecommendedISO}, {exposureRec.RecommendedAperture}, {exposureRec.RecommendedShutterSpeed}";
                    EquipmentRecommendation = $"Consider: {genericRecommendation}\nCurrent: {SelectedLens.NameForLens} on {SelectedCamera.Name}";
                    PhotographyNotes = $"For optimal results: {genericRecommendation}\n\n{exposureRec.FocusingTechnique}";
                }
            }
            catch (Exception ex)
            {
                EquipmentRecommendation = "Unable to generate equipment recommendations";
                PhotographyNotes = $"Error: {ex.Message}";
            }
        }

        private async Task GenerateGenericEquipmentRecommendationAsync()
        {
            var optimalSpecs = GetOptimalEquipmentSpecs(SelectedTarget);
            var genericRecommendation = GenerateGenericEquipmentRecommendation(optimalSpecs);

            EquipmentRecommendation = $"Recommended: {genericRecommendation}";
            ExposureRecommendation = optimalSpecs.RecommendedSettings;
            PhotographyNotes = optimalSpecs.Notes;
        }

        private OptimalEquipmentSpecs GetOptimalEquipmentSpecs(AstroTarget target)
        {
            return target switch
            {
                AstroTarget.MilkyWayCore => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    OptimalFocalLength = 24,
                    MaxAperture = 2.8,
                    MinISO = 1600,
                    MaxISO = 6400,
                    RecommendedSettings = "ISO 3200, f/2.8, 20-25 seconds",
                    Notes = "Wide-angle lens essential for capturing galactic arch. Fast aperture critical for light gathering."
                },
                AstroTarget.Moon => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 200,
                    MaxFocalLength = 800,
                    OptimalFocalLength = 400,
                    MaxAperture = 8.0,
                    MinISO = 100,
                    MaxISO = 800,
                    RecommendedSettings = "ISO 200, f/8, 1/125s",
                    Notes = "Telephoto lens for detail. Moon is bright - low ISO and fast shutter prevent overexposure."
                },
                AstroTarget.Planets => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 300,
                    MaxFocalLength = 1000,
                    OptimalFocalLength = 600,
                    MaxAperture = 6.3,
                    MinISO = 800,
                    MaxISO = 3200,
                    RecommendedSettings = "ISO 1600, f/5.6, 1/60s",
                    Notes = "Long telephoto essential. Planets are small - maximum focal length recommended."
                },
                AstroTarget.DeepSkyObjects => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 50,
                    MaxFocalLength = 300,
                    OptimalFocalLength = 135,
                    MaxAperture = 4.0,
                    MinISO = 1600,
                    MaxISO = 12800,
                    RecommendedSettings = "ISO 6400, f/4, 4-8 minutes",
                    Notes = "Medium telephoto for framing. Very high ISO capability needed. Tracking mount essential."
                },
                AstroTarget.StarTrails => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 14,
                    MaxFocalLength = 50,
                    OptimalFocalLength = 24,
                    MaxAperture = 4.0,
                    MinISO = 100,
                    MaxISO = 800,
                    RecommendedSettings = "ISO 400, f/4, 30s intervals",
                    Notes = "Wide-angle for interesting compositions. Multiple exposures combined in post-processing."
                },
                AstroTarget.MeteorShowers => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 14,
                    MaxFocalLength = 35,
                    OptimalFocalLength = 24,
                    MaxAperture = 2.8,
                    MinISO = 1600,
                    MaxISO = 6400,
                    RecommendedSettings = "ISO 3200, f/2.8, 15-30s",
                    Notes = "Wide field to capture meteors. Point 45-60° away from radiant for longer trails."
                },
                AstroTarget.Constellations => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 35,
                    MaxFocalLength = 135,
                    OptimalFocalLength = 85,
                    MaxAperture = 4.0,
                    MinISO = 800,
                    MaxISO = 3200,
                    RecommendedSettings = "ISO 1600, f/4, 60s",
                    Notes = "Medium lens for constellation framing. Balance stars with constellation patterns."
                },
                AstroTarget.PolarAlignment => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 50,
                    MaxFocalLength = 200,
                    OptimalFocalLength = 100,
                    MaxAperture = 5.6,
                    MinISO = 800,
                    MaxISO = 3200,
                    RecommendedSettings = "ISO 1600, f/5.6, 30s",
                    Notes = "Medium telephoto to see Polaris clearly. Used for mount alignment verification."
                },
                _ => new OptimalEquipmentSpecs
                {
                    MinFocalLength = 24,
                    MaxFocalLength = 200,
                    OptimalFocalLength = 50,
                    MaxAperture = 4.0,
                    MinISO = 1600,
                    MaxISO = 6400,
                    RecommendedSettings = "ISO 3200, f/4, 30s",
                    Notes = "General astrophotography setup."
                }
            };
        }

        private string GenerateGenericEquipmentRecommendation(OptimalEquipmentSpecs specs)
        {
            var focalLengthDesc = specs.MinFocalLength == specs.MaxFocalLength
                ? $"{specs.OptimalFocalLength}mm"
                : $"{specs.MinFocalLength}-{specs.MaxFocalLength}mm";

            var apertureDesc = specs.MaxAperture <= 2.8 ? "fast" : specs.MaxAperture <= 4.0 ? "moderate" : "standard";

            return $"{focalLengthDesc} f/{specs.MaxAperture} {apertureDesc} lens";
        }

        private bool IsUserEquipmentOptimal(OptimalEquipmentSpecs specs)
        {
            if (SelectedLens == null || SelectedCamera == null) return false;

            // Check focal length match
            var userFocalLength = SelectedLens.IsPrime ? SelectedLens.MinMM :
                (SelectedLens.MinMM + SelectedLens.MaxMM.GetValueOrDefault()) / 2;

            var focalLengthMatch = userFocalLength >= specs.MinFocalLength && userFocalLength <= specs.MaxFocalLength;

            // Check aperture capability
            var apertureMatch = SelectedLens.MaxFStop <= specs.MaxAperture;

            // Check ISO capability
            var isoMatch = true;

            return focalLengthMatch && apertureMatch && isoMatch;
        }

        private async Task CalculateFieldOfViewAsync()
        {
            // Skip field of view calculation when no equipment is selected
            FieldOfViewWidth = 0;
            FieldOfViewHeight = 0;
            TargetFitsInFrame = false;

            await Task.CompletedTask; // Keep async signature
        }

        #endregion

        #region Error Handling and Events

        private async Task HandleErrorAsync(OperationErrorSource source, string message)
        {
            HasError = true;
            ErrorMessage = message;
            CalculationStatus = "Error occurred";

            var errorArgs = new OperationErrorEventArgs(source, message);
            ErrorOccurred?.Invoke(this, errorArgs);

            // Log error for debugging
            Debug.WriteLine($"AstroPhotographyCalculator Error [{source}]: {message}");

            await Task.CompletedTask;
        }

        #endregion

        #region Navigation Awareness

        public async Task OnNavigatedToAsync()
        {
            try
            {
                if (!IsInitialized)
                {
                    await LoadLocationsAsync();
                }
            }
            catch (Exception ex)
            {
                await HandleErrorAsync(OperationErrorSource.Navigation, $"Error during navigation: {ex.Message}");
            }
        }

        public async Task OnNavigatedFromAsync()
        {
            // Cancel any ongoing operations when leaving the page
            CancelAllOperations();
            await Task.CompletedTask;
        }

        public void CancelAllOperations()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Supporting Models

        public class OptimalEquipmentSpecs
        {
            public double MinFocalLength { get; set; }
            public double MaxFocalLength { get; set; }
            public double OptimalFocalLength { get; set; }
            public double MaxAperture { get; set; }
            public int MinISO { get; set; }
            public int MaxISO { get; set; }
            public string RecommendedSettings { get; set; } = string.Empty;
            public string Notes { get; set; } = string.Empty;
        }

        public class AstroTargetDisplayModel
        {
            public AstroTarget Target { get; set; }
            public string DisplayName { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
        }

        

        #endregion

        #region Disposal

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                CancelAllOperations();
                _operationLock?.Dispose();
                _cancellationTokenSource?.Dispose();
            }
            base.Dispose();
        }

        ~AstroPhotographyCalculatorViewModel()
        {
            Dispose(false);
        }

        #endregion
    }
}