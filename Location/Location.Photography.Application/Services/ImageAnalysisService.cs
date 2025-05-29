using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Application.Services
{
    public class ImageAnalysisService : IImageAnalysisService
    {
        private const double CalibrationConstant = 12.5;

        public async Task<ImageAnalysisResult> AnalyzeImageAsync(Stream imageStream, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                using var bitmap = SKBitmap.Decode(imageStream);
                if (bitmap == null)
                    throw new InvalidOperationException("Unable to decode image");

                cancellationToken.ThrowIfCancellationRequested();

                var result = new ImageAnalysisResult();
                var luminanceValues = new List<double>();

                int totalPixels = bitmap.Width * bitmap.Height;
                double redSum = 0, greenSum = 0, blueSum = 0;

                // Process all pixels
                for (int y = 0; y < bitmap.Height; y++)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        var color = bitmap.GetPixel(x, y);

                        // Accumulate histogram data
                        result.RedHistogram.Values[color.Red]++;
                        result.GreenHistogram.Values[color.Green]++;
                        result.BlueHistogram.Values[color.Blue]++;

                        // Calculate accurate luminance
                        double luminance = CalculateAccurateLuminance(color);
                        int luminanceIndex = Math.Min(255, Math.Max(0, (int)(luminance * 255)));
                        result.LuminanceHistogram.Values[luminanceIndex]++;
                        luminanceValues.Add(luminance);

                        redSum += color.Red;
                        greenSum += color.Green;
                        blueSum += color.Blue;
                    }
                }

                // Normalize histograms
                NormalizeHistogram(result.RedHistogram.Values, totalPixels);
                NormalizeHistogram(result.GreenHistogram.Values, totalPixels);
                NormalizeHistogram(result.BlueHistogram.Values, totalPixels);
                NormalizeHistogram(result.LuminanceHistogram.Values, totalPixels);

                // Calculate statistics
                result.RedHistogram.Statistics = CalculateHistogramStatistics(result.RedHistogram.Values);
                result.GreenHistogram.Statistics = CalculateHistogramStatistics(result.GreenHistogram.Values);
                result.BlueHistogram.Statistics = CalculateHistogramStatistics(result.BlueHistogram.Values);
                result.LuminanceHistogram.Statistics = CalculateHistogramStatistics(result.LuminanceHistogram.Values);

                // Calculate color temperature and tint
                double avgRed = redSum / totalPixels;
                double avgGreen = greenSum / totalPixels;
                double avgBlue = blueSum / totalPixels;

                result.WhiteBalance = CalculateColorTemperature(avgRed, avgGreen, avgBlue);

                // Calculate contrast metrics
                result.Contrast = CalculateContrastMetrics(luminanceValues);

                // Calculate exposure analysis
                result.Exposure = CalculateExposureAnalysis(luminanceValues, result.LuminanceHistogram.Statistics);

                return result;
            }, cancellationToken);
        }

        public async Task<string> GenerateHistogramImageAsync(double[] histogram, SKColor color, string fileName)
        {
            return await Task.Run(() =>
            {
                string filePath = Path.Combine(FileSystem.AppDataDirectory, fileName);
                int width = 512;
                int height = 256;
                int margin = 10;

                using var surface = SKSurface.Create(new SKImageInfo(width, height));
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);

                // Draw axes
                using var axisPaint = new SKPaint { Color = SKColors.Black, StrokeWidth = 2, IsAntialias = true };
                canvas.DrawLine(margin, height - margin, width - margin, height - margin, axisPaint);
                canvas.DrawLine(margin, height - margin, margin, margin, axisPaint);

                // Draw histogram
                DrawHistogramLine(canvas, histogram, color, width, height, margin);

                // Save image
                using var image = surface.Snapshot();
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(filePath);
                data.SaveTo(stream);

                return filePath;
            });
        }

        private double CalculateAccurateLuminance(SKColor color)
        {
            // Convert to linear RGB first
            double linearR = SRGBToLinear(color.Red / 255.0);
            double linearG = SRGBToLinear(color.Green / 255.0);
            double linearB = SRGBToLinear(color.Blue / 255.0);

            // Use proper Rec. 709 luminance coefficients
            return 0.2126 * linearR + 0.7152 * linearG + 0.0722 * linearB;
        }

        private double SRGBToLinear(double srgb)
        {
            return srgb <= 0.04045
                ? srgb / 12.92
                : Math.Pow((srgb + 0.055) / 1.055, 2.4);
        }

        private ColorTemperatureData CalculateColorTemperature(double avgRed, double avgGreen, double avgBlue)
        {
            var result = new ColorTemperatureData();

            // Convert RGB to CIE XYZ color space
            var (X, Y, Z) = RGBToXYZ(avgRed, avgGreen, avgBlue);

            // Convert XYZ to chromaticity coordinates
            double totalXYZ = X + Y + Z;
            if (totalXYZ == 0)
            {
                result.Temperature = 5500;
                result.Tint = 0;
                return result;
            }

            double x = X / totalXYZ;
            double y = Y / totalXYZ;

            // McCamy's approximation formula
            double n = (x - 0.3320) / (0.1858 - y);
            double cct = 449 * Math.Pow(n, 3) + 3525 * Math.Pow(n, 2) + 6823.3 * n + 5520.33;

            result.Temperature = Math.Max(2000, Math.Min(25000, cct));
            result.Tint = CalculateTintValue(avgRed, avgGreen, avgBlue);
            result.RedRatio = avgRed / (avgRed + avgGreen + avgBlue);
            result.GreenRatio = avgGreen / (avgRed + avgGreen + avgBlue);
            result.BlueRatio = avgBlue / (avgRed + avgGreen + avgBlue);

            return result;
        }

        private (double X, double Y, double Z) RGBToXYZ(double r, double g, double b)
        {
            // Normalize to 0-1 and apply gamma correction
            r = SRGBToLinear(r / 255.0);
            g = SRGBToLinear(g / 255.0);
            b = SRGBToLinear(b / 255.0);

            // sRGB to XYZ transformation matrix (D65 illuminant)
            double X = r * 0.4124564 + g * 0.3575761 + b * 0.1804375;
            double Y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
            double Z = r * 0.0193339 + g * 0.1191920 + b * 0.9503041;

            return (X, Y, Z);
        }

        private double CalculateTintValue(double avgRed, double avgGreen, double avgBlue)
        {
            double greenMagentaRatio = avgGreen / ((avgRed + avgBlue) / 2);
            return Math.Max(-1.0, Math.Min(1.0, (greenMagentaRatio - 1.0) * 2.0));
        }

        private ContrastMetrics CalculateContrastMetrics(List<double> luminanceValues)
        {
            if (!luminanceValues.Any()) return new ContrastMetrics();

            double mean = luminanceValues.Average();
            double min = luminanceValues.Min();
            double max = luminanceValues.Max();

            return new ContrastMetrics
            {
                RMSContrast = CalculateRMSContrast(luminanceValues, mean),
                MichelsonContrast = max > 0 ? (max - min) / (max + min) : 0,
                WeberContrast = min > 0 ? (mean - min) / min : 0,
                DynamicRange = max > min && min > 0 ? Math.Log10(max / min) * 3.32 : 0, // Convert to stops
                GlobalContrast = max - min
            };
        }

        private double CalculateRMSContrast(List<double> values, double mean)
        {
            if (!values.Any()) return 0;
            double sumSquaredDifferences = values.Sum(v => Math.Pow(v - mean, 2));
            return Math.Sqrt(sumSquaredDifferences / values.Count);
        }

        private ExposureAnalysis CalculateExposureAnalysis(List<double> luminanceValues, HistogramStatistics stats)
        {
            if (!luminanceValues.Any()) return new ExposureAnalysis();

            double mean = luminanceValues.Average();
            double median = CalculateMedian(luminanceValues);

            return new ExposureAnalysis
            {
                AverageEV = Math.Log(mean * CalibrationConstant, 2),
                SuggestedEV = Math.Log(0.18 * CalibrationConstant, 2), // 18% gray target
                IsUnderexposed = mean < 0.1 || stats.ShadowClipping,
                IsOverexposed = mean > 0.8 || stats.HighlightClipping,
                HistogramBalance = median,
                ShadowDetail = luminanceValues.Count(v => v < 0.1) / (double)luminanceValues.Count,
                HighlightDetail = luminanceValues.Count(v => v > 0.9) / (double)luminanceValues.Count,
                RecommendedSettings = GenerateExposureRecommendation(mean, stats)
            };
        }

        private string GenerateExposureRecommendation(double mean, HistogramStatistics stats)
        {
            var recommendations = new List<string>();

            if (mean < 0.1)
                recommendations.Add("Increase exposure (+1 to +2 stops)");
            else if (mean > 0.8)
                recommendations.Add("Decrease exposure (-1 to -2 stops)");

            if (stats.ShadowClipping)
                recommendations.Add("Shadow clipping detected - lift shadows");

            if (stats.HighlightClipping)
                recommendations.Add("Highlight clipping detected - reduce highlights");

            if (stats.DynamicRange > 10)
                recommendations.Add("High dynamic range - consider HDR or graduated filters");

            return string.Join("; ", recommendations);
        }

        private HistogramStatistics CalculateHistogramStatistics(double[] histogram)
        {
            var stats = new HistogramStatistics();

            // Calculate mean
            double sum = 0;
            double totalCount = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                sum += i * histogram[i];
                totalCount += histogram[i];
            }
            stats.Mean = totalCount > 0 ? sum / totalCount : 0;

            // Calculate median
            stats.Median = CalculateHistogramMedian(histogram);

            // Calculate standard deviation
            double variance = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                variance += histogram[i] * Math.Pow(i - stats.Mean, 2);
            }
            stats.StandardDeviation = totalCount > 0 ? Math.Sqrt(variance / totalCount) : 0;

            // Detect clipping
            double shadowThreshold = 0.02;
            double highlightThreshold = 0.02;

            double shadowClipping = 0;
            for (int i = 0; i < 6; i++)
                shadowClipping += histogram[i];

            double highlightClipping = 0;
            for (int i = 250; i < 256; i++)
                highlightClipping += histogram[i];

            stats.ShadowClipping = shadowClipping > shadowThreshold;
            stats.HighlightClipping = highlightClipping > highlightThreshold;

            // Calculate dynamic range
            int minNonZero = Array.FindIndex(histogram, h => h > 0.001);
            int maxNonZero = Array.FindLastIndex(histogram, h => h > 0.001);
            stats.DynamicRange = maxNonZero > minNonZero ? maxNonZero - minNonZero : 0;

            return stats;
        }

        private double CalculateHistogramMedian(double[] histogram)
        {
            double totalCount = histogram.Sum();
            double halfCount = totalCount / 2;
            double runningSum = 0;

            for (int i = 0; i < histogram.Length; i++)
            {
                runningSum += histogram[i];
                if (runningSum >= halfCount)
                    return i;
            }

            return 128; // Default to middle
        }

        private double CalculateMedian(List<double> values)
        {
            if (!values.Any()) return 0;

            var sorted = values.OrderBy(v => v).ToList();
            int count = sorted.Count;

            if (count % 2 == 0)
                return (sorted[count / 2 - 1] + sorted[count / 2]) / 2;
            else
                return sorted[count / 2];
        }

        private static void NormalizeHistogram(double[] histogram, int totalPixels)
        {
            if (totalPixels == 0) return;

            double maxValue = 0;
            for (int i = 0; i < histogram.Length; i++)
            {
                histogram[i] /= totalPixels;
                if (histogram[i] > maxValue)
                    maxValue = histogram[i];
            }

            if (maxValue > 0)
            {
                for (int i = 0; i < histogram.Length; i++)
                {
                    histogram[i] /= maxValue;
                }
            }
        }

        private static void DrawHistogramLine(SKCanvas canvas, double[] histogram, SKColor color, int width, int height, int margin)
        {
            int graphWidth = width - (2 * margin);
            int graphHeight = height - (2 * margin);

            using var paint = new SKPaint
            {
                Color = color,
                StrokeWidth = 2,
                IsAntialias = true,
                Style = SKPaintStyle.Stroke
            };

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
}