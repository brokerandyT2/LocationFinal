using FluentAssertions;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Tips.Queries.GetTipById;
using Location.Core.Application.Weather.DTOs;
using Location.Core.BDD.Tests.Support;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        [Then(@"I should receive a successful result")]
        public void ThenIShouldReceiveASuccessfulResult()
        {
            try
            {
                Console.WriteLine("Verifying successful result");

                // Try different result types in order of likelihood
                var resultTypes = new[]
                {
                    // Weather types
                    typeof(WeatherDto),
                    typeof(WeatherForecastDto),
                    
                    // Tip types - ADD MISSING TYPES HERE
                    typeof(TipDto),
                    typeof(List<TipDto>),
                    typeof(GetTipByIdQueryResponse),  // ✅ ADD THIS MISSING TYPE
                    typeof(TipTypeDto),
                    typeof(List<TipTypeDto>),
                    
                    // Location types
                    typeof(Application.Locations.DTOs.LocationDto),
                    typeof(List<Application.Locations.DTOs.LocationListDto>),
                    
                    // Setting types
                    typeof(Application.Settings.Commands.CreateSetting.CreateSettingCommandResponse),
                    typeof(Application.Settings.Commands.UpdateSetting.UpdateSettingCommandResponse),
                    typeof(Application.Settings.Queries.GetSettingByKey.GetSettingByKeyQueryResponse),
                    typeof(List<Application.Settings.Queries.GetAllSettings.GetAllSettingsQueryResponse>),
                    typeof(Dictionary<string, string>),
                    
                    // Common types
                    typeof(bool),
                    typeof(int)
                };

                foreach (var resultType in resultTypes)
                {
                    try
                    {
                        // Use reflection to call GetLastResult<T>() with the specific type
                        var method = typeof(ApiContext).GetMethod("GetLastResult");
                        var genericMethod = method.MakeGenericMethod(resultType);
                        var result = genericMethod.Invoke(_context, new object[] { "LastResult" });

                        if (result != null)
                        {
                            // Check if the result has IsSuccess property
                            var isSuccessProperty = result.GetType().GetProperty("IsSuccess");
                            if (isSuccessProperty != null)
                            {
                                var isSuccess = (bool)isSuccessProperty.GetValue(result);
                                if (isSuccess)
                                {
                                    Console.WriteLine($"Found successful result of type {resultType.Name}");
                                    return; // Success!
                                }
                                else
                                {
                                    // Found a result but it indicates failure
                                    var errorProperty = result.GetType().GetProperty("ErrorMessage");
                                    var errorMessage = errorProperty?.GetValue(result)?.ToString();
                                    Console.WriteLine($"Found failed result of type {resultType.Name}: {errorMessage}");

                                    Assert.Fail($"Operation failed: {errorMessage}");
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Continue to next type
                        Console.WriteLine($"Error checking type {resultType.Name}: {ex.Message}");
                    }
                }

                // If we get here, no result was found
                Console.WriteLine("No result found in context. Checking all stored keys...");

                // Debug: Try to see what's actually stored in the context
                try
                {
                    var contextType = _context.GetType();
                    var scenarioContextField = contextType.GetField("_scenarioContext",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    if (scenarioContextField != null)
                    {
                        var scenarioContext = scenarioContextField.GetValue(_context) as Dictionary<string, object>;
                        if (scenarioContext != null)
                        {
                            Console.WriteLine($"Context contains {scenarioContext.Count} items:");
                            foreach (var kvp in scenarioContext)
                            {
                                Console.WriteLine($"  Key: {kvp.Key}, Type: {kvp.Value?.GetType().Name ?? "null"}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inspecting context: {ex.Message}");
                }

                Assert.Fail("No result found in context. Make sure the result is properly stored.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ThenIShouldReceiveASuccessfulResult: {ex.Message}\n{ex.StackTrace}");
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