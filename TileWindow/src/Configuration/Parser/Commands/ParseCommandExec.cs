namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandExec : ParseCommand
    {
        public override string Instruction => "exec";

        public ParseCommandExec(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data) => handler.CmdExecute(data);

        protected override bool DoValidate(string line)
        {
            // TODO: implement this
            return true;
        }
    }
}
