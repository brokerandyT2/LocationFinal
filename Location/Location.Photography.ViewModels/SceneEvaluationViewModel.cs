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

                IsProcessing = true;
                ClearErrors();

                var photo = await CapturePhotoAsync();
                if (photo != null)
                {
                    using var stream = await photo.OpenReadAsync();
                    AnalysisResult = await _imageAnalysisService.AnalyzeImageAsync(stream, _cancellationTokenSource.Token);
                    await UpdateDisplayAsync();
                    GenerateRecommendations();
                    await GenerateHistogramImagesAsync();
                }
            }
            catch (OperationCanceledException)
            {
                // User cancelled - no error needed
            }
            catch (Exception ex)
            {
                OnSystemError($"Analysis failed: {ex.Message}");
            }
            finally
            {
                IsProcessing = false;
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
                    return await MediaPicker.Default.CapturePhotoAsync();
                }
                catch (Exception ex)
                {
                    SetValidationError($"Error capturing photo: {ex.Message}");
                    return null;
                }
            }
            else
            {
                SetValidationError("Camera capture is not supported on this device.");
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