using BoDi;
using Location.Photography.BDD.Tests.Support;
using System.Collections.Concurrent;
using TechTalk.SpecFlow;

namespace Location.Photography.BDD.Tests.Hooks
{
    [Binding]
    public class TestInitialization
    {
        // Global ServiceProvider to ensure services stay alive
        private static ServiceProvider _globalServiceProvider;

        // Thread-safe container storage to maintain reference for cleanup
        private static readonly ConcurrentDictionary<string, ApiContext> _contextStorage = new ConcurrentDictionary<string, ApiContext>();

        // Use a static test service provider
        private static TestServiceProvider _testServiceProvider;

        [BeforeTestRun]
        public static void BeforeTestRun(IObjectContainer objectContainer)
        {
            try
            {
                Console.WriteLine("Starting Photography test run initialization");

                // Setup global services
                var services = new ServiceCollection();
                services.AddLogging();

                // Build service provider
                _globalServiceProvider = services.BuildServiceProvider();

                // Create a single TestServiceProvider
                _testServiceProvider = new TestServiceProvider();

                // DON'T register anything in the object container here
                // This will be done per-scenario

                Console.WriteLine("Photography test run initialization complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Photography BeforeTestRun: {ex.Message}");
            }
        }

        [AfterTestRun]
        public static void AfterTestRun()
        {
            Console.WriteLine("Starting Photography test run cleanup");

            // Clean up all contexts
            foreach (var context in _contextStorage.Values)
            {
                try
                {
                    context?.ClearContext();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error clearing Photography context: {ex.Message}");
                }
            }
            _contextStorage.Clear();

            // Dispose service provider
            if (_globalServiceProvider != null)
            {
                try
                {
                    _globalServiceProvider.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error disposing Photography service provider: {ex.Message}");
                }
                _globalServiceProvider = null;
            }

            Console.WriteLine("Photography test run cleanup complete");
        }

        [BeforeScenario(Order = -10000)]
        public void BeforeScenario(IObjectContainer container, ScenarioContext scenarioContext)
        {
            try
            {
                // Create a unique key for this scenario
                string scenarioKey = $"{scenarioContext.ScenarioInfo.Title}_{Guid.NewGuid()}";
                Console.WriteLine($"Starting Photography scenario: {scenarioKey}");

                // Register the test service provider
                if (!container.IsRegistered<TestServiceProvider>())
                {
                    container.RegisterInstanceAs(_testServiceProvider);
                }

                // Create a new ApiContext for this scenario
                var apiContext = new ApiContext(_testServiceProvider);

                // Important: Only register if not already registered
                if (!container.IsRegistered<ApiContext>())
                {
                    container.RegisterInstanceAs(apiContext);
                }
                else
                {
                    Console.WriteLine("WARNING: Photography ApiContext is already registered in the container");
                }

                // Store in dictionary with unique key
                _contextStorage[scenarioKey] = apiContext;

                // Store key in scenario context
                scenarioContext["ApiContextKey"] = scenarioKey;

                Console.WriteLine($"Photography scenario initialization complete: {scenarioKey}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Photography BeforeScenario: {ex.Message}\n{ex.StackTrace}");
            }
        }

        [AfterScenario(Order = 10000)]
        public void AfterScenario(ScenarioContext scenarioContext)
        {
            try
            {
                // Get the key from scenario context
                if (scenarioContext != null && scenarioContext.ContainsKey("ApiContextKey"))
                {
                    string key = scenarioContext["ApiContextKey"] as string;
                    if (!string.IsNullOrEmpty(key) && _contextStorage.ContainsKey(key))
                    {
                        Console.WriteLine($"Cleaning up Photography scenario: {key}");

                        // Safely remove and clean up the context
                        if (_contextStorage.TryRemove(key, out var apiContext))
                        {
                            apiContext?.ClearContext();
                            Console.WriteLine($"Successfully cleaned up Photography scenario: {key}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Photography AfterScenario cleanup: {ex.Message}\n{ex.StackTrace}");
            }
        }
    }
}