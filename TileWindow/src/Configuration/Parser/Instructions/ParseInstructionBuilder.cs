using System;
using System.Collections.Generic;
using System.Linq;
using TileWindow.Configuration.Parser.Commands;

namespace TileWindow.Configuration.Parser.Instructions
{
    public interface IParseInstructionBuilder
    {
        Dictionary<string, IParseInstruction> Build(AddErrorDelegate addError);
    }

    public class ParseInstructionBuilder: IParseInstructionBuilder
    {
        private readonly IVariableFinder variableFinder;
        private readonly ICommandExecutor commandExecutor;
        private readonly ICommandHandler commandHandler;

        public ParseInstructionBuilder(IVariableFinder variableFinder, ICommandExecutor commandExecutor, ICommandHandler commandHandler)
        {
            this.variableFinder = variableFinder;
            this.commandExecutor = commandExecutor;
            this.commandHandler = commandHandler;
        }
        
        public Dictionary<string, IParseInstruction> Build(AddErrorDelegate addError)
        {
            Func<Func<IParseInstruction>, IParseInstruction> bindAddError = (f) => {
                var p = f();
                p.AddError += addError;
                return p;
            };

            return new List<IParseInstruction>
            {
                bindAddError(() => new ParseSetVariable()),
                bindAddError(() => new ParseBindsym(variableFinder, commandExecutor, commandHandler)),
                bindAddError(() => new ParseDisableWinKey())
            }
            .ToDictionary(p => p.Instruction);
        }
    }
}