using SQLite;
using System;

namespace Location.Core.Infrastructure.Data.Entities
{
    [Table("LocationEntity")]
    public class LocationEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string City { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public string? PhotoPath { get; set; }

        public bool IsDeleted { get; set; }

        public DateTime Timestamp { get; set; }
    }

    [Table("WeatherEntity")]
    public class WeatherEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int LocationId { get; set; }

        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string Timezone { get; set; } = string.Empty;

        public int TimezoneOffset { get; set; }

        public DateTime LastUpdate { get; set; }
    }

    [Table("WeatherForecastEntity")]
    public class WeatherForecastEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int WeatherId { get; set; }

        public DateTime Date { get; set; }

        public DateTime Sunrise { get; set; }

        public DateTime Sunset { get; set; }

        // Temperature in Celsius
        public double Temperature { get; set; }
        public double MinTemperature { get; set; }
        public double MaxTemperature { get; set; }

        public string Description { get; set; } = string.Empty;

        public string Icon { get; set; } = string.Empty;

        // Wind info
        public double WindSpeed { get; set; }
        public double WindDirection { get; set; }
        public double? WindGust { get; set; }

        public int Humidity { get; set; }

        public int Pressure { get; set; }

        public int Clouds { get; set; }

        public double UvIndex { get; set; }

        public double? Precipitation { get; set; }

        // Moon data
        public DateTime? MoonRise { get; set; }
        public DateTime? MoonSet { get; set; }
        public double MoonPhase { get; set; }
    }

    [Table("TipTypeEntity")]
    public class TipTypeEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string I8n { get; set; } = "en-US";
    }

    [Table("TipEntity")]
    public class TipEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public int TipTypeId { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Content { get; set; } = string.Empty;

        public string Fstop { get; set; } = string.Empty;

        public string ShutterSpeed { get; set; } = string.Empty;

        public string Iso { get; set; } = string.Empty;

        public string I8n { get; set; } = "en-US";
    }

    [Table("SettingEntity")]
    public class SettingEntity
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Unique]
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public DateTime Timestamp { get; set; }
    }

    [Table("Log")]
    public class Log
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string Level { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string Exception { get; set; } = string.Empty;
    }
}