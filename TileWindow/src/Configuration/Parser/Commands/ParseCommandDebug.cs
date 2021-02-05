namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandDebug : ParseCommand
    {
        public override string Instruction => "debug";

        public ParseCommandDebug(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            if (data == "debug graph")
            {
                return handler.CmdDebugGraph(data);
            }
            
            throw new System.InvalidOperationException($"Unknown debug command \"{data}\"");
        }

        protected override bool DoValidate(string line)
        {
            return (line == "debug graph");
        }
    }
}
