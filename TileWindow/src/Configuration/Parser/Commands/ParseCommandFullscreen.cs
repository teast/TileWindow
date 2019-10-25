namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandFullscreen : ParseCommand
    {
        public override string Instruction => "fullscreen";

        public ParseCommandFullscreen(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data) => handler.CmdFullscreen(data);

        protected override bool DoValidate(string line)
        {
            return line == "fullscreen";
        }
    }
}
