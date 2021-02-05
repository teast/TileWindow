using System.Linq;
using System.Text.RegularExpressions;

namespace TileWindow.Configuration.Parser.Instructions.Bar
{
    public class ParseBarColors : IParseInstruction
    {
        private readonly string[] _validSingleColors = new string[] {
            "background",
            "statusline"
        };

        private readonly string[] _validColorClass = new string[] {
            "focused_workspace",
            "inactive_workspace",
            "urgen_workspace"
        };

        public string Instruction => "colors";

        public ParseInstructionResult FetchResult { get; private set; }

        public event AddErrorDelegate AddError;

        public bool Parse(string[] particles, ref ConfigCollection data)
        {
            FetchResult = null;

            // Try parse outer "colors {}" command and return if it was a success
            if (ParseColors(particles, ref data))
            {
                return true;
            }

            // Try parse colors that has only one color on them (and located inside colors mode)
            foreach (var color in _validSingleColors)
            {
                if (ParseSingleColor(color, particles, ref data))
                {
                    return true;
                }
            }

            // Try parse colors that got three colors on them (and located inside colors mode)
            foreach (var color in _validColorClass)
            {
                if (ParseColorClass(color, particles, ref data))
                {
                    return true;
                }
            }

            return false;
        }

        private bool ParseColors(string[] particles, ref ConfigCollection data)
        {
            if (data.Name != "bar" || particles[0] != Instruction)
            {
                return false;
            }

            if (particles[1].StartsWith('{') == false)
            {
                AddError("Invalid value for bar:colors. It should be followed by an {");
                FetchResult = null;
                return true;
            }

            var remaining = string.Join(' ', particles.Skip(1)).Substring(1);
            FetchResult = new ParseInstructionResult(FileParserStateResult.OpenBracket, remaining, this);
            return true;
        }

        private bool ParseSingleColor(string color, string[] particles, ref ConfigCollection data)
        {
            if (data.Name != "colors" || particles[0] != color)
            {
                return false;
            }

            if (particles.Length != 2)
            {
                AddError($"Invalid command \"{string.Join(' ', particles)}\". should be: \"{color} #0055aa\"");
                return true;
            }

            var regex = new Regex(@"^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

            if (regex.IsMatch(particles[1]) == false)
            {
                AddError($"bar:colors should be of format #00aaFF or #00aaFFbb (\"{string.Join(' ', particles)}\" is not)");
                return true;
            }

            data.AddData(color, particles[1]);
            FetchResult = new ParseInstructionResult(FileParserStateResult.None, "", this);
            return true;
        }

        private bool ParseColorClass(string color, string[] particles, ref ConfigCollection data)
        {
            if (data.Name != "colors" || particles[0] != color)
            {
                return false;
            }

            if (particles.Length != 4)
            {
                AddError($"Invalid command \"{string.Join(' ', particles)}\". should be: \"{color} #0055aa #ffaa00 #bbccdd\"");
                return true;
            }

            var regex = new Regex(@"^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$", RegexOptions.Compiled);

            for (int i = 1; i < particles.Length; i++)
            {
                if (regex.IsMatch(particles[i]) == false)
                {
                    AddError($"bar:colors should be of format #00aaFF or #00aaFFbb (\"{particles[i]}\" is not)");
                    return true;
                }
            }

            // Add this type of colors as modes
            var newData = new ConfigCollection(color, data);
            data.Modes.Add(color, newData);
            newData.AddData("border", particles[1]);
            newData.AddData("background", particles[2]);
            newData.AddData("text", particles[3]);

            FetchResult = new ParseInstructionResult(FileParserStateResult.None, "", this);
            return true;
        }
    }
}