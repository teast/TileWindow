namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandMove : ParseCommand
    {
        public override string Instruction => "move";

        public ParseCommandMove(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            var workspaceNumber = "move workspace number ";

            switch (data)
            {
                case "move up":
                    return handler.CmdMoveUp(data);
                case "move down":
                    return handler.CmdMoveDown(data);
                case "move left":
                    return handler.CmdMoveLeft(data);
                case "move right":
                    return handler.CmdMoveRight(data);
            }

            if (data.StartsWith(workspaceNumber) && int.TryParse(data.Substring(workspaceNumber.Length), out int result) && result >= 0 && result < 10)
            {
                return ExecuteMoveWorkspace(handler, result, data);
            }
            
            throw new System.InvalidOperationException($"Unknown move command \"{data}\"");
        }

        protected override bool DoValidate(string line)
        {
            var workspaceNumber = "move workspace number ";
            return (line == "move up" ||
                line == "move down" ||
                line == "move left" ||
                line == "move right" ||
                line.StartsWith(workspaceNumber) && int.TryParse(line.Substring(workspaceNumber.Length), out int result) && result >= 0 && result < 10
                );
        }

        private bool ExecuteMoveWorkspace(ICommandHandler handler, int desktopIndex, string data)
        {
            return handler.CmdMoveToWorkspace(desktopIndex.ToString());
        }
    }
}
