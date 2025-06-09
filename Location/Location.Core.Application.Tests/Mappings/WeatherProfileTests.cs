using AutoMapper;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Mappings
{
    [Category("Tip")]
    [Category("Profile")]
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