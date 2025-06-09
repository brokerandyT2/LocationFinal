using Location.Photography.Application.DTOs;
using Location.Photography.Domain.Models;

namespace Location.Photography.Application.Common.Interfaces
{
    public interface IAstroHourlyPredictionMappingService
    {
        Task<List<AstroHourlyPredictionDto>> MapFromDomainDataAsync(
            List<AstroCalculationResult> calculationResults,
            double latitude,
            double longitude,
            DateTime selectedDate);

        Task<AstroHourlyPredictionDto> MapSingleCalculationAsync(
            AstroCalculationResult calculationResult,
            double latitude,
            double longitude,
            DateTime selectedDate);

        Task<List<AstroHourlyPredictionDto>> GenerateHourlyPredictionsAsync(
            DateTime startTime,
            DateTime endTime,
            double latitude,
            double longitude,
            DateTime selectedDate);
    }
}