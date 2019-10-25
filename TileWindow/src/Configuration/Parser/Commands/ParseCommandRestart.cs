namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandRestart : ParseCommand
    {
        public override string Instruction => "restart";

        public ParseCommandRestart(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data) => handler.CmdRestart(data);

        protected override bool DoValidate(string line)
        {
            return line == "restart";
        }
    }
}
