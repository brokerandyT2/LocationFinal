using Location.Core.Application.Common.Models;

namespace Location.Photography.Application.Services
{
    public interface ISceneEvaluationService
    {
        /// <summary>
        /// Captures and analyzes a scene to generate histograms
        /// </summary>
        Task<Result<SceneEvaluationResultDto>> EvaluateSceneAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Analyzes an existing image to generate histograms
        /// </summary>
        Task<Result<SceneEvaluationResultDto>> AnalyzeImageAsync(string imagePath, CancellationToken cancellationToken = default);
    }

    public class SceneEvaluationResultDto
    {
        public string RedHistogramPath { get; set; } = string.Empty;
        public string GreenHistogramPath { get; set; } = string.Empty;
        public string BlueHistogramPath { get; set; } = string.Empty;
        public string ContrastHistogramPath { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public SceneEvaluationStatsDto Stats { get; set; } = new SceneEvaluationStatsDto();
    }

    public class SceneEvaluationStatsDto
    {
        public double MeanRed { get; set; }
        public double MeanGreen { get; set; }
        public double MeanBlue { get; set; }
        public double MeanContrast { get; set; }
        public double StdDevRed { get; set; }
        public double StdDevGreen { get; set; }
        public double StdDevBlue { get; set; }
        public double StdDevContrast { get; set; }
        public int TotalPixels { get; set; }
    }
}