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

        public ParseInstructionResult FetchResult { get; private set; }

        public ParseBindsym(IVariableFinder variableFinder, ICommandExecutor commandExecutor, ICommandHandler commandHandler)
        {
            this.variableFinder = variableFinder;
            this.commandExecutor = commandExecutor;
            this.commandHandler = commandHandler;
        }

        public bool Parse(string[] particles, ref ConfigCollection data)
        {
            if (particles[0] != Instruction)
            {
                FetchResult = null;
                return false;
            }

            variableFinder.PushContext(data);
            var keybind = variableFinder.ParseAll(particles[1]);
            var command = string.Join(' ', particles.Skip(2));

            if (commandExecutor.Execute(command, data, commandHandler) == null)
            {
                AddError($"Unknown bindsym command \"{command}\"");
                variableFinder.PopContext();
                FetchResult = null;
                return true;
            }

            data.KeyBinds.Add(keybind, command);
            variableFinder.PopContext();
            FetchResult = new ParseInstructionResult(FileParserStateResult.None, "", this);
            return true;
        }
    }
}