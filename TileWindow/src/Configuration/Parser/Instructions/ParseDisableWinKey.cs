namespace TileWindow.Configuration.Parser.Instructions
{
    public class ParseDisableWinKey : IParseInstruction
    {
        public event AddErrorDelegate AddError;
        public string Instruction => "disable_win_key";

        public string Parse(string[] particles, ref ConfigCollection data)
        {
            if (bool.TryParse(particles[1], out bool val))
                data.AddData(Instruction, val);
            else
                AddError("Invalid value for disable_win_key. Should be true or false");
            
            return "";
        }
    }
}