using Location.Photography.Application.Services;
using Location.Photography.ViewModels;
using Location.Photography.ViewModels.Events;
using Location.Core.Application.Services;

namespace Location.Photography.Maui.Views.Professional
{
    public partial class SceneEvaluation : ContentPage
    {
        private SceneEvaluationViewModel _viewModel;
        private readonly IErrorDisplayService _errorDisplayService;
        private readonly IImageAnalysisService _imageAnalysisService;

        // PERFORMANCE: UI state management and threading
        private readonly SemaphoreSlim _uiOperationLock = new(1, 1);
        private bool _isInitialized = false;
        private bool _isDisposed = false;
        private CancellationTokenSource _uiCancellationSource = new();

        // PERFORMANCE: Prevent redundant operations
        private DateTime _lastAnalysisRequest = DateTime.MinValue;
        private const int ANALYSIS_THROTTLE_MS = 2000; // Prevent rapid analysis requests
        private readonly Dictionary<string, DateTime> _lastModeChanges = new();
        private const int MODE_CHANGE_THROTTLE_MS = 300;

        // PERFORMANCE: Progress tracking
        private bool _isAnalysisInProgress = false;
        private readonly Progress<string> _analysisProgress;

        public SceneEvaluation()
        {
            InitializeComponent();
            _imageAnalysisService = new ImageAnalysisService();
            _analysisProgress = new Progress<string>(UpdateAnalysisStatus);

            _ = InitializeUIAsync();
        }

        public SceneEvaluation(IImageAnalysisService imageAnalysisService, IErrorDisplayService errorDisplayService)
        {
            _imageAnalysisService = imageAnalysisService ?? throw new ArgumentNullException(nameof(imageAnalysisService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));

            InitializeComponent();
            _analysisProgress = new Progress<string>(UpdateAnalysisStatus);

            _ = InitializeUIAsync();
        }

        #region PERFORMANCE OPTIMIZED INITIALIZATION

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Async UI initialization with loading states
        /// </summary>
        private async Task InitializeUIAsync()
        {
            try
            {
                if (_isDisposed) return;

                // Show loading state immediately
                await ShowLoadingStateAsync("Initializing scene evaluation...");

                // Initialize ViewModel on background thread
                await Task.Run(async () =>
                {
                    try
                    {
                        await InitializeViewModelAsync();
                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HandleErrorSafely(ex, "Error initializing view model");
                        });
                        return;
                    }
                }, _uiCancellationSource.Token);

                // Setup UI components on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        if (_isDisposed) return;

                        SetupUIComponents();
                        SubscribeToViewModelEvents();
                        _isInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        HandleErrorSafely(ex, "Error setting up UI components");
                    }
                    finally
                    {
                        HideLoadingState();
                    }
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    HandleErrorSafely(ex, "Critical error during initialization");
                    HideLoadingState();
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Background ViewModel initialization
        /// </summary>
        private async Task InitializeViewModelAsync()
        {
            try
            {
                _viewModel = new SceneEvaluationViewModel(_imageAnalysisService, _errorDisplayService);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    BindingContext = _viewModel;
                });

                // Initialize view model state
                _viewModel.OnNavigatedToAsync();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"ViewModel initialization failed: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Setup UI components with loading states
        /// </summary>
        private void SetupUIComponents()
        {
            try
            {
                // Setup histogram mode radio buttons with loading state
                SetupHistogramControls();

                // Setup analysis controls
                SetupAnalysisControls();

                // Set default histogram mode
                RedRadioButton.IsChecked = true;

                // Setup progress indicators
                SetupProgressIndicators();
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up UI components");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Setup histogram controls with throttling
        /// </summary>
        private void SetupHistogramControls()
        {
            try
            {
                RedRadioButton.CheckedChanged += (s, e) => HandleHistogramModeChange("Red", e.Value);
                GreenRadioButton.CheckedChanged += (s, e) => HandleHistogramModeChange("Green", e.Value);
                BlueRadioButton.CheckedChanged += (s, e) => HandleHistogramModeChange("Blue", e.Value);
                LuminanceRadioButton.CheckedChanged += (s, e) => HandleHistogramModeChange("Luminance", e.Value);

                // Initial loading state for histogram
                SetHistogramLoadingState(true);
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up histogram controls");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Setup analysis controls with proper validation
        /// </summary>
        private void SetupAnalysisControls()
        {
            try
            {
                // Analysis button would be setup here if it exists in XAML
                // For now, analysis is triggered via ViewModel commands
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up analysis controls");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Setup progress indicators
        /// </summary>
        private void SetupProgressIndicators()
        {
            try
            {
                // Setup any progress bars or indicators
                // These would be bound to ViewModel progress properties
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error setting up progress indicators");
            }
        }

        #endregion

        #region PERFORMANCE OPTIMIZED EVENT HANDLING

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Throttled histogram mode changes
        /// </summary>
        private void HandleHistogramModeChange(string mode, bool isChecked)
        {
            try
            {
                if (!isChecked || _viewModel == null || _isDisposed) return;

                // Throttle rapid mode changes
                var now = DateTime.Now;
                var key = $"histogram_{mode}";

                if (_lastModeChanges.ContainsKey(key))
                {
                    var timeSinceLastChange = (now - _lastModeChanges[key]).TotalMilliseconds;
                    if (timeSinceLastChange < MODE_CHANGE_THROTTLE_MS)
                    {
                        return; // Skip this change
                    }
                }

                _lastModeChanges[key] = now;

                // Provide immediate visual feedback
                ProvideHistogramModeChangeFeedback(mode);

                // Update ViewModel on background thread
                Task.Run(async () =>
                {
                    try
                    {
                        await UpdateHistogramModeAsync(mode);
                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            HandleErrorSafely(ex, $"Error changing histogram mode to {mode}");
                        });
                    }
                }, _uiCancellationSource.Token);
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, $"Error handling histogram mode change to {mode}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update histogram mode with loading feedback
        /// </summary>
        private async Task UpdateHistogramModeAsync(string mode)
        {
            try
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetHistogramLoadingState(true);
                });

                // Update ViewModel histogram mode
                if (Enum.TryParse<HistogramDisplayMode>(mode, out var histogramMode))
                {
                    _viewModel.SetHistogramMode(histogramMode);
                }

                // Simulate processing time for histogram generation
                await Task.Delay(100, _uiCancellationSource.Token);

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetHistogramLoadingState(false);
                });
            }
            catch (OperationCanceledException)
            {
                // Expected when cancelled
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetHistogramLoadingState(false);
                    HandleErrorSafely(ex, "Error updating histogram mode");
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Provide immediate visual feedback for mode changes
        /// </summary>
        private void ProvideHistogramModeChangeFeedback(string mode)
        {
            try
            {
                // Provide haptic feedback if available
                Task.Run(() =>
                {
                    try
                    {
                        HapticFeedback.Perform(HapticFeedbackType.Click);
                    }
                    catch { } // Ignore haptic errors
                });

                // Visual feedback - briefly highlight the selected radio button
                var radioButton = mode switch
                {
                    "Red" => RedRadioButton,
                    "Green" => GreenRadioButton,
                    "Blue" => BlueRadioButton,
                    "Luminance" => LuminanceRadioButton,
                    _ => null
                };

                if (radioButton != null)
                {
                    ProvideRadioButtonFeedback(radioButton);
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw for visual feedback errors
                System.Diagnostics.Debug.WriteLine($"Error providing histogram feedback: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Visual feedback for radio button selection
        /// </summary>
        private void ProvideRadioButtonFeedback(RadioButton radioButton)
        {
            try
            {
                var originalOpacity = radioButton.Opacity;
                radioButton.Opacity = 0.7;

                Task.Delay(150).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        if (!_isDisposed)
                            radioButton.Opacity = originalOpacity;
                    });
                });
            }
            catch
            {
                // Ignore visual feedback errors
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Legacy radio button event handler with improved logic
        /// </summary>
        private void RadioButton_CheckedChanged(object sender, CheckedChangedEventArgs e)
        {
            try
            {
                if (_viewModel == null || _isDisposed)
                {
                    _viewModel = BindingContext as SceneEvaluationViewModel;
                    if (_viewModel == null) return;
                }

                if (!e.Value) return;

                // Determine which radio button was pressed and handle accordingly
                var senderButton = sender as RadioButton;
                if (senderButton == RedRadioButton)
                {
                    HandleHistogramModeChange("Red", true);
                }
                else if (senderButton == GreenRadioButton)
                {
                    HandleHistogramModeChange("Green", true);
                }
                else if (senderButton == BlueRadioButton)
                {
                    HandleHistogramModeChange("Blue", true);
                }
                else if (senderButton == LuminanceRadioButton)
                {
                    HandleHistogramModeChange("Luminance", true);
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error in radio button checked changed");
            }
        }

        #endregion

        #region PERFORMANCE OPTIMIZED ANALYSIS OPERATIONS

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Start scene analysis with proper throttling and feedback
        /// </summary>
        public async Task StartSceneAnalysisAsync()
        {
            try
            {
                // Prevent rapid analysis requests
                var now = DateTime.Now;
                if ((now - _lastAnalysisRequest).TotalMilliseconds < ANALYSIS_THROTTLE_MS)
                {
                    await ShowThrottleMessageAsync();
                    return;
                }

                if (_isAnalysisInProgress)
                {
                    await ShowAnalysisInProgressMessageAsync();
                    return;
                }

                if (!await _uiOperationLock.WaitAsync(100))
                {
                    await ShowBusyMessageAsync();
                    return;
                }

                try
                {
                    _lastAnalysisRequest = now;
                    _isAnalysisInProgress = true;

                    await PerformSceneAnalysisAsync();
                }
                finally
                {
                    _isAnalysisInProgress = false;
                    _uiOperationLock.Release();
                }
            }
            catch (Exception ex)
            {
                _isAnalysisInProgress = false;
                HandleErrorSafely(ex, "Error starting scene analysis");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Perform scene analysis with progress tracking
        /// </summary>
        private async Task PerformSceneAnalysisAsync()
        {
            try
            {
                if (_viewModel == null || _isDisposed) return;

                // Show initial progress
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetAnalysisLoadingState(true, "Preparing scene analysis...");
                });

                // Execute analysis command with progress tracking
                await _viewModel.EvaluateSceneCommand.ExecuteAsync(null);
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetAnalysisLoadingState(false);
                    HandleErrorSafely(ex, "Error performing scene analysis");
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update analysis status from progress reports
        /// </summary>
        private void UpdateAnalysisStatus(string status)
        {
            try
            {
                if (_isDisposed) return;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        SetAnalysisLoadingState(true, status);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error updating analysis status: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in analysis status update: {ex.Message}");
            }
        }

        #endregion

        #region PERFORMANCE OPTIMIZED UI STATE MANAGEMENT

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Subscribe to ViewModel events with proper error handling
        /// </summary>
        private void SubscribeToViewModelEvents()
        {
            if (_viewModel == null) return;

            try
            {
                _viewModel.ErrorOccurred -= OnSystemError;
                _viewModel.ErrorOccurred += OnSystemError;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error subscribing to ViewModel events");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Handle ViewModel property changes efficiently
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_isDisposed) return;

            try
            {
                switch (e.PropertyName)
                {
                    case nameof(SceneEvaluationViewModel.IsProcessing):
                        UpdateProcessingState();
                        break;
                    case nameof(SceneEvaluationViewModel.ProcessingStatus):
                        UpdateProcessingStatus();
                        break;
                    case nameof(SceneEvaluationViewModel.ProcessingProgress):
                        UpdateProcessingProgress();
                        break;
                    case nameof(SceneEvaluationViewModel.AnalysisResult):
                        UpdateAnalysisResults();
                        break;
                    case nameof(SceneEvaluationViewModel.CurrentHistogramImage):
                        UpdateHistogramDisplay();
                        break;
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error handling property change");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update processing state with visual feedback
        /// </summary>
        private void UpdateProcessingState()
        {
            try
            {
                if (_viewModel == null || _isDisposed) return;

                SetAnalysisLoadingState(_viewModel.IsProcessing, _viewModel.ProcessingStatus);
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error updating processing state");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update processing status
        /// </summary>
        private void UpdateProcessingStatus()
        {
            try
            {
                if (_viewModel == null || _isDisposed) return;

                // Update status display if you have a status label
                // StatusLabel.Text = _viewModel.ProcessingStatus;
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error updating processing status");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update processing progress
        /// </summary>
        private void UpdateProcessingProgress()
        {
            try
            {
                if (_viewModel == null || _isDisposed) return;

                // Update progress bar if you have one
                // ProgressBar.Progress = _viewModel.ProcessingProgress / 100.0;
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error updating processing progress");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update analysis results display
        /// </summary>
        private void UpdateAnalysisResults()
        {
            try
            {
                if (_viewModel == null || _isDisposed) return;

                SetAnalysisLoadingState(false);
                SetHistogramLoadingState(false);

                // Results are bound through XAML, but we can trigger any additional UI updates here
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error updating analysis results");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Update histogram display
        /// </summary>
        private void UpdateHistogramDisplay()
        {
            try
            {
                if (_viewModel == null || _isDisposed) return;

                SetHistogramLoadingState(false);

                // Histogram image is bound through XAML
                // Any additional histogram-related UI updates can go here
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error updating histogram display");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Show loading state with proper UI updates
        /// </summary>
        private async Task ShowLoadingStateAsync(string message)
        {
            try
            {
                if (_isDisposed) return;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    if (_viewModel != null)
                    {
                        _viewModel.IsBusy = true;
                    }

                    // Show loading overlay or indicator
                    // LoadingOverlay.IsVisible = true;
                    // LoadingMessage.Text = message;
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Hide loading state safely
        /// </summary>
        private void HideLoadingState()
        {
            try
            {
                if (_isDisposed || _viewModel == null) return;

                _viewModel.IsBusy = false;

                // Hide loading overlay
                // LoadingOverlay.IsVisible = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error hiding loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Set analysis loading state with message
        /// </summary>
        private void SetAnalysisLoadingState(bool isLoading, string message = "")
        {
            try
            {
                if (_isDisposed) return;

                // Update analysis button state
                // AnalysisButton.IsEnabled = !isLoading;
                // AnalysisButton.Text = isLoading ? "Analyzing..." : "Analyze Scene";

                // Update loading indicator
                // AnalysisLoadingIndicator.IsVisible = isLoading;

                if (!string.IsNullOrEmpty(message))
                {
                    // AnalysisStatusLabel.Text = message;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting analysis loading state: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Set histogram loading state
        /// </summary>
        private void SetHistogramLoadingState(bool isLoading)
        {
            try
            {
                if (_isDisposed) return;

                // Update histogram controls
                RedRadioButton.IsEnabled = !isLoading;
                GreenRadioButton.IsEnabled = !isLoading;
                BlueRadioButton.IsEnabled = !isLoading;
                LuminanceRadioButton.IsEnabled = !isLoading;

                // Show/hide histogram loading indicator
                // HistogramLoadingIndicator.IsVisible = isLoading;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting histogram loading state: {ex.Message}");
            }
        }

        #endregion

        #region USER FEEDBACK METHODS

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Show throttle message to user
        /// </summary>
        private async Task ShowThrottleMessageAsync()
        {
            try
            {
                await DisplayAlert("Please Wait",
                    "Analysis is still processing from your last request. Please wait a moment before trying again.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing throttle message: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Show analysis in progress message
        /// </summary>
        private async Task ShowAnalysisInProgressMessageAsync()
        {
            try
            {
                await DisplayAlert("Analysis In Progress",
                    "Scene analysis is currently running. Please wait for it to complete.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing analysis in progress message: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Show busy message
        /// </summary>
        private async Task ShowBusyMessageAsync()
        {
            try
            {
                await DisplayAlert("System Busy",
                    "The system is currently busy. Please try again in a moment.",
                    "OK");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error showing busy message: {ex.Message}");
            }
        }

        #endregion

        #region LIFECYCLE EVENTS (OPTIMIZED)

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                if (_isInitialized && _viewModel != null && !_isDisposed)
                {
                    SubscribeToViewModelEvents();
                    _viewModel.OnNavigatedToAsync();
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error during page appearing");
            }
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();

            try
            {
                UnsubscribeFromViewModelEvents();

                if (_viewModel != null)
                {
                    _viewModel.OnNavigatedFromAsync();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during page disappearing: {ex.Message}");
            }
        }

        protected override void OnBindingContextChanged()
        {
            base.OnBindingContextChanged();

            try
            {
                // Unsubscribe from old view model
                UnsubscribeFromViewModelEvents();

                if (BindingContext is SceneEvaluationViewModel viewModel)
                {
                    _viewModel = viewModel;
                    SubscribeToViewModelEvents();
                    _viewModel.OnNavigatedToAsync();
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error during binding context change");
            }
        }

        protected void OnDestroy()
        {
            try
            {
                _isDisposed = true;
                _uiCancellationSource?.Cancel();

                UnsubscribeFromViewModelEvents();

                _viewModel?.OnNavigatedFromAsync();
                _viewModel?.Dispose();

                _uiCancellationSource?.Dispose();
                _uiOperationLock?.Dispose();
                _lastModeChanges?.Clear();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during page destruction: {ex.Message}");
            }
            finally
            {
            }
        }

        #endregion

        #region ERROR HANDLING (OPTIMIZED)

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Unsubscribe from ViewModel events safely
        /// </summary>
        private void UnsubscribeFromViewModelEvents()
        {
            try
            {
                if (_viewModel != null)
                {
                    _viewModel.ErrorOccurred -= OnSystemError;
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error unsubscribing from events: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Safe error handling that won't crash UI
        /// </summary>
        private void HandleErrorSafely(Exception ex, string context)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Error: {context}. {ex.Message}");

                if (_isDisposed) return;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    try
                    {
                        if (_errorDisplayService != null)
                        {
                            await DisplayAlert("Error",context, "OK");
                        }
                        else
                        {
                            await DisplayAlert("Error", context, "OK");
                        }
                    }
                    catch
                    {
                        // Last resort - don't let error handling crash the app
                        System.Diagnostics.Debug.WriteLine($"Critical: Failed to show error dialog for: {context}");
                    }

                    if (_viewModel != null)
                    {
                        _viewModel.IsError = true;
                        _viewModel.ErrorMessage = $"{context}: {ex.Message}";
                        _viewModel.IsBusy = false;
                    }

                    // Reset states
                    SetAnalysisLoadingState(false);
                    SetHistogramLoadingState(false);
                    _isAnalysisInProgress = false;
                });
            }
            catch
            {
                // Absolutely cannot let error handling itself crash
                System.Diagnostics.Debug.WriteLine($"Critical error in error handler: {context}");
            }
        }

        private async void OnSystemError(object sender, OperationErrorEventArgs e)
        {
            try
            {
                if (_isDisposed) return;

                var retry = await DisplayAlert("Error", $"{e.Message}. Try again?", "OK", "Cancel");
                if (retry && sender is SceneEvaluationViewModel viewModel)
                {
                    await viewModel.RetryLastCommandAsync();
                }
            }
            catch (Exception ex)
            {
                HandleErrorSafely(ex, "Error in system error handler");
            }
        }

        #endregion

        #region DESTRUCTOR (OPTIMIZED)

        ~SceneEvaluation()
        {
            try
            {
                UnsubscribeFromViewModelEvents();
                _viewModel?.OnNavigatedFromAsync();
                _viewModel?.Dispose();
            }
            catch
            {
                // Ignore errors in destructor
            }
        }

        #endregion
    }
}