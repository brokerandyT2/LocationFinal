using SkiaSharp;

namespace Location.Photography.Application.Services
{
    public interface IImageAnalysisService
    {
        Task<ImageAnalysisResult> AnalyzeImageAsync(Stream imageStream, CancellationToken cancellationToken = default);
        Task<string> GenerateHistogramImageAsync(double[] histogram, SKColor color, string fileName);
    }

    public class ImageAnalysisResult
    {
        public HistogramData RedHistogram { get; set; } = new();
        public HistogramData GreenHistogram { get; set; } = new();
        public HistogramData BlueHistogram { get; set; } = new();
        public HistogramData LuminanceHistogram { get; set; } = new();
        public ColorTemperatureData WhiteBalance { get; set; } = new();
        public ContrastMetrics Contrast { get; set; } = new();
        public ExposureAnalysis Exposure { get; set; } = new();
    }

    public class HistogramData
    {
        public double[] Values { get; set; } = new double[256];
        public HistogramStatistics Statistics { get; set; } = new();
        public string ImagePath { get; set; } = string.Empty;
    }

    public class HistogramStatistics
    {
        public double Mean { get; set; }
        public double Median { get; set; }
        public double StandardDeviation { get; set; }
        public bool ShadowClipping { get; set; }
        public bool HighlightClipping { get; set; }
        public double DynamicRange { get; set; }
        public double Mode { get; set; }
        public double Skewness { get; set; }
    }

    public class ColorTemperatureData
    {
        public double Temperature { get; set; } = 5500;
        public double Tint { get; set; } = 0;
        public double RedRatio { get; set; }
        public double GreenRatio { get; set; }
        public double BlueRatio { get; set; }
    }

    public class ContrastMetrics
    {
        public double RMSContrast { get; set; }
        public double MichelsonContrast { get; set; }
        public double WeberContrast { get; set; }
        public double DynamicRange { get; set; }
        public double GlobalContrast { get; set; }
    }

    public class ExposureAnalysis
    {
        public double AverageEV { get; set; }
        public double SuggestedEV { get; set; }
        public bool IsUnderexposed { get; set; }
        public bool IsOverexposed { get; set; }
        public string RecommendedSettings { get; set; } = string.Empty;
        public double HistogramBalance { get; set; } = 0.5;
        public double ShadowDetail { get; set; }
        public double HighlightDetail { get; set; }
    }

    public enum HistogramDisplayMode
    {
        Red,
        Green,
        Blue,
        Luminance,
        RGB
    }
}