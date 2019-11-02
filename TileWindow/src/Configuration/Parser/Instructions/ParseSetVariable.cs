using System.Linq;

namespace TileWindow.Configuration.Parser.Instructions
{
    public class ParseSetVariable : IParseInstruction
    {
        public event AddErrorDelegate AddError;

        public string Instruction => "set";

        public ParseInstructionResult FetchResult { get; private set; }

        public bool Parse(string[] particles, ref ConfigCollection data)
        {
            if (particles[0] != Instruction)
            {
                FetchResult = null;
                return false;
            }
            
            if (particles[1].StartsWith('$') == false)
            {
                AddError?.Invoke("Invalid variable name. Variables must start with $");
                FetchResult = null;
                return true;
            }

            data.AddVariable(particles[1], string.Join(' ', particles.Skip(2)));
            FetchResult = new ParseInstructionResult(FileParserStateResult.None, "", this);
            return true;
        }
    }
}