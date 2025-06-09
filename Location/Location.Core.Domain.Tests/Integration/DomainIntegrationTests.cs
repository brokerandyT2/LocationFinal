using FluentAssertions;
using Location.Core.Domain.Common;
using Location.Core.Domain.Events;
using Location.Core.Domain.Tests.Helpers;
using Location.Core.Domain.ValueObjects;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Integration
{
    [TestFixture]
    public class DomainIntegrationTests
    {
        [Test]
        public void Location_CompleteLifecycle_ShouldRaiseAppropriateEvents()
        {
            // Arrange
            var coordinate = TestDataBuilder.CreateValidCoordinate();
            var address = TestDataBuilder.CreateValidAddress();

            // Act - Create location
            var location = new Location.Core.Domain.Entities.Location(
                "Space Needle",
                "Iconic Seattle landmark",
                coordinate,
                address
            );

            // Assert - Creation should raise LocationSavedEvent
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.First().Should().BeOfType<LocationSavedEvent>();

            // Act - Update details
            location.ClearDomainEvents();
            location.UpdateDetails("Space Needle - Updated", "Updated description");

            // Assert - Update should raise LocationSavedEvent
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.First().Should().BeOfType<LocationSavedEvent>();

            // Act - Attach photo
            location.ClearDomainEvents();
            location.AttachPhoto("/photos/space-needle.jpg");

            // Assert - Photo attachment should raise PhotoAttachedEvent
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.First().Should().BeOfType<PhotoAttachedEvent>();

            // Act - Delete location
            location.ClearDomainEvents();
            location.Delete();

            // Assert - Deletion should raise LocationDeletedEvent
            location.DomainEvents.Should().ContainSingle();
            location.DomainEvents.First().Should().BeOfType<LocationDeletedEvent>();
            location.IsDeleted.Should().BeTrue();

            // Act - Restore location
            location.ClearDomainEvents();
            location.Restore();

            // Assert - Restore should not raise events
            location.DomainEvents.Should().BeEmpty();
            location.IsDeleted.Should().BeFalse();
        }

        [Test]
        public void Weather_CompleteLifecycle_ShouldRaiseAppropriateEvents()
        {
            // Arrange
            var weather = TestDataBuilder.CreateValidWeather();
            var forecasts = TestDataBuilder.CreateValidWeatherForecasts(7);

            // Act - Update forecasts
            weather.UpdateForecasts(forecasts);

            // Assert - Should raise WeatherUpdatedEvent
            weather.DomainEvents.Should().ContainSingle();
            weather.DomainEvents.First().Should().BeOfType<WeatherUpdatedEvent>();
            weather.Forecasts.Count.Should().Be(7);

            // Act - Get current forecast
            var currentForecast = weather.GetCurrentForecast();

            // Assert - Should return today's forecast
            currentForecast.Should().NotBeNull();
            currentForecast?.Date.Date.Should().Be(DateTime.Today);

            // Act - Get specific date forecast
            var specificDate = DateTime.Today.AddDays(3);
            var specificForecast = weather.GetForecastForDate(specificDate);

            // Assert - Should return correct forecast
            specificForecast.Should().NotBeNull();
            specificForecast?.Date.Date.Should().Be(specificDate.Date);
        }

        [Test]
        public void TipType_WithTips_ShouldManageCollection()
        {
            // Arrange
            var tipType = TestDataBuilder.CreateValidTipType("Landscape Photography");
            SetEntityId(tipType, 1); // Simulate persisted entity

            var tip1 = TestDataBuilder.CreateValidTip(
                tipTypeId: 1,
                title: "Golden Hour",
                content: "Best light conditions"
            );

            var tip2 = TestDataBuilder.CreateValidTip(
                tipTypeId: 1,
                title: "Blue Hour",
                content: "Twilight photography"
            );

            // Act - Add tips
            tipType.AddTip(tip1);
            tipType.AddTip(tip2);

            // Assert - Should contain both tips
            tipType.Tips.Count.Should().Be(2);
            tipType.Tips.Should().Contain(tip1);
            tipType.Tips.Should().Contain(tip2);

            // Act - Remove tip
            tipType.RemoveTip(tip1);

            // Assert - Should only contain tip2
            tipType.Tips.Count.Should().Be(1);
            tipType.Tips.Should().Contain(tip2);
            tipType.Tips.Should().NotContain(tip1);
        }

        [Test]
        public void WeatherForecast_WithMoonData_ShouldCalculatePhaseCorrectly()
        {
            // Arrange
            var forecast = TestDataBuilder.CreateValidWeatherForecast();

            // Act - Set various moon phases and check descriptions
            var phaseTests = new[]
            {
                (0.0, "New Moon"),
                (0.25, "First Quarter"),
                (0.5, "Full Moon"),
                (0.75, "Last Quarter"),
                (0.1, "Waxing Crescent"),
                (0.4, "Waxing Gibbous"),
                (0.6, "Waning Gibbous"),
                (0.9, "Waning Crescent")
            };

            foreach (var (phase, expectedDescription) in phaseTests)
            {
                forecast.SetMoonData(DateTime.Now, DateTime.Now.AddHours(12), phase);

                // Assert
                forecast.MoonPhase.Should().Be(phase);
                forecast.GetMoonPhaseDescription().Should().Be(expectedDescription);
            }
        }

        [Test]
        public void Setting_TypeConversions_ShouldWorkCorrectly()
        {
            // Arrange & Act - Boolean setting
            var boolSetting = TestDataBuilder.CreateValidSetting("enable_feature", "true");
            var boolValue = boolSetting.GetBooleanValue();

            // Assert
            boolValue.Should().BeTrue();

            // Arrange & Act - Integer setting
            var intSetting = TestDataBuilder.CreateValidSetting("max_items", "42");
            var intValue = intSetting.GetIntValue();

            // Assert
            intValue.Should().Be(42);

            // Arrange & Act - DateTime setting
            var dateTime = new DateTime(2024, 1, 15, 14, 30, 0);
            var dateSetting = TestDataBuilder.CreateValidSetting("last_update", dateTime.ToString());
            var dateValue = dateSetting.GetDateTimeValue();

            // Assert
            dateValue.Should().NotBeNull();
            dateValue.Value.Should().Be(dateTime);

            // Arrange & Act - Invalid values
            var invalidSetting = TestDataBuilder.CreateValidSetting("invalid", "not_a_number");
            var invalidBool = invalidSetting.GetBooleanValue();
            var invalidInt = invalidSetting.GetIntValue(99);
            var invalidDate = invalidSetting.GetDateTimeValue();

            // Assert
            invalidBool.Should().BeFalse();
            invalidInt.Should().Be(99);
            invalidDate.Should().BeNull();
        }

        [Test]
        public void ValueObjects_Equality_ShouldWorkCorrectly()
        {
            // Arrange
            var coord1 = new Coordinate(47.6062, -122.3321);
            var coord2 = new Coordinate(47.6062, -122.3321);
            var coord3 = new Coordinate(47.6063, -122.3321);

            var addr1 = new Address("Seattle", "WA");
            var addr2 = new Address("SEATTLE", "wa"); // Different case
            var addr3 = new Address("Portland", "OR");

            var temp1 = Temperature.FromCelsius(20);
            var temp2 = Temperature.FromFahrenheit(68); // Approximately 20°C
            var temp3 = Temperature.FromCelsius(21);

            // Act & Assert - Equality
            coord1.Equals(coord2).Should().BeTrue();
            coord1.Equals(coord3).Should().BeFalse();
            (coord1 == coord2).Should().BeTrue();
            (coord1 != coord3).Should().BeTrue();

            addr1.Equals(addr2).Should().BeTrue(); // Case insensitive
            addr1.Equals(addr3).Should().BeFalse();

            temp1.Equals(temp3).Should().BeFalse();
            temp1.GetHashCode().Should().NotBe(temp3.GetHashCode());
        }

        [Test]
        public void DomainValidation_Integration_ShouldValidateCompleteDomain()
        {
            // Arrange - Create a complete domain scenario
            var location = TestDataBuilder.CreateValidLocation();
            SetEntityId(location, 1); // Simulate persisted entity

            var weather = TestDataBuilder.CreateValidWeather(locationId: 1);
            var forecasts = TestDataBuilder.CreateValidWeatherForecasts(7);
            weather.UpdateForecasts(forecasts);

            var tipType = TestDataBuilder.CreateValidTipType("Photography Tips");
            SetEntityId(tipType, 1);

            var tip1 = TestDataBuilder.CreateValidTip(tipTypeId: 1);
            var tip2 = TestDataBuilder.CreateValidTip(tipTypeId: 1);

            tipType.AddTip(tip1);
            tipType.AddTip(tip2);

            var settings = new[]
            {
                TestDataBuilder.CreateValidSetting("theme", "dark"),
                TestDataBuilder.CreateValidSetting("language", "en-US"),
                TestDataBuilder.CreateValidSetting("max_locations", "100")
            };

            // Act - Perform various domain operations
            location.AttachPhoto("/photos/test.jpg");
            location.UpdateDetails("Updated Location", "Updated Description");

            weather.UpdateForecasts(TestDataBuilder.CreateValidWeatherForecasts(5));

            tip1.UpdatePhotographySettings("f/4", "1/250", "ISO 200");
            tip2.SetLocalization("es-ES");

            settings[0].UpdateValue("light");

            // Assert - Verify domain state
            location.DomainEvents.Count.Should().BeGreaterThan(0);
            location.PhotoPath.Should().NotBeNull();

            weather.Forecasts.Count.Should().Be(5);
            weather.LastUpdate.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));

            tipType.Tips.Count.Should().Be(2);
            tip1.Fstop.Should().Be("f/4");
            tip2.I8n.Should().Be("es-ES");

            settings[0].Value.Should().Be("light");
            settings[2].GetIntValue().Should().Be(100);
        }

        // Helper method to set entity Id using reflection
        private void SetEntityId(Entity entity, int id)
        {
            var idProperty = entity.GetType().GetProperty("Id");
            idProperty.SetValue(entity, id);
        }
    }
}