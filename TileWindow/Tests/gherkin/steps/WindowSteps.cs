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

       [Given(@"a window '(.*)' on screen '(.*)'.")]
        public void CreateWindow(string name, string screenName) =>
            _world.CreateWindow(name, screenName);

       [Given(@"window '(.*)' is focus.")]
        public void WindowFocus(string name) =>
            _world.WindowFocus(name);

       [Then(@"window '(.*)' is in position (.*) on screen '([a-zA-z0-9_-]*)'.")]
        public void WindowPosition(string name, int position, string screen) =>
            _world.WindowPosition(name, position, screen);
        
       [Then(@"window '(.*)' is in position (.*) on screen '([a-zA-z0-9_-]*)' desktop '(.*)'.")]
        public void WindowPositionWithDesktop(string name, int position, string screen, int desktop) =>
            _world.WindowPosition(name, position, screen, desktop);

        [When(@"moving focused window ([a-zA-z0-9_-]*).")]
        public void MoveFocusWindow(string direction) =>
            _world.MoveFocusWindow(direction);
        
        [When(@"moving focused window to desktop '(.*)'.")]
        public void MoveFocusWindowToDesktop(int desktop) =>
            _world.MoveFocusWindowToDesktop(desktop);

        [Then(@"node '(.*)' on screen '(.*)' desktop '(.*)' should have position (.*)x(.*) and size (.*)x(.*)\.")]
        public void NodePositionAndSize(int index, string screen, int desktop, int left, int top, int width, int height) =>
            _world.NodePositionAndSize(index, screen, desktop, left, top, width, height);


   }
}