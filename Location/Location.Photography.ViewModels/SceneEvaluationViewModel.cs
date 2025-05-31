using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.ViewModels.Events;
using Location.Photography.ViewModels.Interfaces;
using System;
using System.Threading;
using System.Threading.Tasks;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class SceneEvaluationViewModel : ViewModelBase, INavigationAware, IDisposable
    {
        #region Fields
        private readonly IImageAnalysisService _imageAnalysisService;
        private readonly IErrorDisplayService _errorDisplayService;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _disposed = false;
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
        #endregion

        #region Constructor
        public SceneEvaluationViewModel() : base(null, null)
        {
            _imageAnalysisService = new ImageAnalysisService();
            _errorDisplayService = null;
        }

        public SceneEvaluationViewModel(IImageAnalysisService imageAnalysisService, IErrorDisplayService errorDisplayService)
            : base(null, errorDisplayService)
        {
            _imageAnalysisService = imageAnalysisService ?? throw new ArgumentNullException(nameof(imageAnalysisService));
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
        }
        #endregion

        #region Commands
        [RelayCommand]
        private async Task EvaluateSceneAsync()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource = new CancellationTokenSource();

                // Update UI immediately
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsProcessing = true;
                    ClearErrors();
                });

                // Capture photo (already optimized above)
                var photo = await CapturePhotoAsync();
                if (photo == null)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsProcessing = false;
                    });
                    return;
                }

                // Perform heavy image analysis on background thread
                var analysisResult = await Task.Run(async () =>
                {
                    try
                    {
                        using var stream = await photo.OpenReadAsync();

                        // Perform image analysis off the UI thread
                        return await _imageAnalysisService.AnalyzeImageAsync(stream, _cancellationTokenSource.Token)
                            .ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Image analysis failed: {ex.Message}", ex);
                    }
                }).ConfigureAwait(false);

                // Update UI with results on main thread
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    try
                    {
                        AnalysisResult = analysisResult;
                    }
                    catch (Exception ex)
                    {
                        OnSystemError($"Error updating analysis results: {ex.Message}");
                    }
                });

                // Generate additional data on background thread
                await Task.Run(async () =>
                {
                    try
                    {
                        // Generate recommendations off UI thread
                        var recommendations = GenerateRecommendationsBackground();

                        // Generate histogram images off UI thread
                        await GenerateHistogramImagesBackgroundAsync().ConfigureAwait(false);

                        // Update display on UI thread
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            try
                            {
                                ExposureRecommendation = recommendations;
                                _ = UpdateDisplayAsync();
                            }
                            catch (Exception ex)
                            {
                                OnSystemError($"Error updating display: {ex.Message}");
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            OnSystemError($"Error in background processing: {ex.Message}");
                        });
                    }
                }).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsProcessing = false;
                });
            }
            catch (Exception ex)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnSystemError($"Analysis failed: {ex.Message}");
                    IsProcessing = false;
                });
            }
            finally
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    IsProcessing = false;
                });
            }
        }


        [RelayCommand]
        private async Task ChangeHistogramModeAsync(string mode)
        {
            if (Enum.TryParse<HistogramDisplayMode>(mode, out var histogramMode))
            {
                SelectedHistogramMode = histogramMode;
                await UpdateDisplayAsync();
                UpdateHistogramVisibility();
            }
        }
        #endregion

        #region Methods
        private async Task<FileResult> CapturePhotoAsync()
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                try
                {
                    // Show loading state immediately
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        IsProcessing = true;
                    });

                    // Capture photo on background thread to avoid UI blocking
                    var photoTask = Task.Run(async () =>
                    {
                        return await MediaPicker.Default.CapturePhotoAsync();
                    });

                    // Add timeout to prevent indefinite blocking
                    var timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), _cancellationTokenSource.Token);
                    var completedTask = await Task.WhenAny(photoTask, timeoutTask);

                    if (completedTask == timeoutTask)
                    {
                        await MainThread.InvokeOnMainThreadAsync(() =>
                        {
                            SetValidationError("Photo capture timed out. Please try again.");
                            IsProcessing = false;
                        });
                        return null;
                    }

                    return await photoTask;
                }
                catch (Exception ex)
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        SetValidationError($"Error capturing photo: {ex.Message}");
                        IsProcessing = false;
                    });
                    return null;
                }
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SetValidationError("Camera capture is not supported on this device.");
                    IsProcessing = false;
                });
                return null;
            }
        }

        private async Task UpdateDisplayAsync()
        {
            if (AnalysisResult == null) return;

            var histogram = SelectedHistogramMode switch
            {
                HistogramDisplayMode.Red => AnalysisResult.RedHistogram,
                HistogramDisplayMode.Green => AnalysisResult.GreenHistogram,
                HistogramDisplayMode.Blue => AnalysisResult.BlueHistogram,
                HistogramDisplayMode.Luminance => AnalysisResult.LuminanceHistogram,
                _ => AnalysisResult.RedHistogram
            };

            CurrentHistogramImage = histogram.ImagePath;
            UpdateAnalysisMetrics(histogram);
        }

        private void UpdateAnalysisMetrics(HistogramData histogram)
        {
            if (histogram?.Statistics == null || AnalysisResult == null) return;

            // Update display properties from analysis results
            ColorTemperature = AnalysisResult.WhiteBalance.Temperature;
            TintValue = AnalysisResult.WhiteBalance.Tint;
            DynamicRange = histogram.Statistics.DynamicRange;
            RmsContrast = AnalysisResult.Contrast.RMSContrast;
            RedMean = AnalysisResult.RedHistogram.Statistics.Mean;
            GreenMean = AnalysisResult.GreenHistogram.Statistics.Mean;
            BlueMean = AnalysisResult.BlueHistogram.Statistics.Mean;

            HasClippingWarning = histogram.Statistics.ShadowClipping || histogram.Statistics.HighlightClipping;

            if (HasClippingWarning)
            {
                ClippingWarningMessage = GenerateClippingWarning(histogram.Statistics);
            }
        }

        private string GenerateClippingWarning(HistogramStatistics statistics)
        {
            var warnings = new System.Collections.Generic.List<string>();

            if (statistics.ShadowClipping)
                warnings.Add("Shadow clipping detected in dark areas");

            if (statistics.HighlightClipping)
                warnings.Add("Highlight clipping detected in bright areas");

            return string.Join(". ", warnings);
        }
        private string GenerateRecommendationsBackground()
        {
            if (AnalysisResult?.Exposure == null) return string.Empty;

            var recommendations = new System.Collections.Generic.List<string>();

            // Use the recommendation from the analysis service
            if (!string.IsNullOrEmpty(AnalysisResult.Exposure.RecommendedSettings))
            {
                recommendations.Add(AnalysisResult.Exposure.RecommendedSettings);
            }

            // Additional exposure recommendations
            if (AnalysisResult.Exposure.IsUnderexposed)
                recommendations.Add("Consider increasing exposure (+1 to +2 stops)");
            else if (AnalysisResult.Exposure.IsOverexposed)
                recommendations.Add("Consider decreasing exposure (-1 to -2 stops)");

            // Dynamic range recommendations
            if (DynamicRange > 10)
                recommendations.Add("High dynamic range scene - consider HDR or graduated filters");
            else if (DynamicRange < 4)
                recommendations.Add("Low contrast scene - consider increasing contrast in post");

            // White balance recommendations
            if (ColorTemperature < 3000)
                recommendations.Add("Very warm light detected - check white balance");
            else if (ColorTemperature > 8000)
                recommendations.Add("Very cool light detected - check white balance");

            return string.Join("\n• ", recommendations);
        }
        private async Task GenerateHistogramImagesBackgroundAsync()
        {
            if (AnalysisResult == null) return;

            try
            {
                // Generate histogram images for all channels on background thread
                var redHistogramTask = _imageAnalysisService.GenerateHistogramImageAsync(
                    AnalysisResult.RedHistogram.Values, SkiaSharp.SKColors.Red, "red_histogram.png");

                var greenHistogramTask = _imageAnalysisService.GenerateHistogramImageAsync(
                    AnalysisResult.GreenHistogram.Values, SkiaSharp.SKColors.Green, "green_histogram.png");

                var blueHistogramTask = _imageAnalysisService.GenerateHistogramImageAsync(
                    AnalysisResult.BlueHistogram.Values, SkiaSharp.SKColors.Blue, "blue_histogram.png");

                var luminanceHistogramTask = _imageAnalysisService.GenerateHistogramImageAsync(
                    AnalysisResult.LuminanceHistogram.Values, SkiaSharp.SKColors.Black, "luminance_histogram.png");

                // Wait for all histogram generation to complete
                var results = await Task.WhenAll(
                    redHistogramTask,
                    greenHistogramTask,
                    blueHistogramTask,
                    luminanceHistogramTask).ConfigureAwait(false);

                // Update results
                AnalysisResult.RedHistogram.ImagePath = results[0];
                AnalysisResult.GreenHistogram.ImagePath = results[1];
                AnalysisResult.BlueHistogram.ImagePath = results[2];
                AnalysisResult.LuminanceHistogram.ImagePath = results[3];
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Error generating histogram images: {ex.Message}", ex);
            }
        }
        private void GenerateRecommendations()
        {
            if (AnalysisResult?.Exposure == null) return;

            var recommendations = new System.Collections.Generic.List<string>();

            // Use the recommendation from the analysis service
            if (!string.IsNullOrEmpty(AnalysisResult.Exposure.RecommendedSettings))
            {
                recommendations.Add(AnalysisResult.Exposure.RecommendedSettings);
            }

            // Additional exposure recommendations
            if (AnalysisResult.Exposure.IsUnderexposed)
                recommendations.Add("Consider increasing exposure (+1 to +2 stops)");
            else if (AnalysisResult.Exposure.IsOverexposed)
                recommendations.Add("Consider decreasing exposure (-1 to -2 stops)");

            // Dynamic range recommendations
            if (DynamicRange > 10)
                recommendations.Add("High dynamic range scene - consider HDR or graduated filters");
            else if (DynamicRange < 4)
                recommendations.Add("Low contrast scene - consider increasing contrast in post");

            // White balance recommendations
            if (ColorTemperature < 3000)
                recommendations.Add("Very warm light detected - check white balance");
            else if (ColorTemperature > 8000)
                recommendations.Add("Very cool light detected - check white balance");

            ExposureRecommendation = string.Join("\n• ", recommendations);
        }

        private async Task GenerateHistogramImagesAsync()
        {
            if (AnalysisResult == null) return;

            try
            {
                // Generate histogram images for all channels
                AnalysisResult.RedHistogram.ImagePath = await _imageAnalysisService.GenerateHistogramImageAsync(
                    AnalysisResult.RedHistogram.Values, SkiaSharp.SKColors.Red, "red_histogram.png");

                AnalysisResult.GreenHistogram.ImagePath = await _imageAnalysisService.GenerateHistogramImageAsync(
                    AnalysisResult.GreenHistogram.Values, SkiaSharp.SKColors.Green, "green_histogram.png");

                AnalysisResult.BlueHistogram.ImagePath = await _imageAnalysisService.GenerateHistogramImageAsync(
                    AnalysisResult.BlueHistogram.Values, SkiaSharp.SKColors.Blue, "blue_histogram.png");

                AnalysisResult.LuminanceHistogram.ImagePath = await _imageAnalysisService.GenerateHistogramImageAsync(
                    AnalysisResult.LuminanceHistogram.Values, SkiaSharp.SKColors.Black, "luminance_histogram.png");

                // Update current display
                await UpdateDisplayAsync();
            }
            catch (Exception ex)
            {
                OnSystemError($"Error generating histogram images: {ex.Message}");
            }
        }

        private void UpdateHistogramVisibility()
        {
            IsRedHistogramVisible = SelectedHistogramMode == HistogramDisplayMode.Red;
            IsGreenHistogramVisible = SelectedHistogramMode == HistogramDisplayMode.Green;
            IsBlueHistogramVisible = SelectedHistogramMode == HistogramDisplayMode.Blue;
            IsLuminanceHistogramVisible = SelectedHistogramMode == HistogramDisplayMode.Luminance;
        }

        public void SetHistogramMode(HistogramDisplayMode mode)
        {
            SelectedHistogramMode = mode;
            UpdateHistogramVisibility();
            _ = UpdateDisplayAsync();
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }

        public void OnNavigatedToAsync()
        {
            // Initialize default state
            SelectedHistogramMode = HistogramDisplayMode.Red;
            UpdateHistogramVisibility();
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
                _disposed = true;
            }
        }
        #endregion

        #region Partial Methods
        partial void OnSelectedHistogramModeChanged(HistogramDisplayMode value)
        {
            UpdateHistogramVisibility();
            _ = UpdateDisplayAsync();
        }

        partial void OnAnalysisResultChanged(ImageAnalysisResult value)
        {
            if (value != null)
            {
                _ = UpdateDisplayAsync();
            }
        }
        #endregion
    }
}