using BoDi;
using Location.Core.BDD.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using System;
using TechTalk.SpecFlow;

namespace Location.Core.BDD.Tests.Hooks
{
    /// <summary>
    /// Contains hooks for test initialization and cleanup at the test run and feature levels
    /// </summary>
    [Binding]
    public class TestInitialization
    {
        // Global ServiceProvider to ensure services stay alive
        public static ServiceProvider GlobalServiceProvider { get; private set; }

        // Store ObjectContainer reference for cleanup
        private static IObjectContainer _globalContainer;

        [BeforeTestRun]
        public static void BeforeTestRun(IObjectContainer objectContainer)
        {
            _globalContainer = objectContainer;

            // Setup global services
            var services = new ServiceCollection();
            services.AddLogging();

            // Register singleton services that should persist across the test run
            services.AddSingleton<TestServiceProvider>();

            // Build service provider
            GlobalServiceProvider = services.BuildServiceProvider();

            // Register the TestServiceProvider in the SpecFlow container
            var testServiceProvider = GlobalServiceProvider.GetRequiredService<TestServiceProvider>();
            objectContainer.RegisterInstanceAs(testServiceProvider);
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            // Properly dispose of resources
            GlobalServiceProvider?.Dispose();
            GlobalServiceProvider = null;
            _globalContainer = null;
        }

        [BeforeFeature]
        public static void BeforeFeature(FeatureContext featureContext)
        {
            // You can initialize feature-specific resources here
            Console.WriteLine($"Starting feature: {featureContext.FeatureInfo.Title}");
        }

        [AfterFeature]
        public static void AfterFeature(FeatureContext featureContext)
        {
            // Clean up feature-specific resources
            Console.WriteLine($"Finished feature: {featureContext.FeatureInfo.Title}");
        }

        [BeforeScenarioBlock]
        public static void BeforeScenarioBlock(IObjectContainer container, ScenarioContext scenarioContext)
        {
            // Ensure ApiContext is available for each scenario block
            if (!container.IsRegistered<ApiContext>())
            {
                try
                {
                    // Get TestServiceProvider from global container
                    var testServiceProvider = container.Resolve<TestServiceProvider>();

                    // Create and register ApiContext
                    var apiContext = new ApiContext();
                    container.RegisterInstanceAs(apiContext);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BeforeScenarioBlock: {ex.Message}");
                    throw;
                }
            }
        }

        [AfterScenario]
        public static void AfterScenario(ScenarioContext scenarioContext)
        {
            // Add any scenario cleanup here if needed
            Console.WriteLine($"Finished scenario: {scenarioContext.ScenarioInfo.Title}");
        }
    }
}