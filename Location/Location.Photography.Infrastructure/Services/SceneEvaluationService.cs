// Location.Photography.Infrastructure/Services/SceneEvaluationService.cs
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application.Services;
using Location.Photography.Infrastructure.Resources;
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
                var captureResult = await _mediaService.CapturePhotoAsync(cancellationToken).ConfigureAwait(false);
                if (!captureResult.IsSuccess)
                {
                    return Result<SceneEvaluationResultDto>.Failure(string.Format(AppResources.SceneEvaluation_Error_FailedToCaptureImage, captureResult.ErrorMessage));
                }

                // Analyze the captured image
                return await AnalyzeImageAsync(captureResult.Data, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating scene");
                return Result<SceneEvaluationResultDto>.Failure(string.Format(AppResources.SceneEvaluation_Error_EvaluatingScene, ex.Message));
            }
        }

        public async Task<Result<SceneEvaluationResultDto>> AnalyzeImageAsync(string imagePath, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!File.Exists(imagePath))
                {
                    return Result<SceneEvaluationResultDto>.Failure(AppResources.SceneEvaluation_Error_FileNotFound);
                }

                // Move heavy image processing to background thread to prevent UI blocking
                var analysisResult = await Task.Run(async () =>
                {
                    // Load the image into memory
                    using SKBitmap bitmap = SKBitmap.Decode(imagePath);
                    if (bitmap == null)
                    {
                        return Result<SceneEvaluationResultDto>.Failure(AppResources.SceneEvaluation_Error_FailedToDecodeImage);
                    }

                    // Initialize arrays for histogram data
                    int[] redHistogram = new int[HistogramBuckets];
                    int[] greenHistogram = new int[HistogramBuckets];
                    int[] blueHistogram = new int[HistogramBuckets];
                    int[] contrastHistogram = new int[HistogramBuckets];

                    // Calculate histogram data and statistics with optimized processing
                    var stats = await CalculateHistogramsAsync(bitmap, redHistogram, greenHistogram, blueHistogram, contrastHistogram, cancellationToken).ConfigureAwait(false);

                    // Save histograms as images
                    string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

                    // Parallelize histogram image generation
                    var histogramTasks = new[]
                    {
                       SaveHistogramAsync(redHistogram, "red", timestamp, SKColors.Red, cancellationToken),
                       SaveHistogramAsync(greenHistogram, "green", timestamp, SKColors.Green, cancellationToken),
                       SaveHistogramAsync(blueHistogram, "blue", timestamp, SKColors.Blue, cancellationToken),
                       SaveHistogramAsync(contrastHistogram, "contrast", timestamp, SKColors.Gray, cancellationToken)
                   };

                    var histogramPaths = await Task.WhenAll(histogramTasks).ConfigureAwait(false);

                    // Return the result
                    return Result<SceneEvaluationResultDto>.Success(new SceneEvaluationResultDto
                    {
                        RedHistogramPath = histogramPaths[0],
                        GreenHistogramPath = histogramPaths[1],
                        BlueHistogramPath = histogramPaths[2],
                        ContrastHistogramPath = histogramPaths[3],
                        ImagePath = imagePath,
                        Stats = stats
                    });

                }, cancellationToken).ConfigureAwait(false);

                return analysisResult;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing image");
                return Result<SceneEvaluationResultDto>.Failure(string.Format(AppResources.SceneEvaluation_Error_FailedToProcessImage, ex.Message));
            }
        }

        public async Task<string> GenerateStackedHistogramImageAsync(
            double[] redHistogram,
            double[] greenHistogram,
            double[] blueHistogram,
            double[] luminanceHistogram,
            string fileName)
        {
            return await Task.Run(() =>
            {
                try
                {
                    int width = HistogramBuckets;
                    int height = 200;

                    // Find maximum value across all histograms for scaling
                    double maxValue = 1.0; // Avoid division by zero
                    foreach (var histogram in new[] { redHistogram, greenHistogram, blueHistogram, luminanceHistogram })
                    {
                        foreach (double value in histogram)
                        {
                            maxValue = Math.Max(maxValue, value);
                        }
                    }

                    using SKBitmap stackedBitmap = new SKBitmap(width, height);
                    using SKCanvas canvas = new SKCanvas(stackedBitmap);

                    canvas.Clear(SKColors.White);

                    var colors = new[] { SKColors.Red, SKColors.Green, SKColors.Blue, SKColors.Gray };
                    var histograms = new[] { redHistogram, greenHistogram, blueHistogram, luminanceHistogram };

                    // Draw each histogram with transparency
                    for (int h = 0; h < histograms.Length; h++)
                    {
                        using SKPaint paint = new SKPaint
                        {
                            Color = colors[h].WithAlpha(128), // Semi-transparent
                            StrokeWidth = 1,
                            IsAntialias = true
                        };

                        for (int i = 0; i < HistogramBuckets - 1; i++)
                        {
                            float x1 = i;
                            float y1 = height - (float)(histograms[h][i] / maxValue * height);
                            float x2 = i + 1;
                            float y2 = height - (float)(histograms[h][i + 1] / maxValue * height);

                            canvas.DrawLine(x1, y1, x2, y2, paint);
                        }
                    }

                    // Save the stacked histogram
                    string filePath = Path.Combine(_cacheDirectory, fileName);
                    using FileStream stream = File.OpenWrite(filePath);
                    stackedBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);

                    return filePath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating stacked histogram image");
                    return string.Empty;
                }
            });
        }

        private async Task<SceneEvaluationStatsDto> CalculateHistogramsAsync(
            SKBitmap bitmap,
            int[] redHistogram,
            int[] greenHistogram,
            int[] blueHistogram,
            int[] contrastHistogram,
            CancellationToken cancellationToken)
        {
            // Optimization: Process pixels in chunks to allow for better cancellation and responsiveness
            const int pixelChunkSize = 10000;

            int width = bitmap.Width;
            int height = bitmap.Height;
            int totalPixels = width * height;

            long redSum = 0, greenSum = 0, blueSum = 0, contrastSum = 0;
            long redSumSquared = 0, greenSumSquared = 0, blueSumSquared = 0, contrastSumSquared = 0;
            int processedPixels = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Yield control every chunk of pixels to allow cancellation and prevent UI freezing
                    if (processedPixels % pixelChunkSize == 0)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (processedPixels > 0) // Don't yield on first iteration
                        {
                            await Task.Yield();
                        }
                    }

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

                    processedPixels++;
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
            string colorName,
            string timestamp,
            SKColor color,
            CancellationToken cancellationToken)
        {
            return await Task.Run(() =>
            {
                try
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

                    using SKBitmap histogramBitmap = new SKBitmap(width, height);
                    using SKCanvas canvas = new SKCanvas(histogramBitmap);

                    canvas.Clear(SKColors.White);

                    using SKPaint paint = new SKPaint
                    {
                        Color = color,
                        StrokeWidth = 1,
                        IsAntialias = true
                    };

                    // Draw histogram
                    for (int i = 0; i < HistogramBuckets - 1; i++)
                    {
                        float x1 = i;
                        float y1 = height - ((float)histogram[i] / maxValue * height);
                        float x2 = i + 1;
                        float y2 = height - ((float)histogram[i + 1] / maxValue * height);

                        canvas.DrawLine(x1, y1, x2, y2, paint);
                    }

                    // Save the histogram
                    string fileName = $"histogram_{colorName}_{timestamp}.png";
                    string filePath = Path.Combine(_cacheDirectory, fileName);

                    using FileStream stream = File.OpenWrite(filePath);
                    histogramBitmap.Encode(stream, SKEncodedImageFormat.Png, 100);

                    return filePath;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error saving {ColorName} histogram", colorName);
                    return string.Empty;
                }
            }, cancellationToken);
        }
    }
}