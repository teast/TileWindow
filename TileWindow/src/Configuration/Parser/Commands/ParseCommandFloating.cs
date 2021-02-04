namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandFloating : ParseCommand
    {
        public override string Instruction => "floating";

        public ParseCommandFloating(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            if (data == "floating toggle")
            {
                return handler.CmdFloatingToggle(data);
            }
            
            throw new System.InvalidOperationException($"Unknown floating command \"{data}\"");
        }

        protected override bool DoValidate(string line)
        {
            return (line == "floating toggle");
        }
    }
}
