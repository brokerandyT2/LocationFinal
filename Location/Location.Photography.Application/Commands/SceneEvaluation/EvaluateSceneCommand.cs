using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.Application.Resources;
using MediatR;

namespace Location.Photography.Application.Commands.SceneEvaluation
{
    public class EvaluateSceneCommand : IRequest<Result<SceneEvaluationResultDto>>
    {
        // No parameters needed for scene evaluation - captures current scene
    }

    public class EvaluateSceneCommandHandler : IRequestHandler<EvaluateSceneCommand, Result<SceneEvaluationResultDto>>
    {
        private readonly ISceneEvaluationService _sceneEvaluationService;

        public EvaluateSceneCommandHandler(ISceneEvaluationService sceneEvaluationService)
        {
            _sceneEvaluationService = sceneEvaluationService ?? throw new ArgumentNullException(nameof(sceneEvaluationService));
        }

        public async Task<Result<SceneEvaluationResultDto>> Handle(EvaluateSceneCommand request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                return await _sceneEvaluationService.EvaluateSceneAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                return Result<SceneEvaluationResultDto>.Failure(string.Format(AppResources.SceneEvaluation_Error_EvaluatingScene + ": {0}", ex.Message));
            }
        }
    }
}