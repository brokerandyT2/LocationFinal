using Location.Photography.Domain.Models;

namespace Location.Photography.BDD.Tests.Models
{
    /// <summary>
    /// Test model for sun calculation scenarios
    /// </summary>
    public class SunCalculationTestModel
    {
        public int? Id { get; set; }

        // Input parameters
        public DateTime Date { get; set; } = DateTime.Today;
        public TimeSpan Time { get; set; } = TimeSpan.FromHours(12);
        public DateTime DateTime { get; set; } = DateTime.Now;
        public double Latitude { get; set; } = 40.7128; // New York City default
        public double Longitude { get; set; } = -74.0060; // New York City default

        // Sun times results
        public DateTime Sunrise { get; set; }
        public DateTime Sunset { get; set; }
        public DateTime SolarNoon { get; set; }
        public DateTime CivilDawn { get; set; }
        public DateTime CivilDusk { get; set; }
        public DateTime NauticalDawn { get; set; }
        public DateTime NauticalDusk { get; set; }
        public DateTime AstronomicalDawn { get; set; }
        public DateTime AstronomicalDusk { get; set; }

        // Golden hour times
        public DateTime GoldenHourMorningStart { get; set; }
        public DateTime GoldenHourMorningEnd { get; set; }
        public DateTime GoldenHourEveningStart { get; set; }
        public DateTime GoldenHourEveningEnd { get; set; }

        // Sun position results
        public double SolarAzimuth { get; set; }
        public double SolarElevation { get; set; }

        // Error handling
        public string ErrorMessage { get; set; } = string.Empty;
        public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

        // Validation
        public bool IsValidCoordinates => Latitude >= -90 && Latitude <= 90 && Longitude >= -180 && Longitude <= 180;
        public bool IsValid => IsValidCoordinates && Date != default && DateTime != default;

        /// <summary>
        /// Creates a SunPositionDto from current values
        /// </summary>
        public SunPositionDto ToSunPositionDto()
        {
            return new SunPositionDto
            {
                Azimuth = SolarAzimuth,
                Elevation = SolarElevation,
                DateTime = DateTime,
                Latitude = Latitude,
                Longitude = Longitude
            };
        }

        /// <summary>
        /// Creates a SunTimesDto from current values
        /// </summary>
        public SunTimesDto ToSunTimesDto()
        {
            return new SunTimesDto
            {
                Date = Date,
                Latitude = Latitude,
                Longitude = Longitude,
                Sunrise = Sunrise,
                Sunset = Sunset,
                SolarNoon = SolarNoon,
                AstronomicalDawn = AstronomicalDawn,
                AstronomicalDusk = AstronomicalDusk,
                NauticalDawn = NauticalDawn,
                NauticalDusk = NauticalDusk,
                CivilDawn = CivilDawn,
                CivilDusk = CivilDusk,
                GoldenHourMorningStart = GoldenHourMorningStart,
                GoldenHourMorningEnd = GoldenHourMorningEnd,
                GoldenHourEveningStart = GoldenHourEveningStart,
                GoldenHourEveningEnd = GoldenHourEveningEnd
            };
        }

        /// <summary>
        /// Updates values from SunPositionDto
        /// </summary>
        public void UpdateFromSunPosition(SunPositionDto sunPosition)
        {
            if (sunPosition != null)
            {
                SolarAzimuth = sunPosition.Azimuth;
                SolarElevation = sunPosition.Elevation;
                DateTime = sunPosition.DateTime;
                Latitude = sunPosition.Latitude;
                Longitude = sunPosition.Longitude;
            }
        }

        /// <summary>
        /// Updates values from SunTimesDto
        /// </summary>
        public void UpdateFromSunTimes(SunTimesDto sunTimes)
        {
            if (sunTimes != null)
            {
                Date = sunTimes.Date;
                Latitude = sunTimes.Latitude;
                Longitude = sunTimes.Longitude;
                Sunrise = sunTimes.Sunrise;
                Sunset = sunTimes.Sunset;
                SolarNoon = sunTimes.SolarNoon;
                AstronomicalDawn = sunTimes.AstronomicalDawn;
                AstronomicalDusk = sunTimes.AstronomicalDusk;
                NauticalDawn = sunTimes.NauticalDawn;
                NauticalDusk = sunTimes.NauticalDusk;
                CivilDawn = sunTimes.CivilDawn;
                CivilDusk = sunTimes.CivilDusk;
                GoldenHourMorningStart = sunTimes.GoldenHourMorningStart;
                GoldenHourMorningEnd = sunTimes.GoldenHourMorningEnd;
                GoldenHourEveningStart = sunTimes.GoldenHourEveningStart;
                GoldenHourEveningEnd = sunTimes.GoldenHourEveningEnd;
            }
        }

        /// <summary>
        /// Synchronizes Date and Time with DateTime property
        /// </summary>
        public void SynchronizeDateTime()
        {
            if (DateTime != default)
            {
                Date = DateTime.Date;
                Time = DateTime.TimeOfDay;
            }
            else if (Date != default)
            {
                DateTime = Date.Add(Time);
            }
        }

        /// <summary>
        /// Calculates golden hour times based on sunrise/sunset - FIXED for accuracy
        /// </summary>
        public void CalculateGoldenHours()
        {
            if (Sunrise != default && Sunset != default)
            {
                // Golden hour is typically within 1 hour of sunrise/sunset
                // Morning golden hour: starts 1 hour before sunrise, ends at sunrise
                GoldenHourMorningStart = Sunrise.AddMinutes(-60);
                GoldenHourMorningEnd = Sunrise; // Ends exactly at sunrise

                // Evening golden hour: starts at sunset, ends 1 hour after sunset
                GoldenHourEveningStart = Sunset; // Starts exactly at sunset
                GoldenHourEveningEnd = Sunset.AddMinutes(60);
            }
            else
            {
                // Fallback calculation if sunrise/sunset not available
                var baseDate = DateTime != default ? DateTime.Date : Date.Date;

                // Estimate sunrise/sunset based on time of year and latitude
                var dayOfYear = baseDate.DayOfYear;
                var seasonalOffset = Math.Sin((dayOfYear - 81) * 2 * Math.PI / 365) * 2; // +/- 2 hours seasonal variation
                var latitudeOffset = Math.Abs(Latitude) / 90 * 3; // Up to 3 hours latitude effect

                var estimatedSunrise = baseDate.AddHours(6 - seasonalOffset - latitudeOffset);
                var estimatedSunset = baseDate.AddHours(18 + seasonalOffset + latitudeOffset);

                GoldenHourMorningStart = estimatedSunrise.AddMinutes(-60);
                GoldenHourMorningEnd = estimatedSunrise;
                GoldenHourEveningStart = estimatedSunset;
                GoldenHourEveningEnd = estimatedSunset.AddMinutes(60);
            }
        }

        /// <summary>
        /// Resets all calculated values
        /// </summary>
        public void ClearResults()
        {
            Sunrise = default;
            Sunset = default;
            SolarNoon = default;
            CivilDawn = default;
            CivilDusk = default;
            NauticalDawn = default;
            NauticalDusk = default;
            AstronomicalDawn = default;
            AstronomicalDusk = default;
            GoldenHourMorningStart = default;
            GoldenHourMorningEnd = default;
            GoldenHourEveningStart = default;
            GoldenHourEveningEnd = default;
            SolarAzimuth = 0;
            SolarElevation = 0;
            ErrorMessage = string.Empty;
        }

        /// <summary>
        /// Creates a copy of this model
        /// </summary>
        public SunCalculationTestModel Clone()
        {
            return new SunCalculationTestModel
            {
                Id = Id,
                Date = Date,
                Time = Time,
                DateTime = DateTime,
                Latitude = Latitude,
                Longitude = Longitude,
                Sunrise = Sunrise,
                Sunset = Sunset,
                SolarNoon = SolarNoon,
                CivilDawn = CivilDawn,
                CivilDusk = CivilDusk,
                NauticalDawn = NauticalDawn,
                NauticalDusk = NauticalDusk,
                AstronomicalDawn = AstronomicalDawn,
                AstronomicalDusk = AstronomicalDusk,
                GoldenHourMorningStart = GoldenHourMorningStart,
                GoldenHourMorningEnd = GoldenHourMorningEnd,
                GoldenHourEveningStart = GoldenHourEveningStart,
                GoldenHourEveningEnd = GoldenHourEveningEnd,
                SolarAzimuth = SolarAzimuth,
                SolarElevation = SolarElevation,
                ErrorMessage = ErrorMessage
            };
        }

        /// <summary>
        /// String representation for debugging
        /// </summary>
        public override string ToString()
        {
            return $"SunCalculation[{Id}]: {Date:yyyy-MM-dd} at ({Latitude:F4}, {Longitude:F4}) " +
                   $"Sunrise: {Sunrise:HH:mm}, Sunset: {Sunset:HH:mm}, Azimuth: {SolarAzimuth:F1}°, Elevation: {SolarElevation:F1}°";
        }

        /// <summary>
        /// Gets a formatted location string
        /// </summary>
        public string GetLocationString()
        {
            return $"{Latitude:F4}°{(Latitude >= 0 ? "N" : "S")}, {Math.Abs(Longitude):F4}°{(Longitude >= 0 ? "E" : "W")}";
        }

        /// <summary>
        /// Gets a formatted time range string for golden hour
        /// </summary>
        public string GetGoldenHourMorningString()
        {
            if (GoldenHourMorningStart != default && GoldenHourMorningEnd != default)
            {
                return $"{GoldenHourMorningStart:HH:mm} - {GoldenHourMorningEnd:HH:mm}";
            }
            return "Not calculated";
        }

        /// <summary>
        /// Gets a formatted time range string for evening golden hour
        /// </summary>
        public string GetGoldenHourEveningString()
        {
            if (GoldenHourEveningStart != default && GoldenHourEveningEnd != default)
            {
                return $"{GoldenHourEveningStart:HH:mm} - {GoldenHourEveningEnd:HH:mm}";
            }
            return "Not calculated";
        }
    }
}