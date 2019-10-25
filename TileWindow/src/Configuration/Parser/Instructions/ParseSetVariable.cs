using System.Linq;

namespace TileWindow.Configuration.Parser.Instructions
{
    public class ParseSetVariable : IParseInstruction
    {
        public event AddErrorDelegate AddError;

        public string Instruction => "set";

        public string Parse(string[] particles, ref ConfigCollection data)
        {
            if (particles[1].StartsWith('$') == false)
            {
                AddError?.Invoke("Invalid variable name. Variables must start with $");
                return "";
            }

            data.AddVariable(particles[1], string.Join(' ', particles.Skip(2)));
            return "";
        }
    }
}