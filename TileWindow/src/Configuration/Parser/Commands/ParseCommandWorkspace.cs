using System;

namespace TileWindow.Configuration.Parser.Commands
{
    public class ParseCommandWorkspace : ParseCommand
    {
        public override string Instruction => "workspace";

        public ParseCommandWorkspace(IVariableFinder variableFinder) : base(variableFinder) { }

        public override bool Execute(ICommandHandler handler, string data)
        {
            var workspaceNumber = "workspace number ";
            var id = Convert.ToInt32(data.Substring(workspaceNumber.Length));
            return handler.CmdShowWorkspace(id.ToString());
        }

        protected override bool DoValidate(string line)
        {
            var workspaceNumber = "workspace number ";
            return line.StartsWith(workspaceNumber) && int.TryParse(line.Substring(workspaceNumber.Length), out int workspaceNr) && workspaceNr >= 0 && workspaceNr < 10;
        }
    }
}
