using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.BDD.Tests.Support;
using Location.Photography.Domain.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechTalk.SpecFlow;

namespace Location.Photography.BDD.Tests.StepDefinitions
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
            Console.WriteLine("Photography CommonSteps constructor executed");
        }

        [Given(@"the photography application is initialized for testing")]
        public void GivenThePhotographyApplicationIsInitializedForTesting()
        {
            try
            {
                // The ApiContext is already initialized by the BeforeScenario hook
                // Here we can add any additional photography-specific initialization if needed
                Console.WriteLine("Photography application initialized successfully for testing");

                // Store this fact in the scenario context for future reference
                _scenarioContext["PhotographyApplicationInitialized"] = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error initializing photography application for testing: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        [Then(@"I should receive a successful photography result")]
        public void ThenIShouldReceiveASuccessfulPhotographyResult()
        {
            try
            {
                Console.WriteLine("Verifying successful photography result");

                // FIXED: Added missing result types for sun position tracking scenarios
                var resultTypes = new[]
                {
                   // Exposure calculator types
                   typeof(ExposureSettingsDto),
                   typeof(string[]), // For shutter speeds, apertures, ISOs
                   
                   // Sun calculation types
                   typeof(SunTimesDto),
                   typeof(SunPositionDto),
                   typeof(Dictionary<string, DateTime>), // Golden hour times
                   
                   // Scene evaluation types
                   typeof(SceneEvaluationResultDto),
                   typeof(Dictionary<string, string>), // Histogram paths
                   typeof(Dictionary<string, double>), // Color analysis
                   typeof(Dictionary<string, object>), // Color cast detection, color comparison
                   
                   // Test model types
                   typeof(Models.ExposureTestModel),
                   typeof(Models.SunCalculationTestModel),
                   typeof(Models.SceneEvaluationTestModel),
                   
                   // FIXED: Added missing collection types for tracking scenarios
                   typeof(List<Models.ExposureTestModel>),
                   typeof(List<Models.SunCalculationTestModel>), // For sun position tracking
                   typeof(List<Models.SceneEvaluationTestModel>),
                   
                   // Common types
                   typeof(bool),
                   typeof(int),
                   typeof(string)
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
                                    Console.WriteLine($"Found successful photography result of type {resultType.Name}");
                                    return; // Success!
                                }
                                else
                                {
                                    // Found a result but it indicates failure
                                    var errorProperty = result.GetType().GetProperty("ErrorMessage");
                                    var errorMessage = errorProperty?.GetValue(result)?.ToString();
                                    Console.WriteLine($"Found failed photography result of type {resultType.Name}: {errorMessage}");

                                    throw new AssertionException($"Photography operation failed: {errorMessage}");
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Continue to next type
                        Console.WriteLine($"Error checking photography type {resultType.Name}: {ex.Message}");
                    }
                }

                // If we get here, no result was found
                Console.WriteLine("No photography result found in context. Checking all stored keys...");

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
                            Console.WriteLine($"Photography context contains {scenarioContext.Count} items:");
                            foreach (var kvp in scenarioContext)
                            {
                                Console.WriteLine($"  Key: {kvp.Key}, Type: {kvp.Value?.GetType().Name ?? "null"}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error inspecting photography context: {ex.Message}");
                }

                throw new AssertionException("No photography result found in context. Make sure the result is properly stored.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ThenIShouldReceiveASuccessfulPhotographyResult: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        [Then(@"I should receive a photography failure result")]
        public void ThenIShouldReceiveAPhotographyFailureResult()
        {
            try
            {
                Console.WriteLine("Verifying photography failure result");

                // Check different result types that might be used in photography scenarios
                var resultTypes = new[]
                {
                   typeof(ExposureSettingsDto),
                   typeof(SunTimesDto),
                   typeof(SunPositionDto),
                   typeof(SceneEvaluationResultDto),
                   typeof(Dictionary<string, double>), // Color analysis
                   typeof(Dictionary<string, object>), // Color cast, comparison
                   typeof(Models.ExposureTestModel),
                   typeof(Models.SunCalculationTestModel),
                   typeof(Models.SceneEvaluationTestModel),
                   typeof(List<Models.SunCalculationTestModel>), // FIXED: Added for tracking scenarios
                   typeof(bool),
                   typeof(int),
                   typeof(string)
               };

                foreach (var resultType in resultTypes)
                {
                    try
                    {
                        var method = typeof(ApiContext).GetMethod("GetLastResult");
                        var genericMethod = method.MakeGenericMethod(resultType);
                        var result = genericMethod.Invoke(_context, new object[] { "LastResult" });

                        if (result != null)
                        {
                            var isSuccessProperty = result.GetType().GetProperty("IsSuccess");
                            if (isSuccessProperty != null)
                            {
                                var isSuccess = (bool)isSuccessProperty.GetValue(result);
                                if (!isSuccess)
                                {
                                    Console.WriteLine($"Found failed photography result of type {resultType.Name}");
                                    return; // Found the failure result
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking photography failure type {resultType.Name}: {ex.Message}");
                    }
                }

                throw new AssertionException("No photography failure result found in context.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ThenIShouldReceiveAPhotographyFailureResult: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        [Then(@"the photography error message should contain ""(.*)""")]
        public void ThenThePhotographyErrorMessageShouldContain(string errorText)
        {
            try
            {
                Console.WriteLine($"Verifying photography error message contains '{errorText}'");

                // Try to get any result with an error message
                var resultTypes = new[]
                {
                   typeof(ExposureSettingsDto),
                   typeof(SunTimesDto),
                   typeof(SunPositionDto),
                   typeof(SceneEvaluationResultDto),
                   typeof(Dictionary<string, double>), // Color analysis
                   typeof(Dictionary<string, object>), // Color cast, comparison
                   typeof(Models.ExposureTestModel),
                   typeof(Models.SunCalculationTestModel),
                   typeof(Models.SceneEvaluationTestModel),
                   typeof(List<Models.SunCalculationTestModel>), // FIXED: Added for tracking scenarios
                   typeof(bool),
                   typeof(string)
               };

                foreach (var resultType in resultTypes)
                {
                    try
                    {
                        var method = typeof(ApiContext).GetMethod("GetLastResult");
                        var genericMethod = method.MakeGenericMethod(resultType);
                        var result = genericMethod.Invoke(_context, new object[] { "LastResult" });

                        if (result != null)
                        {
                            var errorProperty = result.GetType().GetProperty("ErrorMessage");
                            if (errorProperty != null)
                            {
                                var errorMessage = errorProperty.GetValue(result)?.ToString();
                                if (!string.IsNullOrEmpty(errorMessage))
                                {
                                    errorMessage.Should().Contain(errorText, $"Photography error message should contain '{errorText}'");
                                    Console.WriteLine("Photography error message content verified");
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking photography error in type {resultType.Name}: {ex.Message}");
                    }
                }

                throw new AssertionException($"No photography error message found containing '{errorText}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ThenThePhotographyErrorMessageShouldContain: {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        [Then(@"the photography result should not be null")]
        public void ThenThePhotographyResultShouldNotBeNull()
        {
            try
            {
                Console.WriteLine("Verifying photography result is not null");

                // Check if any result exists in context
                bool foundResult = false;
                var resultTypes = new[]
                {
                   typeof(ExposureSettingsDto),
                   typeof(SunTimesDto),
                   typeof(SunPositionDto),
                   typeof(SceneEvaluationResultDto),
                   typeof(Dictionary<string, double>), // Color analysis
                   typeof(Dictionary<string, object>), // Color cast, comparison
                   typeof(Models.ExposureTestModel),
                   typeof(Models.SunCalculationTestModel),
                   typeof(Models.SceneEvaluationTestModel),
                   typeof(List<Models.SunCalculationTestModel>) // FIXED: Added for tracking scenarios
               };

                foreach (var resultType in resultTypes)
                {
                    try
                    {
                        var method = typeof(ApiContext).GetMethod("GetLastResult");
                        var genericMethod = method.MakeGenericMethod(resultType);
                        var result = genericMethod.Invoke(_context, new object[] { "LastResult" });

                        if (result != null)
                        {
                            Console.WriteLine($"Found non-null photography result of type {resultType.Name}");
                            foundResult = true;
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error checking null for photography type {resultType.Name}: {ex.Message}");
                    }
                }

                foundResult.Should().BeTrue("Photography result should not be null");
                Console.WriteLine("Photography result null check verified");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ThenThePhotographyResultShouldNotBeNull: {ex.Message}\n{ex.StackTrace}");
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