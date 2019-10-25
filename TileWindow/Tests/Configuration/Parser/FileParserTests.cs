using Xunit;
using TileWindow.Configuration.Parser;
using System.IO;
using Moq;
using TileWindow.Configuration.Parser.Commands;
using TileWindow.Configuration.Parser.Instructions;
using System.Text;
using FluentAssertions;
using System.Linq;

namespace TileWindow.Tests.Configuration.Parser
{
    public class FileParserTests
    {
        [Fact]
        public void Variable()
        {
            // Arrange
            var file = "# This is a test\nset $Test1 hellu";
            var sut = CreateSut();

            // Act
            sut.Parse(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            // Assert
            sut.Data.Variables.ContainsKey("$Test1").Should().BeTrue();
            sut.Data.Variables["$Test1"].Should().Be("hellu");
            sut.Dispose();
        }

        [Fact]
        public void DisableWinKey()
        {
            // Arrange
            var file = "# This is a test\ndisable_win_key true";
            var sut = CreateSut();

            // Act
            sut.Parse(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            // Assert
            sut.Data.Data.ContainsKey("disable_win_key").Should().BeTrue();
            sut.Data.Data["disable_win_key"].Should().Be(true.ToString());
            sut.Dispose();
        }

        [Fact]
        public void Bindsym_Exec()
        {
            // Arrange
            var file = "# This is a test\nbindsym WIN+Shift+Q kill";
            var sut = CreateSut();

            // Act
            sut.Parse(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            // Assert
            sut.Data.KeyBinds.ContainsKey("WIN+Shift+Q").Should().BeTrue();
            sut.Data.KeyBinds["WIN+Shift+Q"].Should().Be("kill");
            sut.Dispose();
        }

        [Fact]
        public void Bindsym_ExecWithVariable()
        {
            // Arrange
            var file = "# This is a test\nset $Mod WIN\nbindsym $Mod+Shift+Q kill";
            var sut = CreateSut();

            // Act
            sut.Parse(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            // Assert
            sut.Data.KeyBinds.ContainsKey("WIN+Shift+Q").Should().BeTrue();
            sut.Data.KeyBinds["WIN+Shift+Q"].Should().Be("kill");
            sut.Dispose();
        }

        [Fact]
        public void Bindsym_Focus()
        {
            // Arrange
            var file = "# This is a test\nbindsym WIN+Shift+Left focus left";
            var sut = CreateSut();

            // Act
            sut.Parse(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            // Assert
            sut.Data.KeyBinds.ContainsKey("WIN+Shift+Left").Should().BeTrue();
            sut.Data.KeyBinds["WIN+Shift+Left"].Should().Be("focus left");
            sut.Dispose();
        }

        [Fact]
        public void Bindsym_Multiple()
        {
            // Arrange
            var file = "# This is a test\nbindsym WIN+Shift+Left focus left\nbindsym WIN+Shift+Q kill";
            var sut = CreateSut();

            // Act
            sut.Parse(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            // Assert
            sut.Data.KeyBinds.ContainsKey("WIN+Shift+Q").Should().BeTrue();
            sut.Data.KeyBinds["WIN+Shift+Q"].Should().Be("kill");
            sut.Data.KeyBinds.ContainsKey("WIN+Shift+Left").Should().BeTrue();
            sut.Data.KeyBinds["WIN+Shift+Left"].Should().Be("focus left");
            sut.Dispose();
        }

        [Fact]
        public void Bindsym_Multiple_WithSpacesAndTabs()
        {
            // Arrange
            var file = "# This is a test\n  bindsym WIN+Shift+Left focus left\n\t\tbindsym    WIN+Shift+Q    kill";
            var sut = CreateSut();

            // Act
            sut.Parse(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            // Assert
            sut.Data.KeyBinds.ContainsKey("WIN+Shift+Q").Should().BeTrue();
            sut.Data.KeyBinds["WIN+Shift+Q"].Should().Be("kill");
            sut.Data.KeyBinds.ContainsKey("WIN+Shift+Left").Should().BeTrue();
            sut.Data.KeyBinds["WIN+Shift+Left"].Should().Be("focus left");
            sut.Dispose();
        }

        [Fact]
        public void Bindsym_UnknownCommand()
        {
            // Arrange
            var file = "# This is a test\n  bindsym WIN+Shift+Left crazy_non_Existing_command";
            var sut = CreateSut();

            // Act
            sut.Parse(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            // Assert
            sut.Errors.Should().HaveCount(1);
            sut.Errors.First().Item1.Should().Be(2);
        }

        private FileParser CreateSut()
        {
            var variableParser = new VariableFinder();
            var cmdBuilder = new ParseCommandBuilder(variableParser);
            var executor = new CommandExecutor(cmdBuilder);
            var tester = new BlankCommandHandler();
            var instrBuilder = new ParseInstructionBuilder(variableParser, executor, tester);
            return new FileParser(instrBuilder);
        }
    }
}
