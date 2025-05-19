using FluentAssertions;
using Location.Photography.Domain.Services;
using NUnit.Framework;
using System;

namespace Location.Photography.Domain.Tests.Services
{
    [TestFixture]
    public class ISunCalculatorServiceTests
    {
        private interface ISunCalculatorServiceTestImplementation : ISunCalculatorService { }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetSunriseMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetSunrise");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetSunsetMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetSunset");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetSolarNoonMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetSolarNoon");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetCivilDawnMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetCivilDawn");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetCivilDuskMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetCivilDusk");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetNauticalDawnMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetNauticalDawn");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetNauticalDuskMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetNauticalDusk");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetAstronomicalDawnMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetAstronomicalDawn");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetAstronomicalDuskMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetAstronomicalDusk");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(DateTime));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetSolarAzimuthMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetSolarAzimuth");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(double));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }

        [Test]
        public void ISunCalculatorService_ShouldDefineGetSolarElevationMethod()
        {
            // Arrange & Act & Assert
            var methodInfo = typeof(ISunCalculatorService).GetMethod("GetSolarElevation");
            methodInfo.Should().NotBeNull();
            methodInfo.ReturnType.Should().Be(typeof(double));
            var parameters = methodInfo.GetParameters();
            parameters.Length.Should().Be(3);
            parameters[0].ParameterType.Should().Be(typeof(DateTime));
            parameters[1].ParameterType.Should().Be(typeof(double));
            parameters[2].ParameterType.Should().Be(typeof(double));
        }
    }
}