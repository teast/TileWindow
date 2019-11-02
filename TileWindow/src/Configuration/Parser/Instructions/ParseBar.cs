using System.Linq;

namespace TileWindow.Configuration.Parser.Instructions
{
    public class ParseBar : IParseInstruction
    {
        public event AddErrorDelegate AddError;
        public string Instruction => "bar";

        public ParseInstructionResult FetchResult { get; private set; }

        public bool Parse(string[] particles, ref ConfigCollection data)
        {
            if (particles[0] != Instruction)
            {
                FetchResult = null;
                return false;
            }

            if (particles[1].StartsWith('{') == false)
            {
                AddError("Invalid value for bar. It should be followed by an {");
                FetchResult = null;
                return true;
            }
            
            var remaining = string.Join(' ', particles.Skip(1)).Substring(1);
            FetchResult = new ParseInstructionResult(FileParserStateResult.OpenBracket, remaining, this);
            return true;
        }
    }
}