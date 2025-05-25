using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Location.Core.Application.Services;
using Location.Photography.ViewModels.Events;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using OperationErrorEventArgs = Location.Photography.ViewModels.Events.OperationErrorEventArgs;
using OperationErrorSource = Location.Photography.ViewModels.Events.OperationErrorSource;

namespace Location.Photography.ViewModels
{
    public partial class SceneEvaluationViewModel : ViewModelBase
    {
        #region Fields
        private readonly IErrorDisplayService _errorDisplayService;
        private bool _isRedHistogramVisible = true;
        private bool _isGreenHistogramVisible = false;
        private bool _isBlueHistogramVisible = false;
        private bool _isContrastHistogramVisible = false;
        #endregion

        #region Events
        public new event EventHandler<OperationErrorEventArgs>? ErrorOccurred;
        #endregion

        #region Properties
        [ObservableProperty]
        private string _redHistogramImage = string.Empty;

        [ObservableProperty]
        private string _greenHistogramImage = string.Empty;

        [ObservableProperty]
        private string _blueHistogramImage = string.Empty;

        [ObservableProperty]
        private string _contrastHistogramImage = string.Empty;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private double _colorTemperature = 5500;  // Default 5500K (neutral)

        [ObservableProperty]
        private double _tintValue = 0;  // Default 0 (neutral)

        // Explicitly implement histogram visibility properties
        public bool IsRedHistogramVisible
        {
            get => _isRedHistogramVisible;
            set
            {
                if (_isRedHistogramVisible != value)
                {
                    _isRedHistogramVisible = value;
                    OnPropertyChanged(nameof(IsRedHistogramVisible));
                }
            }
        }

        public bool IsGreenHistogramVisible
        {
            get => _isGreenHistogramVisible;
            set
            {
                if (_isGreenHistogramVisible != value)
                {
                    _isGreenHistogramVisible = value;
                    OnPropertyChanged(nameof(IsGreenHistogramVisible));
                }
            }
        }

        public bool IsBlueHistogramVisible
        {
            get => _isBlueHistogramVisible;
            set
            {
                if (_isBlueHistogramVisible != value)
                {
                    _isBlueHistogramVisible = value;
                    OnPropertyChanged(nameof(IsBlueHistogramVisible));
                }
            }
        }

        public bool IsContrastHistogramVisible
        {
            get => _isContrastHistogramVisible;
            set
            {
                if (_isContrastHistogramVisible != value)
                {
                    _isContrastHistogramVisible = value;
                    OnPropertyChanged(nameof(IsContrastHistogramVisible));
                }
            }
        }
        #endregion

        #region Constructor
        public SceneEvaluationViewModel() : base(null, null)
        {
        }

        public SceneEvaluationViewModel(IErrorDisplayService errorDisplayService) : base(null, errorDisplayService)
        {
            _errorDisplayService = errorDisplayService ?? throw new ArgumentNullException(nameof(errorDisplayService));
        }
        #endregion

        #region Methods
        [RelayCommand]
        private async Task EvaluateSceneAsync()
        {
            var command = new AsyncRelayCommand(async () =>
            {
                try
                {
                    IsProcessing = true;
                    ClearErrors();

                    await GetImageAsync();
                }
                catch (Exception ex)
                {
                    OnSystemError($"Error evaluating scene: {ex.Message}");
                }
                finally
                {
                    IsProcessing = false;
                }
            });

            await ExecuteAndTrackAsync(command);
        }

        private async Task GetImageAsync()
        {
            if (MediaPicker.Default.IsCaptureSupported)
            {
                try
                {
                    var photo = await MediaPicker.Default.CapturePhotoAsync();

                    if (photo != null)
                    {
                        await ProcessImageAsync(photo);
                    }
                }
                catch (Exception ex)
                {
                    SetValidationError($"Error capturing photo: {ex.Message}");
                }
            }
            else
            {
                SetValidationError("Camera capture is not supported on this device.");
            }
        }

        private async Task ProcessImageAsync(FileResult photo)
        {
            string path = string.Empty;

            try
            {
                // Save the file into local storage
                string localFilePath = Path.Combine(FileSystem.AppDataDirectory, photo.FileName);
                DirectoryInfo di = new DirectoryInfo(FileSystem.AppDataDirectory);
                var files = di.GetFiles();

                foreach (var file in files)
                {
                    if (file.Extension == ".jpg")
                    {
                        file.Delete();
                    }
                }

                using (Stream sourceStream = await photo.OpenReadAsync())
                using (FileStream localFileStream = File.OpenWrite(localFilePath))
                {
                    path = localFilePath;
                    await sourceStream.CopyToAsync(localFileStream);
                }

                double[] redHistogram = new double[256];
                double[] greenHistogram = new double[256];
                double[] blueHistogram = new double[256];
                double[] contrastHistogram = new double[256];

                int totalPixels = 0;
                double redSum = 0;
                double greenSum = 0;
                double blueSum = 0;

                using (var bitmap = SKBitmap.Decode(path))
                {
                    totalPixels = bitmap.Width * bitmap.Height;

                    for (int y = 0; y < bitmap.Height; y++)
                    {
                        for (int x = 0; x < bitmap.Width; x++)
                        {
                            SKColor color = bitmap.GetPixel(x, y);
                            redHistogram[color.Red]++;
                            greenHistogram[color.Green]++;
                            blueHistogram[color.Blue]++;

                            redSum += color.Red;
                            greenSum += color.Green;
                            blueSum += color.Blue;

                            // Contrast calculation (Luminance Approximation)
                            int contrast = (int)(0.299 * color.Red + 0.587 * color.Green + 0.114 * color.Blue);
                            contrastHistogram[contrast]++;
                        }
                    }

                    // Normalize histograms
                    NormalizeHistogram(redHistogram, totalPixels);
                    NormalizeHistogram(greenHistogram, totalPixels);
                    NormalizeHistogram(blueHistogram, totalPixels);
                    NormalizeHistogram(contrastHistogram, totalPixels);

                    // Calculate color temperature and tint (simplified algorithm)
                    double avgRed = redSum / totalPixels;
                    double avgGreen = greenSum / totalPixels;
                    double avgBlue = blueSum / totalPixels;

                    // Simplified calculation for color temperature
                    double redBlueRatio = avgRed / avgBlue;
                    ColorTemperature = CalculateColorTemperature(redBlueRatio);

                    // Simplified calculation for tint (green-magenta axis)
                    double greenMagentaRatio = avgGreen / ((avgRed + avgBlue) / 2);
                    TintValue = CalculateTintValue(greenMagentaRatio);
                }

                string redPath = Path.Combine(FileSystem.AppDataDirectory, "red.png");
                string bluePath = Path.Combine(FileSystem.AppDataDirectory, "blue.png");
                string greenPath = Path.Combine(FileSystem.AppDataDirectory, "green.png");
                string contrastPath = Path.Combine(FileSystem.AppDataDirectory, "contrast.png");

                // Generate histogram images
                RedHistogramImage = GenerateHistogramImage(redPath, redHistogram, SKColors.Red);
                GreenHistogramImage = GenerateHistogramImage(greenPath, greenHistogram, SKColors.Green);
                BlueHistogramImage = GenerateHistogramImage(bluePath, blueHistogram, SKColors.Blue);
                ContrastHistogramImage = GenerateHistogramImage(contrastPath, contrastHistogram, SKColors.Black);
            }
            catch (Exception ex)
            {
                OnSystemError($"Error processing image: {ex.Message}");
            }
        }

        private double CalculateColorTemperature(double redBlueRatio)
        {
            // Simplified algorithm - in reality would use a more complex model
            if (redBlueRatio > 1.0)
            {
                // More red than blue - warmer
                return 6500 - ((redBlueRatio - 1.0) * 3800);  // Range: 2700K (warm) to 6500K (neutral)
            }
            else
            {
                // More blue than red - cooler
                return 6500 + ((1.0 - redBlueRatio) * 2500);  // Range: 6500K (neutral) to 9000K (cool)
            }
        }

        private double CalculateTintValue(double greenMagentaRatio)
        {
            // Simplified algorithm - in reality would use a more complex model
            // Output range: -1.0 (magenta) to 1.0 (green), with 0 being neutral
            return (greenMagentaRatio - 1.0) * 2.0;  // Scale to range of -1 to 1
        }

        private static void NormalizeHistogram(double[] histogram, int totalPixels)
        {
            double maxValue = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                histogram[i] /= totalPixels;  // Normalize to [0,1]
                if (histogram[i] > maxValue)
                    maxValue = histogram[i]; // Get max value
            }

            // Scale values so the highest value is 100% of the image height
            if (maxValue > 0)
            {
                for (int i = 0; i < histogram.Length; i++)
                {
                    histogram[i] /= maxValue;
                }
            }
        }

        private static string GenerateHistogramImage(string filePath, double[] histodata, SKColor color)
        {
            int width = 512;  // Histogram image width
            int height = 256; // Histogram image height
            int margin = 10;  // Margin for better visibility

            using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                // Draw Axes
                using (var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true })
                {
                    canvas.DrawLine(margin, height - margin, width - margin, height - margin, axisPaint); // X-axis
                    canvas.DrawLine(margin, height - margin, margin, margin, axisPaint); // Y-axis
                }

                // Draw Histogram
                DrawHistogramLine(canvas, histodata, color, width, height, margin);

                // Save Image
                using (var image = surface.Snapshot())
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(filePath))
                {
                    data.SaveTo(stream);
                }
                return filePath;
            }
        }

        private static void DrawHistogramLine(SKCanvas canvas, double[] histogram, SKColor color, int width, int height, int margin)
        {
            int graphWidth = width - (2 * margin);
            int graphHeight = height - (2 * margin);

            using (var paint = new SKPaint
            {
                Color = color,
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Fill
            })
            {
                for (int i = 1; i < histogram.Length; i++)
                {
                    float x1 = margin + ((i - 1) * (graphWidth / 256f));
                    float y1 = height - margin - (float)(histogram[i - 1] * graphHeight);
                    float x2 = margin + (i * (graphWidth / 256f));
                    float y2 = height - margin - (float)(histogram[i] * graphHeight);

                    canvas.DrawLine(x1, y1, x2, y2, paint);
                }
            }
        }

        protected override void OnErrorOccurred(string message)
        {
            ErrorOccurred?.Invoke(this, new OperationErrorEventArgs(OperationErrorSource.Unknown, message));
        }
        #endregion
    }
}