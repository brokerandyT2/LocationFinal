using NUnit.Framework;
using FluentAssertions;
using AutoMapper;
using Location.Core.Application.Mappings; // Make sure this is using the correct namespace
using Location.Core.Application.Weather.DTOs;
using Location.Core.Application.Tests.Utilities;
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
                cfg.AddProfile<Location.Core.Application.Mappings.WeatherProfile>(); // Use the actual implementation
            });

            _mapper = config.CreateMapper();
        }

        [Test]
        public void Configuration_ShouldBeValid()
        {
            // Arrange
            var config = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<Location.Core.Application.Mappings.WeatherProfile>(); // Use the actual implementation
            });

            // Act & Assert
            config.AssertConfigurationIsValid();
        }

        // ... rest of the tests remain the same, but make sure to remove the placeholder WeatherProfile class at the bottom
    }
}