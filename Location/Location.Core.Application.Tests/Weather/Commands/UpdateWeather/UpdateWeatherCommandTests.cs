using NUnit.Framework;
using FluentAssertions;
using Location.Core.Application.Commands.Weather;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Weather.DTOs;
using MediatR;

namespace Location.Core.Application.Tests.Weather.Commands.UpdateWeather
{
    [Category("Weather")]
    [Category("Update")]

    [TestFixture]
    public class UpdateWeatherCommandTests
    {
        [Test]
        public void Constructor_WithDefaultValues_ShouldInitializeProperties()
        {
            // Act
            var command = new UpdateWeatherCommand();

            // Assert
            command.LocationId.Should().Be(0);
            command.ForceUpdate.Should().BeFalse();
        }

        [Test]
        public void Properties_WhenSet_ShouldRetainValues()
        {
            // Arrange
            var command = new UpdateWeatherCommand();

            // Act
            command.LocationId = 42;
            command.ForceUpdate = true;

            // Assert
            command.LocationId.Should().Be(42);
            command.ForceUpdate.Should().BeTrue();
        }

        [Test]
        public void Command_ShouldImplementIRequest()
        {
            // Arrange & Act
            var command = new UpdateWeatherCommand();

            // Assert
            command.Should().BeAssignableTo<IRequest<Result<WeatherDto>>>();
        }

        [Test]
        public void ObjectInitializer_ShouldSetProperties()
        {
            // Act
            var command = new UpdateWeatherCommand
            {
                LocationId = 123,
                ForceUpdate = true
            };

            // Assert
            command.LocationId.Should().Be(123);
            command.ForceUpdate.Should().BeTrue();
        }

        [Test]
        public void Create_WithLocationIdOnly_ShouldHaveCorrectDefaultValues()
        {
            // Act
            var command = new UpdateWeatherCommand { LocationId = 999 };

            // Assert
            command.LocationId.Should().Be(999);
            command.ForceUpdate.Should().BeFalse();
        }

        [Test]
        public void Create_WithZeroLocationId_ShouldBeAllowed()
        {
            // Act
            var command = new UpdateWeatherCommand { LocationId = 0 };

            // Assert
            command.LocationId.Should().Be(0);
        }

        [Test]
        public void Create_WithNegativeLocationId_ShouldBeAllowed()
        {
            // Act
            var command = new UpdateWeatherCommand { LocationId = -1 };

            // Assert
            command.LocationId.Should().Be(-1);
        }

        [Test]
        public void Create_WithMaxIntLocationId_ShouldBeAllowed()
        {
            // Act
            var command = new UpdateWeatherCommand { LocationId = int.MaxValue };

            // Assert
            command.LocationId.Should().Be(int.MaxValue);
        }

        [Test]
        public void Create_WithForceUpdateTrue_ShouldSetCorrectly()
        {
            // Act
            var command = new UpdateWeatherCommand
            {
                LocationId = 1,
                ForceUpdate = true
            };

            // Assert
            command.ForceUpdate.Should().BeTrue();
        }

        [Test]
        public void Create_WithForceUpdateFalse_ShouldSetCorrectly()
        {
            // Act
            var command = new UpdateWeatherCommand
            {
                LocationId = 1,
                ForceUpdate = false
            };

            // Assert
            command.ForceUpdate.Should().BeFalse();
        }
    }
}