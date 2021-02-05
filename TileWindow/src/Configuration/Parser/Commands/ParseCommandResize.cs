namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandResize : ParseCommand
    {
        public override string Instruction => "resize";

        public ParseCommandResize(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            int? i;
            if ((i = ValidateAndGetNr(data, "resize shrink down")).HasValue)
            {
                return handler.CmdResizeUp((0 - i).ToString());
            }

            if ((i = ValidateAndGetNr(data, "resize shrink right")).HasValue)
            {
                return handler.CmdResizeLeft((0 - i).ToString());
            }

            if ((i = ValidateAndGetNr(data, "resize grow right")).HasValue)
            {
                return handler.CmdResizeRight(i.ToString());
            }

            if ((i = ValidateAndGetNr(data, "resize grow down")).HasValue)
            {
                return handler.CmdResizeDown(i.ToString());
            }

            throw new System.InvalidOperationException($"Unknown resize command \"{data}\"");
        }

        protected override bool DoValidate(string line)
        {
            return (ValidateAndGetNr(line, "resize grow down").HasValue ||
                ValidateAndGetNr(line, "resize grow right").HasValue ||
                ValidateAndGetNr(line, "resize shrink right").HasValue ||
                ValidateAndGetNr(line, "resize shrink down").HasValue);
        }

        private int? ValidateAndGetNr(string line, string shouldBe)
        {
            if (line.StartsWith(shouldBe) && int.TryParse(line.Substring(shouldBe.Length), out int actual))
            {
                return actual;
            }
            
            return null;
        }
    }
}
