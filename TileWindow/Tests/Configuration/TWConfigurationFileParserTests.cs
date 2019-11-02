using FluentAssertions;
using Moq;
using TileWindow.Configuration;
using TileWindow.Configuration.Parser;
using Xunit;

namespace TileWindow.Tests.Configuration.Parser
{
    public class TWConfigurationFileParserTests
    {
        [Fact]
        public void Handle_Mode_Correct()
        {
            // Arrange
            var data = new ConfigCollection("Default");
            var bar = new ConfigCollection("bar", data);
            data.Modes.Add("bar", bar);
            bar.AddData("position", "up");
            var sut = CreateSut(data);

            // Act
            var result = sut.ParseStream(null);

            // Assert
            result.Should().ContainKey("Bar:Position");
            result["Bar:Position"].Should().Be("up");
        }

        [Fact]
        public void Handle_Multiple_Mode_Correct()
        {
            // Arrange
            var data = new ConfigCollection("Default");
            var bar = new ConfigCollection("bar", data);
            var colors = new ConfigCollection("colors", bar);
            data.Modes.Add("bar", bar);
            bar.Modes.Add("colors", colors);
            bar.AddData("position", "up");
            colors.AddData("background", "#000000");
            var sut = CreateSut(data);

            // Act
            var result = sut.ParseStream(null);

            // Assert
            result.Should().ContainKey("Bar:Colors:Background");
            result["Bar:Colors:Background"].Should().Be("#000000");
        }

        [Fact]
        public void Renames_BarColorsFocusedWorkspace_Corretly()
        {
            // Arrange
            var expectedBorder = "#001122";
            var expectedBackground = "#112233";
            var expectedText = "ffeedd";
            var data = new ConfigCollection("Default");
            var bar = new ConfigCollection("bar", data);
            var colors = new ConfigCollection("colors", bar);
            var focus = new ConfigCollection("focused_workspace", bar);
            data.Modes.Add("bar", bar);
            bar.Modes.Add("colors", colors);
            colors.Modes.Add(focus.Name, focus);
            focus.AddData("border", expectedBorder);
            focus.AddData("background", expectedBackground);
            focus.AddData("text", expectedText);
            var sut = CreateSut(data);

            // Act
            var result = sut.ParseStream(null);

            // Assert
            result.Should().ContainKey("Bar:Colors:FocusedWorkspace:Border");
            result["Bar:Colors:FocusedWorkspace:Border"].Should().Be(expectedBorder);
            result.Should().ContainKey("Bar:Colors:FocusedWorkspace:Background");
            result["Bar:Colors:FocusedWorkspace:Background"].Should().Be(expectedBackground);
            result.Should().ContainKey("Bar:Colors:FocusedWorkspace:Text");
            result["Bar:Colors:FocusedWorkspace:Text"].Should().Be(expectedText);
        }

        private TWConfigurationFileParser CreateSut(ConfigCollection data)
        {
            var parser = new Mock<IFileParser>();
            parser.SetupGet(m => m.Data).Returns(data);
            var sut = new TWConfigurationFileParser(parser.Object);

            return sut;
        }
    }
}