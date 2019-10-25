namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandRun : ParseCommand
    {
        public override string Instruction => "run";

        public ParseCommandRun(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data) => handler.CmdRun(data);
        protected override bool DoValidate(string line)
        {
            return line == "run";
        }
    }
}
