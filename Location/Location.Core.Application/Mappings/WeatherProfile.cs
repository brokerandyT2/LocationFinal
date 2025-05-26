using AutoMapper;
using Location.Core.Domain.Entities;
using Location.Core.Application.Weather.DTOs;
using System.Linq;

namespace Location.Core.Application.Mappings
{
    /// <summary>
    /// Provides mapping configurations for weather-related domain entities and data transfer objects (DTOs).
    /// </summary>
    /// <remarks>This class defines mappings between domain entities, such as <see
    /// cref="Domain.Entities.Weather"/> and  <see cref="WeatherForecast"/>, and their corresponding DTOs, such as <see
    /// cref="WeatherDto"/> and  <see cref="WeatherForecastDto"/>. These mappings are used to transform data between the
    /// domain layer  and the application layer, ensuring consistency and simplifying data handling.  The mappings
    /// include transformations for properties such as temperature, wind information, humidity,  and other
    /// weather-related attributes. Special handling is applied to nested objects and value objects,  such as
    /// coordinates and wind details, to ensure proper conversion.</remarks>
    public class WeatherProfile : Profile
    {
        /// <summary>
        /// Configures mapping profiles for weather-related domain entities and data transfer objects (DTOs).
        /// </summary>
        /// <remarks>This class defines mappings between domain entities and their corresponding DTOs
        /// using AutoMapper. It includes mappings for weather data, forecasts, and related properties, ensuring
        /// seamless transformation between domain models and DTOs for use in application layers.</remarks>
        public WeatherProfile()
        {
            // Weather to WeatherDto mapping - updated for double temperatures
            CreateMap<Domain.Entities.Weather, WeatherDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.LocationId, opt => opt.MapFrom(src => src.LocationId))
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Coordinate.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Coordinate.Longitude))
                .ForMember(dest => dest.Timezone, opt => opt.MapFrom(src => src.Timezone))
                .ForMember(dest => dest.TimezoneOffset, opt => opt.MapFrom(src => src.TimezoneOffset))
                .ForMember(dest => dest.LastUpdate, opt => opt.MapFrom(src => src.LastUpdate))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Temperature : 0))
                .ForMember(dest => dest.MinimumTemp, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().MinTemperature : 0))
                .ForMember(dest => dest.MaximumTemp, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().MaxTemperature : 0))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Description : ""))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Icon : ""))
                .ForMember(dest => dest.WindSpeed, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Wind.Speed : 0))
                .ForMember(dest => dest.WindDirection, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Wind.Direction : 0))
                .ForMember(dest => dest.WindGust, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Wind.Gust : null))
                .ForMember(dest => dest.Humidity, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Humidity : 0))
                .ForMember(dest => dest.Pressure, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Pressure : 0))
                .ForMember(dest => dest.Clouds, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Clouds : 0))
                .ForMember(dest => dest.UvIndex, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().UvIndex : 0))
                .ForMember(dest => dest.Precipitation, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Precipitation : null))
                .ForMember(dest => dest.Sunrise, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Sunrise : default))
                .ForMember(dest => dest.Sunset, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().Sunset : default))
                .ForMember(dest => dest.MoonRise, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().MoonRise : null))
                .ForMember(dest => dest.MoonSet, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().MoonSet : null))
                .ForMember(dest => dest.MoonPhase, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast().MoonPhase : 0));

            // Weather to WeatherForecastDto mapping
            CreateMap<Domain.Entities.Weather, WeatherForecastDto>()
                .ForMember(dest => dest.WeatherId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.LastUpdate, opt => opt.MapFrom(src => src.LastUpdate))
                .ForMember(dest => dest.Timezone, opt => opt.MapFrom(src => src.Timezone))
                .ForMember(dest => dest.TimezoneOffset, opt => opt.MapFrom(src => src.TimezoneOffset))
                .ForMember(dest => dest.DailyForecasts, opt => opt.MapFrom(src => src.Forecasts));

            // WeatherForecast to DailyForecastDto mapping - updated for double temperatures
            CreateMap<WeatherForecast, DailyForecastDto>()
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.Sunrise, opt => opt.MapFrom(src => src.Sunrise))
                .ForMember(dest => dest.Sunset, opt => opt.MapFrom(src => src.Sunset))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => src.Temperature))
                .ForMember(dest => dest.MinTemperature, opt => opt.MapFrom(src => src.MinTemperature))
                .ForMember(dest => dest.MaxTemperature, opt => opt.MapFrom(src => src.MaxTemperature))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon))
                .ForMember(dest => dest.WindSpeed, opt => opt.MapFrom(src => src.Wind.Speed))
                .ForMember(dest => dest.WindDirection, opt => opt.MapFrom(src => src.Wind.Direction))
                .ForMember(dest => dest.WindGust, opt => opt.MapFrom(src => src.Wind.Gust))
                .ForMember(dest => dest.Humidity, opt => opt.MapFrom(src => src.Humidity))
                .ForMember(dest => dest.Pressure, opt => opt.MapFrom(src => src.Pressure))
                .ForMember(dest => dest.Clouds, opt => opt.MapFrom(src => src.Clouds))
                .ForMember(dest => dest.UvIndex, opt => opt.MapFrom(src => src.UvIndex))
                .ForMember(dest => dest.Precipitation, opt => opt.MapFrom(src => src.Precipitation))
                .ForMember(dest => dest.MoonRise, opt => opt.MapFrom(src => src.MoonRise))
                .ForMember(dest => dest.MoonSet, opt => opt.MapFrom(src => src.MoonSet))
                .ForMember(dest => dest.MoonPhase, opt => opt.MapFrom(src => src.MoonPhase));

            // DailyForecastDto to WeatherForecast mapping - updated for double temperatures
            CreateMap<DailyForecastDto, WeatherForecast>()
                .ForMember(dest => dest.WeatherId, opt => opt.Ignore()) // Will be set during construction
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // Will be set by the database
                .ForMember(dest => dest.Date, opt => opt.MapFrom(src => src.Date))
                .ForMember(dest => dest.Sunrise, opt => opt.MapFrom(src => src.Sunrise))
                .ForMember(dest => dest.Sunset, opt => opt.MapFrom(src => src.Sunset))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => src.Temperature))
                .ForMember(dest => dest.MinTemperature, opt => opt.MapFrom(src => src.MinTemperature))
                .ForMember(dest => dest.MaxTemperature, opt => opt.MapFrom(src => src.MaxTemperature))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon))
                .ForMember(dest => dest.Wind, opt => opt.MapFrom(src => new Domain.ValueObjects.WindInfo(src.WindSpeed, src.WindDirection, src.WindGust)))
                .ForMember(dest => dest.Humidity, opt => opt.MapFrom(src => src.Humidity))
                .ForMember(dest => dest.Pressure, opt => opt.MapFrom(src => src.Pressure))
                .ForMember(dest => dest.Clouds, opt => opt.MapFrom(src => src.Clouds))
                .ForMember(dest => dest.UvIndex, opt => opt.MapFrom(src => src.UvIndex))
                .ForMember(dest => dest.Precipitation, opt => opt.MapFrom(src => src.Precipitation))
                .ForMember(dest => dest.MoonRise, opt => opt.MapFrom(src => src.MoonRise))
                .ForMember(dest => dest.MoonSet, opt => opt.MapFrom(src => src.MoonSet))
                .ForMember(dest => dest.MoonPhase, opt => opt.MapFrom(src => src.MoonPhase));

            // WeatherDto to Weather - if needed
            CreateMap<WeatherDto, Domain.Entities.Weather>()
                .ForMember(dest => dest.Coordinate, opt => opt.MapFrom(src => new Domain.ValueObjects.Coordinate(src.Latitude, src.Longitude)))
                .ForMember(dest => dest.Forecasts, opt => opt.Ignore())
                .ForMember(dest => dest.DomainEvents, opt => opt.Ignore());
        }
    }
}