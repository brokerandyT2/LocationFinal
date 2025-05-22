using FluentAssertions;
using Location.Core.Application.Weather.DTOs;
using Location.Core.BDD.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Location.Core.BDD.Tests.StepDefinitions
{
    [Binding]
    public class CommonSteps
    {
        private readonly ApiContext _context;
        private readonly ScenarioContext _scenarioContext;

        public CommonSteps(ApiContext context, ScenarioContext scenarioContext)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _scenarioContext = scenarioContext ?? throw new ArgumentNullException(nameof(scenarioContext));
            Console.WriteLine("CommonSteps constructor executed");
        }

        [Given(@"the application is initialized for testing")]
        public void GivenTheApplicationIsInitializedForTesting()
        {
            try
            {
                // The ApiContext is already initialized by the BeforeScenario hook
                // Here we can add any additional global initialization if needed
                Console.WriteLine("Application initialized successfully for testing");

                // Store this fact in the scenario context for future reference
                _scenarioContext["ApplicationInitialized"] = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing application for testing: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        [Then(@"I should receive a generic successful result")]
        public void ThenIShouldReceiveAGenericSuccessfulResult()
        {
            try
            {
                Console.WriteLine("Verifying generic successful result");
                var lastResult = _context.GetLastResult<object>();
                lastResult.Should().NotBeNull("Result should be available");
                lastResult.IsSuccess.Should().BeTrue("Operation should be successful");
                Console.WriteLine("Generic successful result verified");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ThenIShouldReceiveAGenericSuccessfulResult: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        [Then(@"I should receive a failure result")]
        public void ThenIShouldReceiveAFailureResult()
        {
            try
            {
                Console.WriteLine("Verifying failure result");

                // The issue is here - we need to get the result with the correct type
                // Instead of trying to cast it as a specific type, let's get the raw result

                // Check if there's a result with WeatherForecastDto type first
                var forecastResult = _context.GetLastResult<WeatherForecastDto>();
                if (forecastResult != null)
                {
                    forecastResult.IsSuccess.Should().BeFalse("Operation should have failed");
                    Console.WriteLine("Failure result verified (WeatherForecastDto)");
                    return;
                }

                // If not found, try different result types that might be used
                var weatherDtoResult = _context.GetLastResult<WeatherDto>();
                if (weatherDtoResult != null)
                {
                    weatherDtoResult.IsSuccess.Should().BeFalse("Operation should have failed");
                    Console.WriteLine("Failure result verified (WeatherDto)");
                    return;
                }

                // Try with other possible types
                var boolResult = _context.GetLastResult<bool>();
                if (boolResult != null)
                {
                    boolResult.IsSuccess.Should().BeFalse("Operation should have failed");
                    Console.WriteLine("Failure result verified (bool)");
                    return;
                }

                // Try with int type
                var intResult = _context.GetLastResult<int>();
                if (intResult != null)
                {
                    intResult.IsSuccess.Should().BeFalse("Operation should have failed");
                    Console.WriteLine("Failure result verified (int)");
                    return;
                }

                // If we get here, we couldn't find a valid result
                Assert.Fail("No result found in context. Make sure the result is properly stored.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ThenIShouldReceiveAFailureResult: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        [Then(@"the error message should contain ""(.*)""")]
        public void ThenTheErrorMessageShouldContain(string errorText)
        {
            try
            {
                Console.WriteLine($"Verifying error message contains '{errorText}'");
                var lastResult = _context.GetLastResult<object>();
                lastResult.Should().NotBeNull("Result should be available");
                lastResult.ErrorMessage.Should().Contain(errorText, $"Error message should contain '{errorText}'");
                Console.WriteLine("Error message content verified");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ThenTheErrorMessageShouldContain: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        // Helper method for waiting a short time if needed
        protected async Task WaitShortly()
        {
            try
            {
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while waiting: {ex.Message}");
            }
        }
    }
}