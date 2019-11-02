using System.Linq;

namespace TileWindow.Configuration.Parser.Instructions.Bar
{
    public class ParseBarPosition : IParseInstruction
    {
        public string Instruction => "position";

        public ParseInstructionResult FetchResult { get; private set; }

        public event AddErrorDelegate AddError;

        public bool Parse(string[] particles, ref ConfigCollection data)
        {
            FetchResult = null;

            if (data.Name != "bar" || particles[0] != Instruction)
                return false;

            var positions = new [] { "top", "bottom", "left", "right" };

            if(!positions.Contains(particles[1]))
            {
                AddError($"Unkown bar:position value \"{particles[1]}\"");
                return true;
            }

            data.AddData(Instruction, particles[1]);
            FetchResult = new ParseInstructionResult(FileParserStateResult.None, "", this);
            return true;
        }
    }
}