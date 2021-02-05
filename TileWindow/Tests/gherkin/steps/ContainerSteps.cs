using System;
using TechTalk.SpecFlow;

namespace TileWindow.Tests.Gherkin.Steps
{
    [Binding]
    public sealed class ContainerSteps
    {
        private readonly ScenarioContext _scenarioContext;
        private readonly TestWorld _world;

        public ContainerSteps(ScenarioContext scenarioContext)
        {
            _scenarioContext = scenarioContext;
            if (!_scenarioContext.ContainsKey("World"))
            {
                _scenarioContext["World"] = new TestWorld();
            }

            _world = (TestWorld)_scenarioContext["World"];
        }

        [When(@"requesting orientation (.*)\.")]
        public void ChangeOrientation(string orientation) =>
            _world.ChangeOrientation(orientation);

        [Then(@"node '(.*)' on screen '(.*)' desktop '(.*)' should have name '(.*)'\.")]
        public void NodeHaveName(int nodeIndex, string screen, int desktop, string name) =>
            _world.NodeHaveName(nodeIndex, screen, desktop, name);

        [Then(@"node '(.*)' on screen '(.*)' desktop '(.*)' should be of type '(.*)'\.")]
        public void NodeBeOfType(int nodeIndex, string screen, int desktop, string typeName)
        {
            var fullyAssemblyName = $"TileWindow.Nodes.{typeName}, TileWindow";
            var t = Type.GetType(fullyAssemblyName);
            if (t == null)
            {
                throw new InvalidCastException($"Could not find type with name '{typeName}' (fully assembly name: '{fullyAssemblyName}')");
            }
            
            _world.NodeBeOfType(nodeIndex, screen, desktop, t);
        }
    }
}