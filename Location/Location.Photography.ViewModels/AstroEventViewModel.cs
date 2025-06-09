// Location.Photography.ViewModels/AstroEventViewModel.cs
using CommunityToolkit.Mvvm.ComponentModel;
using Location.Photography.Domain.Models;

namespace Location.Photography.ViewModels
{
    public partial class AstroEventViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _name = string.Empty;

        [ObservableProperty]
        private AstroTarget _target;

        [ObservableProperty]
        private DateTime _startTime;

        [ObservableProperty]
        private DateTime _endTime;

        [ObservableProperty]
        private DateTime? _peakTime;

        [ObservableProperty]
        private double _azimuth;

        [ObservableProperty]
        private double _altitude;

        [ObservableProperty]
        private double _magnitude;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _constellation = string.Empty;

        [ObservableProperty]
        private bool _isVisible;

        [ObservableProperty]
        private string _eventType = string.Empty;

        [ObservableProperty]
        private double _angularSize;

        [ObservableProperty]
        private string _recommendedEquipment = string.Empty;
        public string _timeFormat;
        public string _dateFormat;
        public AstroEventViewModel()
        {
        }

        public AstroEventViewModel(AstroCalculationResult result)
        {
            Target = result.Target;
            Name = GetTargetDisplayName(result.Target);
            StartTime = result.CalculationTime;
            EndTime = result.SetTime ?? result.CalculationTime.AddHours(8);
            PeakTime = result.OptimalTime;
            Azimuth = result.Azimuth;
            Altitude = result.Altitude;
            IsVisible = result.IsVisible;
            Description = result.Description;
            RecommendedEquipment = result.Equipment;
            EventType = GetEventType(result.Target);

        }

        public AstroEventViewModel(PlanetPositionData planetData)
        {
            Target = AstroTarget.Planets;
            Name = GetPlanetDisplayName(planetData.Planet);
            StartTime = planetData.Rise ?? planetData.DateTime;
            EndTime = planetData.Set ?? planetData.DateTime.AddHours(12);
            PeakTime = planetData.Transit;
            Azimuth = planetData.Azimuth;
            Altitude = planetData.Altitude;
            Magnitude = planetData.ApparentMagnitude;
            AngularSize = planetData.AngularDiameter;
            IsVisible = planetData.IsVisible;
            Description = $"Magnitude {planetData.ApparentMagnitude:F1}, {planetData.AngularDiameter:F1}″";
            RecommendedEquipment = planetData.RecommendedEquipment;
            EventType = "Planet";

        }

        public AstroEventViewModel(DeepSkyObjectData dsoData)
        {
            Target = AstroTarget.DeepSkyObjects;
            Name = !string.IsNullOrEmpty(dsoData.CommonName) ? dsoData.CommonName : dsoData.CatalogId;
            StartTime = dsoData.DateTime;
            EndTime = dsoData.DateTime.AddHours(8);
            PeakTime = dsoData.OptimalViewingTime;
            Azimuth = dsoData.Azimuth;
            Altitude = dsoData.Altitude;
            Magnitude = dsoData.Magnitude;
            AngularSize = dsoData.AngularSize;
            IsVisible = dsoData.IsVisible;
            Description = $"{dsoData.ObjectType}, Mag {dsoData.Magnitude:F1}, {dsoData.AngularSize:F1}'";
            Constellation = dsoData.ParentConstellation.ToString();
            RecommendedEquipment = dsoData.RecommendedEquipment;
            EventType = dsoData.ObjectType;

        }

        public AstroEventViewModel(MeteorShowerData meteorData)
        {
            Target = AstroTarget.MeteorShowers;
            Name = meteorData.Name;
            StartTime = meteorData.ActivityStart;
            EndTime = meteorData.ActivityEnd;
            PeakTime = meteorData.PeakDate;
            Azimuth = meteorData.RadiantAzimuth;
            Altitude = meteorData.RadiantAltitude;
            IsVisible = meteorData.OptimalConditions;
            Description = $"ZHR: {meteorData.ZenithHourlyRate}, Moon: {meteorData.MoonIllumination:P0}";
            EventType = "Meteor Shower";

        }

        public AstroEventViewModel(EnhancedMoonData moonData)
        {
            Target = AstroTarget.Moon;
            Name = $"Moon ({moonData.PhaseName})";
            StartTime = moonData.Rise ?? moonData.DateTime;
            EndTime = moonData.Set ?? moonData.DateTime.AddHours(12);
            PeakTime = moonData.Transit;
            Azimuth = moonData.Azimuth;
            Altitude = moonData.Altitude;
            IsVisible = moonData.Altitude > 0;
            Description = $"Phase: {moonData.Illumination:F0}%, Distance: {moonData.Distance:F0}km";
            EventType = moonData.PhaseName;
            AngularSize = moonData.AngularDiameter;

        }

        /// <summary>
        /// Gets the optimal time for this event - peak time if available, otherwise average of start/end
        /// </summary>
        public DateTime GetOptimalTime()
        {
            if (PeakTime.HasValue)
                return PeakTime.Value;

            // Calculate average of start and end times
            var span = EndTime - StartTime;
            return StartTime.Add(TimeSpan.FromTicks(span.Ticks / 2));
        }

        /// <summary>
        /// Gets formatted time string for display
        /// </summary>
        public string GetFormattedTime()
        {
            var optimalTime = GetOptimalTime();
            return "Peak Time: " + optimalTime.ToString(_timeFormat);
        }

        /// <summary>
        /// Gets display string for event duration
        /// </summary>
        public string GetDurationString()
        {
            var duration = EndTime - StartTime;
            if (duration.TotalDays >= 1)
                return $"{duration.TotalDays:F0}d";
            else if (duration.TotalHours >= 1)
                return $"{duration.TotalHours:F1}h";
            else
                return $"{duration.TotalMinutes:F0}m";
        }

        /// <summary>
        /// Gets visibility quality score (0-1)
        /// </summary>
        public double GetVisibilityScore()
        {
            if (!IsVisible) return 0.0;

            double score = 0.5; // Base score for being visible

            // Altitude bonus (higher is better)
            if (Altitude > 60) score += 0.3;
            else if (Altitude > 30) score += 0.2;
            else if (Altitude > 15) score += 0.1;

            // Magnitude bonus (brighter is better, lower magnitude = brighter)
            if (Magnitude < 0) score += 0.2;
            else if (Magnitude < 3) score += 0.1;

            return Math.Min(1.0, score);
        }

        private string GetTargetDisplayName(AstroTarget target)
        {
            return target switch
            {
                AstroTarget.Moon => "Moon",
                AstroTarget.Planets => "Planets",
                AstroTarget.MilkyWayCore => "Milky Way Core",
                AstroTarget.DeepSkyObjects => "Deep Sky Objects",
                AstroTarget.StarTrails => "Star Trails",
                AstroTarget.Comets => "Comets",
                AstroTarget.MeteorShowers => "Meteor Showers",
                AstroTarget.PolarAlignment => "Polar Alignment",
                AstroTarget.Constellations => "Constellations",
                AstroTarget.NorthernLights => "Northern Lights",
                _ => target.ToString()
            };
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

        private string GetEventType(AstroTarget target)
        {
            return target switch
            {
                AstroTarget.Moon => "Lunar",
                AstroTarget.Planets => "Planetary",
                AstroTarget.MilkyWayCore => "Galactic",
                AstroTarget.DeepSkyObjects => "Deep Sky",
                AstroTarget.StarTrails => "Star Trail",
                AstroTarget.Comets => "Comet",
                AstroTarget.MeteorShowers => "Meteor",
                AstroTarget.PolarAlignment => "Alignment",
                AstroTarget.Constellations => "Constellation",
                AstroTarget.NorthernLights => "Aurora",
                _ => "Unknown"
            };
        }

        public override string ToString()
        {
            return $"{Name} - {GetFormattedTime()}";
        }
    }
}