using TechTalk.SpecFlow;

namespace TileWindow.Tests.Gherkin.Steps
{
    [Binding]
    public sealed class ScreenSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly TestWorld _world;

        public ScreenSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            if (!_scenarioContext.ContainsKey("World"))
            {
                _scenarioContext["World"] = new TestWorld();
            }

            _world = (TestWorld)_scenarioContext["World"];
        }

        [Given(@"a primary screen '(.*)' position (.*)x(.*) with resolution (.*)x(.*).")]
        public void CreatePrimaryScreen(string name, int posX, int posY, int resolutionX, int resolutionY) =>
            _world.CreateScreen(name, true, posX, posY, resolutionX, resolutionY);

        [Given(@"a screen '(.*)' position (.*)x(.*) with resolution (.*)x(.*).")]
        public void CreateScreen(string name, int posX, int posY, int resolutionX, int resolutionY) =>
            _world.CreateScreen(name, false, posX, posY, resolutionX, resolutionY);

        [Given(@"screen '(.*)' has orientation (.*).")]
        public void ScreenOrientation(string name, string orientation) =>
            _world.ScreenOrientation(name, orientation);
   }
}