namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandFocus : ParseCommand
    {
        public override string Instruction => "focus";

        public ParseCommandFocus(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            switch (data)
            {
                case "focus up":
                    return handler.CmdFocusUp(data);
                case "focus left":
                    return handler.CmdFocusLeft(data);
                case "focus right":
                    return handler.CmdFocusRight(data);
                case "focus down":
                    return handler.CmdFocusDown(data);
            }

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
