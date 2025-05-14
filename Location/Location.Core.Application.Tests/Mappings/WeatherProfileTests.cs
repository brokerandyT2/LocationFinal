using NUnit.Framework;
using FluentAssertions;
using AutoMapper;
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Tests.Helpers;
using Location.Core.Domain.ValueObjects;
using System.Linq;

namespace Location.Core.Application.Tests.Mappings
{
    [TestFixture]
    public class WeatherProfileTests
    {
        private IMapper _mapper;

        [SetUp]
        public void Setup()
        {
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<WeatherProfile>();
            });

            _mapper = config.CreateMapper();
        }

        [Test]
        public void Configuration_ShouldBeValid()
        {
            // Arrange
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<WeatherProfile>();
            });

            // Act & Assert
            config.AssertConfigurationIsValid();
        }

        [Test]
        public void Map_WeatherToWeatherDto_ShouldMapCorrectly()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var weather = new Domain.Entities.Weather(1, coordinate, "America/Los_Angeles", -7);
            var forecasts = TestDataBuilder.CreateValidWeatherForecasts(1);
            weather.UpdateForecasts(forecasts);

            // Act
            var dto = _mapper.Map<WeatherDto>(weather);

            // Assert
            dto.Should().NotBeNull();
            dto.Id.Should().Be(weather.Id);
            dto.LocationId.Should().Be(1);
            dto.Latitude.Should().Be(47.6062);
            dto.Longitude.Should().Be(-122.3321);
            dto.Timezone.Should().Be("America/Los_Angeles");
            dto.TimezoneOffset.Should().Be(-7);
            dto.LastUpdate.Should().BeCloseTo(weather.LastUpdate, System.TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Map_WeatherWithCurrentForecast_ShouldMapCurrentConditions()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var weather = new Domain.Entities.Weather(1, coordinate, "America/Los_Angeles", -7);
            var forecasts = TestDataBuilder.CreateValidWeatherForecasts(1);
            weather.UpdateForecasts(forecasts);

            // Act
            var dto = _mapper.Map<WeatherDto>(weather);

            // Assert
            var currentForecast = weather.GetCurrentForecast();
            dto.Temperature.Should().Be(currentForecast!.Temperature.Celsius);
            dto.Description.Should().Be(currentForecast.Description);
            dto.Icon.Should().Be(currentForecast.Icon);
            dto.WindSpeed.Should().Be(currentForecast.Wind.Speed);
            dto.WindDirection.Should().Be(currentForecast.Wind.Direction);
            dto.Humidity.Should().Be(currentForecast.Humidity);
            dto.Pressure.Should().Be(currentForecast.Pressure);
            dto.Clouds.Should().Be(currentForecast.Clouds);
            dto.UvIndex.Should().Be(currentForecast.UvIndex);
        }

        [Test]
        public void Map_WeatherWithWindGust_ShouldMapGustValue()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var weather = new Domain.Entities.Weather(1, coordinate, "America/Los_Angeles", -7);
            var wind = new WindInfo(15, 270, 25); // With gust
            var forecast = CreateForecastWithWind(wind);
            weather.UpdateForecasts(new[] { forecast });

            // Act
            var dto = _mapper.Map<WeatherDto>(weather);

            // Assert
            dto.WindGust.Should().Be(25);
        }

        [Test]
        public void Map_WeatherToWeatherForecastDto_ShouldMapCorrectly()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var weather = new Domain.Entities.Weather(1, coordinate, "America/Los_Angeles", -7);
            var forecasts = TestDataBuilder.CreateValidWeatherForecasts(5);
            weather.UpdateForecasts(forecasts);

            // Act
            var dto = _mapper.Map<WeatherForecastDto>(weather);

            // Assert
            dto.Should().NotBeNull();
            dto.WeatherId.Should().Be(weather.Id);
            dto.LastUpdate.Should().BeCloseTo(weather.LastUpdate, System.TimeSpan.FromSeconds(1));
            dto.Timezone.Should().Be("America/Los_Angeles");
            dto.TimezoneOffset.Should().Be(-7);
            dto.DailyForecasts.Should().HaveCount(5);
        }

        [Test]
        public void Map_WeatherForecastToDetailDailyForecastDto_ShouldMapCorrectly()
        {
            // Arrange
            var forecast = TestDataBuilder.CreateValidWeatherForecast();
            forecast.SetMoonData(System.DateTime.Now, System.DateTime.Now.AddHours(12), 0.75);
            forecast.SetPrecipitation(5.5);

            // Act
            var dto = _mapper.Map<DailyForecastDto>(forecast);

            // Assert
            dto.Should().NotBeNull();
            dto.Date.Should().Be(forecast.Date);
            dto.Sunrise.Should().Be(forecast.Sunrise);
            dto.Sunset.Should().Be(forecast.Sunset);
            dto.Temperature.Should().Be(forecast.Temperature.Celsius);
            dto.MinTemperature.Should().Be(forecast.MinTemperature.Celsius);
            dto.MaxTemperature.Should().Be(forecast.MaxTemperature.Celsius);
            dto.Description.Should().Be(forecast.Description);
            dto.Icon.Should().Be(forecast.Icon);
            dto.WindSpeed.Should().Be(forecast.Wind.Speed);
            dto.WindDirection.Should().Be(forecast.Wind.Direction);
            dto.WindGust.Should().Be(forecast.Wind.Gust);
            dto.Humidity.Should().Be(forecast.Humidity);
            dto.Pressure.Should().Be(forecast.Pressure);
            dto.Clouds.Should().Be(forecast.Clouds);
            dto.UvIndex.Should().Be(forecast.UvIndex);
            dto.Precipitation.Should().Be(5.5);
            dto.MoonRise.Should().Be(forecast.MoonRise);
            dto.MoonSet.Should().Be(forecast.MoonSet);
            dto.MoonPhase.Should().Be(0.75);
        }

        [Test]
        public void Map_WeatherWithNoForecasts_ShouldMapDefaultValues()
        {
            // Arrange
            var coordinate = new Coordinate(47.6062, -122.3321);
            var weather = new Domain.Entities.Weather(1, coordinate, "America/Los_Angeles", -7);

            // Act
            var dto = _mapper.Map<WeatherDto>(weather);

            // Assert
            dto.Temperature.Should().Be(0);
            dto.Description.Should().BeEmpty();
            dto.Icon.Should().BeEmpty();
            dto.WindSpeed.Should().Be(0);
            dto.WindDirection.Should().Be(0);
            dto.WindGust.Should().BeNull();
        }

        [Test]
        public void Map_CollectionOfWeatherForecasts_ShouldMapAllItems()
        {
            // Arrange
            var forecasts = TestDataBuilder.CreateValidWeatherForecasts(7);

            // Act
            var dtos = _mapper.Map<DailyForecastDto[]>(forecasts);

            // Assert
            dtos.Should().HaveCount(7);
            dtos.Should().OnlyContain(dto => dto != null);
            dtos.First().Date.Should().Be(forecasts.First().Date);
            dtos.Last().Date.Should().Be(forecasts.Last().Date);
        }

        private Domain.Entities.WeatherForecast CreateForecastWithWind(WindInfo wind)
        {
            return new Domain.Entities.WeatherForecast(
                1,
                System.DateTime.Today,
                System.DateTime.Today.AddHours(6),
                System.DateTime.Today.AddHours(18),
                Temperature.FromCelsius(20),
                Temperature.FromCelsius(15),
                Temperature.FromCelsius(25),
                "Clear",
                "01d",
                wind,
                65,
                1013,
                10,
                5.0
            );
        }
    }

    // Placeholder for the actual implementation
    public class WeatherProfile : Profile
    {
        public WeatherProfile()
        {
            CreateMap<Domain.Entities.Weather, WeatherDto>()
                .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Coordinate.Latitude))
                .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Coordinate.Longitude))
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Temperature.Celsius : 0))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Description : string.Empty))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Icon : string.Empty))
                .ForMember(dest => dest.WindSpeed, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Wind.Speed : 0))
                .ForMember(dest => dest.WindDirection, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Wind.Direction : 0))
                .ForMember(dest => dest.WindGust, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Wind.Gust : null))
                .ForMember(dest => dest.Humidity, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Humidity : 0))
                .ForMember(dest => dest.Pressure, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Pressure : 0))
                .ForMember(dest => dest.Clouds, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Clouds : 0))
                .ForMember(dest => dest.UvIndex, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.UvIndex : 0))
                .ForMember(dest => dest.Precipitation, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Precipitation : null))
                .ForMember(dest => dest.Sunrise, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Sunrise : default))
                .ForMember(dest => dest.Sunset, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.Sunset : default))
                .ForMember(dest => dest.MoonRise, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.MoonRise : null))
                .ForMember(dest => dest.MoonSet, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.MoonSet : null))
                .ForMember(dest => dest.MoonPhase, opt => opt.MapFrom(src =>
                    src.GetCurrentForecast() != null ? src.GetCurrentForecast()!.MoonPhase : 0));

            CreateMap<Domain.Entities.Weather, WeatherForecastDto>()
                .ForMember(dest => dest.WeatherId, opt => opt.MapFrom(src => src.Id));

            CreateMap<Domain.Entities.WeatherForecast, DailyForecastDto>()
                .ForMember(dest => dest.Temperature, opt => opt.MapFrom(src => src.Temperature.Celsius))
                .ForMember(dest => dest.MinTemperature, opt => opt.MapFrom(src => src.MinTemperature.Celsius))
                .ForMember(dest => dest.MaxTemperature, opt => opt.MapFrom(src => src.MaxTemperature.Celsius))
                .ForMember(dest => dest.WindSpeed, opt => opt.MapFrom(src => src.Wind.Speed))
                .ForMember(dest => dest.WindDirection, opt => opt.MapFrom(src => src.Wind.Direction))
                .ForMember(dest => dest.WindGust, opt => opt.MapFrom(src => src.Wind.Gust));
        }
    }
}