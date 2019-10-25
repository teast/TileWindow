using System.Linq;
using TileWindow.Configuration.Parser.Commands;

namespace TileWindow.Configuration.Parser.Instructions
{
    /// <summary>
    /// Bindsym instruction binds keyboard shortcuts to an command
    /// </summary>
    public class ParseBindsym : IParseInstruction
    {
        private readonly IVariableFinder variableFinder;
        private readonly ICommandExecutor commandExecutor;
        private readonly ICommandHandler commandHandler;

        public event AddErrorDelegate AddError;
        public string Instruction => "bindsym";

        public ParseBindsym(IVariableFinder variableFinder, ICommandExecutor commandExecutor, ICommandHandler commandHandler)
        {
            this.variableFinder = variableFinder;
            this.commandExecutor = commandExecutor;
            this.commandHandler = commandHandler;
        }
        public string Parse(string[] particles, ref ConfigCollection data)
        {
            variableFinder.PushContext(data);
            var keybind = variableFinder.ParseAll(particles[1]);
            var command = string.Join(' ', particles.Skip(2));

            if (commandExecutor.Execute(command, data, commandHandler) == null)
            {
                AddError($"Unknown bindsym command \"{command}\"");
                variableFinder.PopContext();
                return "";
            }

            data.KeyBinds.Add(keybind, command);
            variableFinder.PopContext();
            return "";
        }
    }
}