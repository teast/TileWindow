namespace TileWindow.Configuration.Parser.Instructions
{
    public class ParseDisableWinKey : IParseInstruction
    {
        public event AddErrorDelegate AddError;
        public string Instruction => "disable_win_key";

        public ParseInstructionResult FetchResult { get; private set; }

        public bool Parse(string[] particles, ref ConfigCollection data)
        {
            if (particles[0] != Instruction)
            {
                FetchResult = null;
                return false;
            }
            
            if (bool.TryParse(particles[1], out bool val))
            {
                data.AddData(Instruction, val);
            }
            else
            {
                AddError("Invalid value for disable_win_key. Should be true or false");
                FetchResult = null;
                return true;
            }
            
            FetchResult = new ParseInstructionResult(FileParserStateResult.None, "", this);
            return true;
        }
    }
}