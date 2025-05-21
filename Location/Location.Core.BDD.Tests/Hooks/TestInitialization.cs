using BoDi;
using Location.Core.BDD.Tests.Support;
using Microsoft.Extensions.DependencyInjection;
using System;
using TechTalk.SpecFlow;

namespace Location.Core.BDD.Tests.Hooks
{
    [Binding]
    public class TestInitialization
    {
        // Global ServiceProvider to ensure services stay alive
        public static ServiceProvider GlobalServiceProvider { get; private set; }

        // Container storage to maintain reference for cleanup
        private static Dictionary<string, ApiContext> _contextStorage = new Dictionary<string, ApiContext>();

        [BeforeTestRun]
        public static void BeforeTestRun(IObjectContainer objectContainer)
        {
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

            // Create storage for contexts
            objectContainer.RegisterInstanceAs(_contextStorage);
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            // Properly dispose of resources
            GlobalServiceProvider?.Dispose();
            GlobalServiceProvider = null;

            // Clear context storage
            if (_contextStorage != null)
            {
                foreach (var context in _contextStorage.Values)
                {
                    try { context?.ClearContext(); } catch { }
                }
                _contextStorage.Clear();
            }
        }

        [BeforeScenario(Order = -1000)]
        public static void BeforeScenario(IObjectContainer container, ScenarioContext scenarioContext)
        {
            try
            {
                // Use the scenario ID as a key to ensure uniqueness
                string scenarioKey = $"{scenarioContext.ScenarioInfo.Title}_{Guid.NewGuid()}";

                // Create ApiContext and store it both in container and our storage
                var apiContext = new ApiContext();
                container.RegisterInstanceAs(apiContext);

                // Store in our dictionary for global access
                _contextStorage[scenarioKey] = apiContext;

                // Store the key in scenario context for later retrieval
                scenarioContext["ApiContextKey"] = scenarioKey;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in BeforeScenario: {ex.Message}");
            }
        }

        [AfterScenario(Order = 1000)]
        public static void AfterScenario(ScenarioContext scenarioContext)
        {
            try
            {
                // Get the key from scenario context
                if (scenarioContext.ContainsKey("ApiContextKey") &&
                    scenarioContext["ApiContextKey"] is string key &&
                    _contextStorage.ContainsKey(key))
                {
                    // Get the context and clean it up
                    var apiContext = _contextStorage[key];
                    apiContext?.ClearContext();

                    // Remove from storage
                    _contextStorage.Remove(key);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AfterScenario cleanup: {ex.Message}");
            }
        }
    }
}