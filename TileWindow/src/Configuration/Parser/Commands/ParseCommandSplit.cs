namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandSplit : ParseCommand
    {
        public override string Instruction => "split";

        public ParseCommandSplit(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            if (data == "split vertical")
                return handler.CmdSplitVertical(data);
            if (data == "split horizontal")
                return handler.CmdSplitHorizontal(data);
            if (data == "split toggle")
                return handler.CmdSplitToggle(data);

            throw new System.InvalidOperationException($"Unknown split command \"{data}\"");
        }

        protected override bool DoValidate(string line)
        {
            return (line == "split vertical" ||
                line == "split horizontal" ||
                line == "split toggle");
        }
    }
}
