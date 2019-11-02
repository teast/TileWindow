namespace TileWindow.Configuration.Parser.Instructions
{
    public interface IParseInstruction
    {
        event AddErrorDelegate AddError;

        string Instruction { get; }

        ParseInstructionResult FetchResult { get; }

        bool Parse(string[] particles, ref ConfigCollection data);
    }

    public class ParseInstructionResult
    {
        public FileParserStateResult Status { get; }
        public string Remaining { get; }

        public IParseInstruction Instruction { get; }

        public ParseInstructionResult(FileParserStateResult status, string remaining, IParseInstruction instruction)
        {
            Status = status;
            Remaining = remaining;
            Instruction = instruction;
        }
    }
}