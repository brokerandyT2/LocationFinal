// Location.Photography.Application/Queries/AstroLocation/GetAstroEventsForDateQuery.cs
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using MediatR;

namespace Location.Photography.Application.Queries.AstroLocation
{
    public class GetAstroEventsForDateQuery : IRequest<Result<List<AstroEventDto>>>
    {
        public DateTime Date { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public int MinimumAltitude { get; set; } = 10; // Minimum altitude in degrees
        public bool IncludeDayTimeEvents { get; set; } = false;

        public class GetAstroEventsForDateQueryHandler : IRequestHandler<GetAstroEventsForDateQuery, Result<List<AstroEventDto>>>
        {
            private readonly IAstroCalculationService _astroCalculationService;
            private readonly IMeteorShowerDataService _meteorShowerDataService;

            public GetAstroEventsForDateQueryHandler(
                IAstroCalculationService astroCalculationService,
                IMeteorShowerDataService meteorShowerDataService)
            {
                _astroCalculationService = astroCalculationService ?? throw new ArgumentNullException(nameof(astroCalculationService));
                _meteorShowerDataService = meteorShowerDataService ?? throw new ArgumentNullException(nameof(meteorShowerDataService));
            }

            public async Task<Result<List<AstroEventDto>>> Handle(GetAstroEventsForDateQuery request, CancellationToken cancellationToken)
            {
                try
                {
                    var events = new List<AstroEventDto>();

                    // Calculate for the evening of the requested date (astronomical night)
                    var eventDate = request.Date.Date.AddHours(20); // Start at 8 PM local time

                    // Get Moon data
                    var moonData = await _astroCalculationService.GetEnhancedMoonDataAsync(
                        eventDate, request.Latitude, request.Longitude, cancellationToken);
                    if (moonData != null && (moonData.Altitude > request.MinimumAltitude || request.IncludeDayTimeEvents))
                    {
                        events.Add(CreateMoonEvent(moonData));
                    }

                    // Get visible planets
                    var planetTypes = new[] { PlanetType.Venus, PlanetType.Mars, PlanetType.Jupiter, PlanetType.Saturn };
                    foreach (var planetType in planetTypes)
                    {
                        var planetData = await _astroCalculationService.GetPlanetPositionAsync(
                            planetType, eventDate, request.Latitude, request.Longitude, cancellationToken);
                        if (planetData != null && planetData.IsVisible &&
                            (planetData.Altitude > request.MinimumAltitude || request.IncludeDayTimeEvents))
                        {
                            events.Add(CreatePlanetEvent(planetData));
                        }
                    }

                    // Get Milky Way Core visibility
                    var milkyWayData = await _astroCalculationService.GetMilkyWayDataAsync(
                        eventDate, request.Latitude, request.Longitude, cancellationToken);
                    if (milkyWayData != null && milkyWayData.IsVisible &&
                        milkyWayData.GalacticCenterAltitude > request.MinimumAltitude)
                    {
                        events.Add(CreateMilkyWayEvent(milkyWayData, eventDate));
                    }

                    // Get prominent Deep Sky Objects
                    var dsoTargets = new[] { "M31", "M42", "M45", "M13", "M57", "M27", "M51", "M81", "M82" };
                    foreach (var dsoId in dsoTargets)
                    {
                        var dsoData = await _astroCalculationService.GetDeepSkyObjectDataAsync(
                            dsoId, eventDate, request.Latitude, request.Longitude, cancellationToken);
                        if (dsoData != null && dsoData.IsVisible && dsoData.Altitude > request.MinimumAltitude)
                        {
                            events.Add(CreateDSOEvent(dsoData));
                        }
                    }

                    // Get active meteor showers
                    var meteorShowers = await _meteorShowerDataService.GetActiveShowersAsync(
                        request.Date, cancellationToken);
                    foreach (var shower in meteorShowers)
                    {
                        var radiantPos = shower.GetRadiantPosition(eventDate, request.Latitude, request.Longitude);
                        if (radiantPos.Altitude > request.MinimumAltitude)
                        {
                            events.Add(CreateMeteorShowerEvent(shower, radiantPos, eventDate));
                        }
                    }

                    // Get ISS passes if available
                    try
                    {
                        var issPasses = await _astroCalculationService.GetISSPassesAsync(
                            request.Date, request.Date, request.Latitude, request.Longitude, cancellationToken);
                        foreach (var pass in issPasses.Where(p => p.MaxAltitude > request.MinimumAltitude))
                        {
                            events.Add(CreateISSEvent(pass));
                        }
                    }
                    catch
                    {
                        // ISS data may not always be available, continue without it
                    }

                    // Sort events by visibility score (best first), then by optimal time
                    var sortedEvents = events
                        .OrderByDescending(e => CalculateVisibilityScore(e))
                        .ThenBy(e => GetOptimalTime(e))
                        .ToList();

                    return Result<List<AstroEventDto>>.Success(sortedEvents);
                }
                catch (Exception ex)
                {
                    return Result<List<AstroEventDto>>.Failure($"Error retrieving astro events: {ex.Message}");
                }
            }

            private AstroEventDto CreateMoonEvent(EnhancedMoonData moonData)
            {
                return new AstroEventDto
                {
                    Name = $"Moon ({moonData.PhaseName})",
                    Target = AstroTarget.Moon,
                    StartTime = moonData.Rise ?? moonData.DateTime,
                    EndTime = moonData.Set ?? moonData.DateTime.AddHours(12),
                    PeakTime = moonData.Transit,
                    Azimuth = moonData.Azimuth,
                    Altitude = moonData.Altitude,
                    IsVisible = moonData.Altitude > 0,
                    Description = $"Phase: {moonData.Illumination:F0}%, Distance: {moonData.Distance:F0}km",
                    EventType = moonData.PhaseName,
                    AngularSize = moonData.AngularDiameter,
                    Magnitude = -12.7, // Approximate moon magnitude
                    Constellation = "",
                    RecommendedEquipment = "Telephoto lens or telescope"
                };
            }

            private AstroEventDto CreatePlanetEvent(PlanetPositionData planetData)
            {
                return new AstroEventDto
                {
                    Name = GetPlanetDisplayName(planetData.Planet),
                    Target = AstroTarget.Planets,
                    StartTime = planetData.Rise ?? planetData.DateTime,
                    EndTime = planetData.Set ?? planetData.DateTime.AddHours(12),
                    PeakTime = planetData.Transit,
                    Azimuth = planetData.Azimuth,
                    Altitude = planetData.Altitude,
                    Magnitude = planetData.ApparentMagnitude,
                    AngularSize = planetData.AngularDiameter,
                    IsVisible = planetData.IsVisible,
                    Description = $"Magnitude {planetData.ApparentMagnitude:F1}, {planetData.AngularDiameter:F1}″",
                    RecommendedEquipment = planetData.RecommendedEquipment,
                    EventType = "Planet",
                    Constellation = ""
                };
            }

            private AstroEventDto CreateMilkyWayEvent(MilkyWayData milkyWayData, DateTime eventDate)
            {
                return new AstroEventDto
                {
                    Name = "Milky Way Core",
                    Target = AstroTarget.MilkyWayCore,
                    StartTime = milkyWayData.Rise ?? eventDate,
                    EndTime = milkyWayData.Set ?? eventDate.AddHours(8),
                    PeakTime = milkyWayData.OptimalViewingTime,
                    Azimuth = milkyWayData.GalacticCenterAzimuth,
                    Altitude = milkyWayData.GalacticCenterAltitude,
                    IsVisible = true,
                    Description = $"Dark sky quality: {milkyWayData.DarkSkyQuality:P0}",
                    EventType = "Galactic Core",
                    RecommendedEquipment = "Wide-angle lens, tracker recommended",
                    Magnitude = 0, // N/A for extended object
                    AngularSize = 0,
                    Constellation = "Sagittarius"
                };
            }

            private AstroEventDto CreateDSOEvent(DeepSkyObjectData dsoData)
            {
                return new AstroEventDto
                {
                    Name = !string.IsNullOrEmpty(dsoData.CommonName) ? dsoData.CommonName : dsoData.CatalogId,
                    Target = AstroTarget.DeepSkyObjects,
                    StartTime = dsoData.DateTime,
                    EndTime = dsoData.DateTime.AddHours(8),
                    PeakTime = dsoData.OptimalViewingTime,
                    Azimuth = dsoData.Azimuth,
                    Altitude = dsoData.Altitude,
                    Magnitude = dsoData.Magnitude,
                    AngularSize = dsoData.AngularSize,
                    IsVisible = dsoData.IsVisible,
                    Description = $"{dsoData.ObjectType}, Mag {dsoData.Magnitude:F1}, {dsoData.AngularSize:F1}'",
                    Constellation = dsoData.ParentConstellation.ToString(),
                    RecommendedEquipment = dsoData.RecommendedEquipment,
                    EventType = dsoData.ObjectType
                };
            }

            private AstroEventDto CreateMeteorShowerEvent(Domain.Entities.MeteorShower shower, Domain.Entities.RadiantPosition radiantPos, DateTime eventDate)
            {
                return new AstroEventDto
                {
                    Name = shower.Designation,
                    Target = AstroTarget.MeteorShowers,
                    StartTime = shower.Activity.IsActiveOn(eventDate) ? eventDate : eventDate.AddDays(-1),
                    EndTime = eventDate.AddHours(8),
                    PeakTime = eventDate, // Use event date as peak for simplicity
                    Azimuth = radiantPos.Azimuth,
                    Altitude = radiantPos.Altitude,
                    IsVisible = radiantPos.IsVisible,
                    Description = $"ZHR: {shower.Activity.ZHR}, Radiant: {radiantPos.DirectionDescription}",
                    EventType = "Meteor Shower",
                    RecommendedEquipment = "Wide-angle lens, no tracking",
                    Magnitude = 0, // Variable
                    AngularSize = 0,
                    Constellation = ""
                };
            }

            private AstroEventDto CreateISSEvent(ISSPassData pass)
            {
                return new AstroEventDto
                {
                    Name = "ISS Pass",
                    Target = AstroTarget.Comets, // Closest available target type
                    StartTime = pass.StartTime,
                    EndTime = pass.EndTime,
                    PeakTime = pass.MaxTime,
                    Azimuth = pass.StartAzimuth,
                    Altitude = pass.MaxAltitude,
                    Magnitude = pass.Magnitude,
                    IsVisible = true,
                    Description = $"Max altitude: {pass.MaxAltitude:F0}°, Duration: {pass.Duration.TotalMinutes:F0}m",
                    EventType = "Satellite Pass",
                    RecommendedEquipment = "Binoculars or naked eye",
                    AngularSize = 0,
                    Constellation = ""
                };
            }

            private double CalculateVisibilityScore(AstroEventDto evt)
            {
                if (!evt.IsVisible) return 0.0;

                double score = 0.5; // Base score for being visible

                // Altitude bonus (higher is better)
                if (evt.Altitude > 60) score += 0.3;
                else if (evt.Altitude > 30) score += 0.2;
                else if (evt.Altitude > 15) score += 0.1;

                // Magnitude bonus (brighter is better, lower magnitude = brighter)
                if (evt.Magnitude < 0) score += 0.2;
                else if (evt.Magnitude < 3) score += 0.1;

                return Math.Min(1.0, score);
            }

            private DateTime GetOptimalTime(AstroEventDto evt)
            {
                if (evt.PeakTime.HasValue)
                    return evt.PeakTime.Value;

                // Calculate average of start and end times
                var span = evt.EndTime - evt.StartTime;
                return evt.StartTime.Add(TimeSpan.FromTicks(span.Ticks / 2));
            }

            private string GetPlanetDisplayName(PlanetType planet)
            {
                return planet switch
                {
                    PlanetType.Mercury => "Mercury",
                    PlanetType.Venus => "Venus",
                    PlanetType.Mars => "Mars",
                    PlanetType.Jupiter => "Jupiter",
                    PlanetType.Saturn => "Saturn",
                    PlanetType.Uranus => "Uranus",
                    PlanetType.Neptune => "Neptune",
                    PlanetType.Pluto => "Pluto",
                    _ => planet.ToString()
                };
            }
        }
    }

    // DTO for transferring astro event data from Application to ViewModels layer
    public class AstroEventDto
    {
        public string Name { get; set; } = string.Empty;
        public AstroTarget Target { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime? PeakTime { get; set; }
        public double Azimuth { get; set; }
        public double Altitude { get; set; }
        public double Magnitude { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Constellation { get; set; } = string.Empty;
        public bool IsVisible { get; set; }
        public string EventType { get; set; } = string.Empty;
        public double AngularSize { get; set; }
        public string RecommendedEquipment { get; set; } = string.Empty;
    }
}
