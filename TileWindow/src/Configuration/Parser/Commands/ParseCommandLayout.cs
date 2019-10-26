namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandLayout : ParseCommand
    {
        public override string Instruction => "layout";

        public ParseCommandLayout(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            if (data == "layout stacking")
                return handler.CmdLayoutStacking(data);
            if (data == "layout toggle split")
                return handler.CmdLayoutToggleSplit(data);
            
            throw new System.InvalidOperationException($"Unknown layout command \"{data}\"");
        }

        protected override bool DoValidate(string line)
        {
            return (line == "layout stacking" ||
                    line == "layout toggle split");
        }
    }
}
