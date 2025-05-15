
using NUnit.Framework;
using FluentAssertions;
using Location.Core.Infrastructure.External.Models;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Location.Core.Infrastructure.Tests.External.Models
{
    [TestFixture]
    public class OpenWeatherResponseTests
    {
        [Test]
        public void OpenWeatherResponse_ShouldDeserializeFromJson()
        {
            // Arrange
            var json = @"{
                ""lat"": 47.6062,
                ""lon"": -122.3321,
                ""timezone"": ""America/Los_Angeles"",
                ""timezone_offset"": -28800,
                ""current"": {
                    ""dt"": 1640000000,
                    ""sunrise"": 1639991234,
                    ""sunset"": 1640025678,
                    ""temp"": 283.15,
                    ""feels_like"": 281.15,
                    ""pressure"": 1013,
                    ""humidity"": 65,
                    ""dew_point"": 276.15,
                    ""uvi"": 5.5,
                    ""clouds"": 75,
                    ""visibility"": 10000,
                    ""wind_speed"": 5.14,
                    ""wind_deg"": 180,
                    ""wind_gust"": 8.23,
                    ""weather"": [{
                        ""id"": 803,
                        ""main"": ""Clouds"",
                        ""description"": ""broken clouds"",
                        ""icon"": ""04d""
                    }]
                },
                ""daily"": [{
                    ""dt"": 1640000000,
                    ""sunrise"": 1639991234,
                    ""sunset"": 1640025678,
                    ""moonrise"": 1640012345,
                    ""moonset"": 1640056789,
                    ""moon_phase"": 0.5,
                    ""temp"": {
                        ""day"": 285.15,
                        ""min"": 280.15,
                        ""max"": 290.15,
                        ""night"": 282.15,
                        ""eve"": 284.15,
                        ""morn"": 281.15
                    },
                    ""feels_like"": {
                        ""day"": 283.15,
                        ""night"": 280.15,
                        ""eve"": 282.15,
                        ""morn"": 279.15
                    },
                    ""pressure"": 1015,
                    ""humidity"": 70,
                    ""dew_point"": 279.15,
                    ""wind_speed"": 6.2,
                    ""wind_deg"": 225,
                    ""wind_gust"": 10.5,
                    ""weather"": [{
                        ""id"": 500,
                        ""main"": ""Rain"",
                        ""description"": ""light rain"",
                        ""icon"": ""10d""
                    }],
                    ""clouds"": 45,
                    ""pop"": 0.8,
                    ""rain"": 2.5,
                    ""uvi"": 7.2
                }]
            }";

            // Act
            var response = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

            // Assert
            response.Should().NotBeNull();
            response!.Lat.Should().Be(47.6062);
            response.Lon.Should().Be(-122.3321);
            response.Timezone.Should().Be("America/Los_Angeles");
            response.TimezoneOffset.Should().Be(-28800);

            response.Current.Should().NotBeNull();
            response.Current.Dt.Should().Be(1640000000);
            response.Current.Temp.Should().Be(283.15);
            response.Current.Humidity.Should().Be(65);
            response.Current.WindSpeed.Should().Be(5.14);
            response.Current.WindGust.Should().Be(8.23);

            response.Current.Weather.Should().HaveCount(1);
            response.Current.Weather[0].Main.Should().Be("Clouds");
            response.Current.Weather[0].Description.Should().Be("broken clouds");

            response.Daily.Should().HaveCount(1);
            response.Daily[0].Temp.Day.Should().Be(285.15);
            response.Daily[0].Rain.Should().Be(2.5);
            response.Daily[0].MoonPhase.Should().Be(0.5);
        }

        [Test]
        public void OpenWeatherResponse_ShouldHandleNullOptionalFields()
        {
            // Arrange
            var json = @"{
                ""lat"": 47.6062,
                ""lon"": -122.3321,
                ""timezone"": ""America/Los_Angeles"",
                ""timezone_offset"": -28800,
                ""current"": {
                    ""dt"": 1640000000,
                    ""sunrise"": 1639991234,
                    ""sunset"": 1640025678,
                    ""temp"": 283.15,
                    ""feels_like"": 281.15,
                    ""pressure"": 1013,
                    ""humidity"": 65,
                    ""dew_point"": 276.15,
                    ""uvi"": 5.5,
                    ""clouds"": 75,
                    ""visibility"": 10000,
                    ""wind_speed"": 5.14,
                    ""wind_deg"": 180,
                    ""weather"": []
                },
                ""daily"": []
            }";

            // Act
            var response = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

            // Assert
            response.Should().NotBeNull();
            response!.Current.WindGust.Should().BeNull();
            response.Current.Weather.Should().BeEmpty();
            response.Daily.Should().BeEmpty();
        }

        [Test]
        public void CurrentWeather_ShouldInitializeWithDefaults()
        {
            // Act
            var current = new CurrentWeather();

            // Assert
            current.Dt.Should().Be(0);
            current.Temp.Should().Be(0);
            current.WindGust.Should().BeNull();
            current.Weather.Should().NotBeNull();
            current.Weather.Should().BeEmpty();
        }

        [Test]
        public void DailyForecast_ShouldInitializeWithDefaults()
        {
            // Act
            var daily = new DailyForecast();

            // Assert
            daily.Dt.Should().Be(0);
            daily.Temp.Should().NotBeNull();
            daily.FeelsLike.Should().NotBeNull();
            daily.Weather.Should().NotBeNull();
            daily.Weather.Should().BeEmpty();
            daily.WindGust.Should().BeNull();
            daily.Rain.Should().BeNull();
        }

        [Test]
        public void WeatherDescription_ShouldInitializeWithDefaults()
        {
            // Act
            var weather = new WeatherDescription();

            // Assert
            weather.Id.Should().Be(0);
            weather.Main.Should().BeEmpty();
            weather.Description.Should().BeEmpty();
            weather.Icon.Should().BeEmpty();
        }

        [Test]
        public void DailyTemperature_ShouldInitializeWithDefaults()
        {
            // Act
            var temp = new DailyTemperature();

            // Assert
            temp.Day.Should().Be(0);
            temp.Min.Should().Be(0);
            temp.Max.Should().Be(0);
            temp.Night.Should().Be(0);
            temp.Eve.Should().Be(0);
            temp.Morn.Should().Be(0);
        }

        [Test]
        public void DailyFeelsLike_ShouldInitializeWithDefaults()
        {
            // Act
            var feelsLike = new DailyFeelsLike();

            // Assert
            feelsLike.Day.Should().Be(0);
            feelsLike.Night.Should().Be(0);
            feelsLike.Eve.Should().Be(0);
            feelsLike.Morn.Should().Be(0);
        }

        [Test]
        public void OpenWeatherResponse_ShouldSerializeToJson()
        {
            // Arrange
            var response = new OpenWeatherResponse
            {
                Lat = 47.6062,
                Lon = -122.3321,
                Timezone = "America/Los_Angeles",
                TimezoneOffset = -28800,
                Current = new CurrentWeather
                {
                    Dt = 1640000000,
                    Temp = 283.15,
                    Weather = new List<WeatherDescription>
                    {
                        new WeatherDescription
                        {
                            Id = 800,
                            Main = "Clear",
                            Description = "clear sky",
                            Icon = "01d"
                        }
                    }
                },
                Daily = new List<DailyForecast>
                {
                    new DailyForecast
                    {
                        Dt = 1640000000,
                        Temp = new DailyTemperature { Day = 285.15 }
                    }
                }
            };

            // Act
            var json = JsonSerializer.Serialize(response);

            // Assert
            json.Should().Contain("\"lat\":47.6062");
            json.Should().Contain("\"timezone\":\"America/Los_Angeles\"");
            json.Should().Contain("\"temp\":283.15");
            json.Should().Contain("\"description\":\"clear sky\"");
        }

        [Test]
        public void OpenWeatherResponse_ShouldHandleEmptyCollections()
        {
            // Arrange
            var response = new OpenWeatherResponse
            {
                Lat = 47.6062,
                Lon = -122.3321,
                Timezone = "UTC",
                TimezoneOffset = 0,
                Current = new CurrentWeather
                {
                    Weather = new List<WeatherDescription>()
                },
                Daily = new List<DailyForecast>()
            };

            // Act
            var json = JsonSerializer.Serialize(response);
            var deserialized = JsonSerializer.Deserialize<OpenWeatherResponse>(json);

            // Assert
            deserialized.Should().NotBeNull();
            deserialized!.Current.Weather.Should().BeEmpty();
            deserialized.Daily.Should().BeEmpty();
        }

        [Test]
        public void DailyForecast_WithCompleteData_ShouldDeserialize()
        {
            // Arrange
            var json = @"{
                ""dt"": 1640000000,
                ""sunrise"": 1639991234,
                ""sunset"": 1640025678,
                ""moonrise"": 1640012345,
                ""moonset"": 1640056789,
                ""moon_phase"": 0.25,
                ""temp"": {
                    ""day"": 15.5,
                    ""min"": 10.2,
                    ""max"": 20.8,
                    ""night"": 12.3,
                    ""eve"": 14.5,
                    ""morn"": 11.2
                },
                ""pressure"": 1015,
                ""humidity"": 70,
                ""wind_speed"": 3.5,
                ""wind_deg"": 180,
                ""wind_gust"": 5.2,
                ""clouds"": 45,
                ""pop"": 0.8,
                ""rain"": 2.5,
                ""uvi"": 7.2
            }";

            // Act
            var forecast = JsonSerializer.Deserialize<DailyForecast>(json);

            // Assert
            forecast.Should().NotBeNull();
            forecast!.MoonPhase.Should().Be(0.25);
            forecast.Pop.Should().Be(0.8);
            forecast.Rain.Should().Be(2.5);
            forecast.Pressure.Should().Be(1015);
            forecast.WindGust.Should().Be(5.2);
        }
    }
}