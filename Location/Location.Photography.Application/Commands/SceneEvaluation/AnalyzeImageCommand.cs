// Location.Photography.Application/Commands/SceneEvaluation/AnalyzeImageCommand.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using MediatR;

namespace Location.Photography.Application.Commands.SceneEvaluation
{
    public class AnalyzeImageCommand : IRequest<Result<SceneEvaluationResultDto>>
    {
        public string ImagePath { get; set; } = string.Empty;
    }

    public class AnalyzeImageCommandHandler : IRequestHandler<AnalyzeImageCommand, Result<SceneEvaluationResultDto>>
    {
        private readonly ISceneEvaluationService _sceneEvaluationService;

        public AnalyzeImageCommandHandler(ISceneEvaluationService sceneEvaluationService)
        {
            _sceneEvaluationService = sceneEvaluationService ?? throw new ArgumentNullException(nameof(sceneEvaluationService));
        }

        public async Task<Result<SceneEvaluationResultDto>> Handle(AnalyzeImageCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await _sceneEvaluationService.AnalyzeImageAsync(request.ImagePath, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<SceneEvaluationResultDto>.Failure($"Error analyzing image: {ex.Message}");
            }
        }
    }
}