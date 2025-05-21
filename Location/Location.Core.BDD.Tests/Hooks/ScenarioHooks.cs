using BoDi;
using Location.Core.BDD.Tests.Support;
using TechTalk.SpecFlow;
using System;

namespace Location.Core.BDD.Tests.Hooks
{
    [Binding]
    public class ScenarioHooks
    {
        private readonly IObjectContainer _objectContainer;

        public ScenarioHooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
        }

        // REMOVE: BeforeScenario method to avoid duplicate ApiContext registration
        // We now handle this in TestInitialization.BeforeScenario with Order = -100

        // This method is also in TestInitialization, but we'll keep it here for backward compatibility
        // and make it safe by checking if cleanup has already been done
        [AfterScenario(Order = 100)] // Run this after other AfterScenario methods
        public void CleanupScenario()
        {
            try
            {
                // Only try to clean up if ApiContext exists and hasn't been cleaned up yet
                if (_objectContainer.IsRegistered<ApiContext>())
                {
                    var apiContext = _objectContainer.Resolve<ApiContext>();

                    // Check if context still has content before clearing
                    if (apiContext != null)
                    {
                        apiContext.ClearContext();
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw
                Console.WriteLine($"Error in CleanupScenario: {ex.Message}");
            }
        }
    }
}