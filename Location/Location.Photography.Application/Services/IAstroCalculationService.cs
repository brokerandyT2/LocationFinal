// Location.Photography.Application/Services/IAstroCalculationService.cs
using Location.Photography.Domain.Models;

namespace Location.Photography.Application.Services
{
    /// <summary>
    /// Service for advanced astronomical calculations focused on astrophotography using CosineKitty Astronomy Engine
    /// </summary>
    public interface IAstroCalculationService
    {
        // === PLANETARY CALCULATIONS ===

        /// <summary>
        /// Gets position data for a specific planet at given time and observer location
        /// </summary>
        Task<PlanetPositionData> GetPlanetPositionAsync(PlanetType planet, DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all visible planets for astrophotography at given time and location
        /// </summary>
        Task<List<PlanetPositionData>> GetVisiblePlanetsAsync(DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates planetary conjunctions within specified date range
        /// </summary>
        Task<List<PlanetaryConjunction>> GetPlanetaryConjunctionsAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets planet opposition dates (best viewing times) within date range
        /// </summary>
        Task<List<PlanetaryEvent>> GetPlanetOppositionsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        // === ENHANCED LUNAR CALCULATIONS ===

        /// <summary>
        /// Gets detailed moon data including libration for lunar photography
        /// </summary>
        Task<EnhancedMoonData> GetEnhancedMoonDataAsync(DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates supermoon events within date range
        /// </summary>
        Task<List<SupermoonEvent>> GetSupermoonEventsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets lunar eclipse details with umbra/penumbra timing
        /// </summary>
        Task<List<LunarEclipseData>> GetLunarEclipsesAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates optimal lunar photography windows based on phase and elevation
        /// </summary>
        Task<List<LunarPhotographyWindow>> GetOptimalLunarWindowsAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default);

        // === DEEP SKY CALCULATIONS ===

        /// <summary>
        /// Gets Milky Way galactic center position and visibility
        /// </summary>
        Task<MilkyWayData> GetMilkyWayDataAsync(DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates constellation rise/set times and optimal viewing windows
        /// </summary>
        Task<ConstellationData> GetConstellationDataAsync(ConstellationType constellation, DateTime date, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets deep sky object (Messier/NGC) visibility and position
        /// </summary>
        Task<DeepSkyObjectData> GetDeepSkyObjectDataAsync(string catalogId, DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates International Space Station pass times
        /// </summary>
        Task<List<ISSPassData>> GetISSPassesAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default);

        // === METEOR SHOWER CALCULATIONS ===

        /// <summary>
        /// Gets meteor shower data including peak times and radiant positions
        /// </summary>
        Task<List<MeteorShowerData>> GetMeteorShowersAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates optimal meteor shower viewing conditions
        /// </summary>
        Task<MeteorShowerConditions> GetMeteorShowerConditionsAsync(MeteorShowerType shower, DateTime date, double latitude, double longitude, CancellationToken cancellationToken = default);

        // === ASTROPHOTOGRAPHY-SPECIFIC CALCULATIONS ===

        /// <summary>
        /// Calculates optimal exposure settings for astrophotography based on equipment and target
        /// </summary>
        Task<AstroExposureRecommendation> GetAstroExposureRecommendationAsync(AstroTarget target, CameraEquipmentData equipment, AstroConditions conditions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates field of view for astrophotography targets
        /// </summary>
        Task<AstroFieldOfViewData> GetAstroFieldOfViewAsync(double focalLength, double sensorWidth, double sensorHeight, AstroTarget target, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets star trail calculations for long exposure photography
        /// </summary>
        Task<StarTrailData> GetStarTrailDataAsync(DateTime startTime, TimeSpan exposureDuration, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Calculates polar alignment data for equatorial mounts
        /// </summary>
        Task<PolarAlignmentData> GetPolarAlignmentDataAsync(DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default);

        // === COORDINATE TRANSFORMATIONS ===

        /// <summary>
        /// Converts between coordinate systems (equatorial, galactic, alt-az)
        /// </summary>
        Task<CoordinateTransformResult> TransformCoordinatesAsync(CoordinateType fromType, CoordinateType toType, double coordinate1, double coordinate2, DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default);

        /// <summary>
        /// Applies atmospheric refraction corrections for precise positioning
        /// </summary>
        Task<AtmosphericCorrectionData> GetAtmosphericCorrectionAsync(double altitude, double azimuth, double temperature, double pressure, double humidity, CancellationToken cancellationToken = default);
    }
}