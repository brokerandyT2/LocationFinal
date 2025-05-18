// Location.Photography.Infrastructure/Services/SceneEvaluationService.cs
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Microsoft.Extensions.Logging;
using SkiaSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    public class SceneEvaluationService : ISceneEvaluationService
    {
        private readonly ILogger<SceneEvaluationService> _logger;
        private readonly IMediaService _mediaService;
        private readonly string _cacheDirectory;

        private const int HistogramBuckets = 256;

        public SceneEvaluationService(
            ILogger<SceneEvaluationService> logger,
            IMediaService mediaService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediaService = mediaService ?? throw new ArgumentNullException(nameof(mediaService));

            // Create cache directory
            _cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Location.Photography",
                "SceneEvaluation");

            Directory.CreateDirectory(_cacheDirectory);
        }

        public async Task<Result<SceneEvaluationResultDto>> EvaluateSceneAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                // Capture image using media service
                var captureResult = await _mediaService.CapturePhotoAsync(cancellationToken);
                if (!captureResult.IsSuccess)
                {
                    return Result<SceneEvaluationResultDto>.Failure($"Failed to capture image: {captureResult.ErrorMessage}");
                }

                // Analyze the captured image
                return await AnalyzeImageAsync(captureResult.Data, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating scene");
                return Result<SceneEvaluationResultDto>.Failure($"Error evaluating scene: {ex.Message}");
            }
        }

        public async Task<Result<SceneEvaluationResultDto>> AnalyzeImageAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(imagePath))
                {
                    return Result<SceneEvaluationResultDto>.Failure("File not found");
                }

                // Load the image into memory
                using SKBitmap bitmap = SKBitmap.Decode(imagePath);
                if (bitmap == null)
                {
                    return Result<SceneEvaluationResultDto>.Failure("Failed to decode image");
                }

                // Initialize arrays for histogram data
                int[] redHistogram = new int[HistogramBuckets];
                int[] greenHistogram = new int[HistogramBuckets];
                int[] blueHistogram = new int[HistogramBuckets];
                int[] contrastHistogram = new int[HistogramBuckets];

                // Calculate histogram data and statistics
                var stats = CalculateHistograms(bitmap, redHistogram, greenHistogram, blueHistogram, contrastHistogram);

                // Save histograms as images
                string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
                string redHistogramPath = await SaveHistogramAsync(redHistogram, "red", timestamp, SKColors.Red, cancellationToken);
                string greenHistogramPath = await SaveHistogramAsync(greenHistogram, "green", timestamp, SKColors.Green, cancellationToken);
                string blueHistogramPath = await SaveHistogramAsync(blueHistogram, "blue", timestamp, SKColors.Blue, cancellationToken);
                string contrastHistogramPath = await SaveHistogramAsync(contrastHistogram, "contrast", timestamp, SKColors.Gray, cancellationToken);

                // Return the result
                return Result<SceneEvaluationResultDto>.Success(new SceneEvaluationResultDto
                {
                    RedHistogramPath = redHistogramPath,
                    GreenHistogramPath = greenHistogramPath,
                    BlueHistogramPath = blueHistogramPath,
                    ContrastHistogramPath = contrastHistogramPath,
                    ImagePath = imagePath,
                    Stats = stats
                });
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image");
                return Result<SceneEvaluationResultDto>.Failure($"Failed to process image: {ex.Message}");
            }
        }

        private SceneEvaluationStatsDto CalculateHistograms(
            SKBitmap bitmap,
            int[] redHistogram,
            int[] greenHistogram,
            int[] blueHistogram,
            int[] contrastHistogram)
        {
            // Initialize statistics values
            long redSum = 0;
            long greenSum = 0;
            long blueSum = 0;
            long contrastSum = 0;
            long redSumSquared = 0;
            long greenSumSquared = 0;
            long blueSumSquared = 0;
            long contrastSumSquared = 0;

            // Process each pixel in the image
            int totalPixels = bitmap.Width * bitmap.Height;
            for (int y = 0; y < bitmap.Height; y++)
            {
                for (int x = 0; x < bitmap.Width; x++)
                {
                    SKColor pixel = bitmap.GetPixel(x, y);

                    // Calculate contrast value (grayscale)
                    int contrast = (int)(pixel.Red * 0.299 + pixel.Green * 0.587 + pixel.Blue * 0.114);

                    // Increment histogram buckets
                    redHistogram[pixel.Red]++;
                    greenHistogram[pixel.Green]++;
                    blueHistogram[pixel.Blue]++;
                    contrastHistogram[contrast]++;

                    // Add to sums for mean calculation
                    redSum += pixel.Red;
                    greenSum += pixel.Green;
                    blueSum += pixel.Blue;
                    contrastSum += contrast;

                    // Add to sum of squares for standard deviation calculation
                    redSumSquared += pixel.Red * pixel.Red;
                    greenSumSquared += pixel.Green * pixel.Green;
                    blueSumSquared += pixel.Blue * pixel.Blue;
                    contrastSumSquared += contrast * contrast;
                }
            }

            // Calculate means
            double redMean = (double)redSum / totalPixels;
            double greenMean = (double)greenSum / totalPixels;
            double blueMean = (double)blueSum / totalPixels;
            double contrastMean = (double)contrastSum / totalPixels;

            // Calculate standard deviations
            double redStdDev = Math.Sqrt((double)redSumSquared / totalPixels - redMean * redMean);
            double greenStdDev = Math.Sqrt((double)greenSumSquared / totalPixels - greenMean * greenMean);
            double blueStdDev = Math.Sqrt((double)blueSumSquared / totalPixels - blueMean * blueMean);
            double contrastStdDev = Math.Sqrt((double)contrastSumSquared / totalPixels - contrastMean * contrastMean);

            // Return statistics
            return new SceneEvaluationStatsDto
            {
                MeanRed = redMean,
                MeanGreen = greenMean,
                MeanBlue = blueMean,
                MeanContrast = contrastMean,
                StdDevRed = redStdDev,
                StdDevGreen = greenStdDev,
                StdDevBlue = blueStdDev,
                StdDevContrast = contrastStdDev,
                TotalPixels = totalPixels
            };
        }

        private async Task<string> SaveHistogramAsync(
            int[] histogram,
            string channel,
            string timestamp,
            SKColor color,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int width = HistogramBuckets;
            int height = 200;

            // Find maximum value for scaling
            int maxValue = 1; // Avoid division by zero
            foreach (int value in histogram)
            {
                maxValue = Math.Max(maxValue, value);
            }

            // Create the histogram image
            using SKBitmap histogramBitmap = new SKBitmap(width, height);
            using SKCanvas canvas = new SKCanvas(histogramBitmap);

            // Clear the canvas
            canvas.Clear(SKColors.White);

            // Draw the histogram
            using SKPaint paint = new SKPaint
            {
                Color = color,
                StrokeWidth = 1
            };

            for (int i = 0; i < HistogramBuckets; i++)
            {
                float barHeight = (float)histogram[i] / maxValue * height;
                canvas.DrawLine(i, height, i, height - barHeight, paint);
            }

            // Save the histogram image
            string fileName = $"histogram_{channel}_{timestamp}.png";
            string filePath = Path.Combine(_cacheDirectory, fileName);

            using SKFileWStream fileStream = new SKFileWStream(filePath);
            histogramBitmap.Encode(fileStream, SKEncodedImageFormat.Png, 100);

            return filePath;
        }
    }
}