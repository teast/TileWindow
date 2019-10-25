using System.Collections.Generic;

namespace TileWindow.Configuration.Parser.Commands
{
    public interface IParseCommandBuilder
    {
        IVariableFinder VariableFinder { get; }
        List<ParseCommand> Build();
    }

    public class ParseCommandBuilder: IParseCommandBuilder
    {
        public IVariableFinder VariableFinder { get; private set; }

        public ParseCommandBuilder(IVariableFinder variableFinder)
        {
            this.VariableFinder = variableFinder;
        }
        
        public List<ParseCommand> Build()
        {
            return new List<ParseCommand>
            {
                new ParseCommandExec(VariableFinder),
                new ParseCommandFocus(VariableFinder),
                new ParseCommandMove(VariableFinder),
                new ParseCommandKill(VariableFinder),
                new ParseCommandSplit(VariableFinder),
                new ParseCommandWorkspace(VariableFinder),
                new ParseCommandLayout(VariableFinder),
                new ParseCommandFloating(VariableFinder),
                new ParseCommandDebug(VariableFinder),
                new ParseCommandRestart(VariableFinder),
                new ParseCommandResize(VariableFinder),
                new ParseCommandFullscreen(VariableFinder)
            };
        }
    }
}