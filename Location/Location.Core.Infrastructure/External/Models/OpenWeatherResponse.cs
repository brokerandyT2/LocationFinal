using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Location.Core.Infrastructure.External.Models
{
    public class OpenWeatherResponse
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }

        [JsonPropertyName("lon")]
        public double Lon { get; set; }

        [JsonPropertyName("timezone")]
        public string Timezone { get; set; } = string.Empty;

        [JsonPropertyName("timezone_offset")]
        public int TimezoneOffset { get; set; }

        [JsonPropertyName("current")]
        public CurrentWeather Current { get; set; } = new();

        [JsonPropertyName("hourly")]
        public List<HourlyWeather> Hourly { get; set; } = new();

        [JsonPropertyName("daily")]
        public List<DailyForecast> Daily { get; set; } = new();
    }

    public class CurrentWeather
    {
        [JsonPropertyName("dt")]
        public long Dt { get; set; }

        [JsonPropertyName("sunrise")]
        public long Sunrise { get; set; }

        [JsonPropertyName("sunset")]
        public long Sunset { get; set; }

        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("dew_point")]
        public double DewPoint { get; set; }

        [JsonPropertyName("uvi")]
        public double Uvi { get; set; }

        [JsonPropertyName("clouds")]
        public int Clouds { get; set; }

        [JsonPropertyName("visibility")]
        public int Visibility { get; set; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("wind_deg")]
        public double WindDeg { get; set; }

        [JsonPropertyName("wind_gust")]
        public double? WindGust { get; set; }

        [JsonPropertyName("weather")]
        public List<WeatherDescription> Weather { get; set; } = new();
    }

    public class HourlyWeather
    {
        [JsonPropertyName("dt")]
        public long Dt { get; set; }

        [JsonPropertyName("temp")]
        public double Temp { get; set; }

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("dew_point")]
        public double DewPoint { get; set; }

        [JsonPropertyName("uvi")]
        public double Uvi { get; set; }

        [JsonPropertyName("clouds")]
        public int Clouds { get; set; }

        [JsonPropertyName("visibility")]
        public int Visibility { get; set; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("wind_deg")]
        public double WindDeg { get; set; }

        [JsonPropertyName("wind_gust")]
        public double? WindGust { get; set; }

        [JsonPropertyName("weather")]
        public List<WeatherDescription> Weather { get; set; } = new();

        [JsonPropertyName("pop")]
        public double Pop { get; set; }
    }

    public class DailyForecast
    {
        [JsonPropertyName("dt")]
        public long Dt { get; set; }

        [JsonPropertyName("sunrise")]
        public long Sunrise { get; set; }

        [JsonPropertyName("sunset")]
        public long Sunset { get; set; }

        [JsonPropertyName("moonrise")]
        public long MoonRise { get; set; }

        [JsonPropertyName("moonset")]
        public long MoonSet { get; set; }

        [JsonPropertyName("moon_phase")]
        public double MoonPhase { get; set; }

        [JsonPropertyName("temp")]
        public DailyTemperature Temp { get; set; } = new();

        [JsonPropertyName("feels_like")]
        public DailyFeelsLike FeelsLike { get; set; } = new();

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; }

        [JsonPropertyName("dew_point")]
        public double DewPoint { get; set; }

        [JsonPropertyName("wind_speed")]
        public double WindSpeed { get; set; }

        [JsonPropertyName("wind_deg")]
        public double WindDeg { get; set; }

        [JsonPropertyName("wind_gust")]
        public double? WindGust { get; set; }

        [JsonPropertyName("weather")]
        public List<WeatherDescription> Weather { get; set; } = new();

        [JsonPropertyName("clouds")]
        public int Clouds { get; set; }

        [JsonPropertyName("pop")]
        public double Pop { get; set; }

        [JsonPropertyName("rain")]
        public double? Rain { get; set; }

        [JsonPropertyName("uvi")]
        public double Uvi { get; set; }
    }

    public class DailyTemperature
    {
        [JsonPropertyName("day")]
        public double Day { get; set; }

        [JsonPropertyName("min")]
        public double Min { get; set; }

        [JsonPropertyName("max")]
        public double Max { get; set; }

        [JsonPropertyName("night")]
        public double Night { get; set; }

        [JsonPropertyName("eve")]
        public double Eve { get; set; }

        [JsonPropertyName("morn")]
        public double Morn { get; set; }
    }

    public class DailyFeelsLike
    {
        [JsonPropertyName("day")]
        public double Day { get; set; }

        [JsonPropertyName("night")]
        public double Night { get; set; }

        [JsonPropertyName("eve")]
        public double Eve { get; set; }

        [JsonPropertyName("morn")]
        public double Morn { get; set; }
    }

    public class WeatherDescription
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("main")]
        public string Main { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }
}