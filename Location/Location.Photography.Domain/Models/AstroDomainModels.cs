// Location.Photography.Domain/Models/AstroDomainModels.cs
namespace Location.Photography.Domain.Models
{
    // === ENUMS ===

    public enum PlanetType
    {
        Mercury,
        Venus,
        Mars,
        Jupiter,
        Saturn,
        Uranus,
        Neptune,
        Pluto
    }

    public enum ConstellationType
    {
        Orion,
        Cassiopeia,
        UrsaMajor,
        UrsaMinor,
        Draco,
        Cygnus,
        Lyra,
        Aquila,
        Sagittarius,
        Scorpius,
        Centaurus,
        Crux,
        Andromeda,
        Perseus,
        Auriga,
        Gemini,
        Cancer,
        Leo,
        Virgo,
        Libra,
        Capricornus,
        Aquarius,
        Pisces,
        Aries,
        Taurus
    }

    public enum MeteorShowerType
    {
        Quadrantids,
        Lyrids,
        EtaAquariids,
        Perseids,
        Draconids,
        Orionids,
        Leonids,
        Geminids,
        Ursids
    }

    public enum AstroTarget
    {
        Moon,
        Planets,
        MilkyWayCore,
        DeepSkyObjects,
        StarTrails,
        Comets,
        MeteorShowers,
        PolarAlignment,
        Constellations,
        NorthernLights
    }

    public enum CoordinateType
    {
        Equatorial,
        Galactic,
        AltitudeAzimuth,
        Ecliptic
    }

    // === PLANETARY MODELS ===

    public class PlanetPositionData
    {
        public PlanetType Planet { get; set; }
        public DateTime DateTime { get; set; }
        public double RightAscension { get; set; } // Hours
        public double Declination { get; set; } // Degrees
        public double Azimuth { get; set; } // Degrees
        public double Altitude { get; set; } // Degrees
        public double Distance { get; set; } // AU
        public double ApparentMagnitude { get; set; }
        public double AngularDiameter { get; set; } // Arc seconds
        public bool IsVisible { get; set; }
        public DateTime? Rise { get; set; }
        public DateTime? Set { get; set; }
        public DateTime? Transit { get; set; }
        public string RecommendedEquipment { get; set; } = string.Empty;
        public string PhotographyNotes { get; set; } = string.Empty;
    }

    public class PlanetaryConjunction
    {
        public DateTime DateTime { get; set; }
        public PlanetType Planet1 { get; set; }
        public PlanetType Planet2 { get; set; }
        public double Separation { get; set; } // Arc minutes
        public double Altitude { get; set; } // Degrees above horizon
        public double Azimuth { get; set; } // Degrees
        public string VisibilityDescription { get; set; } = string.Empty;
        public string PhotographyRecommendation { get; set; } = string.Empty;
    }

    public class PlanetaryEvent
    {
        public DateTime DateTime { get; set; }
        public PlanetType Planet { get; set; }
        public string EventType { get; set; } = string.Empty; // Opposition, Greatest Elongation, etc.
        public double ApparentMagnitude { get; set; }
        public double AngularDiameter { get; set; }
        public string OptimalViewingConditions { get; set; } = string.Empty;
        public string EquipmentRecommendations { get; set; } = string.Empty;
    }

    // === ENHANCED LUNAR MODELS ===

    public class EnhancedMoonData
    {
        public DateTime DateTime { get; set; }
        public double Phase { get; set; } // 0-1
        public string PhaseName { get; set; } = string.Empty;
        public double Illumination { get; set; } // Percentage
        public double Azimuth { get; set; }
        public double Altitude { get; set; }
        public double Distance { get; set; } // km
        public double AngularDiameter { get; set; } // Arc minutes
        public DateTime? Rise { get; set; }
        public DateTime? Set { get; set; }
        public DateTime? Transit { get; set; }

        // Libration data for lunar photography
        public double LibrationLatitude { get; set; } // Degrees
        public double LibrationLongitude { get; set; } // Degrees
        public double PositionAngle { get; set; } // Degrees

        // Photography-specific data
        public bool IsSupermoon { get; set; }
        public double OpticalLibration { get; set; }
        public string OptimalPhotographyPhase { get; set; } = string.Empty;
        public List<string> VisibleFeatures { get; set; } = new();
        public string RecommendedExposureSettings { get; set; } = string.Empty;
    }

    public class SupermoonEvent
    {
        public DateTime DateTime { get; set; }
        public double Distance { get; set; } // km
        public double AngularDiameter { get; set; } // Arc minutes
        public double PercentLarger { get; set; } // Compared to average
        public string EventName { get; set; } = string.Empty;
        public string PhotographyOpportunity { get; set; } = string.Empty;
    }

    public class LunarEclipseData
    {
        public DateTime DateTime { get; set; }
        public string EclipseType { get; set; } = string.Empty; // Total, Partial, Penumbral
        public DateTime PenumbralBegin { get; set; }
        public DateTime? PartialBegin { get; set; }
        public DateTime? TotalityBegin { get; set; }
        public DateTime? Maximum { get; set; }
        public DateTime? TotalityEnd { get; set; }
        public DateTime? PartialEnd { get; set; }
        public DateTime PenumbralEnd { get; set; }
        public double Magnitude { get; set; }
        public bool IsVisible { get; set; }
        public string PhotographyPlanning { get; set; } = string.Empty;
        public List<string> ExposureRecommendations { get; set; } = new();
    }

    public class LunarPhotographyWindow
    {
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public double Phase { get; set; }
        public string PhaseName { get; set; } = string.Empty;
        public double OptimalAltitude { get; set; }
        public string PhotographyType { get; set; } = string.Empty; // Craters, Terminator, Full disk
        public string RecommendedSettings { get; set; } = string.Empty;
        public List<string> VisibleFeatures { get; set; } = new();
        public double QualityScore { get; set; } // 0-1
    }

    // === DEEP SKY MODELS ===

    public class MilkyWayData
    {
        public DateTime DateTime { get; set; }
        public double GalacticCenterAzimuth { get; set; }
        public double GalacticCenterAltitude { get; set; }
        public bool IsVisible { get; set; }
        public DateTime? Rise { get; set; }
        public DateTime? Set { get; set; }
        public DateTime? OptimalViewingTime { get; set; }
        public string Season { get; set; } = string.Empty;
        public double DarkSkyQuality { get; set; } // 0-1
        public string PhotographyRecommendations { get; set; } = string.Empty;
        public List<string> CompositionSuggestions { get; set; } = new();
    }

    public class ConstellationData
    {
        public ConstellationType Constellation { get; set; }
        public DateTime DateTime { get; set; }
        public double CenterRightAscension { get; set; }
        public double CenterDeclination { get; set; }
        public double CenterAzimuth { get; set; }
        public double CenterAltitude { get; set; }
        public DateTime? Rise { get; set; }
        public DateTime? Set { get; set; }
        public DateTime? OptimalViewingTime { get; set; }
        public bool IsCircumpolar { get; set; }
        public List<DeepSkyObjectData> NotableObjects { get; set; } = new();
        public string PhotographyNotes { get; set; } = string.Empty;
    }

    public class DeepSkyObjectData
    {
        public string CatalogId { get; set; } = string.Empty; // M31, NGC 7000, etc.
        public string CommonName { get; set; } = string.Empty;
        public string ObjectType { get; set; } = string.Empty; // Galaxy, Nebula, Cluster
        public DateTime DateTime { get; set; }
        public double RightAscension { get; set; }
        public double Declination { get; set; }
        public double Azimuth { get; set; }
        public double Altitude { get; set; }
        public double Magnitude { get; set; }
        public double AngularSize { get; set; } // Arc minutes
        public bool IsVisible { get; set; }
        public DateTime? OptimalViewingTime { get; set; }
        public string RecommendedEquipment { get; set; } = string.Empty;
        public string ExposureGuidance { get; set; } = string.Empty;
        public ConstellationType ParentConstellation { get; set; }
    }

    public class ISSPassData
    {
        public DateTime StartTime { get; set; }
        public DateTime MaxTime { get; set; }
        public DateTime EndTime { get; set; }
        public double StartAzimuth { get; set; }
        public double MaxAltitude { get; set; }
        public double EndAzimuth { get; set; }
        public double Magnitude { get; set; }
        public TimeSpan Duration { get; set; }
        public string PassType { get; set; } = string.Empty; // Visible, Flare, etc.
        public string PhotographyPotential { get; set; } = string.Empty;
    }

    // === METEOR SHOWER MODELS ===

    public class MeteorShowerData
    {
        public MeteorShowerType ShowerType { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTime PeakDate { get; set; }
        public DateTime ActivityStart { get; set; }
        public DateTime ActivityEnd { get; set; }
        public double RadiantRightAscension { get; set; }
        public double RadiantDeclination { get; set; }
        public double RadiantAzimuth { get; set; }
        public double RadiantAltitude { get; set; }
        public int ZenithHourlyRate { get; set; }
        public double MoonIllumination { get; set; }
        public bool OptimalConditions { get; set; }
        public string PhotographyStrategy { get; set; } = string.Empty;
    }

    public class MeteorShowerConditions
    {
        public MeteorShowerType Shower { get; set; }
        public DateTime DateTime { get; set; }
        public double RadiantAltitude { get; set; }
        public double MoonAltitude { get; set; }
        public double MoonIllumination { get; set; }
        public double ExpectedRate { get; set; } // Meteors per hour
        public double ConditionsScore { get; set; } // 0-1
        public string Recommendation { get; set; } = string.Empty;
        public string OptimalCameraDirection { get; set; } = string.Empty;
    }

    // === ASTROPHOTOGRAPHY EQUIPMENT MODELS ===

    public class CameraEquipmentData
    {
        public string CameraModel { get; set; } = string.Empty;
        public double SensorWidth { get; set; } // mm
        public double SensorHeight { get; set; } // mm
        public double PixelSize { get; set; } // microns
        public int ISORange { get; set; }
        public string LensModel { get; set; } = string.Empty;
        public double FocalLength { get; set; } // mm
        public double Aperture { get; set; } // f-stop
        public bool HasTracker { get; set; }
        public double TrackerAccuracy { get; set; } // arc seconds
    }

    public class AstroConditions
    {
        public DateTime DateTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double BortleScale { get; set; } // 1-9
        public double Temperature { get; set; } // Celsius
        public double Humidity { get; set; } // Percentage
        public double CloudCover { get; set; } // Percentage
        public double Seeing { get; set; } // Arc seconds
        public double Transparency { get; set; } // 0-1
    }

    public class AstroExposureRecommendation
    {
        public AstroTarget Target { get; set; }
        public string RecommendedISO { get; set; } = string.Empty;
        public string RecommendedAperture { get; set; } = string.Empty;
        public string RecommendedShutterSpeed { get; set; } = string.Empty;
        public int NumberOfFrames { get; set; }
        public TimeSpan TotalExposureTime { get; set; }
        public string FocusingTechnique { get; set; } = string.Empty;
        public List<string> ProcessingNotes { get; set; } = new();
        public string TrackerRequirements { get; set; } = string.Empty;
    }

    public class AstroFieldOfViewData
    {
        public double FieldOfViewWidth { get; set; } // Degrees
        public double FieldOfViewHeight { get; set; } // Degrees
        public double PixelScale { get; set; } // Arc seconds per pixel
        public AstroTarget Target { get; set; }
        public bool TargetFitsInFrame { get; set; }
        public double TargetCoveragePercentage { get; set; }
        public string CompositionRecommendations { get; set; } = string.Empty;
        public List<string> AlternativeFocalLengths { get; set; } = new();
    }

    public class StarTrailData
    {
        public DateTime StartTime { get; set; }
        public TimeSpan ExposureDuration { get; set; }
        public double CelestialPoleAzimuth { get; set; }
        public double CelestialPoleAltitude { get; set; }
        public double StarTrailLength { get; set; } // Degrees
        public double Rotation { get; set; } // Degrees
        public string OptimalComposition { get; set; } = string.Empty;
        public List<string> ExposureStrategy { get; set; } = new();
    }

    public class PolarAlignmentData
    {
        public DateTime DateTime { get; set; }
        public double PolarisAzimuth { get; set; }
        public double PolarisAltitude { get; set; }
        public double PolarisOffsetAngle { get; set; } // From true pole
        public double PolarisOffsetDistance { get; set; } // Arc minutes
        public string AlignmentInstructions { get; set; } = string.Empty;
        public List<string> ReferenceStars { get; set; } = new();
    }

    // === COORDINATE TRANSFORMATION MODELS ===

    public class CoordinateTransformResult
    {
        public CoordinateType FromType { get; set; }
        public CoordinateType ToType { get; set; }
        public double InputCoordinate1 { get; set; }
        public double InputCoordinate2 { get; set; }
        public double OutputCoordinate1 { get; set; }
        public double OutputCoordinate2 { get; set; }
        public DateTime DateTime { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class AtmosphericCorrectionData
    {
        public double TrueAltitude { get; set; } // Degrees
        public double ApparentAltitude { get; set; } // Degrees
        public double RefractionCorrection { get; set; } // Arc minutes
        public double AtmosphericExtinction { get; set; } // Magnitudes
        public string CorrectionNotes { get; set; } = string.Empty;
    }
}