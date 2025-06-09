using BoDi;
using TechTalk.SpecFlow;

namespace Location.Photography.BDD.Tests.Hooks
{
    [Binding]
    public class ScenarioHooks
    {
        private readonly IObjectContainer _objectContainer;

        public ScenarioHooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
        }

        // Remove the BeforeScenario hook entirely to avoid duplicate ApiContext registration

        [AfterScenario(Order = 100)] // Run this after other AfterScenario methods but before TestInitialization cleanup
        public void CleanupScenario()
        {
            try
            {
                // We don't need to do any cleanup here as it will be handled by TestInitialization
                Console.WriteLine("Photography ScenarioHooks cleanup completed");
            }
            catch (Exception ex)
            {
                // Log but don't throw
                Console.WriteLine($"Error in Photography ScenarioHooks.CleanupScenario: {ex.Message}");
            }
        }
    }
}