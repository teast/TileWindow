using TechTalk.SpecFlow;

namespace TileWindow.Tests.Gherkin.Steps
{
    [Binding]
    public sealed class DesktopSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly TestWorld _world;

        public DesktopSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            if (!_scenarioContext.ContainsKey("World"))
            {
                _scenarioContext["World"] = new TestWorld();
            }

            _world = (TestWorld)_scenarioContext["World"];
        }

        [Given("a virtual desktop with (.*) desktops.")]
        public void CreateDesktop(int desktops) =>
            _world.CreateDesktop(desktops);
   }
}