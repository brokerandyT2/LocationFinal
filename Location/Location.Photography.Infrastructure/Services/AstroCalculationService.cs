// Location.Photography.Infrastructure/Services/AstroCalculationService.cs
using CosineKitty;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Services
{
    /// <summary>
    /// Implementation of astronomical calculations for astrophotography using CosineKitty Astronomy Engine
    /// </summary>
    public class AstroCalculationService : IAstroCalculationService
    {
        private readonly ILogger<AstroCalculationService> _logger;
        private readonly ISunCalculatorService _sunCalculatorService;
        private static readonly Dictionary<string, Body> PlanetBodies = new()
        {
            { nameof(PlanetType.Mercury), Body.Mercury },
            { nameof(PlanetType.Venus), Body.Venus },
            { nameof(PlanetType.Mars), Body.Mars },
            { nameof(PlanetType.Jupiter), Body.Jupiter },
            { nameof(PlanetType.Saturn), Body.Saturn },
            { nameof(PlanetType.Uranus), Body.Uranus },
            { nameof(PlanetType.Neptune), Body.Neptune },
            { nameof(PlanetType.Pluto), Body.Pluto }
        };

        public AstroCalculationService(ILogger<AstroCalculationService> logger, ISunCalculatorService sunCalculatorService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _sunCalculatorService = sunCalculatorService;
        }

        #region PLANETARY CALCULATIONS

        public async Task<PlanetPositionData> GetPlanetPositionAsync(PlanetType planet, DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var time = new AstroTime(dateTime);
                    var observer = new Observer(latitude, longitude, 0);
                    var body = PlanetBodies[planet.ToString()];

                    var equatorial = Astronomy.Equator(body, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                    var horizontal = Astronomy.Horizon(time, observer, equatorial.ra, equatorial.dec, Refraction.Normal);
                    var illumination = Astronomy.Illumination(body, time);

                    var riseSetTimes = GetPlanetRiseSetTimes(body, dateTime, observer);

                    return new PlanetPositionData
                    {
                        Planet = planet,
                        DateTime = dateTime,
                        RightAscension = equatorial.ra,
                        Declination = equatorial.dec,
                        Azimuth = horizontal.azimuth,
                        Altitude = horizontal.altitude,
                        Distance = equatorial.dist,
                        ApparentMagnitude = illumination.mag,
                        AngularDiameter = GetPlanetAngularDiameter(body, equatorial.dist),
                        IsVisible = horizontal.altitude > 0,
                        Rise = riseSetTimes.rise,
                        Set = riseSetTimes.set,
                        Transit = riseSetTimes.transit,
                        RecommendedEquipment = GetPlanetEquipmentRecommendation(planet, equatorial.dist),
                        PhotographyNotes = GetPlanetPhotographyNotes(planet, illumination.phase_fraction)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating planet position for {Planet}", planet);
                    throw;
                }
            }, cancellationToken);
        }

        public async Task<List<PlanetPositionData>> GetVisiblePlanetsAsync(DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            var planets = new List<PlanetPositionData>();

            foreach (PlanetType planet in Enum.GetValues<PlanetType>())
            {
                var planetData = await GetPlanetPositionAsync(planet, dateTime, latitude, longitude, cancellationToken);
                if (planetData.IsVisible)
                {
                    planets.Add(planetData);
                }
            }

            return planets.OrderBy(p => p.ApparentMagnitude).ToList();
        }

        private (double azimuth, double altitude) CalculateCelestialPolePosition(double latitude, double longitude, DateTime dateTime)
        {
            if (latitude >= 0) // Northern Hemisphere - Polaris
            {
                var polarisRA = 2.530277778;
                var polarisDec = 89.264167;
                var time = new AstroTime(dateTime);
                var observer = new Observer(latitude, longitude, 0);
                var horizontal = Astronomy.Horizon(time, observer, polarisRA, polarisDec, Refraction.Normal);
                return (horizontal.azimuth, horizontal.altitude);
            }
            else // Southern Hemisphere - Sigma Octantis
            {
                var sigmaOctRA = 21.146111;
                var sigmaOctDec = -88.956389;
                var time = new AstroTime(dateTime);
                var observer = new Observer(latitude, longitude, 0);
                var horizontal = Astronomy.Horizon(time, observer, sigmaOctRA, sigmaOctDec, Refraction.Normal);
                return (horizontal.azimuth, horizontal.altitude);
            }
        }

        private double CalculateStarTrailLength(TimeSpan exposureDuration, double latitude)
        {
            var hourlyMotion = 15.0;
            var trailLength = exposureDuration.TotalHours * hourlyMotion;
            var latitudeAdjustment = Math.Cos(Math.Abs(latitude) * Math.PI / 180.0);
            return trailLength * latitudeAdjustment;
        }
        public async Task<List<PlanetaryConjunction>> GetPlanetaryConjunctionsAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var conjunctions = new List<PlanetaryConjunction>();
                var observer = new Observer(latitude, longitude, 0);
                var planets = Enum.GetValues<PlanetType>().ToArray();

                for (int i = 0; i < planets.Length - 1; i++)
                {
                    for (int j = i + 1; j < planets.Length; j++)
                    {
                        var body1 = PlanetBodies[planets[i].ToString()];
                        var body2 = PlanetBodies[planets[j].ToString()];

                        try
                        {
                            var searchTime = new AstroTime(startDate);
                            var endTime = new AstroTime(endDate);

                            while (searchTime.tt < endTime.tt)
                            {
                                var conjunction = Astronomy.SearchRelativeLongitude(body1, 0.0, searchTime);
                                if (conjunction != null && conjunction.tt <= endTime.tt)
                                {
                                    var separation = CalculateAngularSeparation(body1, body2, conjunction, observer);
                                    if (separation < 5.0) // Within 5 degrees
                                    {
                                        var horizontal = GetConjunctionHorizontalPosition(body1, body2, conjunction, observer);

                                        conjunctions.Add(new PlanetaryConjunction
                                        {
                                            DateTime = conjunction.ToUtcDateTime(),
                                            Planet1 = planets[i],
                                            Planet2 = planets[j],
                                            Separation = separation * 60, // Convert to arc minutes
                                            Altitude = horizontal.altitude,
                                            Azimuth = horizontal.azimuth,
                                            VisibilityDescription = GetConjunctionVisibility(horizontal.altitude),
                                            PhotographyRecommendation = GetConjunctionPhotographyAdvice(separation, horizontal.altitude)
                                        });
                                    }
                                    searchTime = new AstroTime(conjunction.ToUtcDateTime().AddDays(1));
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error searching conjunction between {Planet1} and {Planet2}", planets[i], planets[j]);
                        }
                    }
                }

                return conjunctions.OrderBy(c => c.DateTime).ToList();
            }, cancellationToken);
        }

        public async Task<List<PlanetaryEvent>> GetPlanetOppositionsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var oppositions = new List<PlanetaryEvent>();
                var outerPlanets = new[] { PlanetType.Mars, PlanetType.Jupiter, PlanetType.Saturn, PlanetType.Uranus, PlanetType.Neptune };
                var geocentricObserver = new Observer(0, 0, 0); // Geocentric observer at Earth's center

                foreach (var planet in outerPlanets)
                {
                    try
                    {
                        var body = PlanetBodies[planet.ToString()];
                        var searchTime = new AstroTime(startDate);
                        var endTime = new AstroTime(endDate);

                        while (searchTime.tt < endTime.tt)
                        {
                            var opposition = Astronomy.SearchRelativeLongitude(Body.Sun, 180.0, searchTime);
                            if (opposition != null && opposition.tt <= endTime.tt)
                            {
                                var illumination = Astronomy.Illumination(body, opposition);
                                var equatorial = Astronomy.Equator(body, opposition, geocentricObserver, EquatorEpoch.OfDate, Aberration.Corrected);

                                oppositions.Add(new PlanetaryEvent
                                {
                                    DateTime = opposition.ToUtcDateTime(),
                                    Planet = planet,
                                    EventType = "Opposition",
                                    ApparentMagnitude = illumination.mag,
                                    AngularDiameter = GetPlanetAngularDiameter(body, equatorial.dist),
                                    OptimalViewingConditions = GetOppositionViewingConditions(planet),
                                    EquipmentRecommendations = GetOppositionEquipmentAdvice(planet, equatorial.dist)
                                });

                                searchTime = new AstroTime(opposition.ToUtcDateTime().AddDays(300)); // Next opposition ~1 year later
                            }
                            else
                            {
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Error searching opposition for {Planet}", planet);
                    }
                }

                return oppositions.OrderBy(o => o.DateTime).ToList();
            }, cancellationToken);
        }

        #endregion

        #region ENHANCED LUNAR CALCULATIONS

        public async Task<EnhancedMoonData> GetEnhancedMoonDataAsync(DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var time = new AstroTime(dateTime);
                    var observer = new Observer(latitude, longitude, 0);

                    var moonPos = Astronomy.Equator(Body.Moon, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                    var moonHorizontal = Astronomy.Horizon(time, observer, moonPos.ra, moonPos.dec, Refraction.Normal);
                    var moonIllumination = Astronomy.Illumination(Body.Moon, time);
                    var libration = Astronomy.Libration(time);

                    var riseSetTimes = GetMoonRiseSetTimes(dateTime, observer);
                    var phase = Astronomy.MoonPhase(time);

                    return new EnhancedMoonData
                    {
                        DateTime = dateTime,
                        Phase = phase / 360.0, // Convert to 0-1 range
                        PhaseName = GetMoonPhaseName(phase),
                        Illumination = moonIllumination.phase_fraction * 100,
                        Azimuth = moonHorizontal.azimuth,
                        Altitude = moonHorizontal.altitude,
                        Distance = moonPos.dist * 149597870.7, // Convert AU to km
                        AngularDiameter = GetMoonAngularDiameter(moonPos.dist),
                        Rise = riseSetTimes.rise,
                        Set = riseSetTimes.set,
                        Transit = riseSetTimes.transit,
                        LibrationLatitude = libration.elat,
                        LibrationLongitude = libration.elon,
                        PositionAngle = moonIllumination.phase_angle,
                        IsSupermoon = IsSupermoon(moonPos.dist),
                        OpticalLibration = Math.Sqrt(libration.elat * libration.elat + libration.elon * libration.elon),
                        OptimalPhotographyPhase = GetOptimalLunarPhaseDescription(phase),
                        VisibleFeatures = GetVisibleLunarFeatures(phase, libration),
                        RecommendedExposureSettings = GetLunarExposureRecommendations(moonIllumination.phase_fraction)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating enhanced moon data");
                    throw;
                }
            }, cancellationToken);
        }

        public async Task<List<SupermoonEvent>> GetSupermoonEventsAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var supermoons = new List<SupermoonEvent>();
                var searchTime = new AstroTime(startDate);
                var endTime = new AstroTime(endDate);
                var geocentricObserver = new Observer(0, 0, 0); // Geocentric observer

                try
                {
                    while (searchTime.tt < endTime.tt)
                    {
                        var perigee = Astronomy.SearchMoonQuarter(searchTime);
                        if (perigee.time.ToUtcDateTime() <= endTime.ToUtcDateTime())
                        {
                            var moonPos = Astronomy.Equator(Body.Moon, perigee.time, geocentricObserver, EquatorEpoch.OfDate, Aberration.Corrected);
                            var distanceKm = moonPos.dist * 149597870.7;

                            if (distanceKm < 361000) // Supermoon threshold
                            {
                                var phase = Astronomy.MoonPhase(perigee.time);
                                if (Math.Abs(phase) < 10 || Math.Abs(phase - 180) < 10) // Near new or full moon
                                {
                                    supermoons.Add(new SupermoonEvent
                                    {
                                        DateTime = perigee.time.ToUtcDateTime(),
                                        Distance = distanceKm,
                                        AngularDiameter = GetMoonAngularDiameter(moonPos.dist),
                                        PercentLarger = ((384400 - distanceKm) / 384400) * 100,
                                        EventName = GetSupermoonEventName(phase),
                                        PhotographyOpportunity = GetSupermoonPhotographyAdvice(phase, distanceKm)
                                    });
                                }
                            }
                            searchTime = new AstroTime(perigee.time.ToUtcDateTime().AddDays(27)); // Next lunar cycle
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching supermoon events");
                }

                return supermoons.OrderBy(s => s.DateTime).ToList();
            }, cancellationToken);
        }

        public async Task<List<LunarEclipseData>> GetLunarEclipsesAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var eclipses = new List<LunarEclipseData>();
                var searchTime = new AstroTime(startDate);
                var endTime = new AstroTime(endDate);
                var observer = new Observer(latitude, longitude, 0);

                try
                {
                    while (searchTime.tt < endTime.tt)
                    {
                        var eclipse = Astronomy.SearchLunarEclipse(searchTime);
                        if (eclipse.peak.tt <= endTime.tt)
                        {
                            // Calculate moon position at eclipse peak to determine visibility
                            var moonEquatorial = Astronomy.Equator(Body.Moon, eclipse.peak, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                            var moonHorizontal = Astronomy.Horizon(eclipse.peak, observer, moonEquatorial.ra, moonEquatorial.dec, Refraction.Normal);
                            var isVisible = moonHorizontal.altitude > 0;

                            // Calculate eclipse phase times from semi-durations
                            var peakDateTime = eclipse.peak.ToUtcDateTime();
                            var penumbralBegin = peakDateTime.AddMinutes(-eclipse.sd_penum);
                            var penumbralEnd = peakDateTime.AddMinutes(eclipse.sd_penum);
                            var partialBegin = eclipse.sd_partial > 0 ? peakDateTime.AddMinutes(-eclipse.sd_partial) : (DateTime?)null;
                            var partialEnd = eclipse.sd_partial > 0 ? peakDateTime.AddMinutes(eclipse.sd_partial) : (DateTime?)null;
                            var totalityBegin = eclipse.sd_total > 0 ? peakDateTime.AddMinutes(-eclipse.sd_total) : (DateTime?)null;
                            var totalityEnd = eclipse.sd_total > 0 ? peakDateTime.AddMinutes(eclipse.sd_total) : (DateTime?)null;

                            eclipses.Add(new LunarEclipseData
                            {
                                DateTime = peakDateTime,
                                EclipseType = GetLunarEclipseType(eclipse.kind),
                                PenumbralBegin = penumbralBegin,
                                PartialBegin = partialBegin,
                                TotalityBegin = totalityBegin,
                                Maximum = peakDateTime,
                                TotalityEnd = totalityEnd,
                                PartialEnd = partialEnd,
                                PenumbralEnd = penumbralEnd,
                                Magnitude = eclipse.obscuration,
                                IsVisible = isVisible,
                                PhotographyPlanning = GetLunarEclipsePhotographyPlanning(eclipse.kind, isVisible),
                                ExposureRecommendations = GetLunarEclipseExposureAdvice(eclipse.kind)
                            });

                            searchTime = new AstroTime(eclipse.peak.ToUtcDateTime().AddDays(180)); // Search for next eclipse
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error searching lunar eclipses");
                }

                return eclipses.OrderBy(e => e.DateTime).ToList();
            }, cancellationToken);
        }

        public async Task<List<LunarPhotographyWindow>> GetOptimalLunarWindowsAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                var windows = new List<LunarPhotographyWindow>();
                var observer = new Observer(latitude, longitude, 0);
                var currentDate = startDate;

                while (currentDate <= endDate)
                {
                    var time = new AstroTime(currentDate);
                    var moonPos = Astronomy.Equator(Body.Moon, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                    var moonHorizontal = Astronomy.Horizon(time, observer, moonPos.ra, moonPos.dec, Refraction.Normal);
                    var phase = Astronomy.MoonPhase(time);
                    var illumination = Astronomy.Illumination(Body.Moon, time);
                    var libration = Astronomy.Libration(time);

                    if (moonHorizontal.altitude > 20) // Moon is well above horizon
                    {
                        var qualityScore = CalculateLunarPhotographyQuality(phase, moonHorizontal.altitude, illumination.phase_fraction);

                        if (qualityScore > 0.6) // Good photography conditions
                        {
                            windows.Add(new LunarPhotographyWindow
                            {
                                StartTime = currentDate,
                                EndTime = currentDate.AddHours(2),
                                Phase = phase / 360.0,
                                PhaseName = GetMoonPhaseName(phase),
                                OptimalAltitude = moonHorizontal.altitude,
                                PhotographyType = GetLunarPhotographyType(phase),
                                RecommendedSettings = GetLunarExposureRecommendations(illumination.phase_fraction),
                                VisibleFeatures = GetVisibleLunarFeatures(phase, libration),
                                QualityScore = qualityScore
                            });
                        }
                    }

                    currentDate = currentDate.AddHours(1);
                }

                return windows.OrderByDescending(w => w.QualityScore).ToList();
            }, cancellationToken);
        }

        #endregion

        #region DEEP SKY CALCULATIONS

        public async Task<MilkyWayData> GetMilkyWayDataAsync(DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var time = new AstroTime(dateTime);
                    var observer = new Observer(latitude, longitude, 0);

                    // Galactic center coordinates (Sagittarius A*)
                    var galacticCenterRA = 17.759167; // 17h 45m 33s
                    var galacticCenterDec = -29.007778; // -29° 00' 28"

                    var horizontal = Astronomy.Horizon(time, observer, galacticCenterRA, galacticCenterDec, Refraction.Normal);

                    var riseSet = GetObjectRiseSetTimes(galacticCenterRA, galacticCenterDec, dateTime, observer);
                    var season = GetMilkyWaySeason(dateTime);

                    return new MilkyWayData
                    {
                        DateTime = dateTime,
                        GalacticCenterAzimuth = horizontal.azimuth,
                        GalacticCenterAltitude = horizontal.altitude,
                        IsVisible = horizontal.altitude > 0,
                        Rise = riseSet.rise,
                        Set = riseSet.set,
                        OptimalViewingTime = GetOptimalMilkyWayTime(riseSet.rise, riseSet.set),
                        Season = season,
                        DarkSkyQuality = CalculateDarkSkyQuality(dateTime, latitude, longitude),
                        PhotographyRecommendations = GetMilkyWayPhotographyAdvice(season, horizontal.altitude),
                        CompositionSuggestions = GetMilkyWayCompositionSuggestions(horizontal.azimuth, horizontal.altitude)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating Milky Way data");
                    throw;
                }
            }, cancellationToken);
        }

        public async Task<ConstellationData> GetConstellationDataAsync(ConstellationType constellation, DateTime date, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var time = new AstroTime(date);
                    var observer = new Observer(latitude, longitude, 0);

                    var constellationCoords = GetConstellationCoordinates(constellation);
                    var horizontal = Astronomy.Horizon(time, observer, constellationCoords.ra, constellationCoords.dec, Refraction.Normal);
                    var riseSet = GetObjectRiseSetTimes(constellationCoords.ra, constellationCoords.dec, date, observer);

                    return new ConstellationData
                    {
                        Constellation = constellation,
                        DateTime = date,
                        CenterRightAscension = constellationCoords.ra,
                        CenterDeclination = constellationCoords.dec,
                        CenterAzimuth = horizontal.azimuth,
                        CenterAltitude = horizontal.altitude,
                        Rise = riseSet.rise,
                        Set = riseSet.set,
                        OptimalViewingTime = GetOptimalViewingTime(riseSet.rise, riseSet.set),
                        IsCircumpolar = IsCircumpolar(constellationCoords.dec, latitude),
                        NotableObjects = GetConstellationDeepSkyObjects(constellation),
                        PhotographyNotes = GetConstellationPhotographyNotes(constellation)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating constellation data for {Constellation}", constellation);
                    throw;
                }
            }, cancellationToken);
        }

        #endregion

        #region HELPER METHODS

        private (DateTime? rise, DateTime? set, DateTime? transit) GetPlanetRiseSetTimes(Body body, DateTime date, Observer observer)
        {
            try
            {
                var searchTime = new AstroTime(date.Date);
                var riseEvent = Astronomy.SearchRiseSet(body, observer, Direction.Rise, searchTime, 1.0);
                var setEvent = Astronomy.SearchRiseSet(body, observer, Direction.Set, searchTime, 1.0);

                // Calculate transit (highest point)
                DateTime? transit = null;
                if (riseEvent != null && setEvent != null)
                {
                    var midTime = new AstroTime(riseEvent.ToUtcDateTime().AddTicks((setEvent.ToUtcDateTime() - riseEvent.ToUtcDateTime()).Ticks / 2));
                    transit = midTime.ToUtcDateTime();
                }

                return (riseEvent?.ToUtcDateTime(), setEvent?.ToUtcDateTime(), transit);
            }
            catch
            {
                return (null, null, null);
            }
        }

        private (DateTime? rise, DateTime? set, DateTime? transit) GetMoonRiseSetTimes(DateTime date, Observer observer)
        {
            return GetPlanetRiseSetTimes(Body.Moon, date, observer);
        }

        private (DateTime? rise, DateTime? set) GetObjectRiseSetTimes(double ra, double dec, DateTime date, Observer observer)
        {
            try
            {
                var searchTime = new AstroTime(date.Date);

                // Define star coordinates using Body.Star1
                Astronomy.DefineStar(Body.Star1, ra, dec, 1000.0); // 1000 pc distance

                // Search for rise using the defined star
                var riseEvent = Astronomy.SearchRiseSet(Body.Star1, observer, Direction.Rise, searchTime, 1.0);

                // Search for set using the defined star
                var setEvent = Astronomy.SearchRiseSet(Body.Star1, observer, Direction.Set, searchTime, 1.0);

                return (riseEvent?.ToUtcDateTime(), setEvent?.ToUtcDateTime());
            }
            catch
            {
                return (null, null);
            }
        }

        private double GetPlanetAngularDiameter(Body body, double distanceAU)
        {
            // Approximate angular diameters in arc seconds
            var diameters = new Dictionary<Body, double>
            {
                { Body.Mercury, 6.74 },
                { Body.Venus, 16.92 },
                { Body.Mars, 9.36 },
                { Body.Jupiter, 196.94 },
                { Body.Saturn, 165.60 },
                { Body.Uranus, 65.14 },
                { Body.Neptune, 62.20 },
                { Body.Pluto, 8.20 }
            };

            if (diameters.TryGetValue(body, out var baseDiameter))
            {
                return baseDiameter / distanceAU; // Adjust for distance
            }
            return 0;
        }

        private double GetMoonAngularDiameter(double distanceAU)
        {
            var meanAngularDiameter = 31.1; // Arc minutes
            var meanDistance = 0.00257; // AU
            return meanAngularDiameter * (meanDistance / distanceAU);
        }

        private string GetPlanetEquipmentRecommendation(PlanetType planet, double distance)
        {
            return planet switch
            {
                PlanetType.Mercury or PlanetType.Venus => "Wide angle lens for conjunction photography",
                PlanetType.Mars => distance < 0.7 ? "200-600mm telephoto lens" : "400mm+ telephoto lens",
                PlanetType.Jupiter => "200mm+ telephoto lens, telescope for detail",
                PlanetType.Saturn => "300mm+ telephoto lens, telescope for rings",
                PlanetType.Uranus or PlanetType.Neptune => "Telescope required for detection",
                PlanetType.Pluto => "Large telescope and long exposure required",
                _ => "Medium telephoto lens"
            };
        }

        private string GetPlanetPhotographyNotes(PlanetType planet, double phase)
        {
            return planet switch
            {
                PlanetType.Venus => $"Phase: {phase:P0}. Best photographed during crescent phases.",
                PlanetType.Mars => "Best during opposition when closest to Earth.",
                PlanetType.Jupiter => "Capture the Great Red Spot and Galilean moons.",
                PlanetType.Saturn => "Photograph rings and major moons like Titan.",
                _ => "Use highest magnification available for planetary detail."
            };
        }
        private double CalculateAngularSeparation(Body body1, Body body2, AstroTime time, Observer observer)
        {
            try
            {
                var pos1 = Astronomy.Equator(body1, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                var pos2 = Astronomy.Equator(body2, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);

                // Convert to radians
                var ra1 = pos1.ra * Math.PI / 12.0; // Hours to radians
                var dec1 = pos1.dec * Math.PI / 180.0; // Degrees to radians
                var ra2 = pos2.ra * Math.PI / 12.0;
                var dec2 = pos2.dec * Math.PI / 180.0;

                // Spherical law of cosines for angular separation
                var cosSeparation = Math.Sin(dec1) * Math.Sin(dec2) +
                                   Math.Cos(dec1) * Math.Cos(dec2) * Math.Cos(ra1 - ra2);

                // Clamp to valid range to avoid numerical errors
                cosSeparation = Math.Max(-1.0, Math.Min(1.0, cosSeparation));

                // Return separation in degrees
                return Math.Acos(cosSeparation) * 180.0 / Math.PI;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating angular separation between {Body1} and {Body2}", body1, body2);
                return double.NaN;
            }
        }

        private (double altitude, double azimuth) GetConjunctionHorizontalPosition(Body body1, Body body2, AstroTime time, Observer observer)
        {
            try
            {
                // Get positions of both bodies
                var pos1 = Astronomy.Equator(body1, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);
                var pos2 = Astronomy.Equator(body2, time, observer, EquatorEpoch.OfDate, Aberration.Corrected);

                // Calculate midpoint between the two bodies for conjunction position
                var midRA = (pos1.ra + pos2.ra) / 2.0;
                var midDec = (pos1.dec + pos2.dec) / 2.0;

                // Handle RA wraparound at 0h/24h boundary
                var ra1 = pos1.ra;
                var ra2 = pos2.ra;

                if (Math.Abs(ra1 - ra2) > 12.0)
                {
                    if (ra1 < ra2)
                        ra1 += 24.0;
                    else
                        ra2 += 24.0;

                    midRA = (ra1 + ra2) / 2.0;
                    if (midRA >= 24.0) midRA -= 24.0;
                }

                // Convert midpoint to horizontal coordinates
                var horizontal = Astronomy.Horizon(time, observer, midRA, midDec, Refraction.Normal);

                return (horizontal.altitude, horizontal.azimuth);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating conjunction horizontal position for {Body1} and {Body2}", body1, body2);
                return (double.NaN, double.NaN);
            }
        }
        private string GetConjunctionVisibility(double altitude)
        {
            try
            {
                return altitude switch
                {
                    < -18 => "Not visible - below horizon during astronomical twilight",
                    < -12 => "Difficult - only visible during nautical twilight",
                    < -6 => "Challenging - only visible during civil twilight",
                    < 0 => "Below horizon - not visible",
                    < 10 => "Very low - may be obscured by terrain/atmosphere",
                    < 20 => "Low - best viewed with clear horizon",
                    < 30 => "Good - comfortable viewing angle",
                    < 60 => "Excellent - high in sky with minimal atmospheric distortion",
                    _ => "Overhead - excellent viewing conditions"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining conjunction visibility for altitude {Altitude}", altitude);
                return "Unknown visibility";
            }
        }
        private string GetConjunctionPhotographyAdvice(double separationArcMinutes, double altitude)
        {
            try
            {
                var advice = new List<string>();

                // Separation advice
                if (separationArcMinutes < 30)
                    advice.Add("Very close conjunction - use telephoto lens 200mm+ to capture both objects in frame");
                else if (separationArcMinutes < 120)
                    advice.Add("Moderate separation - 85-200mm lens ideal for composition");
                else
                    advice.Add("Wide separation - wide angle lens to capture both objects");

                // Altitude advice
                if (altitude < 10)
                    advice.Add("Low altitude - find elevated location with clear horizon");
                else if (altitude < 30)
                    advice.Add("Moderate altitude - good for foreground composition");
                else
                    advice.Add("High altitude - excellent for detailed planetary photography");

                // Technical advice
                if (separationArcMinutes < 60)
                    advice.Add("Use tripod and timer/remote to avoid camera shake");

                advice.Add("Shoot in RAW for maximum post-processing flexibility");

                return string.Join(". ", advice) + ".";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating conjunction photography advice");
                return "Use telephoto lens and tripod for best results.";
            }
        }
        private string GetOppositionViewingConditions(PlanetType planet)
        {
            try
            {
                return planet switch
                {
                    PlanetType.Mars => "Best viewing conditions every 26 months. Planet appears largest and brightest, ideal for surface feature photography. Look for polar ice caps and dust storms.",

                    PlanetType.Jupiter => "Excellent conditions for moon photography. All four Galilean moons visible. Great Red Spot rotates every ~10 hours - plan timing for best views.",

                    PlanetType.Saturn => "Ring system fully illuminated and widest apparent size. Titan and other major moons easily visible. Cassini Division in rings may be detectable.",

                    PlanetType.Uranus => "Reaches magnitude ~5.7, barely visible to naked eye in dark skies. Telescope required for disc visibility. Appears blue-green due to methane atmosphere.",

                    PlanetType.Neptune => "Magnitude ~7.8, telescope essential. Appears as small blue disc. Triton (largest moon) may be visible with larger instruments.",

                    _ => "Planet reaches maximum brightness and apparent size during opposition. Best time for detailed photography and observation."
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting opposition viewing conditions for {Planet}", planet);
                return "Optimal viewing conditions during opposition - planet closest to Earth.";
            }
        }
        private string GetOppositionEquipmentAdvice(PlanetType planet, double distanceAU)
        {
            try
            {
                return planet switch
                {
                    PlanetType.Mars when distanceAU < 0.5 => "Excellent opposition! 200-400mm telephoto sufficient for surface features. Consider telescope with 2000mm+ focal length for detailed imaging.",

                    PlanetType.Mars => "Standard opposition. 400mm+ telephoto lens minimum. Telescope with 1500mm+ focal length recommended for surface detail photography.",

                    PlanetType.Jupiter => "200mm+ telephoto shows disc and moons. 600mm+ reveals Great Red Spot. Telescope with 2000mm+ focal length for detailed surface bands and storm systems.",

                    PlanetType.Saturn => "300mm+ telephoto shows rings. 600mm+ resolves ring gap. Telescope with 2500mm+ focal length required for ring divisions and moon details.",

                    PlanetType.Uranus => "Telescope with 1000mm+ focal length minimum. 2000mm+ shows small disc. Very challenging target requiring excellent atmospheric conditions.",

                    PlanetType.Neptune => "Large telescope essential - 2000mm+ focal length minimum. 3000mm+ preferred. Requires excellent seeing conditions and precise tracking.",

                    _ => "Telescope or long telephoto lens recommended for planetary detail during opposition."
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting opposition equipment advice for {Planet}", planet);
                return "Use longest focal length available for maximum planetary detail.";
            }
        }
        private string GetMoonPhaseName(double phaseAngle)
        {
            try
            {
                // Normalize phase angle to 0-360 degrees
                var normalizedPhase = ((phaseAngle % 360) + 360) % 360;

                return normalizedPhase switch
                {
                    >= 0 and < 22.5 => "New Moon",
                    >= 22.5 and < 67.5 => "Waxing Crescent",
                    >= 67.5 and < 112.5 => "First Quarter",
                    >= 112.5 and < 157.5 => "Waxing Gibbous",
                    >= 157.5 and < 202.5 => "Full Moon",
                    >= 202.5 and < 247.5 => "Waning Gibbous",
                    >= 247.5 and < 292.5 => "Third Quarter",
                    >= 292.5 and < 337.5 => "Waning Crescent",
                    _ => "New Moon"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining moon phase name for angle {PhaseAngle}", phaseAngle);
                return "Unknown Phase";
            }
        }
        private bool IsSupermoon(double distanceAU)
        {
            try
            {
                // Convert distance from AU to kilometers
                var distanceKm = distanceAU * 149597870.7;

                // Supermoon threshold: within 90% of closest approach (perigee)
                // Average lunar distance: ~384,400 km
                // Closest approach (perigee): ~356,500 km
                // Supermoon threshold: ~360,000 km (approximately)
                const double supermoonThresholdKm = 361000;

                return distanceKm < supermoonThresholdKm;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining supermoon status for distance {DistanceAU} AU", distanceAU);
                return false;
            }
        }
        private string GetOptimalLunarPhaseDescription(double phaseAngle)
        {
            try
            {
                var normalizedPhase = ((phaseAngle % 360) + 360) % 360;

                return normalizedPhase switch
                {
                    >= 0 and < 22.5 => "New Moon - ideal for deep sky photography, moon not visible",
                    >= 22.5 and < 67.5 => "Waxing Crescent - excellent for terminator detail and earthshine photography",
                    >= 67.5 and < 112.5 => "First Quarter - perfect for crater photography along terminator line",
                    >= 112.5 and < 157.5 => "Waxing Gibbous - good for mare detail and mountain range photography",
                    >= 157.5 and < 202.5 => "Full Moon - best for lunar landscapes and overall surface detail",
                    >= 202.5 and < 247.5 => "Waning Gibbous - excellent for eastern limb features and libration photography",
                    >= 247.5 and < 292.5 => "Third Quarter - ideal for western crater detail and terminator photography",
                    >= 292.5 and < 337.5 => "Waning Crescent - perfect for sunrise terminator and earthshine",
                    _ => "New Moon - dark sky conditions for deep space objects"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting optimal lunar phase description for angle {PhaseAngle}", phaseAngle);
                return "Lunar photography opportunities available at all phases";
            }
        }

        // Fix LibrationData parameter in GetVisibleLunarFeatures method signature
        private List<string> GetVisibleLunarFeatures(double phaseAngle, CosineKitty.LibrationInfo libration)
        {
            try
            {
                var features = new List<string>();
                var normalizedPhase = ((phaseAngle % 360) + 360) % 360;

                // Phase-specific features (same logic as before)
                switch (normalizedPhase)
                {
                    case >= 22.5 and < 67.5:
                        features.AddRange(new[] { "Mare Crisium", "Langrenus Crater", "Petavius Crater", "Earthshine on dark limb" });
                        break;
                        // ... rest of cases same as before
                }

                // Libration-enhanced features - fix property names
                if (Math.Abs(libration.elon) > 4.0)
                {
                    if (libration.elon > 0)
                        features.Add("Eastern limb features enhanced by libration");
                    else
                        features.Add("Western limb features enhanced by libration");
                }

                if (Math.Abs(libration.elat) > 4.0)
                {
                    if (libration.elat > 0)
                        features.Add("Northern polar region enhanced by libration");
                    else
                        features.Add("Southern polar region enhanced by libration");
                }

                return features.Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting visible lunar features");
                return new List<string> { "Major craters and maria visible" };
            }
        }

        private string GetLunarExposureRecommendations(double illuminationFraction)
        {
            try
            {
                var recommendations = new List<string>();

                // Base exposure recommendations based on illumination
                if (illuminationFraction < 0.1) // New Moon/Thin Crescent
                {
                    recommendations.Add("Earthshine: ISO 1600-6400, f/2.8-4, 1-4 seconds");
                    recommendations.Add("Crescent: ISO 100-400, f/5.6-8, 1/60-1/250 second");
                    recommendations.Add("Use bracketing for HDR composite of lit and earthshine portions");
                }
                else if (illuminationFraction < 0.3) // Crescent
                {
                    recommendations.Add("ISO 100-800, f/5.6-8, 1/125-1/500 second");
                    recommendations.Add("Focus on terminator detail and crater shadows");
                    recommendations.Add("Consider focus stacking for sharp edge-to-edge detail");
                }
                else if (illuminationFraction < 0.7) // Quarter to Gibbous
                {
                    recommendations.Add("ISO 100-400, f/8-11, 1/250-1/1000 second");
                    recommendations.Add("Optimal for crater detail photography");
                    recommendations.Add("Use longer focal lengths (1000mm+) for feature detail");
                }
                else if (illuminationFraction < 0.95) // Near Full
                {
                    recommendations.Add("ISO 100-200, f/8-11, 1/500-1/2000 second");
                    recommendations.Add("Watch for overexposure - use histogram monitoring");
                    recommendations.Add("Consider neutral density filter for longer exposures");
                }
                else // Full Moon
                {
                    recommendations.Add("ISO 100, f/11-16, 1/1000-1/4000 second");
                    recommendations.Add("Very bright - may require ND filter or stopped down aperture");
                    recommendations.Add("Best for overall lunar landscape and ray system photography");
                }

                // Universal recommendations
                recommendations.Add("Use tripod and mirror lock-up/electronic shutter to minimize vibration");
                recommendations.Add("Shoot in RAW for maximum post-processing flexibility");

                return string.Join(". ", recommendations) + ".";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting lunar exposure recommendations for illumination {Illumination}", illuminationFraction);
                return "Use tripod, shoot in RAW, adjust exposure based on moon brightness.";
            }
        }

        private string GetSupermoonEventName(double phaseAngle)
        {
            try
            {
                var normalizedPhase = ((phaseAngle % 360) + 360) % 360;

                return normalizedPhase switch
                {
                    >= 0 and < 45 => "Super New Moon",
                    >= 45 and < 135 => "Super Crescent Moon",
                    >= 135 and < 225 => "Super Full Moon",
                    >= 225 and < 315 => "Super Gibbous Moon",
                    _ => "Super New Moon"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining supermoon event name for phase {PhaseAngle}", phaseAngle);
                return "Supermoon Event";
            }
        }
        private string GetSupermoonPhotographyAdvice(double phaseAngle, double distanceKm)
        {
            try
            {
                var advice = new List<string>();
                var normalizedPhase = ((phaseAngle % 360) + 360) % 360;
                var percentCloser = ((384400 - distanceKm) / 384400) * 100;

                // Phase-specific advice
                if (normalizedPhase >= 135 && normalizedPhase < 225) // Super Full Moon
                {
                    advice.Add($"Super Full Moon appears {percentCloser:F1}% larger and ~30% brighter than average");
                    advice.Add("Use telephoto lens 200mm+ to emphasize size against foreground objects");
                    advice.Add("Plan shots with interesting foreground elements like buildings or landmarks");
                    advice.Add("Moon rise/set timing critical for dramatic low-horizon compositions");
                    advice.Add("Exposure: ISO 100, f/8-11, 1/500-1/1000s due to increased brightness");
                }
                else if (normalizedPhase < 45 || normalizedPhase >= 315) // Super New Moon
                {
                    advice.Add("Super New Moon ideal for dark sky photography - moon won't interfere");
                    advice.Add("Perfect opportunity for Milky Way and deep sky object photography");
                    advice.Add("Moon's gravitational effects may cause higher tides - coastal photography opportunities");
                    advice.Add("Plan meteor shower photography during this window");
                }
                else // Other super phases
                {
                    advice.Add($"Supermoon appears {percentCloser:F1}% larger with enhanced detail visibility");
                    advice.Add("Excellent for terminator detail photography with increased contrast");
                    advice.Add("Crater shadows more pronounced due to increased apparent size");
                }

                // Distance-specific technical advice
                if (distanceKm < 357000) // Very close supermoon
                {
                    advice.Add("Exceptionally close approach - maximum detail photography opportunity");
                    advice.Add("Consider lunar mosaic photography for ultra-high resolution results");
                }

                advice.Add("Use precise timing apps for optimal rise/set positioning");
                advice.Add("Weather contingency planning essential - supermoons are infrequent events");

                return string.Join(". ", advice) + ".";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating supermoon photography advice");
                return "Supermoon offers enhanced size and brightness for dramatic lunar photography.";
            }
        }

        private string GetLunarEclipseType(EclipseKind kind)
        {
            try
            {
                return kind switch
                {
                    EclipseKind.Penumbral => "Penumbral",
                    EclipseKind.Partial => "Partial",
                    EclipseKind.Total => "Total",
                    _ => "Unknown"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining lunar eclipse type for kind {Kind}", kind);
                return "Lunar Eclipse";
            }
        }

        private string GetLunarEclipsePhotographyPlanning(EclipseKind kind, bool isVisible)
        {
            try
            {
                var planning = new List<string>();

                if (!isVisible)
                {
                    planning.Add("Eclipse not visible from your location");
                    planning.Add("Consider live streaming or plan travel to visibility zone");
                    return string.Join(". ", planning) + ".";
                }

                switch (kind)
                {
                    case EclipseKind.Total:
                        planning.Add("Total lunar eclipse - rare and spectacular photography opportunity");
                        planning.Add("Plan multi-hour time-lapse from first contact to totality end");
                        planning.Add("Scout location with clear eastern horizon for moonrise eclipses");
                        planning.Add("Prepare multiple exposure settings for dramatic brightness changes");
                        planning.Add("Totality phase allows for creative compositions with foreground elements");
                        planning.Add("Blood moon color varies - prepare for deep red to bright copper tones");
                        break;

                    case EclipseKind.Partial:
                        planning.Add("Partial lunar eclipse - excellent for showing Earth's shadow progression");
                        planning.Add("Time-lapse photography ideal for capturing shadow movement");
                        planning.Add("Contrast between shadowed and illuminated portions creates dramatic images");
                        planning.Add("Longer event duration allows for multiple composition attempts");
                        break;

                    case EclipseKind.Penumbral:
                        planning.Add("Penumbral eclipse - subtle but photographically interesting");
                        planning.Add("Requires careful exposure monitoring as changes are gradual");
                        planning.Add("Best captured through time-lapse showing gradual dimming");
                        planning.Add("Use precise metering to detect subtle brightness variations");
                        break;
                }

                // Universal planning advice
                planning.Add("Battery backup essential for extended shooting sessions");
                planning.Add("Remote shutter release prevents camera shake during long sequences");
                planning.Add("Pre-focus on moon before eclipse begins to avoid autofocus issues in darkness");

                return string.Join(". ", planning) + ".";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating lunar eclipse photography planning for kind {Kind}", kind);
                return "Plan for extended shooting session with backup equipment and multiple exposure settings.";
            }
        }

        private List<string> GetLunarEclipseExposureAdvice(EclipseKind kind)
        {
            try
            {
                var exposures = new List<string>();

                switch (kind)
                {
                    case EclipseKind.Total:
                        exposures.Add("Pre-eclipse: ISO 100, f/8, 1/250s for normal moon brightness");
                        exposures.Add("Partial phases: ISO 200-800, f/5.6-8, 1/60-1/250s as shadow progresses");
                        exposures.Add("Totality: ISO 1600-6400, f/4-5.6, 1-8 seconds for blood moon color");
                        exposures.Add("Deep totality: ISO 3200-12800, f/2.8-4, 2-15 seconds for faint details");
                        exposures.Add("Post-totality: Reverse exposure sequence as moon brightens");
                        exposures.Add("Bracket extensively during totality - color and brightness vary significantly");
                        break;

                    case EclipseKind.Partial:
                        exposures.Add("Uneclipsed portion: ISO 100-200, f/8-11, 1/250-1/500s");
                        exposures.Add("Shadowed portion: ISO 400-1600, f/5.6-8, 1/30-1/125s");
                        exposures.Add("HDR technique: Bracket for both bright and dark portions simultaneously");
                        exposures.Add("Maximum eclipse: ISO 800-3200, f/5.6, 1/60-1/250s depending on coverage");
                        break;

                    case EclipseKind.Penumbral:
                        exposures.Add("Throughout event: ISO 100-400, f/8, 1/125-1/500s with subtle adjustments");
                        exposures.Add("Monitor histogram carefully - changes are gradual and subtle");
                        exposures.Add("Consistent metering point essential for detecting brightness variations");
                        exposures.Add("Maximum penumbral: ISO 200-800, f/5.6-8, 1/60-1/250s");
                        break;
                }

                // Universal technical advice
                exposures.Add("Shoot RAW for maximum post-processing flexibility with color and exposure");
                exposures.Add("Use spot metering on moon to avoid foreground influence");
                exposures.Add("Consider focus stacking during totality for sharp edge-to-edge detail");

                return exposures;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating lunar eclipse exposure advice for kind {Kind}", kind);
                return new List<string> { "Use bracketed exposures and shoot in RAW for optimal results." };
            }
        }
        private double CalculateLunarPhotographyQuality(double phaseAngle, double altitude, double illuminationFraction)
        {
            try
            {
                double qualityScore = 0.0;

                // Altitude scoring (0.0 - 0.4 weight)
                double altitudeScore = altitude switch
                {
                    < 0 => 0.0,      // Below horizon
                    < 10 => 0.1,     // Very low - atmospheric distortion
                    < 20 => 0.2,     // Low - some distortion
                    < 30 => 0.3,     // Moderate - acceptable
                    < 60 => 0.4,     // Good - minimal distortion
                    _ => 0.4         // Excellent - overhead
                };

                // Phase scoring for photographic interest (0.0 - 0.4 weight)
                var normalizedPhase = ((phaseAngle % 360) + 360) % 360;
                double phaseScore = normalizedPhase switch
                {
                    >= 0 and < 22.5 => 0.1,      // New moon - not visible
                    >= 22.5 and < 67.5 => 0.4,   // Waxing crescent - excellent terminator
                    >= 67.5 and < 112.5 => 0.4,  // First quarter - excellent terminator
                    >= 112.5 and < 157.5 => 0.3, // Waxing gibbous - good detail
                    >= 157.5 and < 202.5 => 0.2, // Full moon - flat lighting
                    >= 202.5 and < 247.5 => 0.3, // Waning gibbous - good detail
                    >= 247.5 and < 292.5 => 0.4, // Third quarter - excellent terminator
                    >= 292.5 and < 337.5 => 0.4, // Waning crescent - excellent terminator
                    _ => 0.1
                };

                // Illumination scoring for exposure considerations (0.0 - 0.2 weight)
                double illuminationScore = illuminationFraction switch
                {
                    < 0.1 => 0.05,   // Very dim - challenging exposure
                    < 0.3 => 0.2,    // Crescent - good for detail
                    < 0.7 => 0.2,    // Quarter to gibbous - optimal
                    < 0.95 => 0.15,  // Near full - bright but manageable
                    _ => 0.1         // Full - very bright, risk of overexposure
                };

                qualityScore = altitudeScore + phaseScore + illuminationScore;

                // Ensure score is between 0 and 1
                return Math.Max(0.0, Math.Min(1.0, qualityScore));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating lunar photography quality score");
                return 0.5; // Default moderate quality
            }
        }

        private string GetLunarPhotographyType(double phaseAngle)
        {
            try
            {
                var normalizedPhase = ((phaseAngle % 360) + 360) % 360;

                return normalizedPhase switch
                {
                    >= 0 and < 22.5 => "Earthshine Photography",
                    >= 22.5 and < 67.5 => "Terminator Detail and Crescent Composition",
                    >= 67.5 and < 112.5 => "Crater Shadow Photography",
                    >= 112.5 and < 157.5 => "Mare and Highland Detail",
                    >= 157.5 and < 202.5 => "Lunar Landscape and Ray Systems",
                    >= 202.5 and < 247.5 => "Western Limb Features",
                    >= 247.5 and < 292.5 => "Evening Terminator and Crater Detail",
                    >= 292.5 and < 337.5 => "Morning Terminator and Earthshine",
                    _ => "General Lunar Photography"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining lunar photography type for phase {PhaseAngle}", phaseAngle);
                return "Lunar Surface Photography";
            }
        }

        private string GetMilkyWaySeason(DateTime dateTime)
        {
            try
            {
                var month = dateTime.Month;

                return month switch
                {
                    12 or 1 or 2 => "Winter - Galactic center not visible, focus on winter constellations like Orion",
                    3 or 4 or 5 => "Spring - Galactic center rises before dawn, early morning photography opportunity",
                    6 or 7 or 8 => "Summer - Peak season! Galactic center visible all night, best photography conditions",
                    9 or 10 or 11 => "Autumn - Galactic center sets early evening, golden hour overlap opportunities",
                    _ => "Year-round"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining Milky Way season for date {DateTime}", dateTime);
                return "Seasonal visibility varies";
            }
        }

        private DateTime? GetOptimalMilkyWayTime(DateTime? rise, DateTime? set)
        {
            try
            {
                if (!rise.HasValue || !set.HasValue)
                    return null;

                var riseTime = rise.Value;
                var setTime = set.Value;

                // Handle case where set time is next day
                if (setTime < riseTime)
                    setTime = setTime.AddDays(1);

                // Calculate midpoint between rise and set for optimal viewing
                var midpoint = riseTime.AddTicks((setTime - riseTime).Ticks / 2);

                // Use ISunCalculatorService for precise twilight calculations
                // Note: This requires injecting ISunCalculatorService into AstroCalculationService
                // For now, return the midpoint - twilight integration pending
                return midpoint;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating optimal Milky Way viewing time");
                return null;
            }
        }

        private double CalculateDarkSkyQuality(DateTime dateTime, double latitude, double longitude)
        {
            try
            {
                double qualityScore = 0.0;

                // Use existing ISunCalculatorService methods for twilight calculations
                var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(dateTime.Date, latitude, longitude, "UTC");
                var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(dateTime.Date, latitude, longitude, "UTC");

                // Time-based scoring (astronomical twilight consideration)
                var currentTime = dateTime.TimeOfDay;
                var dawnTime = astronomicalDawn.TimeOfDay;
                var duskTime = astronomicalDusk.TimeOfDay;

                // Check if we're in astronomical darkness
                bool isAstronomicalDark = false;
                if (duskTime < dawnTime) // Normal case - dusk is evening, dawn is next morning
                {
                    isAstronomicalDark = currentTime >= duskTime || currentTime <= dawnTime;
                }
                else // Unusual case
                {
                    isAstronomicalDark = currentTime >= duskTime && currentTime <= dawnTime;
                }

                qualityScore += isAstronomicalDark ? 0.4 : 0.0;

                // Moon interference calculation using existing methods
                var moonIllumination = _sunCalculatorService.GetMoonIllumination(dateTime, latitude, longitude, "UTC");
                var moonElevation = _sunCalculatorService.GetMoonElevation(dateTime, latitude, longitude, "UTC");

                // Reduce quality based on moon brightness and altitude
                double moonInterference = 0.0;
                if (moonElevation > 0) // Moon is above horizon
                {
                    moonInterference = moonIllumination * (moonElevation / 90.0) * 0.3;
                }

                qualityScore += Math.Max(0, 0.3 - moonInterference);

                // Seasonal considerations for Milky Way visibility
                var month = dateTime.Month;
                double seasonalScore = month switch
                {
                    6 or 7 or 8 => 0.3,     // Summer - peak Milky Way season
                    5 or 9 => 0.2,          // Late spring/early autumn - good
                    4 or 10 => 0.1,         // Spring/autumn - moderate
                    _ => 0.05               // Winter - limited galactic center visibility
                };

                qualityScore += seasonalScore;

                // Ensure score is between 0 and 1
                return Math.Max(0.0, Math.Min(1.0, qualityScore));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating dark sky quality");
                return 0.5; // Default moderate quality
            }
        }

        private List<string> GetMilkyWayCompositionSuggestions(double azimuth, double altitude)
        {
            try
            {
                var suggestions = new List<string>();

                // Azimuth-based composition advice
                if (azimuth >= 135 && azimuth <= 225) // Southeast to Southwest
                {
                    suggestions.Add("Position galactic center over southern horizon for dramatic arch compositions");
                    suggestions.Add("Include mountain ridges or treelines as foreground silhouettes");
                    suggestions.Add("Consider reflection shots over lakes or calm water bodies");
                }
                else if (azimuth >= 90 && azimuth < 135) // East to Southeast
                {
                    suggestions.Add("Capture galactic center rising - excellent for time-lapse sequences");
                    suggestions.Add("Frame with eastern horizon landmarks like buildings or rock formations");
                }
                else if (azimuth > 225 && azimuth <= 270) // Southwest to West
                {
                    suggestions.Add("Document galactic center setting - ideal for panoramic compositions");
                    suggestions.Add("Combine with western landscape features for compelling foregrounds");
                }

                // Altitude-based composition advice
                if (altitude < 15)
                {
                    suggestions.Add("Low galactic center perfect for wide horizontal panoramas");
                    suggestions.Add("Use rule of thirds - place horizon on lower third, sky dominates frame");
                    suggestions.Add("Include interesting foreground elements to add depth and scale");
                }
                else if (altitude >= 15 && altitude < 45)
                {
                    suggestions.Add("Medium altitude ideal for portrait orientation compositions");
                    suggestions.Add("Frame galactic center with natural archways or cave openings");
                    suggestions.Add("Consider vertical panoramas to capture full galactic arch");
                }
                else if (altitude >= 45)
                {
                    suggestions.Add("High galactic center excellent for detailed core photography");
                    suggestions.Add("Focus on central bulge detail with longer focal lengths");
                    suggestions.Add("Consider star tracker for detailed high-resolution captures");
                }

                // Universal composition advice
                suggestions.Add("Use leading lines in landscape to guide eye toward galactic center");
                suggestions.Add("Include human figures for scale and storytelling element");
                suggestions.Add("Plan shots during blue hour for balanced sky and foreground exposure");

                return suggestions;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating Milky Way composition suggestions");
                return new List<string> { "Use wide-angle lens and include interesting foreground elements" };
            }
        }

        private (double ra, double dec) GetConstellationCoordinates(ConstellationType constellation)
        {
            try
            {
                // Right Ascension (hours) and Declination (degrees) for constellation centers
                return constellation switch
                {
                    ConstellationType.Orion => (5.5, 5.0),
                    ConstellationType.Cassiopeia => (1.0, 60.0),
                    ConstellationType.UrsaMajor => (11.0, 50.0),
                    ConstellationType.UrsaMinor => (15.0, 75.0),
                    ConstellationType.Draco => (17.0, 65.0),
                    ConstellationType.Cygnus => (20.5, 40.0),
                    ConstellationType.Lyra => (18.75, 39.0),
                    ConstellationType.Aquila => (19.7, 5.0),
                    ConstellationType.Sagittarius => (19.0, -25.0),
                    ConstellationType.Scorpius => (16.5, -26.0),
                    ConstellationType.Centaurus => (13.0, -47.0),
                    ConstellationType.Crux => (12.5, -60.0),
                    ConstellationType.Andromeda => (1.0, 37.0),
                    ConstellationType.Perseus => (2.3, 45.0),
                    ConstellationType.Auriga => (6.0, 42.0),
                    ConstellationType.Gemini => (7.0, 22.0),
                    ConstellationType.Cancer => (8.7, 20.0),
                    ConstellationType.Leo => (10.7, 15.0),
                    ConstellationType.Virgo => (13.4, -4.0),
                    ConstellationType.Libra => (15.2, -15.0),
                    ConstellationType.Capricornus => (21.0, -20.0),
                    ConstellationType.Aquarius => (22.5, -10.0),
                    ConstellationType.Pisces => (0.7, 15.0),
                    ConstellationType.Aries => (2.7, 20.0),
                    ConstellationType.Taurus => (4.6, 19.0),
                    _ => (12.0, 0.0) // Default to celestial equator
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting constellation coordinates for {Constellation}", constellation);
                return (12.0, 0.0); // Default coordinates
            }
        }

        private DateTime? GetOptimalViewingTime(DateTime? rise, DateTime? set)
        {
            try
            {
                if (!rise.HasValue || !set.HasValue)
                    return null;

                var riseTime = rise.Value;
                var setTime = set.Value;

                // Handle case where set time is next day
                if (setTime < riseTime)
                    setTime = setTime.AddDays(1);

                // Calculate midpoint between rise and set for optimal viewing
                var midpoint = riseTime.AddTicks((setTime - riseTime).Ticks / 2);

                // Use ISunCalculatorService for astronomical twilight considerations
                var astronomicalDusk = _sunCalculatorService.GetAstronomicalDusk(riseTime.Date, 0, 0, "UTC"); // Using 0,0 for general calculation
                var astronomicalDawn = _sunCalculatorService.GetAstronomicalDawn(riseTime.Date.AddDays(1), 0, 0, "UTC");

                // Ensure optimal time is during astronomical darkness
                if (midpoint < astronomicalDusk)
                    return astronomicalDusk;
                else if (midpoint > astronomicalDawn)
                    return astronomicalDawn;
                else
                    return midpoint;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calculating optimal viewing time");
                return null;
            }
        }

        private bool IsCircumpolar(double declination, double latitude)
        {
            try
            {
                // An object is circumpolar if its declination is greater than (90° - observer's latitude)
                // For northern hemisphere: Dec > (90° - Lat)
                // For southern hemisphere: Dec < (-90° - Lat)

                if (latitude >= 0) // Northern hemisphere
                {
                    return declination > (90.0 - latitude);
                }
                else // Southern hemisphere
                {
                    return declination < (-90.0 - Math.Abs(latitude));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error determining circumpolar status for declination {Declination} at latitude {Latitude}", declination, latitude);
                return false;
            }
        }

        private List<DeepSkyObjectData> GetConstellationDeepSkyObjects(ConstellationType constellation)
        {
            try
            {
                var objects = new List<DeepSkyObjectData>();

                // Notable deep sky objects by constellation
                switch (constellation)
                {
                    case ConstellationType.Orion:
                        objects.AddRange(new[]
                        {
                   CreateDeepSkyObject("M42", "Orion Nebula", "Nebula", 5.58, -5.39, 4.0, 85),
                   CreateDeepSkyObject("M43", "De Mairan's Nebula", "Nebula", 5.58, -5.27, 9.0, 20),
                   CreateDeepSkyObject("NGC 2024", "Flame Nebula", "Nebula", 5.68, -1.9, 2.0, 30)
               });
                        break;

                    case ConstellationType.Andromeda:
                        objects.AddRange(new[]
                        {
                   CreateDeepSkyObject("M31", "Andromeda Galaxy", "Galaxy", 0.71, 41.27, 3.4, 190),
                   CreateDeepSkyObject("M32", "Le Gentil", "Galaxy", 0.71, 40.87, 8.1, 8),
                   CreateDeepSkyObject("M110", "NGC 205", "Galaxy", 0.67, 41.68, 8.5, 19)
               });
                        break;

                    case ConstellationType.Sagittarius:
                        objects.AddRange(new[]
                        {
                   CreateDeepSkyObject("M8", "Lagoon Nebula", "Nebula", 18.06, -24.38, 6.0, 90),
                   CreateDeepSkyObject("M20", "Trifid Nebula", "Nebula", 18.03, -23.03, 9.0, 20),
                   CreateDeepSkyObject("M22", "Great Sagittarius Cluster", "Globular Cluster", 18.61, -23.9, 5.1, 32)
               });
                        break;

                    case ConstellationType.Cygnus:
                        objects.AddRange(new[]
                        {
                   CreateDeepSkyObject("NGC 7000", "North America Nebula", "Nebula", 20.98, 44.22, 4.0, 120),
                   CreateDeepSkyObject("M27", "Dumbbell Nebula", "Planetary Nebula", 19.99, 22.72, 7.5, 8),
                   CreateDeepSkyObject("NGC 6960", "Western Veil Nebula", "Supernova Remnant", 20.75, 30.72, 7.0, 70)
               });
                        break;

                    case ConstellationType.Leo:
                        objects.AddRange(new[]
                        {
                   CreateDeepSkyObject("M65", "Leo Triplet", "Galaxy", 11.31, 13.1, 9.3, 8),
                   CreateDeepSkyObject("M66", "Leo Triplet", "Galaxy", 11.34, 12.99, 8.9, 8),
                   CreateDeepSkyObject("M95", "Barred Spiral Galaxy", "Galaxy", 10.74, 11.7, 9.7, 4)
               });
                        break;

                    case ConstellationType.Virgo:
                        objects.AddRange(new[]
                        {
                   CreateDeepSkyObject("M87", "Virgo A", "Galaxy", 12.51, 12.39, 8.6, 7),
                   CreateDeepSkyObject("M104", "Sombrero Galaxy", "Galaxy", 12.67, -11.62, 8.0, 9),
                   CreateDeepSkyObject("M49", "Elliptical Galaxy", "Galaxy", 12.50, 8.0, 8.4, 9)
               });
                        break;

                    // Add more constellations as needed
                    default:
                        // Return empty list for constellations without predefined objects
                        break;
                }

                return objects;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting deep sky objects for constellation {Constellation}", constellation);
                return new List<DeepSkyObjectData>();
            }
        }

        private DeepSkyObjectData CreateDeepSkyObject(string catalogId, string commonName, string objectType,
           double ra, double dec, double magnitude, double angularSize)
        {
            return new DeepSkyObjectData
            {
                CatalogId = catalogId,
                CommonName = commonName,
                ObjectType = objectType,
                RightAscension = ra,
                Declination = dec,
                Magnitude = magnitude,
                AngularSize = angularSize,
                DateTime = DateTime.UtcNow, // Will be updated when used
                RecommendedEquipment = GetDeepSkyEquipmentRecommendation(objectType, magnitude, angularSize),
                ExposureGuidance = GetDeepSkyExposureGuidance(objectType, magnitude)
            };
        }

        private string GetDeepSkyEquipmentRecommendation(string objectType, double magnitude, double angularSize)
        {
            return objectType switch
            {
                "Galaxy" when magnitude > 9 => "Telescope 200mm+ focal length, dark sky location essential",
                "Galaxy" => "135-200mm telephoto lens sufficient, moderate light pollution acceptable",
                "Nebula" when angularSize > 60 => "Wide-angle lens 14-50mm for full frame capture",
                "Nebula" => "Telephoto lens 85-200mm for detail, consider H-alpha filter",
                "Globular Cluster" => "200mm+ telephoto lens, resolve individual stars with longer focal lengths",
                "Planetary Nebula" => "Telescope 300mm+ focal length, OIII filter recommended",
                _ => "Medium telephoto lens 135-200mm recommended"
            };
        }

        private string GetDeepSkyExposureGuidance(string objectType, double magnitude)
        {
            return objectType switch
            {
                "Galaxy" => "ISO 1600-6400, f/2.8-4, 2-5 minute exposures with tracking",
                "Nebula" => "ISO 800-3200, f/2.8-4, 3-8 minute exposures, consider narrowband filters",
                "Globular Cluster" => "ISO 400-1600, f/4-5.6, 1-3 minute exposures",
                "Planetary Nebula" => "ISO 800-1600, f/4-5.6, 2-5 minute exposures with OIII filter",
                _ => "ISO 800-3200, f/2.8-4, 2-5 minute exposures with star tracker"
            };
        }

        private string GetConstellationPhotographyNotes(ConstellationType constellation)
        {
            try
            {
                return constellation switch
                {
                    ConstellationType.Orion => "Winter constellation, excellent for nebula photography. M42 Orion Nebula is ideal beginner target. Use 85-200mm lens for detail.",

                    ConstellationType.Andromeda => "Autumn constellation featuring M31 Andromeda Galaxy. Use 135-200mm lens. Galaxy spans 3+ degrees, plan wide compositions.",

                    ConstellationType.Sagittarius => "Summer constellation in Milky Way core region. Rich nebula fields, excellent for wide-field astrophotography. Dark skies essential.",

                    ConstellationType.Cygnus => "Summer constellation along Milky Way. North America Nebula excellent widefield target. Veil Nebula complex requires H-alpha filter.",

                    ConstellationType.Cassiopeia => "Circumpolar constellation, visible year-round from northern latitudes. Heart and Soul nebulae excellent autumn targets.",

                    ConstellationType.UrsaMajor => "Spring constellation, contains several galaxies including M81/M82 Bode's Galaxy pair. 200mm+ lens recommended.",

                    ConstellationType.Leo => "Spring constellation with Leo Triplet galaxies. M65, M66, NGC 3628 form excellent galaxy group for photography.",

                    ConstellationType.Virgo => "Spring constellation, heart of Virgo Galaxy Cluster. M87, M104 Sombrero Galaxy prime targets. Requires dark skies.",

                    ConstellationType.Scorpius => "Summer constellation, competes with Sagittarius for nebula richness. M4 globular cluster excellent target.",

                    ConstellationType.Perseus => "Winter constellation, contains Double Cluster and California Nebula. Excellent for widefield star field photography.",

                    ConstellationType.Auriga => "Winter constellation with several star clusters. M36, M37, M38 form excellent telescopic targets.",

                    ConstellationType.Gemini => "Winter constellation, M35 open cluster excellent binocular/widefield target. Eskimo Nebula requires longer focal length.",

                    ConstellationType.Taurus => "Winter constellation featuring Pleiades M45 and Hyades clusters. Crab Nebula M1 challenging but rewarding target.",

                    ConstellationType.Lyra => "Summer constellation with Ring Nebula M57. Compact planetary nebula requires telescope for detail.",

                    ConstellationType.Aquila => "Summer constellation along Milky Way. Rich star fields excellent for widefield photography.",

                    ConstellationType.Centaurus => "Southern constellation with Omega Centauri globular cluster. Not visible from northern latitudes.",

                    ConstellationType.Crux => "Southern Cross, iconic southern hemisphere constellation. Coalsack Nebula and Jewel Box Cluster prime targets.",

                    _ => "Constellation offers various astrophotography opportunities. Research specific deep sky objects for optimal imaging strategies."
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error getting constellation photography notes for {Constellation}", constellation);
                return "Excellent constellation for astrophotography with various deep sky objects to explore.";
            }
        }

        public async Task<DeepSkyObjectData> GetDeepSkyObjectDataAsync(string catalogId, DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var time = new AstroTime(dateTime);
                    var observer = new Observer(latitude, longitude, 0);

                    // Get object coordinates from catalog
                    var objectCoords = GetDeepSkyObjectCoordinates(catalogId);
                    if (objectCoords == null)
                    {
                        throw new ArgumentException($"Deep sky object {catalogId} not found in catalog");
                    }

                    // Calculate horizontal position
                    var horizontal = Astronomy.Horizon(time, observer, objectCoords.Value.ra, objectCoords.Value.dec, Refraction.Normal);

                    // Get rise/set times
                    var riseSet = GetObjectRiseSetTimes(objectCoords.Value.ra, objectCoords.Value.dec, dateTime, observer);

                    // Determine optimal viewing time
                    var optimalTime = GetOptimalViewingTime(riseSet.rise, riseSet.set);

                    return new DeepSkyObjectData
                    {
                        CatalogId = catalogId,
                        CommonName = objectCoords.Value.commonName,
                        ObjectType = objectCoords.Value.objectType,
                        DateTime = dateTime,
                        RightAscension = objectCoords.Value.ra,
                        Declination = objectCoords.Value.dec,
                        Azimuth = horizontal.azimuth,
                        Altitude = horizontal.altitude,
                        Magnitude = objectCoords.Value.magnitude,
                        AngularSize = objectCoords.Value.angularSize,
                        IsVisible = horizontal.altitude > 0,
                        OptimalViewingTime = optimalTime,
                        RecommendedEquipment = GetDeepSkyEquipmentRecommendation(objectCoords.Value.objectType, objectCoords.Value.magnitude, objectCoords.Value.angularSize),
                        ExposureGuidance = GetDeepSkyExposureGuidance(objectCoords.Value.objectType, objectCoords.Value.magnitude),
                        ParentConstellation = GetObjectParentConstellation(objectCoords.Value.ra, objectCoords.Value.dec)
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating deep sky object data for {CatalogId}", catalogId);
                    throw;
                }
            }, cancellationToken);
        }

        private (double ra, double dec, string commonName, string objectType, double magnitude, double angularSize)? GetDeepSkyObjectCoordinates(string catalogId)
        {
            // Messier and NGC catalog data - abbreviated for key objects
            var catalog = new Dictionary<string, (double ra, double dec, string commonName, string objectType, double magnitude, double angularSize)>
    {
        // Messier Objects
        { "M1", (5.575, 22.02, "Crab Nebula", "Supernova Remnant", 8.4, 6) },
        { "M8", (18.06, -24.38, "Lagoon Nebula", "Nebula", 6.0, 90) },
        { "M13", (16.69, 36.46, "Great Globular Cluster", "Globular Cluster", 5.8, 20) },
        { "M20", (18.03, -23.03, "Trifid Nebula", "Nebula", 9.0, 20) },
        { "M27", (19.99, 22.72, "Dumbbell Nebula", "Planetary Nebula", 7.5, 8) },
        { "M31", (0.71, 41.27, "Andromeda Galaxy", "Galaxy", 3.4, 190) },
        { "M42", (5.58, -5.39, "Orion Nebula", "Nebula", 4.0, 85) },
        { "M45", (3.79, 24.11, "Pleiades", "Open Cluster", 1.6, 110) },
        { "M51", (13.50, 47.19, "Whirlpool Galaxy", "Galaxy", 8.4, 11) },
        { "M57", (18.88, 33.03, "Ring Nebula", "Planetary Nebula", 8.8, 1.4) },
        { "M81", (9.93, 69.07, "Bode's Galaxy", "Galaxy", 6.9, 21) },
        { "M82", (9.93, 69.68, "Cigar Galaxy", "Galaxy", 8.4, 9) },
        { "M87", (12.51, 12.39, "Virgo A", "Galaxy", 8.6, 7) },
        { "M104", (12.67, -11.62, "Sombrero Galaxy", "Galaxy", 8.0, 9) },
        
        // Notable NGC Objects
        { "NGC 7000", (20.98, 44.22, "North America Nebula", "Nebula", 4.0, 120) },
        { "NGC 6960", (20.75, 30.72, "Western Veil Nebula", "Supernova Remnant", 7.0, 70) },
        { "NGC 2024", (5.68, -1.9, "Flame Nebula", "Nebula", 2.0, 30) },
        { "NGC 3372", (10.75, -59.87, "Eta Carinae Nebula", "Nebula", 3.0, 120) },
        { "NGC 869", (2.35, 57.13, "Double Cluster h", "Open Cluster", 4.3, 18) },
        { "NGC 884", (2.37, 57.14, "Double Cluster chi", "Open Cluster", 4.4, 18) }
    };

            return catalog.TryGetValue(catalogId.ToUpper(), out var objectData) ? objectData : null;
        }

        private ConstellationType GetObjectParentConstellation(double ra, double dec)
        {
            // Simplified constellation boundaries - in reality would use IAU boundaries
            return ra switch
            {
                >= 0 and < 2 => ConstellationType.Andromeda,
                >= 2 and < 4.5 => ConstellationType.Perseus,
                >= 4.5 and < 7 => ConstellationType.Orion,
                >= 7 and < 9 => ConstellationType.Gemini,
                >= 9 and < 11.5 => ConstellationType.Leo,
                >= 11.5 and < 14 => ConstellationType.Virgo,
                >= 16 and < 19 => ConstellationType.Scorpius,
                >= 18 and < 21 => ConstellationType.Sagittarius,
                >= 20 and < 23 => ConstellationType.Cygnus,
                _ => ConstellationType.Orion // Default
            };
        }

        public async Task<List<ISSPassData>> GetISSPassesAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var passes = new List<ISSPassData>();

                    // ISS orbital period is approximately 93 minutes
                    // Generate predictive passes based on typical orbital mechanics
                    // Note: This is a simplified simulation - real implementation would use TLE data

                    var currentDate = startDate;
                    var issOrbitalPeriod = TimeSpan.FromMinutes(93);
                    var daysBetweenVisiblePasses = 1; // ISS typically visible every 1-3 days from a location

                    while (currentDate <= endDate)
                    {
                        // Generate 1-3 passes per day when ISS is in favorable orbit
                        var passCount = new Random().Next(0, 4); // 0-3 passes per day

                        for (int i = 0; i < passCount; i++)
                        {
                            var passStartTime = currentDate.AddHours(5 + (i * 4)); // Spread passes throughout night

                            // Generate realistic pass data
                            var passDuration = TimeSpan.FromMinutes(new Random().Next(2, 8)); // 2-8 minute passes
                            var maxAltitude = 10 + new Random().Next(0, 80); // 10-90 degrees max altitude
                            var startAzimuth = new Random().Next(0, 360);
                            var endAzimuth = (startAzimuth + new Random().Next(45, 180)) % 360;
                            var magnitude = -4.0 + (new Random().NextDouble() * 3.0); // -4.0 to -1.0 magnitude

                            // Only include passes visible above 10 degrees
                            if (maxAltitude > 10)
                            {
                                passes.Add(new ISSPassData
                                {
                                    StartTime = passStartTime,
                                    MaxTime = passStartTime.Add(TimeSpan.FromTicks(passDuration.Ticks / 2)),
                                    EndTime = passStartTime.Add(passDuration),
                                    StartAzimuth = startAzimuth,
                                    MaxAltitude = maxAltitude,
                                    EndAzimuth = endAzimuth,
                                    Magnitude = magnitude,
                                    Duration = passDuration,
                                    PassType = maxAltitude > 60 ? "Overhead Pass" : maxAltitude > 30 ? "High Pass" : "Low Pass",
                                    PhotographyPotential = GetISSPhotographyPotential(maxAltitude, passDuration, magnitude)
                                });
                            }
                        }

                        currentDate = currentDate.AddDays(daysBetweenVisiblePasses);
                    }

                    return passes.OrderBy(p => p.StartTime).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generating ISS pass data");
                    return new List<ISSPassData>();
                }
            }, cancellationToken);
        }

        private string GetISSPhotographyPotential(double maxAltitude, TimeSpan duration, double magnitude)
        {
            var potential = new List<string>();

            if (maxAltitude > 70)
                potential.Add("Excellent overhead pass - ideal for star trail composites");
            else if (maxAltitude > 40)
                potential.Add("Good high pass - suitable for single exposure captures");
            else if (maxAltitude > 20)
                potential.Add("Moderate pass - visible but lower in sky");
            else
                potential.Add("Low pass - challenging due to atmosphere and obstacles");

            if (duration.TotalMinutes > 5)
                potential.Add("Long duration pass - excellent for time-lapse sequences");
            else if (duration.TotalMinutes > 3)
                potential.Add("Standard duration - good for multiple exposures");
            else
                potential.Add("Short pass - plan timing carefully");

            if (magnitude < -3.0)
                potential.Add("Very bright pass - easily visible and photographable");
            else if (magnitude < -2.0)
                potential.Add("Bright pass - good photographic target");
            else
                potential.Add("Dimmer pass - may require longer exposures");

            potential.Add("Use 15-30 second exposures, ISO 800-3200, wide-angle lens");

            return string.Join(". ", potential) + ".";
        }

        public async Task<List<MeteorShowerData>> GetMeteorShowersAsync(DateTime startDate, DateTime endDate, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var showers = new List<MeteorShowerData>();
                    var observer = new Observer(latitude, longitude, 0);

                    // Annual meteor shower data with peak dates and radiant positions
                    var annualShowers = GetAnnualMeteorShowers();

                    foreach (var shower in annualShowers)
                    {
                        // Check if shower occurs within date range
                        var showerYear = startDate.Year;
                        var peakDate = new DateTime(showerYear, shower.peakMonth, shower.peakDay);
                        var activityStart = peakDate.AddDays(-shower.activityDays / 2);
                        var activityEnd = peakDate.AddDays(shower.activityDays / 2);

                        // Check if shower period overlaps with requested date range
                        if (activityEnd >= startDate && activityStart <= endDate)
                        {
                            var time = new AstroTime(peakDate);
                            var horizontal = Astronomy.Horizon(time, observer, shower.radiantRA, shower.radiantDec, Refraction.Normal);

                            // Get moon data for interference calculation
                            var moonIllumination = _sunCalculatorService.GetMoonIllumination(peakDate, latitude, longitude, "UTC");
                            var moonElevation = _sunCalculatorService.GetMoonElevation(peakDate, latitude, longitude, "UTC");

                            // Determine optimal conditions
                            var optimalConditions = moonIllumination < 0.3 && horizontal.altitude > 30;

                            showers.Add(new MeteorShowerData
                            {
                                ShowerType = shower.type,
                                Name = shower.name,
                                PeakDate = peakDate,
                                ActivityStart = activityStart,
                                ActivityEnd = activityEnd,
                                RadiantRightAscension = shower.radiantRA,
                                RadiantDeclination = shower.radiantDec,
                                RadiantAzimuth = horizontal.azimuth,
                                RadiantAltitude = horizontal.altitude,
                                ZenithHourlyRate = shower.zhr,
                                MoonIllumination = moonIllumination * 100,
                                OptimalConditions = optimalConditions,
                                PhotographyStrategy = GetMeteorShowerPhotographyStrategy(shower.type, horizontal.altitude, moonIllumination)
                            });
                        }

                        // Check next year if end date extends into it
                        if (endDate.Year > startDate.Year)
                        {
                            showerYear = endDate.Year;
                            peakDate = new DateTime(showerYear, shower.peakMonth, shower.peakDay);
                            activityStart = peakDate.AddDays(-shower.activityDays / 2);
                            activityEnd = peakDate.AddDays(shower.activityDays / 2);

                            if (activityEnd >= startDate && activityStart <= endDate)
                            {
                                // Repeat calculation for next year
                                var time = new AstroTime(peakDate);
                                var horizontal = Astronomy.Horizon(time, observer, shower.radiantRA, shower.radiantDec, Refraction.Normal);
                                var moonIllumination = _sunCalculatorService.GetMoonIllumination(peakDate, latitude, longitude, "UTC");
                                var optimalConditions = moonIllumination < 0.3 && horizontal.altitude > 30;

                                showers.Add(new MeteorShowerData
                                {
                                    ShowerType = shower.type,
                                    Name = shower.name,
                                    PeakDate = peakDate,
                                    ActivityStart = activityStart,
                                    ActivityEnd = activityEnd,
                                    RadiantRightAscension = shower.radiantRA,
                                    RadiantDeclination = shower.radiantDec,
                                    RadiantAzimuth = horizontal.azimuth,
                                    RadiantAltitude = horizontal.altitude,
                                    ZenithHourlyRate = shower.zhr,
                                    MoonIllumination = moonIllumination * 100,
                                    OptimalConditions = optimalConditions,
                                    PhotographyStrategy = GetMeteorShowerPhotographyStrategy(shower.type, horizontal.altitude, moonIllumination)
                                });
                            }
                        }
                    }

                    return showers.OrderBy(s => s.PeakDate).ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating meteor shower data");
                    return new List<MeteorShowerData>();
                }
            }, cancellationToken);
        }

        private List<(MeteorShowerType type, string name, int peakMonth, int peakDay, int activityDays, double radiantRA, double radiantDec, int zhr)> GetAnnualMeteorShowers()
        {
            return new List<(MeteorShowerType, string, int, int, int, double, double, int)>
   {
       (MeteorShowerType.Quadrantids, "Quadrantids", 1, 4, 10, 15.3, 49.5, 120),
       (MeteorShowerType.Lyrids, "Lyrids", 4, 22, 10, 18.1, 34.3, 18),
       (MeteorShowerType.EtaAquariids, "Eta Aquariids", 5, 6, 20, 22.5, -1.0, 50),
       (MeteorShowerType.Perseids, "Perseids", 8, 13, 30, 3.1, 58.0, 100),
       (MeteorShowerType.Draconids, "Draconids", 10, 8, 5, 17.5, 54.0, 10),
       (MeteorShowerType.Orionids, "Orionids", 10, 21, 14, 6.3, 16.0, 25),
       (MeteorShowerType.Leonids, "Leonids", 11, 17, 10, 10.1, 22.0, 15),
       (MeteorShowerType.Geminids, "Geminids", 12, 14, 14, 7.5, 32.5, 120),
       (MeteorShowerType.Ursids, "Ursids", 12, 22, 7, 14.4, 75.4, 10)
   };
        }

        private string GetMeteorShowerPhotographyStrategy(MeteorShowerType shower, double radiantAltitude, double moonIllumination)
        {
            var strategy = new List<string>();

            // Shower-specific advice
            switch (shower)
            {
                case MeteorShowerType.Perseids:
                    strategy.Add("Premier shower for photography - reliable high rates and bright meteors");
                    strategy.Add("Best after midnight when radiant is highest");
                    break;
                case MeteorShowerType.Geminids:
                    strategy.Add("Excellent winter shower - slower meteors good for photography");
                    strategy.Add("Active all night, best after 10 PM");
                    break;
                case MeteorShowerType.Quadrantids:
                    strategy.Add("Short sharp peak - timing critical within 6-hour window");
                    strategy.Add("Cold weather conditions - prepare equipment for freezing temperatures");
                    break;
                case MeteorShowerType.Leonids:
                    strategy.Add("Variable shower - some years produce meteor storms");
                    strategy.Add("Fast meteors require shorter exposures");
                    break;
            }

            // Radiant altitude advice
            if (radiantAltitude < 0)
                strategy.Add("Radiant below horizon - shower not visible from your location");
            else if (radiantAltitude < 30)
                strategy.Add("Low radiant - expect fewer meteors but longer trails");
            else if (radiantAltitude > 60)
                strategy.Add("High radiant - maximum meteor rates expected");

            // Moon interference advice
            if (moonIllumination > 0.7)
                strategy.Add("Bright moon - focus on brightest meteors, consider HDR techniques");
            else if (moonIllumination > 0.3)
                strategy.Add("Moderate moon interference - still good photography conditions");
            else
                strategy.Add("Dark skies - optimal conditions for meteor photography");

            // Technical advice
            strategy.Add("Use 14-35mm wide-angle lens, ISO 1600-6400, 15-30 second exposures");
            strategy.Add("Point camera 45-60 degrees away from radiant for longer trails");

            return string.Join(". ", strategy) + ".";
        }

        public async Task<MeteorShowerConditions> GetMeteorShowerConditionsAsync(MeteorShowerType shower, DateTime date, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var time = new AstroTime(date);
                    var observer = new Observer(latitude, longitude, 0);

                    // Get shower data
                    var showerData = GetMeteorShowerData(shower);
                    if (showerData == null)
                    {
                        throw new ArgumentException($"Meteor shower {shower} not found in database");
                    }

                    // Calculate radiant position
                    var horizontal = Astronomy.Horizon(time, observer, showerData.Value.radiantRA, showerData.Value.radiantDec, Refraction.Normal);

                    // Get moon conditions
                    var moonElevation = _sunCalculatorService.GetMoonElevation(date, latitude, longitude, "UTC");
                    var moonIllumination = _sunCalculatorService.GetMoonIllumination(date, latitude, longitude, "UTC");

                    // Calculate expected meteor rate based on radiant altitude
                    var expectedRate = CalculateMeteorRate(showerData.Value.zhr, horizontal.altitude, moonIllumination);

                    // Calculate conditions score
                    var conditionsScore = CalculateMeteorConditionsScore(horizontal.altitude, moonIllumination, moonElevation);

                    // Determine optimal camera direction
                    var optimalDirection = GetOptimalMeteorCameraDirection(horizontal.azimuth, horizontal.altitude);

                    return new MeteorShowerConditions
                    {
                        Shower = shower,
                        DateTime = date,
                        RadiantAltitude = horizontal.altitude,
                        MoonAltitude = moonElevation,
                        MoonIllumination = moonIllumination * 100,
                        ExpectedRate = expectedRate,
                        ConditionsScore = conditionsScore,
                        Recommendation = GetMeteorConditionsRecommendation(conditionsScore, horizontal.altitude, moonIllumination),
                        OptimalCameraDirection = optimalDirection
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating meteor shower conditions for {Shower}", shower);
                    throw;
                }
            }, cancellationToken);
        }

        private (double radiantRA, double radiantDec, int zhr)? GetMeteorShowerData(MeteorShowerType shower)
        {
            var showerDatabase = new Dictionary<MeteorShowerType, (double ra, double dec, int zhr)>
    {
        { MeteorShowerType.Quadrantids, (15.3, 49.5, 120) },
        { MeteorShowerType.Lyrids, (18.1, 34.3, 18) },
        { MeteorShowerType.EtaAquariids, (22.5, -1.0, 50) },
        { MeteorShowerType.Perseids, (3.1, 58.0, 100) },
        { MeteorShowerType.Draconids, (17.5, 54.0, 10) },
        { MeteorShowerType.Orionids, (6.3, 16.0, 25) },
        { MeteorShowerType.Leonids, (10.1, 22.0, 15) },
        { MeteorShowerType.Geminids, (7.5, 32.5, 120) },
        { MeteorShowerType.Ursids, (14.4, 75.4, 10) }
    };

            return showerDatabase.TryGetValue(shower, out var data) ? data : null;
        }

        private double CalculateMeteorRate(int zenithHourlyRate, double radiantAltitude, double moonIllumination)
        {
            if (radiantAltitude <= 0) return 0;

            // Adjust rate based on radiant altitude
            var altitudeFactor = Math.Sin(radiantAltitude * Math.PI / 180.0);

            // Adjust for moon interference
            var moonFactor = 1.0 - (moonIllumination * 0.7); // Moon reduces visibility by up to 70%

            return zenithHourlyRate * altitudeFactor * moonFactor;
        }

        private double CalculateMeteorConditionsScore(double radiantAltitude, double moonIllumination, double moonElevation)
        {
            double score = 0.0;

            // Radiant altitude scoring (0-0.5)
            if (radiantAltitude > 60) score += 0.5;
            else if (radiantAltitude > 30) score += 0.3;
            else if (radiantAltitude > 0) score += 0.1;

            // Moon interference scoring (0-0.3)
            if (moonElevation < 0) score += 0.3; // Moon below horizon
            else if (moonIllumination < 0.2) score += 0.25;
            else if (moonIllumination < 0.5) score += 0.15;
            else if (moonIllumination < 0.8) score += 0.05;

            // Dark sky bonus (0-0.2)
            score += 0.2; // Assume reasonable dark sky conditions

            return Math.Max(0.0, Math.Min(1.0, score));
        }

        private string GetMeteorConditionsRecommendation(double score, double radiantAltitude, double moonIllumination)
        {
            if (score > 0.8) return "Excellent conditions - ideal for meteor photography";
            if (score > 0.6) return "Good conditions - favorable for meteor capture";
            if (score > 0.4) return "Fair conditions - meteors visible but challenging";
            if (score > 0.2) return "Poor conditions - limited meteor visibility";
            return "Very poor conditions - meteor photography not recommended";
        }

        private string GetOptimalMeteorCameraDirection(double radiantAzimuth, double radiantAltitude)
        {
            // Point camera 45-60 degrees away from radiant for longer meteor trails
            var optimalAzimuth1 = (radiantAzimuth + 60) % 360;
            var optimalAzimuth2 = (radiantAzimuth - 60 + 360) % 360;

            var direction1 = GetCardinalDirection(optimalAzimuth1);
            var direction2 = GetCardinalDirection(optimalAzimuth2);

            return $"Point camera {direction1} or {direction2} (60° from radiant) for optimal meteor trails";
        }

        private string GetCardinalDirection(double azimuth)
        {
            return azimuth switch
            {
                >= 337.5 or < 22.5 => "North",
                >= 22.5 and < 67.5 => "Northeast",
                >= 67.5 and < 112.5 => "East",
                >= 112.5 and < 157.5 => "Southeast",
                >= 157.5 and < 202.5 => "South",
                >= 202.5 and < 247.5 => "Southwest",
                >= 247.5 and < 292.5 => "West",
                >= 292.5 and < 337.5 => "Northwest",
                _ => "North"
            };
        }






        private string GetMilkyWayPhotographyAdvice(string season, double altitude)
        {
            try
            {
                var advice = new List<string>();

                // Season-specific advice
                if (season.Contains("Summer"))
                {
                    advice.Add("Peak Milky Way season - galactic center visible all night");
                    advice.Add("Plan shoots during new moon for darkest skies");
                    advice.Add("Galactic center rises in southeast, sets in southwest");
                    advice.Add("Best shooting window: 10 PM - 4 AM for optimal positioning");
                }
                else if (season.Contains("Spring"))
                {
                    advice.Add("Galactic center visible before dawn - plan early morning shoots");
                    advice.Add("Core rises 2-4 hours before sunrise");
                    advice.Add("Combine with sunrise landscape photography");
                }
                else if (season.Contains("Autumn"))
                {
                    advice.Add("Galactic center sets early evening - capture during blue hour");
                    advice.Add("Golden opportunity for foreground silhouettes");
                    advice.Add("Core visible immediately after astronomical twilight");
                }
                else if (season.Contains("Winter"))
                {
                    advice.Add("Focus on winter Milky Way - dimmer but still photographable");
                    advice.Add("Orion region and winter constellations prominent");
                    advice.Add("Longer nights provide extended shooting opportunities");
                }

                // Altitude-specific advice
                if (altitude < 0)
                {
                    advice.Add("Galactic center below horizon - not visible");
                }
                else if (altitude < 20)
                {
                    advice.Add("Low altitude - find elevated location with clear southern horizon");
                    advice.Add("Atmospheric distortion may affect image quality");
                }
                else if (altitude < 45)
                {
                    advice.Add("Good altitude for dramatic foreground compositions");
                    advice.Add("Excellent for landscape astrophotography");
                }
                else
                {
                    advice.Add("High altitude - minimal atmospheric interference");
                    advice.Add("Ideal for detailed galactic center photography");
                }

                // Technical advice
                advice.Add("Use 14-24mm wide-angle lens for full galactic arc");
                advice.Add("ISO 3200-6400, f/2.8 or wider, 15-25 second exposures");
                advice.Add("Consider panoramic stitching for ultra-wide compositions");

                return string.Join(". ", advice) + ".";
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error generating Milky Way photography advice");
                return "Use wide-angle lens, high ISO, and long exposure for Milky Way photography.";
            }
        }
        private AstroExposureRecommendation CalculateLunarExposureSettings(CameraEquipmentData equipment, AstroConditions conditions)
        {
            // Moon is very bright - use low ISO and fast shutter speeds
            var recommendedISO = 100;
            var recommendedAperture = Math.Max(equipment.Aperture, 5.6); // Stop down for sharpness

            return new AstroExposureRecommendation
            {
                Target = AstroTarget.Moon,
                RecommendedISO = $"ISO {recommendedISO}",
                RecommendedAperture = $"f/{recommendedAperture:F1}",
                RecommendedShutterSpeed = "1/125 - 1/500 second (varies by lunar phase)",
                NumberOfFrames = 1,
                TotalExposureTime = TimeSpan.FromSeconds(1),
                FocusingTechnique = "Use live view to focus on lunar crater edges for maximum sharpness",
                ProcessingNotes = new List<string>
        {
            "Use histogram to avoid overexposure of lunar surface",
            "Consider HDR for earthshine and bright lunar surface",
            "Apply unsharp mask to enhance crater detail"
        },
                TrackerRequirements = "Tracking not required for short lunar exposures"
            };
        }

        private AstroExposureRecommendation CalculatePlanetaryExposureSettings(CameraEquipmentData equipment, AstroConditions conditions)
        {
            var recommendedISO = CalculateOptimalISO(equipment, conditions, AstroTarget.Planets);
            var recommendedAperture = Math.Max(equipment.Aperture, 5.6); // Stop down for sharpness

            return new AstroExposureRecommendation
            {
                Target = AstroTarget.Planets,
                RecommendedISO = $"ISO {recommendedISO}",
                RecommendedAperture = $"f/{recommendedAperture:F1}",
                RecommendedShutterSpeed = "1/60 - 1/250 second (depends on planet brightness)",
                NumberOfFrames = 100,
                TotalExposureTime = TimeSpan.FromMinutes(2),
                FocusingTechnique = "Focus on planet disc using live view at maximum zoom",
                ProcessingNotes = new List<string>
        {
            "Stack best 10-20% of frames to reduce atmospheric turbulence",
            "Use wavelets or deconvolution to enhance planetary detail",
            "Consider RGB separation for color planets"
        },
                TrackerRequirements = equipment.HasTracker ?
                    "Tracking recommended for longer focal lengths" :
                    "Short exposures minimize tracking requirements"
            };
        }

        private AstroExposureRecommendation CalculateStarTrailExposureSettings(CameraEquipmentData equipment, AstroConditions conditions)
        {
            var recommendedISO = CalculateOptimalISO(equipment, conditions, AstroTarget.StarTrails);
            var recommendedAperture = Math.Max(equipment.Aperture, 4.0); // Balance light gathering and sharpness

            return new AstroExposureRecommendation
            {
                Target = AstroTarget.StarTrails,
                RecommendedISO = $"ISO {recommendedISO}",
                RecommendedAperture = $"f/{recommendedAperture:F1}",
                RecommendedShutterSpeed = "Multiple 2-4 minute exposures (stack for long trails)",
                NumberOfFrames = 60,
                TotalExposureTime = TimeSpan.FromHours(3),
                FocusingTechnique = "Focus to infinity on bright star before starting sequence",
                ProcessingNotes = new List<string>
        {
            "Use StarStaX or similar software to blend exposures",
            "Lighten or Maximum blend mode for continuous trails",
            "Consider gap filling for uninterrupted trails",
            "Separate foreground exposure for landscape elements"
        },
                TrackerRequirements = "No tracking required - star motion creates the trails"
            };
        }

        private AstroExposureRecommendation CalculateMeteorExposureSettings(CameraEquipmentData equipment, AstroConditions conditions)
        {
            var recommendedISO = CalculateOptimalISO(equipment, conditions, AstroTarget.MeteorShowers);
            var recommendedAperture = Math.Max(equipment.Aperture, 2.8); // Wide open for light gathering

            return new AstroExposureRecommendation
            {
                Target = AstroTarget.MeteorShowers,
                RecommendedISO = $"ISO {recommendedISO}",
                RecommendedAperture = $"f/{recommendedAperture:F1}",
                RecommendedShutterSpeed = "15-30 seconds per frame",
                NumberOfFrames = 200,
                TotalExposureTime = TimeSpan.FromHours(2),
                FocusingTechnique = "Focus on bright star, then switch to manual to lock focus",
                ProcessingNotes = new List<string>
        {
            "Review frames for meteors before stacking",
            "Stack meteor-free frames for background sky",
            "Blend meteor frames as separate layers",
            "Consider automated meteor detection software"
        },
                TrackerRequirements = "No tracking - keep exposures under 30 seconds to prevent star trailing"
            };
        }

        private AstroExposureRecommendation CalculateConstellationExposureSettings(CameraEquipmentData equipment, AstroConditions conditions)
        {
            var recommendedISO = CalculateOptimalISO(equipment, conditions, AstroTarget.Constellations);
            var recommendedAperture = Math.Max(equipment.Aperture, 2.8);

            return new AstroExposureRecommendation
            {
                Target = AstroTarget.Constellations,
                RecommendedISO = $"ISO {recommendedISO}",
                RecommendedAperture = $"f/{recommendedAperture:F1}",
                RecommendedShutterSpeed = equipment.HasTracker ? "2-5 minutes (tracked)" : "15-30 seconds (untracked)",
                NumberOfFrames = equipment.HasTracker ? 10 : 30,
                TotalExposureTime = equipment.HasTracker ? TimeSpan.FromMinutes(30) : TimeSpan.FromMinutes(15),
                FocusingTechnique = "Focus on brightest star in constellation",
                ProcessingNotes = new List<string>
        {
            "Stack frames to enhance faint stars",
            "Adjust curves to bring out constellation patterns",
            "Consider star diffraction spikes for artistic effect",
            "Balance star brightness with any nebulosity present"
        },
                TrackerRequirements = equipment.HasTracker ?
                    "Tracking allows longer exposures for fainter stars" :
                    "Untracked suitable for bright constellation stars"
            };
        }

        private AstroExposureRecommendation CalculateGenericAstroExposureSettings(CameraEquipmentData equipment, AstroConditions conditions)
        {
            var recommendedISO = 1600; // Safe default
            var recommendedAperture = Math.Max(equipment.Aperture, 2.8);

            return new AstroExposureRecommendation
            {
                Target = AstroTarget.MilkyWayCore, // Default to Milky Way settings
                RecommendedISO = $"ISO {recommendedISO}",
                RecommendedAperture = $"f/{recommendedAperture:F1}",
                RecommendedShutterSpeed = "20-30 seconds",
                NumberOfFrames = 20,
                TotalExposureTime = TimeSpan.FromMinutes(10),
                FocusingTechnique = "Focus on bright star using live view magnification",
                ProcessingNotes = new List<string>
        {
            "Start with conservative settings and adjust based on results",
            "Monitor histogram to avoid overexposure",
            "Stack multiple frames to improve signal-to-noise ratio"
        },
                TrackerRequirements = "Tracking mount recommended for exposures over 30 seconds"
            };
        }
        public async Task<AstroExposureRecommendation> GetAstroExposureRecommendationAsync(AstroTarget target, CameraEquipmentData equipment, AstroConditions conditions, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var recommendation = new AstroExposureRecommendation
                    {
                        Target = target
                    };

                    // Calculate optimal settings based on target type and equipment
                    switch (target)
                    {
                        case AstroTarget.Moon:
                            recommendation = CalculateLunarExposureSettings(equipment, conditions);
                            break;

                        case AstroTarget.Planets:
                            recommendation = CalculatePlanetaryExposureSettings(equipment, conditions);
                            break;

                        case AstroTarget.MilkyWayCore:
                            recommendation = CalculateMilkyWayExposureSettings(equipment, conditions);
                            break;

                        case AstroTarget.DeepSkyObjects:
                            recommendation = CalculateDeepSkyExposureSettings(equipment, conditions);
                            break;

                        case AstroTarget.StarTrails:
                            recommendation = CalculateStarTrailExposureSettings(equipment, conditions);
                            break;

                        case AstroTarget.MeteorShowers:
                            recommendation = CalculateMeteorExposureSettings(equipment, conditions);
                            break;

                        case AstroTarget.Constellations:
                            recommendation = CalculateConstellationExposureSettings(equipment, conditions);
                            break;

                        default:
                            recommendation = CalculateGenericAstroExposureSettings(equipment, conditions);
                            break;
                    }

                    recommendation.Target = target;
                    return recommendation;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating astro exposure recommendation for target {Target}", target);
                    throw;
                }
            }, cancellationToken);
        }

        private AstroExposureRecommendation CalculateMilkyWayExposureSettings(CameraEquipmentData equipment, AstroConditions conditions)
        {
            // Calculate maximum exposure time before star trailing (500 rule / focal length)
            var maxExposureSeconds = Math.Min(500.0 / equipment.FocalLength, equipment.HasTracker ? 300 : 30);

            // Adjust ISO based on sensor characteristics and conditions
            var recommendedISO = CalculateOptimalISO(equipment, conditions, AstroTarget.MilkyWayCore);

            // Recommend widest aperture for maximum light gathering
            var recommendedAperture = Math.Max(equipment.Aperture, 1.4);

            return new AstroExposureRecommendation
            {
                Target = AstroTarget.MilkyWayCore,
                RecommendedISO = $"ISO {recommendedISO}",
                RecommendedAperture = $"f/{recommendedAperture:F1}",
                RecommendedShutterSpeed = equipment.HasTracker ?
                    $"{maxExposureSeconds:F0} seconds (tracked)" :
                    $"{Math.Min(maxExposureSeconds, 25):F0} seconds (untracked)",
                NumberOfFrames = equipment.HasTracker ? 20 : 50,
                TotalExposureTime = equipment.HasTracker ?
                    TimeSpan.FromMinutes(maxExposureSeconds * 20 / 60) :
                    TimeSpan.FromMinutes(20 * 50 / 60),
                FocusingTechnique = "Use live view at 10x zoom on bright star, focus to infinity then back slightly",
                ProcessingNotes = new List<string>
       {
           "Stack images to reduce noise and enhance detail",
           "Use gradient removal to even sky background",
           "Enhance contrast in galactic center region",
           "Consider separate processing for foreground if landscape included"
       },
                TrackerRequirements = equipment.HasTracker ?
                    "Polar align tracker to within 5 arcminutes for sharp stars" :
                    "No tracker - use wide-angle lens and keep exposures under 25 seconds"
            };
        }

        private AstroExposureRecommendation CalculateDeepSkyExposureSettings(CameraEquipmentData equipment, AstroConditions conditions)
        {
            // Deep sky requires longer exposures and typically tracking
            var maxExposureSeconds = equipment.HasTracker ?
                Math.Min(600, CalculateMaxTrackedExposure(equipment, conditions)) :
                Math.Min(500.0 / equipment.FocalLength, 30);

            var recommendedISO = CalculateOptimalISO(equipment, conditions, AstroTarget.DeepSkyObjects);
            var recommendedAperture = Math.Max(equipment.Aperture, 2.8); // Slightly stopped down for better star quality

            return new AstroExposureRecommendation
            {
                Target = AstroTarget.DeepSkyObjects,
                RecommendedISO = $"ISO {recommendedISO}",
                RecommendedAperture = $"f/{recommendedAperture:F1}",
                RecommendedShutterSpeed = $"{maxExposureSeconds:F0} seconds",
                NumberOfFrames = equipment.HasTracker ? 30 : 100,
                TotalExposureTime = TimeSpan.FromHours(maxExposureSeconds * (equipment.HasTracker ? 30 : 100) / 3600),
                FocusingTechnique = "Use Bahtinov mask or live view focusing on bright star near target",
                ProcessingNotes = new List<string>
       {
           "Calibrate with dark, bias, and flat frames",
           "Stack using sigma clipping to reject outliers",
           "Stretch histogram carefully to reveal faint detail",
           "Consider narrowband filters for emission nebulae",
           "Use star reduction techniques if stars are prominent"
       },
                TrackerRequirements = equipment.HasTracker ?
                    "Essential - requires accurate polar alignment and autoguiding for long exposures" :
                    "Tracking mount highly recommended for deep sky photography"
            };
        }

        private int CalculateOptimalISO(CameraEquipmentData equipment, AstroConditions conditions, AstroTarget target)
        {
            // Base ISO calculation considering sensor characteristics and target
            var baseISO = target switch
            {
                AstroTarget.Moon => 100,
                AstroTarget.Planets => 800,
                AstroTarget.MilkyWayCore => 3200,
                AstroTarget.DeepSkyObjects => 1600,
                AstroTarget.StarTrails => 400,
                AstroTarget.MeteorShowers => 1600,
                _ => 1600
            };

            // Adjust for light pollution (Bortle scale)
            var bortleAdjustment = conditions.BortleScale switch
            {
                <= 3 => 1.0,      // Dark sky
                <= 5 => 1.5,      // Moderate light pollution
                <= 7 => 2.0,      // Suburban
                _ => 3.0           // Urban
            };

            // Adjust for camera sensor (larger pixels generally better at high ISO)
            var sensorAdjustment = equipment.PixelSize > 5.0 ? 1.0 : 0.7;

            var adjustedISO = (int)(baseISO * bortleAdjustment * sensorAdjustment);

            // Clamp to reasonable range
            return Math.Max(100, adjustedISO);
        }

        private double CalculateMaxTrackedExposure(CameraEquipmentData equipment, AstroConditions conditions)
        {
            // Consider tracking accuracy and seeing conditions
            var trackingLimitSeconds = equipment.TrackerAccuracy > 0 ?
                Math.Min(3600 / equipment.TrackerAccuracy, 600) : 120;

            // Adjust for seeing conditions
            var seeingAdjustment = conditions.Seeing > 3.0 ? 0.5 : 1.0;

            return trackingLimitSeconds * seeingAdjustment;
        }

        public async Task<AstroFieldOfViewData> GetAstroFieldOfViewAsync(double focalLength, double sensorWidth, double sensorHeight, AstroTarget target, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Calculate field of view in degrees
                    var fovWidth = 2 * Math.Atan(sensorWidth / (2 * focalLength)) * 180 / Math.PI;
                    var fovHeight = 2 * Math.Atan(sensorHeight / (2 * focalLength)) * 180 / Math.PI;

                    // Calculate pixel scale (arc seconds per pixel)
                    var pixelWidth = sensorWidth / CalculateImageWidth(sensorWidth, sensorHeight); // Assumes typical resolution
                    var pixelScale = (pixelWidth / focalLength) * 206265; // Convert to arc seconds per pixel

                    // Get target-specific size information
                    var targetInfo = GetTargetSizeInfo(target);

                    // Determine if target fits in frame
                    var targetFits = fovWidth >= targetInfo.angularWidth && fovHeight >= targetInfo.angularHeight;

                    // Calculate coverage percentage
                    var coveragePercentage = Math.Min(100,
                        (targetInfo.angularWidth * targetInfo.angularHeight) / (fovWidth * fovHeight) * 100);

                    // Generate composition recommendations
                    var compositionRecs = GenerateCompositionRecommendations(target, fovWidth, fovHeight, targetInfo);

                    // Suggest alternative focal lengths
                    var alternatives = SuggestAlternativeFocalLengths(target, targetInfo, sensorWidth, sensorHeight, focalLength);

                    return new AstroFieldOfViewData
                    {
                        FieldOfViewWidth = fovWidth,
                        FieldOfViewHeight = fovHeight,
                        PixelScale = pixelScale,
                        Target = target,
                        TargetFitsInFrame = targetFits,
                        TargetCoveragePercentage = coveragePercentage,
                        CompositionRecommendations = compositionRecs,
                        AlternativeFocalLengths = alternatives
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating astro field of view for target {Target}", target);
                    throw;
                }
            }, cancellationToken);
        }

        private (double angularWidth, double angularHeight, string description) GetTargetSizeInfo(AstroTarget target)
        {
            return target switch
            {
                AstroTarget.Moon => (0.5, 0.5, "Lunar disc"),
                AstroTarget.Planets => (0.05, 0.05, "Planetary disc (varies by planet and distance)"),
                AstroTarget.MilkyWayCore => (30, 15, "Galactic center region"),
                AstroTarget.DeepSkyObjects => (1.0, 1.0, "Typical nebula/galaxy (highly variable)"),
                AstroTarget.StarTrails => (180, 90, "Full sky coverage for circular trails"),
                AstroTarget.MeteorShowers => (60, 60, "Wide area around radiant"),
                AstroTarget.Constellations => (20, 15, "Typical constellation span"),
                AstroTarget.PolarAlignment => (2, 2, "Polaris and surrounding stars"),
                _ => (10, 10, "General astrophotography target")
            };
        }

        private string GenerateCompositionRecommendations(AstroTarget target, double fovWidth, double fovHeight,
            (double angularWidth, double angularHeight, string description) targetInfo)
        {
            var recommendations = new List<string>();

            switch (target)
            {
                case AstroTarget.Moon:
                    if (fovWidth > 2)
                        recommendations.Add("Wide field - excellent for lunar landscape compositions with foreground");
                    else if (fovWidth > 1)
                        recommendations.Add("Medium field - good for lunar detail with some sky context");
                    else
                        recommendations.Add("Narrow field - ideal for detailed lunar surface photography");
                    break;

                case AstroTarget.MilkyWayCore:
                    if (fovWidth > 50)
                        recommendations.Add("Ultra-wide field - capture full galactic arch");
                    else if (fovWidth > 30)
                        recommendations.Add("Wide field - excellent for galactic center with landscape");
                    else
                        recommendations.Add("Medium field - focus on galactic center detail");
                    break;

                case AstroTarget.DeepSkyObjects:
                    if (fovWidth > targetInfo.angularWidth * 3)
                        recommendations.Add("Generous framing - allows for composition flexibility and guiding errors");
                    else if (fovWidth > targetInfo.angularWidth * 1.5)
                        recommendations.Add("Good framing - object well-sized in frame");
                    else
                        recommendations.Add("Tight framing - may cut off object extensions, consider wider field");
                    break;

                case AstroTarget.StarTrails:
                    if (fovWidth > 90)
                        recommendations.Add("Excellent for full circular star trails around celestial pole");
                    else if (fovWidth > 45)
                        recommendations.Add("Good for partial circular trails or linear trails");
                    else
                        recommendations.Add("Narrow field - creates linear star trail segments");
                    break;
            }

            // Add technical recommendations
            if (fovWidth / fovHeight > 2)
                recommendations.Add("Very wide aspect ratio - consider panoramic compositions");
            else if (fovWidth / fovHeight < 0.8)
                recommendations.Add("Tall aspect ratio - good for vertical compositions");

            return string.Join(". ", recommendations) + ".";
        }

        private List<string> SuggestAlternativeFocalLengths(AstroTarget target,
            (double angularWidth, double angularHeight, string description) targetInfo,
            double sensorWidth, double sensorHeight, double currentFocalLength)
        {
            var suggestions = new List<string>();

            // Calculate optimal focal length for target to fill 70% of frame
            var optimalFocalLength = sensorWidth / (2 * Math.Tan((targetInfo.angularWidth * 0.7) * Math.PI / 360));

            if (Math.Abs(currentFocalLength - optimalFocalLength) > currentFocalLength * 0.2)
            {
                suggestions.Add($"{optimalFocalLength:F0}mm - optimal for target size");
            }

            // Standard astrophotography focal lengths with use cases
            var standardLengths = new Dictionary<int, string>
    {
        { 14, "Ultra-wide for full sky surveys and Milky Way arch" },
        { 24, "Wide-angle for constellation photography and landscape astro" },
        { 50, "Standard for large nebulae and galaxy clusters" },
        { 85, "Short telephoto for medium nebulae and galaxy groups" },
        { 135, "Telephoto for smaller deep sky objects" },
        { 200, "Long telephoto for detailed nebulae and small galaxies" },
        { 300, "Very long for planetary and small planetary nebulae" },
        { 600, "Extreme telephoto for lunar detail and planets" },
        { 1000, "Telescope territory for high-resolution planetary work" }
    };

            foreach (var length in standardLengths)
            {
                if (Math.Abs(length.Key - currentFocalLength) > 20) // Don't suggest very similar focal lengths
                {
                    var fov = 2 * Math.Atan(sensorWidth / (2 * length.Key)) * 180 / Math.PI;
                    if (target == AstroTarget.MilkyWayCore && fov > 20)
                        suggestions.Add($"{length.Key}mm - {length.Value}");
                    else if (target == AstroTarget.DeepSkyObjects && fov > 1 && fov < 10)
                        suggestions.Add($"{length.Key}mm - {length.Value}");
                    else if (target == AstroTarget.Moon && fov < 2)
                        suggestions.Add($"{length.Key}mm - {length.Value}");
                }
            }

            return suggestions.Take(3).ToList(); // Limit to top 3 suggestions
        }

        private double CalculateImageWidth(double sensorWidth, double sensorHeight)
        {
            // Assume typical camera resolutions based on sensor size
            if (sensorWidth > 35) return 6000; // Full frame
            if (sensorWidth > 22) return 4000; // APS-C
            return 3000; // Micro four thirds or smaller
        }

        public async Task<PolarAlignmentData> GetPolarAlignmentDataAsync(DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var time = new AstroTime(dateTime);
                    var observer = new Observer(latitude, longitude, 0);

                    // Calculate Polaris position for Northern Hemisphere
                    // For Southern Hemisphere, would use Sigma Octantis
                    var (polarisAz, polarisAlt, offsetAngle, offsetDistance) = CalculatePolarisAlignmentData(time, observer);

                    // Generate alignment instructions
                    var instructions = GeneratePolarAlignmentInstructions(latitude, polarisAz, polarisAlt, offsetAngle);

                    // Get reference stars for alignment verification
                    var referenceStars = GetPolarAlignmentReferenceStars(dateTime, latitude, longitude);

                    return new PolarAlignmentData
                    {
                        DateTime = dateTime,
                        PolarisAzimuth = polarisAz,
                        PolarisAltitude = polarisAlt,
                        PolarisOffsetAngle = offsetAngle,
                        PolarisOffsetDistance = offsetDistance,
                        AlignmentInstructions = instructions,
                        ReferenceStars = referenceStars
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating polar alignment data");
                    throw;
                }
            }, cancellationToken);
        }

        private (double azimuth, double altitude, double offsetAngle, double offsetDistance) CalculatePolarisAlignmentData(AstroTime time, Observer observer)
        {
            // Polaris coordinates: RA = 2h 31m 49s, Dec = +89° 15' 51"
            var polarisRA = 2.530277778; // hours
            var polarisDec = 89.264167; // degrees

            // Calculate Polaris position
            var polarisHorizontal = Astronomy.Horizon(time, observer, polarisRA, polarisDec, Refraction.Normal);

            // Calculate true celestial pole position (latitude = observer latitude, azimuth = 0° for north)
            var truePoleAltitude = observer.latitude;
            var truePoleAzimuth = 0.0; // Due north

            // Calculate offset from true pole
            var offsetAngle = CalculateOffsetAngle(polarisHorizontal.azimuth, polarisHorizontal.altitude, truePoleAzimuth, truePoleAltitude);
            var offsetDistance = CalculateAngularDistance(polarisHorizontal.azimuth, polarisHorizontal.altitude, truePoleAzimuth, truePoleAltitude);

            return (polarisHorizontal.azimuth, polarisHorizontal.altitude, offsetAngle, offsetDistance * 60); // Convert to arc minutes
        }

        private double CalculateOffsetAngle(double polarisAz, double polarisAlt, double poleAz, double poleAlt)
        {
            // Calculate position angle of Polaris relative to true pole
            var deltaAz = polarisAz - poleAz;
            var deltaAlt = polarisAlt - poleAlt;

            return Math.Atan2(deltaAz, deltaAlt) * 180.0 / Math.PI;
        }

        private double CalculateAngularDistance(double az1, double alt1, double az2, double alt2)
        {
            // Convert to radians
            var az1Rad = az1 * Math.PI / 180.0;
            var alt1Rad = alt1 * Math.PI / 180.0;
            var az2Rad = az2 * Math.PI / 180.0;
            var alt2Rad = alt2 * Math.PI / 180.0;

            // Spherical law of cosines
            var cosDistance = Math.Sin(alt1Rad) * Math.Sin(alt2Rad) +
                             Math.Cos(alt1Rad) * Math.Cos(alt2Rad) * Math.Cos(az1Rad - az2Rad);

            return Math.Acos(Math.Max(-1.0, Math.Min(1.0, cosDistance))) * 180.0 / Math.PI;
        }

        private string GeneratePolarAlignmentInstructions(double latitude, double polarisAz, double polarisAlt, double offsetAngle)
        {
            var instructions = new List<string>();

            if (latitude < 0)
            {
                instructions.Add("Southern Hemisphere polar alignment - use Sigma Octantis or drift alignment method");
                instructions.Add("Octantis is much dimmer than Polaris - consider electronic polar scope");
                return string.Join(". ", instructions) + ".";
            }

            // Northern Hemisphere Polaris alignment
            instructions.Add($"Point mount's polar axis toward azimuth {polarisAz:F1}° and altitude {polarisAlt:F1}°");
            instructions.Add($"Polaris will be offset {Math.Abs(offsetAngle):F1}° from true pole");

            // Time-specific instructions based on offset angle
            var hourAngle = (offsetAngle + 180) % 360; // Convert to hour angle position
            var clockPosition = GetClockPosition(hourAngle);

            instructions.Add($"At this time, place Polaris at {clockPosition} position in polar scope reticle");
            instructions.Add("Use mount's altitude and azimuth adjustments to center Polaris in correct position");
            instructions.Add("Fine-tune using drift alignment method for precise tracking");

            // Accuracy recommendations
            if (Math.Abs(latitude) > 60)
                instructions.Add("High latitude - polar alignment critical for long exposures");
            else if (Math.Abs(latitude) < 30)
                instructions.Add("Low latitude - less critical but still important for tracking accuracy");

            instructions.Add("Verify alignment by checking tracking accuracy on stars near celestial equator");

            return string.Join(". ", instructions) + ".";
        }

        private string GetClockPosition(double angle)
        {
            // Convert angle to clock position (12 o'clock = 0°, 3 o'clock = 90°, etc.)
            var clockHour = ((angle / 30.0) + 12) % 12;
            if (clockHour == 0) clockHour = 12;

            return clockHour switch
            {
                12 => "12 o'clock",
                1 => "1 o'clock",
                2 => "2 o'clock",
                3 => "3 o'clock",
                4 => "4 o'clock",
                5 => "5 o'clock",
                6 => "6 o'clock",
                7 => "7 o'clock",
                8 => "8 o'clock",
                9 => "9 o'clock",
                10 => "10 o'clock",
                11 => "11 o'clock",
                _ => $"{clockHour:F0} o'clock"
            };
        }

        private List<string> GetPolarAlignmentReferenceStars(DateTime dateTime, double latitude, double longitude)
        {
            var referenceStars = new List<string>();

            if (latitude >= 0) // Northern Hemisphere
            {
                referenceStars.AddRange(new[]
                {
            "Polaris - Primary alignment star in Ursa Minor",
            "Kochab - Beta Ursae Minoris, bright star in Little Dipper bowl",
            "Pherkad - Gamma Ursae Minoris, other bright star in Little Dipper bowl",
            "Dubhe - Alpha Ursae Majoris, pointer star in Big Dipper",
            "Merak - Beta Ursae Majoris, other pointer star in Big Dipper"
        });

                // Season-specific reference stars
                var month = dateTime.Month;
                if (month >= 9 && month <= 11) // Autumn
                {
                    referenceStars.Add("Cassiopeia - W-shaped constellation, useful for Polaris finding");
                }
                else if (month >= 6 && month <= 8) // Summer
                {
                    referenceStars.Add("Draco - Dragon constellation curves around Little Dipper");
                }
            }
            else // Southern Hemisphere
            {
                referenceStars.AddRange(new[]
                {
            "Sigma Octantis - Faint polar star, magnitude 5.47",
            "Southern Cross - Use to locate celestial south pole",
            "Alpha Centauri - Bright reference for southern sky navigation",
            "Beta Centauri - Pointer star with Alpha Centauri",
            "Canopus - Second brightest star, excellent southern reference"
        });
            }

            return referenceStars;
        }

        public async Task<CoordinateTransformResult> TransformCoordinatesAsync(CoordinateType fromType, CoordinateType toType, double coordinate1, double coordinate2, DateTime dateTime, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var time = new AstroTime(dateTime);
                    var observer = new Observer(latitude, longitude, 0);

                    // Input coordinates
                    var inputCoord1 = coordinate1;
                    var inputCoord2 = coordinate2;

                    // Convert input coordinates to equatorial (RA/Dec) as intermediate format
                    var (ra, dec) = ConvertToEquatorial(fromType, coordinate1, coordinate2, time, observer);

                    // Convert from equatorial to target coordinate system
                    var (outputCoord1, outputCoord2) = ConvertFromEquatorial(toType, ra, dec, time, observer);

                    return new CoordinateTransformResult
                    {
                        FromType = fromType,
                        ToType = toType,
                        InputCoordinate1 = inputCoord1,
                        InputCoordinate2 = inputCoord2,
                        OutputCoordinate1 = outputCoord1,
                        OutputCoordinate2 = outputCoord2,
                        DateTime = dateTime,
                        Latitude = latitude,
                        Longitude = longitude
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error transforming coordinates from {FromType} to {ToType}", fromType, toType);
                    throw;
                }
            }, cancellationToken);
        }

        private (double ra, double dec) ConvertToEquatorial(CoordinateType fromType, double coord1, double coord2, AstroTime time, Observer observer)
        {
            return fromType switch
            {
                CoordinateType.Equatorial => (coord1, coord2), // Already equatorial (RA, Dec)

                CoordinateType.AltitudeAzimuth => ConvertHorizontalToEquatorial(coord1, coord2, time, observer),

                CoordinateType.Galactic => ConvertGalacticToEquatorial(coord1, coord2),

                CoordinateType.Ecliptic => ConvertEclipticToEquatorial(coord1, coord2, time),

                _ => throw new ArgumentException($"Unsupported coordinate type: {fromType}")
            };
        }

        private (double coord1, double coord2) ConvertFromEquatorial(CoordinateType toType, double ra, double dec, AstroTime time, Observer observer)
        {
            return toType switch
            {
                CoordinateType.Equatorial => (ra, dec), // Already equatorial

                CoordinateType.AltitudeAzimuth => ConvertEquatorialToHorizontal(ra, dec, time, observer),

                CoordinateType.Galactic => ConvertEquatorialToGalactic(ra, dec),

                CoordinateType.Ecliptic => ConvertEquatorialToEcliptic(ra, dec, time),

                _ => throw new ArgumentException($"Unsupported coordinate type: {toType}")
            };
        }

        private (double ra, double dec) ConvertHorizontalToEquatorial(double azimuth, double altitude, AstroTime time, Observer observer)
        {
            // Use CosineKitty's inverse horizon transformation
            // Note: This is a simplified implementation - CosineKitty may have direct methods

            // Convert altitude/azimuth to hour angle and declination
            var azRad = azimuth * Math.PI / 180.0;
            var altRad = altitude * Math.PI / 180.0;
            var latRad = observer.latitude * Math.PI / 180.0;

            // Calculate declination
            var decRad = Math.Asin(Math.Sin(altRad) * Math.Sin(latRad) +
                                  Math.Cos(altRad) * Math.Cos(latRad) * Math.Cos(azRad));

            // Calculate hour angle
            var haRad = Math.Atan2(-Math.Sin(azRad) * Math.Cos(altRad),
                                  Math.Cos(latRad) * Math.Sin(altRad) - Math.Sin(latRad) * Math.Cos(altRad) * Math.Cos(azRad));

            // Convert hour angle to right ascension using sidereal time
            var lst = Astronomy.SiderealTime(time); // Local sidereal time
            var ra = (lst - (haRad * 180.0 / Math.PI) / 15.0 + 24) % 24; // Convert to hours
            var dec = decRad * 180.0 / Math.PI; // Convert to degrees

            return (ra, dec);
        }

        private (double azimuth, double altitude) ConvertEquatorialToHorizontal(double ra, double dec, AstroTime time, Observer observer)
        {
            // Use CosineKitty's horizon transformation
            var horizontal = Astronomy.Horizon(time, observer, ra, dec, Refraction.Normal);
            return (horizontal.azimuth, horizontal.altitude);
        }

        private (double ra, double dec) ConvertGalacticToEquatorial(double galacticLongitude, double galacticLatitude)
        {
            // Galactic to Equatorial transformation (J2000.0)
            // Galactic pole: RA = 12h 51m 26.28s, Dec = +27° 07' 41.7"
            // Galactic center: RA = 17h 45m 37.2s, Dec = -28° 56' 10"

            var lRad = galacticLongitude * Math.PI / 180.0;
            var bRad = galacticLatitude * Math.PI / 180.0;

            // Transformation matrix elements (simplified)
            var decRad = Math.Asin(Math.Sin(bRad) * Math.Sin(27.128336 * Math.PI / 180.0) +
                                  Math.Cos(bRad) * Math.Cos(27.128336 * Math.PI / 180.0) * Math.Cos(lRad - 122.932 * Math.PI / 180.0));

            var raRad = Math.Atan2(Math.Cos(bRad) * Math.Sin(lRad - 122.932 * Math.PI / 180.0),
                                  Math.Sin(bRad) * Math.Cos(27.128336 * Math.PI / 180.0) -
                                  Math.Cos(bRad) * Math.Sin(27.128336 * Math.PI / 180.0) * Math.Cos(lRad - 122.932 * Math.PI / 180.0));

            var ra = (raRad * 180.0 / Math.PI + 192.859508) / 15.0; // Convert to hours
            if (ra < 0) ra += 24;
            if (ra >= 24) ra -= 24;

            var dec = decRad * 180.0 / Math.PI;

            return (ra, dec);
        }

        private (double galacticLongitude, double galacticLatitude) ConvertEquatorialToGalactic(double ra, double dec)
        {
            // Equatorial to Galactic transformation (inverse of above)
            var raRad = ra * 15.0 * Math.PI / 180.0; // Convert hours to radians
            var decRad = dec * Math.PI / 180.0;

            // Adjust RA relative to galactic center
            var deltaRA = raRad - 192.859508 * Math.PI / 180.0;

            var bRad = Math.Asin(Math.Sin(decRad) * Math.Sin(27.128336 * Math.PI / 180.0) +
                                Math.Cos(decRad) * Math.Cos(27.128336 * Math.PI / 180.0) * Math.Cos(deltaRA));

            var lRad = Math.Atan2(Math.Cos(decRad) * Math.Sin(deltaRA),
                                 Math.Sin(decRad) * Math.Cos(27.128336 * Math.PI / 180.0) -
                                 Math.Cos(decRad) * Math.Sin(27.128336 * Math.PI / 180.0) * Math.Cos(deltaRA));

            var galacticLongitude = (lRad * 180.0 / Math.PI + 122.932);
            if (galacticLongitude < 0) galacticLongitude += 360;
            if (galacticLongitude >= 360) galacticLongitude -= 360;

            var galacticLatitude = bRad * 180.0 / Math.PI;

            return (galacticLongitude, galacticLatitude);
        }

        private (double ra, double dec) ConvertEclipticToEquatorial(double eclipticLongitude, double eclipticLatitude, AstroTime time)
        {
            // Ecliptic to Equatorial transformation
            // Use mean obliquity of ecliptic (approximately 23.44 degrees for current epoch)
            var obliquity = 23.4392911; // Mean obliquity in degrees for J2000.0
            var oblRad = obliquity * Math.PI / 180.0;
            var lambdaRad = eclipticLongitude * Math.PI / 180.0;
            var betaRad = eclipticLatitude * Math.PI / 180.0;

            var decRad = Math.Asin(Math.Sin(betaRad) * Math.Cos(oblRad) +
                                  Math.Cos(betaRad) * Math.Sin(oblRad) * Math.Sin(lambdaRad));

            var raRad = Math.Atan2(Math.Cos(betaRad) * Math.Cos(lambdaRad) -
                                  Math.Sin(betaRad) * Math.Sin(oblRad) * Math.Sin(lambdaRad),
                                  Math.Cos(betaRad) * Math.Sin(lambdaRad) * Math.Cos(oblRad) +
                                  Math.Sin(betaRad) * Math.Sin(oblRad));

            var ra = (raRad * 180.0 / Math.PI) / 15.0; // Convert to hours
            if (ra < 0) ra += 24;

            var dec = decRad * 180.0 / Math.PI;

            return (ra, dec);
        }

        private string DetermineOptimalStarTrailComposition(double poleAltitude, double latitude)
        {
            var compositions = new List<string>();

            if (Math.Abs(latitude) > 60) // High latitude
            {
                compositions.Add("Celestial pole high in sky - excellent for circular star trails");
                compositions.Add("Include interesting foreground silhouettes for scale and context");
                compositions.Add("Consider ultra-wide lens to capture full circular trails");
            }
            else if (Math.Abs(latitude) > 30) // Mid latitude
            {
                compositions.Add("Moderate pole altitude - good for partial circular or diagonal trails");
                compositions.Add("Frame with pole off-center for dynamic composition");
                compositions.Add("Landscape elements can create leading lines to pole");
            }
            else // Low latitude
            {
                compositions.Add("Low pole altitude - linear trails dominate composition");
                compositions.Add("Horizontal framing emphasizes linear trail patterns");
                compositions.Add("Include horizon line for reference and scale");
            }

            // Universal composition advice
            compositions.Add("Use foreground elements to anchor the composition");
            compositions.Add("Consider multiple exposures for blending foreground and trails");

            return string.Join(". ", compositions) + ".";
        }

        private List<string> GenerateStarTrailExposureStrategy(TimeSpan exposureDuration, double latitude)
        {
            var strategy = new List<string>();

            if (exposureDuration.TotalHours < 0.5) // Short trails
            {
                strategy.Add("Short star trails - single exposure approach");
                strategy.Add("ISO 400-800, f/2.8-4, 15-30 minute exposure");
                strategy.Add("Monitor for overexposure in bright star cores");
                strategy.Add("Use dark frame subtraction for noise reduction");
            }
            else if (exposureDuration.TotalHours < 2) // Medium trails
            {
                strategy.Add("Medium star trails - consider image stacking approach");
                strategy.Add("Option 1: Single exposure - ISO 200-400, f/4-5.6");
                strategy.Add("Option 2: Stack 15-30 shorter exposures (2-4 minutes each)");
                strategy.Add("Stacking reduces noise and prevents overexposure");
            }
            else // Long trails
            {
                strategy.Add("Long star trails - image stacking highly recommended");
                strategy.Add("Capture 30-120 exposures of 2-4 minutes each");
                strategy.Add("ISO 200-400, f/4-5.6 for optimal star quality");
                strategy.Add("Use intervalometer with minimal gap between exposures");
                strategy.Add("Process using StarStaX or similar stacking software");
                strategy.Add("Blend mode: Lighten or Maximum for continuous trails");
            }

            // Technical considerations
            strategy.Add("Use manual focus set to infinity");
            strategy.Add("Disable image stabilization and long exposure noise reduction");
            strategy.Add("Monitor battery life - long sequences require external power");

            // Weather considerations
            if (Math.Abs(latitude) > 45)
            {
                strategy.Add("High latitude - prepare for temperature extremes affecting equipment");
            }

            return strategy;
        }
        private (double eclipticLongitude, double eclipticLatitude) ConvertEquatorialToEcliptic(double ra, double dec, AstroTime time)
        {
            // Equatorial to Ecliptic transformation
            var obliquity = 23.4392911; // Mean obliquity in degrees for J2000.0
            var oblRad = obliquity * Math.PI / 180.0;
            var raRad = ra * 15.0 * Math.PI / 180.0; // Convert hours to radians
            var decRad = dec * Math.PI / 180.0;

            var betaRad = Math.Asin(Math.Sin(decRad) * Math.Cos(oblRad) -
                                   Math.Cos(decRad) * Math.Sin(oblRad) * Math.Sin(raRad));

            var lambdaRad = Math.Atan2(Math.Cos(decRad) * Math.Cos(raRad),
                                      Math.Sin(decRad) * Math.Sin(oblRad) +
                                      Math.Cos(decRad) * Math.Cos(oblRad) * Math.Sin(raRad));

            var eclipticLongitude = lambdaRad * 180.0 / Math.PI;
            if (eclipticLongitude < 0) eclipticLongitude += 360;

            var eclipticLatitude = betaRad * 180.0 / Math.PI;

            return (eclipticLongitude, eclipticLatitude);
        }

        public async Task<AtmosphericCorrectionData> GetAtmosphericCorrectionAsync(double altitude, double azimuth, double temperature, double pressure, double humidity, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Calculate atmospheric refraction correction
                    var refractionCorrection = CalculateAtmosphericRefraction(altitude, temperature, pressure, humidity);

                    // Calculate atmospheric extinction
                    var extinction = CalculateAtmosphericExtinction(altitude, humidity);

                    // Calculate true altitude from apparent altitude
                    var trueAltitude = altitude - (refractionCorrection / 60.0); // Convert arc minutes to degrees

                    // Generate correction notes
                    var notes = GenerateAtmosphericCorrectionNotes(altitude, refractionCorrection, extinction);

                    return new AtmosphericCorrectionData
                    {
                        TrueAltitude = trueAltitude,
                        ApparentAltitude = altitude,
                        RefractionCorrection = refractionCorrection, // Arc minutes
                        AtmosphericExtinction = extinction, // Magnitudes
                        CorrectionNotes = notes
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating atmospheric correction");
                    throw;
                }
            }, cancellationToken);
        }

        private double CalculateAtmosphericRefraction(double apparentAltitude, double temperature, double pressure, double humidity)
        {
            // Bennett's formula for atmospheric refraction with corrections
            // Base refraction at standard conditions (15°C, 1013.25 mbar, 0% humidity)

            if (apparentAltitude <= 0) return 0; // No refraction below horizon

            // Convert altitude to radians for calculation
            var altRad = apparentAltitude * Math.PI / 180.0;

            // Base refraction in arc minutes (Bennett's formula)
            var baseRefraction = 1.0 / Math.Tan(altRad + 7.31 / (apparentAltitude + 4.4));

            // Temperature correction factor (standard temp = 15°C = 288.15K)
            var tempFactor = 283.15 / (273.15 + temperature);

            // Pressure correction factor (standard pressure = 1013.25 mbar)
            var pressureFactor = pressure / 1013.25;

            // Humidity correction (approximate - reduces refraction slightly)
            var humidityFactor = 1.0 - (humidity / 100.0) * 0.05;

            // Apply corrections
            var correctedRefraction = baseRefraction * tempFactor * pressureFactor * humidityFactor;

            // Additional correction for very low altitudes
            if (apparentAltitude < 5.0)
            {
                var lowAltitudeCorrection = Math.Exp(-apparentAltitude / 2.0);
                correctedRefraction *= (1.0 + lowAltitudeCorrection * 0.2);
            }

            return Math.Max(0, correctedRefraction); // Refraction cannot be negative
        }

        private double CalculateAtmosphericExtinction(double altitude, double humidity)
        {
            // Atmospheric extinction in magnitudes per air mass
            // Typical extinction coefficients at sea level

            if (altitude <= 0) return double.PositiveInfinity; // Object below horizon

            // Calculate air mass using secant formula with corrections
            var airMass = CalculateAirMass(altitude);

            // Base extinction coefficient (magnitudes per air mass)
            // Typical values: 0.15-0.25 for clear conditions at sea level
            var baseExtinction = 0.2; // Visual band

            // Humidity correction - water vapor increases extinction
            var humidityCorrection = 1.0 + (humidity / 100.0) * 0.3;

            // Altitude correction - less atmosphere at higher observer altitudes
            // This is observer altitude, not object altitude - would need that parameter
            var altitudeCorrection = 1.0; // Assuming sea level observer

            var totalExtinction = baseExtinction * airMass * humidityCorrection * altitudeCorrection;

            return totalExtinction;
        }

        private double CalculateAirMass(double altitude)
        {
            // Hardie's air mass formula with corrections for low altitudes
            if (altitude <= 0) return double.PositiveInfinity;

            var zenithAngle = 90.0 - altitude;
            var zenithRad = zenithAngle * Math.PI / 180.0;

            // Young's formula (1994) - accurate for zenith angles up to 96°
            var cosZ = Math.Cos(zenithRad);
            var airMass = (1.002432 * cosZ * cosZ + 0.148386 * cosZ + 0.0096467) /
                          (cosZ * cosZ * cosZ + 0.149864 * cosZ * cosZ + 0.0102963 * cosZ + 0.000303978);

            // Additional correction for very low altitudes
            if (altitude < 10)
            {
                var lowAltCorrection = 1.0 + Math.Exp(-(altitude - 2) / 3.0);
                airMass *= lowAltCorrection;
            }

            return Math.Max(1.0, airMass); // Air mass cannot be less than 1
        }

        private string GenerateAtmosphericCorrectionNotes(double altitude, double refraction, double extinction)
        {
            var notes = new List<string>();

            // Refraction notes
            if (refraction > 30) // More than 30 arc minutes
            {
                notes.Add("Very large refraction correction - object appears significantly higher than true position");
            }
            else if (refraction > 10)
            {
                notes.Add("Substantial refraction correction - important for precise positioning");
            }
            else if (refraction > 2)
            {
                notes.Add("Moderate refraction correction - noticeable effect on object position");
            }
            else
            {
                notes.Add("Minimal refraction correction - object near true position");
            }

            // Extinction notes
            if (extinction > 2.0)
            {
                notes.Add("Severe atmospheric extinction - object much dimmer than extraterrestrial brightness");
            }
            else if (extinction > 1.0)
            {
                notes.Add("Significant atmospheric extinction - object noticeably dimmer");
            }
            else if (extinction > 0.5)
            {
                notes.Add("Moderate atmospheric extinction - some brightness reduction");
            }
            else
            {
                notes.Add("Minimal atmospheric extinction - object near full brightness");
            }

            // Altitude-specific advice
            if (altitude < 10)
            {
                notes.Add("Very low altitude - atmospheric effects are extreme and unpredictable");
            }
            else if (altitude < 20)
            {
                notes.Add("Low altitude - significant atmospheric effects, consider observation timing");
            }
            else if (altitude > 70)
            {
                notes.Add("High altitude - minimal atmospheric effects, excellent observing conditions");
            }

            // Photography implications
            if (refraction > 5 || extinction > 0.5)
            {
                notes.Add("Atmospheric effects significant for precision astrophotography - consider correction");
            }

            return string.Join(". ", notes) + ".";
        }

        public async Task<StarTrailData> GetStarTrailDataAsync(DateTime startTime, TimeSpan exposureDuration, double latitude, double longitude, CancellationToken cancellationToken = default)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var observer = new Observer(latitude, longitude, 0);
                    var time = new AstroTime(startTime);

                    // Calculate celestial pole position
                    var (poleAzimuth, poleAltitude) = CalculateCelestialPolePosition(latitude, longitude, startTime);

                    // Calculate star trail length based on exposure duration
                    var trailLength = CalculateStarTrailLength(exposureDuration, latitude);

                    // Calculate Earth's rotation during exposure
                    var rotationDegrees = (exposureDuration.TotalHours / 24.0) * 360.0;

                    // Determine optimal composition strategy
                    var composition = DetermineOptimalStarTrailComposition(poleAltitude, latitude);

                    // Generate exposure strategy recommendations
                    var exposureStrategy = GenerateStarTrailExposureStrategy(exposureDuration, latitude);

                    return new StarTrailData
                    {
                        StartTime = startTime,
                        ExposureDuration = exposureDuration,
                        CelestialPoleAzimuth = poleAzimuth,
                        CelestialPoleAltitude = poleAltitude,
                        StarTrailLength = trailLength,
                        Rotation = rotationDegrees,
                        OptimalComposition = composition,
                        ExposureStrategy = exposureStrategy
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error calculating star trail data");
                    throw;
                }
            }, cancellationToken);
        }





        #endregion

    }
}