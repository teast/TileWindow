namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandFocus : ParseCommand
    {
        public override string Instruction => "focus";

        public ParseCommandFocus(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            if (data == "focus up")
                return handler.CmdFocusUp(data);
            if (data == "focus left")
                return handler.CmdFocusLeft(data);
            if (data == "focus right")
                return handler.CmdFocusRight(data);
            if (data == "focus down")
                return handler.CmdFocusDown(data);
            
            throw new System.InvalidOperationException($"Unknown focus command \"{data}\"");
        }

        protected override bool DoValidate(string line)
        {
            return (line == "focus up" ||
                line == "focus down" ||
                line == "focus left" ||
                line == "focus right");
        }
    }
}
