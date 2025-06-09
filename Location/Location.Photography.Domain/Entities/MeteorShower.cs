namespace Location.Photography.Domain.Entities
{
    /// <summary>
    /// Represents a meteor shower with its activity periods and characteristics
    /// </summary>
    public class MeteorShower
    {
        /// <summary>
        /// Short code identifier (e.g., "PER", "GEM", "LYR")
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Full display name (e.g., "Perseids", "Geminids", "Lyrids")
        /// </summary>
        public string Designation { get; set; } = string.Empty;

        /// <summary>
        /// Activity period with start, peak, and finish dates
        /// </summary>
        public MeteorShowerActivity Activity { get; set; } = new();

        /// <summary>
        /// Right Ascension of radiant in degrees
        /// </summary>
        public double RadiantRA { get; set; }

        /// <summary>
        /// Declination of radiant in degrees
        /// </summary>
        public double RadiantDec { get; set; }

        /// <summary>
        /// Meteoroid velocity in km/s
        /// </summary>
        public int SpeedKmS { get; set; }

        /// <summary>
        /// Parent comet or asteroid that creates this meteor shower
        /// </summary>
        public string ParentBody { get; set; } = string.Empty;

        /// <summary>
        /// Checks if this shower is active on the given date
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>True if shower is active on this date</returns>
        public bool IsActiveOn(DateTime date)
        {
            return Activity.IsActiveOn(date);
        }

        /// <summary>
        /// Gets the expected ZHR for the given date during the shower
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>Expected ZHR, with peak ZHR on peak date</returns>
        public double GetExpectedZHR(DateTime date)
        {
            if (!IsActiveOn(date))
                return 0;

            return Activity.GetExpectedZHR(date);
        }

        /// <summary>
        /// Calculates the radiant position for the given date and time
        /// </summary>
        /// <param name="dateTime">Date and time</param>
        /// <param name="latitude">Observer latitude in degrees</param>
        /// <param name="longitude">Observer longitude in degrees</param>
        /// <returns>Radiant position data</returns>
        public RadiantPosition GetRadiantPosition(DateTime dateTime, double latitude, double longitude)
        {
            // Calculate radiant altitude and azimuth using basic celestial mechanics
            // This is a simplified calculation - real implementation would use precise algorithms

            var hourAngle = CalculateHourAngle(dateTime, longitude);
            var altitude = CalculateAltitude(RadiantDec, latitude, hourAngle);
            var azimuth = CalculateAzimuth(RadiantDec, latitude, hourAngle, altitude);

            return new RadiantPosition
            {
                Altitude = altitude,
                Azimuth = azimuth,
                IsVisible = altitude > 0
            };
        }

        private double CalculateHourAngle(DateTime dateTime, double longitude)
        {
            // Simplified hour angle calculation
            var utcHours = dateTime.ToUniversalTime().TimeOfDay.TotalHours;
            var localSiderealTime = (utcHours + longitude / 15.0) * 15.0;
            return localSiderealTime - RadiantRA;
        }

        private double CalculateAltitude(double declination, double latitude, double hourAngle)
        {
            // Convert to radians
            var decRad = declination * Math.PI / 180.0;
            var latRad = latitude * Math.PI / 180.0;
            var haRad = hourAngle * Math.PI / 180.0;

            // Calculate altitude
            var sinAlt = Math.Sin(decRad) * Math.Sin(latRad) +
                        Math.Cos(decRad) * Math.Cos(latRad) * Math.Cos(haRad);

            return Math.Asin(sinAlt) * 180.0 / Math.PI;
        }

        private double CalculateAzimuth(double declination, double latitude, double hourAngle, double altitude)
        {
            // Convert to radians
            var decRad = declination * Math.PI / 180.0;
            var latRad = latitude * Math.PI / 180.0;
            var haRad = hourAngle * Math.PI / 180.0;
            var altRad = altitude * Math.PI / 180.0;

            // Calculate azimuth
            var cosAz = (Math.Sin(decRad) - Math.Sin(altRad) * Math.Sin(latRad)) /
                       (Math.Cos(altRad) * Math.Cos(latRad));

            var azimuth = Math.Acos(Math.Max(-1, Math.Min(1, cosAz))) * 180.0 / Math.PI;

            // Adjust for quadrant
            if (Math.Sin(haRad) > 0)
                azimuth = 360.0 - azimuth;

            return azimuth;
        }
    }

    /// <summary>
    /// Represents the activity period and intensity of a meteor shower
    /// </summary>
    public class MeteorShowerActivity
    {
        /// <summary>
        /// Start date in MM-DD format (e.g., "07-17")
        /// </summary>
        public string Start { get; set; } = string.Empty;

        /// <summary>
        /// Peak date in MM-DD format (e.g., "08-12")
        /// </summary>
        public string Peak { get; set; } = string.Empty;

        /// <summary>
        /// Finish date in MM-DD format (e.g., "08-24")
        /// </summary>
        public string Finish { get; set; } = string.Empty;

        /// <summary>
        /// Zenith Hourly Rate at peak
        /// </summary>
        public int ZHR { get; set; }

        /// <summary>
        /// Checks if this shower is active on the given date
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>True if shower is active on this date</returns>
        public bool IsActiveOn(DateTime date)
        {
            try
            {
                var startDate = ParseActivityDate(Start, date.Year);
                var finishDate = ParseActivityDate(Finish, date.Year);

                // Handle year boundary crossing (e.g., Quadrantids: Dec 28 - Jan 12)
                if (finishDate < startDate)
                {
                    // Shower crosses year boundary
                    if (date.Month >= startDate.Month)
                    {
                        // Current year part (e.g., Dec 28 - Dec 31)
                        return date >= startDate;
                    }
                    else
                    {
                        // Next year part (e.g., Jan 1 - Jan 12)
                        finishDate = ParseActivityDate(Finish, date.Year);
                        return date <= finishDate;
                    }
                }
                else
                {
                    // Normal case - same year
                    return date >= startDate && date <= finishDate;
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the expected ZHR for the given date, with peak ZHR on peak date
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>Expected ZHR with peak intensity on peak date</returns>
        public double GetExpectedZHR(DateTime date)
        {
            if (!IsActiveOn(date))
                return 0;

            try
            {
                var peakDate = ParseActivityDate(Peak, date.Year);

                // Handle year boundary crossing for peak date
                if (date.Month == 1 && peakDate.Month == 12)
                    peakDate = ParseActivityDate(Peak, date.Year - 1);
                else if (date.Month == 12 && peakDate.Month == 1)
                    peakDate = ParseActivityDate(Peak, date.Year + 1);

                var daysDifference = Math.Abs((date - peakDate).TotalDays);

                // Peak ZHR on peak date, declining with distance from peak
                if (daysDifference == 0)
                    return ZHR;
                else if (daysDifference <= 1)
                    return ZHR * 0.8; // 80% of peak
                else if (daysDifference <= 2)
                    return ZHR * 0.6; // 60% of peak
                else if (daysDifference <= 3)
                    return ZHR * 0.4; // 40% of peak
                else
                    return ZHR * 0.2; // 20% of peak
            }
            catch
            {
                return ZHR * 0.5; // Default to half peak if date parsing fails
            }
        }

        private DateTime ParseActivityDate(string activityDate, int year)
        {
            if (string.IsNullOrEmpty(activityDate))
                throw new ArgumentException("Activity date cannot be null or empty");

            var parts = activityDate.Split('-');
            if (parts.Length != 2)
                throw new ArgumentException($"Invalid activity date format: {activityDate}");

            if (!int.TryParse(parts[0], out var month) || !int.TryParse(parts[1], out var day))
                throw new ArgumentException($"Invalid activity date format: {activityDate}");

            return new DateTime(year, month, day);
        }
    }

    /// <summary>
    /// Represents the position of a meteor shower radiant in the sky
    /// </summary>
    public class RadiantPosition
    {
        /// <summary>
        /// Altitude above horizon in degrees
        /// </summary>
        public double Altitude { get; set; }

        /// <summary>
        /// Azimuth from north in degrees
        /// </summary>
        public double Azimuth { get; set; }

        /// <summary>
        /// Whether the radiant is above the horizon
        /// </summary>
        public bool IsVisible { get; set; }

        /// <summary>
        /// Gets a descriptive string for the altitude
        /// </summary>
        public string AltitudeDescription => Altitude switch
        {
            >= 60 => "High in sky",
            >= 30 => "Moderate altitude",
            >= 10 => "Low on horizon",
            > 0 => "Just above horizon",
            _ => "Below horizon"
        };

        /// <summary>
        /// Gets a descriptive string for the azimuth direction
        /// </summary>
        public string DirectionDescription => Azimuth switch
        {
            >= 337.5 or < 22.5 => "North",
            >= 22.5 and < 67.5 => "Northeast",
            >= 67.5 and < 112.5 => "East",
            >= 112.5 and < 157.5 => "Southeast",
            >= 157.5 and < 202.5 => "South",
            >= 202.5 and < 247.5 => "Southwest",
            >= 247.5 and < 292.5 => "West",
            >= 292.5 and < 337.5 => "Northwest",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Container for multiple meteor showers data
    /// </summary>
    public class MeteorShowerData
    {
        /// <summary>
        /// List of all meteor showers
        /// </summary>
        public List<MeteorShower> Showers { get; set; } = new();

        /// <summary>
        /// Gets all showers active on the specified date
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <returns>List of active meteor showers</returns>
        public List<MeteorShower> GetActiveShowers(DateTime date)
        {
            return Showers.Where(s => s.IsActiveOn(date)).ToList();
        }

        /// <summary>
        /// Gets showers active on the specified date with minimum ZHR
        /// </summary>
        /// <param name="date">Date to check</param>
        /// <param name="minZHR">Minimum ZHR threshold</param>
        /// <returns>List of active meteor showers meeting ZHR threshold</returns>
        public List<MeteorShower> GetActiveShowers(DateTime date, int minZHR)
        {
            return Showers
                .Where(s => s.IsActiveOn(date) && s.GetExpectedZHR(date) >= minZHR)
                .OrderByDescending(s => s.GetExpectedZHR(date))
                .ToList();
        }

        /// <summary>
        /// Gets a shower by its code
        /// </summary>
        /// <param name="code">Shower code (e.g., "PER", "GEM")</param>
        /// <returns>Meteor shower or null if not found</returns>
        public MeteorShower? GetShowerByCode(string code)
        {
            return Showers.FirstOrDefault(s =>
                string.Equals(s.Code, code, StringComparison.OrdinalIgnoreCase));
        }
    }
}