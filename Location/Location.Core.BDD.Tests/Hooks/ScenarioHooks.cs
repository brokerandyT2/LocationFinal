using BoDi;
using Location.Core.BDD.Tests.Support;
using TechTalk.SpecFlow;

namespace Location.Core.BDD.Tests.Hooks
{
    /// <summary>
    /// Contains hooks that run before and after each scenario
    /// </summary>
    [Binding]
    public class ScenarioHooks
    {
        private readonly IObjectContainer _objectContainer;
        private ApiContext _apiContext;

        /// <summary>
        /// Initializes a new instance of the ScenarioHooks class
        /// </summary>
        /// <param name="objectContainer">The SpecFlow object container for dependency injection</param>
        public ScenarioHooks(IObjectContainer objectContainer)
        {
            _objectContainer = objectContainer;
        }

        /// <summary>
        /// Initializes the API context before each scenario
        /// </summary>
        [BeforeScenario]
        public void InitializeApiContext()
        {
            // Create a new API context for the scenario with mocked services
            _apiContext = new ApiContext(useMocks: true);

            // Register in SpecFlow's container for injection into step definitions
            _objectContainer.RegisterInstanceAs(_apiContext);
        }

        /// <summary>
        /// Cleans up after each scenario
        /// </summary>
        [AfterScenario]
        public void CleanupScenario()
        {
            // Clear the API context
            _apiContext.ClearContext();
        }
    }
}