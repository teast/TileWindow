using TechTalk.SpecFlow;

namespace TileWindow.Tests.Gherkin.Steps
{
    [Binding]
    public sealed class WindowSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly TestWorld _world;

        public WindowSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            if (!_scenarioContext.ContainsKey("World"))
            {
                _scenarioContext["World"] = new TestWorld();
            }

            _world = (TestWorld)_scenarioContext["World"];
        }

       [Given("a window '(.*)' on screen '(.*)'.")]
        public void CreateWindow(string name, string screenName) =>
            _world.CreateWindow(name, screenName);

       [Given("window '(.*)' is focus.")]
        public void WindowFocus(string name) =>
            _world.WindowFocus(name);

       [Then("window '(.*)' is in position (.*) on screen '(.*)'.")]
        public void WindowPosition(string name, int position, string screen) =>
            _world.WindowPosition(name, position, screen);
        
        [When("moving focused window (.*).")]
        public void MoveFocusWindow(string direction) =>
            _world.MoveFocusWindow(direction);
   }
}