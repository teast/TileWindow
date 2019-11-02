using Xunit;
using TileWindow.Configuration.Parser;
using FluentAssertions;
using TileWindow.Configuration.Parser.Instructions.Bar;

namespace TileWindow.Tests.Configuration.Parser.Instruction.Bar
{
    public class ParseBarColorsTests
    {
        [Fact]
        public void Parse_Colors_Correct()
        {
            // Arrange
            var line = "colors {";
            var sut = new ParseBarColors();
            var data = new ConfigCollection("bar");

            // Act
            var result = sut.Parse(line.Split(' '), ref data);
            var resultData = sut.FetchResult;

            // Assert
            result.Should().BeTrue();
            resultData.Status.Should().Be(FileParserStateResult.OpenBracket);
            resultData.Instruction.Should().Be(sut);
        }

        [Fact]
        public void Parse_Background_Color()
        {
            // Arrange
            var line = "background #00aabb";
            var sut = new ParseBarColors();
            var data = new ConfigCollection("colors");

            // Act
            var result = sut.Parse(line.Split(' '), ref data);
            var resultData = sut.FetchResult;

            // Assert
            result.Should().BeTrue();
            resultData.Status.Should().Be(FileParserStateResult.None);
            resultData.Instruction.Should().Be(sut);
            data.Data.Should().ContainKey("background");
            data.Data["background"].Should().Be("#00aabb");
        }

        [Fact]
        public void Parse_FocusedWorkspace_Color()
        {
            // Arrange
            var line = "focused_workspace #00aabb #aabbcc #003311";
            var sut = new ParseBarColors();
            var data = new ConfigCollection("colors");

            // Act
            var result = sut.Parse(line.Split(' '), ref data);
            var resultData = sut.FetchResult;

            // Assert
            result.Should().BeTrue();
            resultData.Status.Should().Be(FileParserStateResult.None);
            resultData.Instruction.Should().Be(sut);
            data.Modes.Should().ContainKey("focused_workspace");
            data.Modes["focused_workspace"].Data.Should().ContainKey("border");
            data.Modes["focused_workspace"].Data["border"].Should().Be("#00aabb");
            data.Modes["focused_workspace"].Data.Should().ContainKey("background");
            data.Modes["focused_workspace"].Data["background"].Should().Be("#aabbcc");
            data.Modes["focused_workspace"].Data.Should().ContainKey("text");
            data.Modes["focused_workspace"].Data["text"].Should().Be("#003311");
        }
    }
}