using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using Location.Photography.ViewModels;

public class EquipmentRecommendationServiceAdapter : Location.Photography.ViewModels.Interfaces.IEquipmentRecommendationService
{
    private readonly Location.Photography.Application.Services.IEquipmentRecommendationService _applicationService;

    public EquipmentRecommendationServiceAdapter(Location.Photography.Application.Services.IEquipmentRecommendationService applicationService)
    {
        _applicationService = applicationService ?? throw new ArgumentNullException(nameof(applicationService));
    }

    public Task<Result<GenericEquipmentRecommendation>> GetGenericRecommendationAsync(AstroTarget target, CancellationToken cancellationToken = default)
    {
       return _applicationService.GetGenericRecommendationAsync(target, cancellationToken);
    }

    public Task<Result<List<HourlyEquipmentRecommendation>>> GetHourlyEquipmentRecommendationsAsync(AstroTarget target, List<DateTime> predictionTimes, CancellationToken cancellationToken = default)
    {
   return _applicationService.GetHourlyEquipmentRecommendationsAsync(target, predictionTimes, cancellationToken);
    }

    public Task<Result<UserEquipmentRecommendation>> GetUserEquipmentRecommendationAsync(AstroTarget target, CancellationToken cancellationToken = default)
    {
        return _applicationService.GetUserEquipmentRecommendationAsync(target, cancellationToken);
    }
}