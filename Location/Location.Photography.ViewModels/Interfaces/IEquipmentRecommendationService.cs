// Location.Photography.Application/Services/IEquipmentRecommendationService.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;

namespace Location.Photography.ViewModels.Interfaces
{
    public interface IEquipmentRecommendationService
    {
        /// <summary>
        /// Gets specific user equipment recommendations for astrophotography target
        /// </summary>
        Task<Result<UserEquipmentRecommendation>> GetUserEquipmentRecommendationAsync(
            AstroTarget target,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets equipment recommendations for hourly predictions
        /// </summary>
        Task<Result<List<HourlyEquipmentRecommendation>>> GetHourlyEquipmentRecommendationsAsync(
            AstroTarget target,
            List<DateTime> predictionTimes,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets generic equipment recommendations when no user equipment available
        /// </summary>
        Task<Result<GenericEquipmentRecommendation>> GetGenericRecommendationAsync(
            AstroTarget target,
            CancellationToken cancellationToken = default);
    }


}

