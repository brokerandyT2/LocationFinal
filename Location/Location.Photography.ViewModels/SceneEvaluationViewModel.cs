using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class SceneEvaluationViewModel : ViewModelBase, IDisposable
    {
        #region Fields
        private readonly IImageAnalysisService _imageAnalysisService;
        private readonly IErrorDisplayService _errorDisplayService;

        // PERFORMANCE: Threading and resource management
        private CancellationTokenSource _cancellationTokenSource = new();
        private readonly SemaphoreSlim _processingLock = new(1, 1);
        private bool _disposed = false;

        // PERFORMANCE: Caching for repeated operations
        private readonly Dictionary<string, ImageAnalysisResult> _analysisCache = new();
        private readonly Dictionary<HistogramDisplayMode, string> _histogramImageCache = new();
        private string _lastAnalyzedImageHash = string.Empty;
        private string _stackedHistogramImagePath = string.Empty;
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Properties
        [ObservableProperty]
        private ImageAnalysisResult _analysisResult;

        [ObservableProperty]
        private HistogramDisplayMode _selectedHistogramMode = HistogramDisplayMode.Red;

        [ObservableProperty]
        private bool _showExposureWarnings = true;

        [ObservableProperty]
        private string _currentHistogramImage = string.Empty;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private double _colorTemperature = 5500;

        [ObservableProperty]
        private double _tintValue = 0;

        [ObservableProperty]
        private bool _displayAll = false;

        // Professional analysis properties
        [ObservableProperty]
        private double _dynamicRange;

        [ObservableProperty]
        private string _exposureRecommendation = string.Empty;

        [ObservableProperty]
        private bool _hasClippingWarning;

        [ObservableProperty]
        private string _clippingWarningMessage = string.Empty;

        [ObservableProperty]
        private double _rmsContrast;

        [ObservableProperty]
        private double _redMean;

        [ObservableProperty]
        private double _greenMean;

        [ObservableProperty]
        private double _blueMean;

        // Histogram visibility properties
        [ObservableProperty]
        private bool _isRedHistogramVisible = true;

        [ObservableProperty]
        private bool _isGreenHistogramVisible = false;

        [ObservableProperty]
        private bool _isBlueHistogramVisible = false;

        [ObservableProperty]
        private bool _isLuminanceHistogramVisible = false;

        // PERFORMANCE: Progress tracking
        [ObservableProperty]
        private string _processingStatus = string.Empty;

        [ObservableProperty]
        private double _processingProgress = 0.0;
        #endregion

        #region Constructor
        public SceneEvaluationViewModel() : base(null, null)
        {
            _imageAnalysisService = new ImageAnalysisService();
            _errorDisplayService = null;
            InitializeCommands();
        }

        public SceneEvaluationViewModel(IImageAnalysisService imageAnalysisService, IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _imageAnalysisService = imageAnalysisService ?? throw new ArgumentNullException(nameof(imageAnalysisService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
            InitializeCommands();
        }

        private void InitializeCommands()
        {
            // Commands are initialized in the RelayCommand attributes
        }
        #endregion

        #region Commands
        [RelayCommand]
        private async Task EvaluateSceneAsync()
        {
            // Clear previous analysis data when starting new analysis
            ClearPreviousAnalysisData();

            // Prevent concurrent processing
            if (!await _processingLock.WaitAsync(100))
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetValidationError("Analysis already in progress. Please wait.");
                });
                return;
            }

            try
            {
                await ExecuteSceneEvaluationOptimizedAsync();
            }
            finally
            {
                _processingLock.Release();
            }
        }

        [RelayCommand]
        private async Task ChangeHistogramModeAsync(string mode)
        {
            if (Enum.TryParse<HistogramDisplayMode>(mode, out var histogramMode))
            {
                SelectedHistogramMode = histogramMode;
                await UpdateDisplayOptimizedAsync();
                UpdateHistogramVisibility();
            }
        }
        #endregion

        #region PERFORMANCE OPTIMIZED METHODS

        /// <summary>
        /// Clear previous analysis data when starting new analysis
        /// </summary>
        private void ClearPreviousAnalysisData()
        {
            _histogramImageCache.Clear();
            _stackedHistogramImagePath = string.Empty;
            _lastAnalyzedImageHash = string.Empty;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Streamlined scene evaluation with progress tracking
        /// </summary>
        private async Task ExecuteSceneEvaluationOptimizedAsync()
        {
            try
            {
                _imageAnalysisService.ClearHistogramCache();

                // Cancel any existing operation
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                // Start progress tracking on UI thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsProcessing = true;
                    _processingProgress = 0.0;
                    _processingStatus = "Initializing camera...";
                    ClearErrors();
                });

                // Phase 1: Capture photo with timeout and progress
                var progress = new Progress<string>(status =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _processingStatus = status;
                    });
                });

                var photo = await CapturePhotoOptimizedAsync(progress);
                if (photo == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsProcessing = false;
                        ProcessingStatus = "Photo capture cancelled";
                    });
                    return;
                }

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    ProcessingProgress = 25.0;
                    ProcessingStatus = "Processing image...";
                });

                // Phase 2: Generate image hash for caching
                var imageHash = await GenerateImageHashAsync(photo);

                // Check cache first
                if (!string.IsNullOrEmpty(imageHash) && _analysisCache.TryGetValue(imageHash, out var cachedResult))
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        ProcessingProgress = 90.0;
                        ProcessingStatus = "Loading cached analysis...";
                        AnalysisResult = cachedResult;
                        _ = UpdateDisplayOptimizedAsync();
                        ProcessingProgress = 100.0;
                        ProcessingStatus = "Analysis complete (cached)";
                        IsProcessing = false;
                    });
                    return;
                }

                // Phase 3: Perform image analysis on background thread with chunked progress
                var analysisTask = Task.Run(async () =>
                {
                    try
                    {
                        using var stream = await photo.OpenReadAsync();

                        // Create progress reporter for analysis phases
                        var analysisProgress = new Progress<double>(progressValue =>
                        {
                            var overallProgress = 25.0 + (progressValue * 0.5); // 25-75%
                            MainThread.BeginInvokeOnMainThread(() =>
                            {
                                _processingProgress = overallProgress;
                            });
                        });

                        var result = await _imageAnalysisService.AnalyzeImageAsync(
                            stream,
                            _cancellationTokenSource.Token);

                        return result;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Image analysis failed: {ex.Message}", ex);
                    }
                }, _cancellationTokenSource.Token);

                var analysisResult = await analysisTask;

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    _processingProgress = 75.0;
                    _processingStatus = "Generating recommendations...";
                });

                // Phase 4: Process additional data on background thread
                var enhancedData = await Task.Run(async () =>
                {
                    try
                    {
                        var recommendations = GenerateRecommendationsOptimized(analysisResult);
                        var histogramImages = await GenerateHistogramImagesBatchAsync(analysisResult);
                        var stackedHistogram = await GenerateStackedHistogramImageAsync(analysisResult);

                        return new
                        {
                            Recommendations = recommendations,
                            HistogramImages = histogramImages,
                            StackedHistogramPath = stackedHistogram
                        };
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Enhancement processing failed: {ex.Message}", ex);
                    }
                }, _cancellationTokenSource.Token);

                // Phase 5: Update UI with all results in single batch
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        BeginPropertyChangeBatch();

                        // Store in cache
                        if (!string.IsNullOrEmpty(imageHash))
                        {
                            _analysisCache[imageHash] = analysisResult;
                            _lastAnalyzedImageHash = imageHash;

                            // Cleanup old cache entries (keep only last 3)
                            if (_analysisCache.Count > 3)
                            {
                                var oldestKey = _analysisCache.Keys.First();
                                _analysisCache.Remove(oldestKey);
                            }
                        }

                        // Update all properties
                        AnalysisResult = analysisResult;
                        ExposureRecommendation = enhancedData.Recommendations;

                        // Cache histogram images
                        foreach (var kvp in enhancedData.HistogramImages)
                        {
                            _histogramImageCache[kvp.Key] = kvp.Value;
                        }

                        // Cache stacked histogram
                        _stackedHistogramImagePath = enhancedData.StackedHistogramPath;

                        _processingProgress = 90.0;
                        _processingStatus = "Finalizing display...";

                        _ = UpdateDisplayOptimizedAsync();

                        _processingProgress = 100.0;
                        _processingStatus = "Analysis complete";

                        _ = EndPropertyChangeBatchAsync();
                    }
                    catch (Exception ex)
                    {
                        OnSystemError($"Error updating analysis results: {ex.Message}");
                    }
                    finally
                    {
                        IsProcessing = false;
                    }
                });

                // Cleanup: Delay before clearing status
                _ = Task.Delay(2000, _cancellationTokenSource.Token).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        _processingStatus = string.Empty;
                        _processingProgress = 0.0;
                    });
                }, TaskScheduler.Default);

            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsProcessing = false;
                    _processingStatus = "Analysis cancelled";
                    _processingProgress = 0.0;
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Scene evaluation failed: {ex.Message}");
                    IsProcessing = false;
                    _processingStatus = "Analysis failed";
                    _processingProgress = 0.0;
                });
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Photo capture with timeout and progress
        /// </summary>
        private async Task<FileResult> CapturePhotoOptimizedAsync(IProgress<string> progress)
        {
            if (!MediaPicker.Default.IsCaptureSupported)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetValidationError("Camera capture is not supported on this device.");
                });
                return null;
            }

            try
            {
                progress?.Report("Opening camera...");

                // Capture photo with timeout
                var captureTask = Task.Run(async () =>
                {
                    return await MediaPicker.Default.CapturePhotoAsync();
                });

                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(60), _cancellationTokenSource.Token);
                var completedTask = await Task.WhenAny(captureTask, timeoutTask);

                if (completedTask == timeoutTask)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        SetValidationError("Photo capture timed out. Please try again.");
                    });
                    return null;
                }

                progress?.Report("Photo captured successfully");
                return await captureTask;
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetValidationError($"Error capturing photo: {ex.Message}");
                });
                return null;
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Generate image hash for caching
        /// </summary>
        private async Task<string> GenerateImageHashAsync(FileResult photo)
        {
            try
            {
                using var stream = await photo.OpenReadAsync();
                using var buffer = new MemoryStream();
                await stream.CopyToAsync(buffer);

                var bytes = buffer.ToArray();
                using var sha256 = System.Security.Cryptography.SHA256.Create();
                var hash = sha256.ComputeHash(bytes);
                return Convert.ToHexString(hash);
            }
            catch
            {
                return string.Empty; // Return empty string if hashing fails
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized display updates with caching and stacked histogram support
        /// </summary>
        private async Task UpdateDisplayOptimizedAsync()
        {
            if (AnalysisResult == null) return;

            try
            {
                if (DisplayAll)
                {
                    // Show stacked histogram
                    if (!string.IsNullOrEmpty(_stackedHistogramImagePath))
                    {
                        CurrentHistogramImage = _stackedHistogramImagePath;
                    }
                }
                else
                {
                    // Check histogram cache first for individual histogram
                    if (_histogramImageCache.TryGetValue(SelectedHistogramMode, out var cachedImagePath))
                    {
                        CurrentHistogramImage = cachedImagePath;
                    }
                    else
                    {
                        // Generate on demand if not cached
                        var histogram = SelectedHistogramMode switch
                        {
                            HistogramDisplayMode.Red => AnalysisResult.RedHistogram,
                            HistogramDisplayMode.Green => AnalysisResult.GreenHistogram,
                            HistogramDisplayMode.Blue => AnalysisResult.BlueHistogram,
                            HistogramDisplayMode.Luminance => AnalysisResult.LuminanceHistogram,
                            _ => AnalysisResult.RedHistogram
                        };

                        if (histogram?.ImagePath != null)
                        {
                            CurrentHistogramImage = histogram.ImagePath;
                            _histogramImageCache[SelectedHistogramMode] = histogram.ImagePath;
                        }
                    }
                }

                UpdateAnalysisMetricsOptimized();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating display: {ex.Message}");
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Batch metric updates
        /// </summary>
        private void UpdateAnalysisMetricsOptimized()
        {
            if (AnalysisResult == null) return;

            try
            {
                BeginPropertyChangeBatch();

                var histogram = SelectedHistogramMode switch
                {
                    HistogramDisplayMode.Red => AnalysisResult.RedHistogram,
                    HistogramDisplayMode.Green => AnalysisResult.GreenHistogram,
                    HistogramDisplayMode.Blue => AnalysisResult.BlueHistogram,
                    HistogramDisplayMode.Luminance => AnalysisResult.LuminanceHistogram,
                    _ => AnalysisResult.RedHistogram
                };

                if (histogram?.Statistics != null)
                {
                    // Update display properties from analysis results
                    ColorTemperature = AnalysisResult.WhiteBalance.Temperature;
                    TintValue = AnalysisResult.WhiteBalance.Tint;
                    DynamicRange = histogram.Statistics.DynamicRange;
                    RmsContrast = AnalysisResult.Contrast.RMSContrast;
                    RedMean = AnalysisResult.RedHistogram.Statistics.Mean;
                    GreenMean = AnalysisResult.GreenHistogram.Statistics.Mean;
                    BlueMean = AnalysisResult.BlueHistogram.Statistics.Mean;

                    _hasClippingWarning = histogram.Statistics.ShadowClipping || histogram.Statistics.HighlightClipping;

                    if (_hasClippingWarning)
                    {
                        _clippingWarningMessage = GenerateClippingWarningOptimized(histogram.Statistics);
                    }
                }

                _ = EndPropertyChangeBatchAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error updating metrics: {ex.Message}");
                _ = EndPropertyChangeBatchAsync();
            }
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized clipping warning generation
        /// </summary>
        private string GenerateClippingWarningOptimized(HistogramStatistics statistics)
        {
            var warnings = new List<string>(2); // Pre-size for expected capacity

            if (statistics.ShadowClipping)
                warnings.Add("Shadow clipping detected in dark areas");

            if (statistics.HighlightClipping)
                warnings.Add("Highlight clipping detected in bright areas");

            return warnings.Count > 0 ? string.Join(". ", warnings) : string.Empty;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Optimized recommendations generation
        /// </summary>
        private string GenerateRecommendationsOptimized(ImageAnalysisResult analysisResult)
        {
            if (analysisResult?.Exposure == null) return string.Empty;

            var recommendations = new List<string>(6); // Pre-size for expected capacity

            // Use the recommendation from the analysis service
            if (!string.IsNullOrEmpty(analysisResult.Exposure.RecommendedSettings))
            {
                recommendations.Add(analysisResult.Exposure.RecommendedSettings);
            }

            // Additional exposure recommendations
            if (analysisResult.Exposure.IsUnderexposed)
                recommendations.Add("Consider increasing exposure (+1 to +2 stops)");
            else if (analysisResult.Exposure.IsOverexposed)
                recommendations.Add("Consider decreasing exposure (-1 to -2 stops)");

            // Dynamic range recommendations
            var dynamicRange = DynamicRange;
            if (dynamicRange > 10)
                recommendations.Add("High dynamic range scene - consider HDR or graduated filters");
            else if (dynamicRange < 4)
                recommendations.Add("Low contrast scene - consider increasing contrast in post");

            // White balance recommendations
            var colorTemp = ColorTemperature;
            if (colorTemp < 3000)
                recommendations.Add("Very warm light detected - check white balance");
            else if (colorTemp > 8000)
                recommendations.Add("Very cool light detected - check white balance");

            return recommendations.Count > 0 ? string.Join("\n• ", recommendations) : string.Empty;
        }

        /// <summary>
        /// PERFORMANCE OPTIMIZATION: Batch histogram image generation
        /// </summary>
        private async Task<Dictionary<HistogramDisplayMode, string>> GenerateHistogramImagesBatchAsync(ImageAnalysisResult analysisResult)
        {
            if (analysisResult == null) return new Dictionary<HistogramDisplayMode, string>();

            try
            {
                // Generate all histogram images in parallel
                var tasks = new[]
                {
                    Task.Run(async () =>
                    {
                        var path = await _imageAnalysisService.GenerateHistogramImageAsync(
                            analysisResult.RedHistogram.Values, SkiaSharp.SKColors.Red, "red_histogram.png");
                        return (HistogramDisplayMode.Red, path);
                    }),
                    Task.Run(async () =>
                    {
                        var path = await _imageAnalysisService.GenerateHistogramImageAsync(
                            analysisResult.GreenHistogram.Values, SkiaSharp.SKColors.Green, "green_histogram.png");
                        return (HistogramDisplayMode.Green, path);
                    }),
                    Task.Run(async () =>
                    {
                        var path = await _imageAnalysisService.GenerateHistogramImageAsync(
                            analysisResult.BlueHistogram.Values, SkiaSharp.SKColors.Blue, "blue_histogram.png");
                        return (HistogramDisplayMode.Blue, path);
                    }),
                    Task.Run(async () =>
                    {
                        var path = await _imageAnalysisService.GenerateHistogramImageAsync(
                            analysisResult.LuminanceHistogram.Values, SkiaSharp.SKColors.Black, "luminance_histogram.png");
                        return (HistogramDisplayMode.Luminance, path);
                    })
                };

                var results = await Task.WhenAll(tasks);

                // Update analysis result with paths
                analysisResult.RedHistogram.ImagePath = results[0].Item2;
                analysisResult.GreenHistogram.ImagePath = results[1].Item2;
                analysisResult.BlueHistogram.ImagePath = results[2].Item2;
                analysisResult.LuminanceHistogram.ImagePath = results[3].Item2;

                return results.ToDictionary(r => r.Item1, r => r.Item2);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating histogram images: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Generate stacked histogram image showing all channels
        /// </summary>
        private async Task<string> GenerateStackedHistogramImageAsync(ImageAnalysisResult analysisResult)
        {
            if (analysisResult == null) return string.Empty;

            try
            {
                return await _imageAnalysisService.GenerateStackedHistogramImageAsync(
                    analysisResult.RedHistogram.Values,
                    analysisResult.GreenHistogram.Values,
                    analysisResult.BlueHistogram.Values,
                    analysisResult.LuminanceHistogram.Values,
                    "stacked_histogram.png");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating stacked histogram: {ex.Message}", ex);
            }
        }

        #endregion

        #region Methods

        private void UpdateHistogramVisibility()
        {
            BeginPropertyChangeBatch();

            _isRedHistogramVisible = SelectedHistogramMode == HistogramDisplayMode.Red;
            _isGreenHistogramVisible = SelectedHistogramMode == HistogramDisplayMode.Green;
            _isBlueHistogramVisible = SelectedHistogramMode == HistogramDisplayMode.Blue;
            _isLuminanceHistogramVisible = SelectedHistogramMode == HistogramDisplayMode.Luminance;

            _ = EndPropertyChangeBatchAsync();
        }

        public void SetHistogramMode(HistogramDisplayMode mode)
        {
            SelectedHistogramMode = mode;
            UpdateHistogramVisibility();
            _ = UpdateDisplayOptimizedAsync();
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            // Initialize default state
            SelectedHistogramMode = HistogramDisplayMode.Red;
            DisplayAll = false;
            UpdateHistogramVisibility();

            // Clear any processing state
            IsProcessing = false;
            _processingStatus = string.Empty;
            _processingProgress = 0.0;
        }

        public void OnNavigatedFromAsync()
        {
            // Cancel any ongoing operations
            _cancellationTokenSource?.Cancel();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _processingLock?.Dispose();

                // Clear caches
                _analysisCache.Clear();
                _histogramImageCache.Clear();

                _disposed = true;
            }
        }
        #endregion

        #region Partial Methods
        partial void OnSelectedHistogramModeChanged(HistogramDisplayMode value)
        {
            if (!DisplayAll)
            {
                UpdateHistogramVisibility();
                _ = UpdateDisplayOptimizedAsync();
            }
        }

        partial void OnDisplayAllChanged(bool value)
        {
            _ = UpdateDisplayOptimizedAsync();
        }

        partial void OnAnalysisResultChanged(ImageAnalysisResult value)
        {
            if (value != null)
            {
                _ = UpdateDisplayOptimizedAsync();
            }
        }
        #endregion
    }
}