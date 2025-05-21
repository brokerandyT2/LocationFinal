// In ScenarioHooks.cs
using BoDi;
using Location.Core.BDD.Tests.Support;
using TechTalk.SpecFlow;

[Binding]
public class ScenarioHooks
{
    private readonly IObjectContainer _objectContainer;
    private ApiContext _apiContext;

    public ScenarioHooks(IObjectContainer objectContainer)
    {
        _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
    }

    [BeforeScenario(Order = 0)]
    public void InitializeApiContext()
    {
        // Create with parameterless constructor
        _apiContext = new ApiContext();

        // Register in SpecFlow's container
        _objectContainer.RegisterInstanceAs(_apiContext);
    }

    [AfterScenario]
    public void CleanupScenario()
    {
        _apiContext?.ClearContext();
    }
}