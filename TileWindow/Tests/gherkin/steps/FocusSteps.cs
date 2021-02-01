using TechTalk.SpecFlow;

namespace TileWindow.Tests.Gherkin.Steps
{
    [Binding]
    public sealed class FocusSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly TestWorld _world;

        public FocusSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            if (!_scenarioContext.ContainsKey("World"))
            {
                _scenarioContext["World"] = new TestWorld();
            }

            _world = (TestWorld)_scenarioContext["World"];
        }

        [When("moving focus (.*).")]
        public void MoveFocus(string direction) =>
            _world.MoveFocus(direction);
   }
}