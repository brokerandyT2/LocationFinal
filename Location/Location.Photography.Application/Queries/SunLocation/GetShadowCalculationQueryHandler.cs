// Location.Photography.Application/Queries/SunLocation/GetShadowCalculationQueryHandler.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Location.Photography.Application.Queries.SunLocation
{
    public class GetShadowCalculationQueryHandler : IRequestHandler<GetShadowCalculationQuery, Result<ShadowCalculationResult>>
    {
        private readonly ISunCalculatorService _sunCalculatorService;
        private readonly ILogger<GetShadowCalculationQueryHandler> _logger;

        public GetShadowCalculationQueryHandler(
            ISunCalculatorService sunCalculatorService,
            ILogger<GetShadowCalculationQueryHandler> logger)
        {
            _sunCalculatorService = sunCalculatorService ?? throw new ArgumentNullException(nameof(sunCalculatorService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<ShadowCalculationResult>> Handle(GetShadowCalculationQuery request, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var sunElevation = _sunCalculatorService.GetSolarElevation(request.DateTime, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString());
                var sunAzimuth = _sunCalculatorService.GetSolarAzimuth(request.DateTime, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString());

                // Calculate shadow length using trigonometry
                var shadowLength = sunElevation > 0
                    ? request.ObjectHeight / Math.Tan(sunElevation * Math.PI / 180.0)
                    : double.MaxValue; // No shadow when sun is below horizon

                // Shadow direction is opposite to sun azimuth
                var shadowDirection = (sunAzimuth + 180) % 360;

                // Apply terrain factor
                var terrainMultiplier = GetTerrainMultiplier(request.TerrainType);
                shadowLength *= terrainMultiplier;

                // Calculate shadow progression throughout the day
                var shadowProgression = new List<ShadowTimePoint>();
                var startTime = request.DateTime.Date.AddHours(6);
                var endTime = request.DateTime.Date.AddHours(18);

                for (var time = startTime; time <= endTime; time = time.AddHours(1))
                {
                    var elevation = _sunCalculatorService.GetSolarElevation(time, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString());
                    var azimuth = _sunCalculatorService.GetSolarAzimuth(time, request.Latitude, request.Longitude, TimeZoneInfo.Local.ToString());

                    if (elevation > 0)
                    {
                        shadowProgression.Add(new ShadowTimePoint
                        {
                            Time = time,
                            Length = request.ObjectHeight / Math.Tan(elevation * Math.PI / 180.0) * terrainMultiplier,
                            Direction = (azimuth + 180) % 360
                        });
                    }
                }

                var result = new ShadowCalculationResult
                {
                    ShadowLength = shadowLength,
                    ShadowDirection = shadowDirection,
                    ObjectHeight = request.ObjectHeight,
                    CalculationTime = request.DateTime,
                    Terrain = request.TerrainType,
                    ShadowProgression = shadowProgression
                };

                return await Task.FromResult(Result<ShadowCalculationResult>.Success(result));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating shadow data for coordinates {Latitude}, {Longitude} at {DateTime}",
                    request.Latitude, request.Longitude, request.DateTime);
                return Result<ShadowCalculationResult>.Failure($"Error calculating shadows: {ex.Message}");
            }
        }

        private double GetTerrainMultiplier(TerrainType terrain)
        {
            return terrain switch
            {
                TerrainType.Flat => 1.0,
                TerrainType.Urban => 0.8, // Buildings create partial shadows
                TerrainType.Forest => 0.6, // Trees create dappled shadows  
                TerrainType.Mountain => 1.2, // Higher elevation can extend shadows
                TerrainType.Beach => 1.1, // Reflective surfaces can affect shadow intensity
                _ => 1.0
            };
        }
    }
}