namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandKill : ParseCommand
    {
        public override string Instruction => "kill";

        public ParseCommandKill(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data) => handler.CmdKill(data);

        protected override bool DoValidate(string line)
        {
            return line == "kill";
        }
    }
}
