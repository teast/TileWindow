namespace TileWindow.Configuration.Parser.Instructions
{
    public interface IParseInstruction
    {
        event AddErrorDelegate AddError;

        string Instruction { get; }

        string Parse(string[] particles, ref ConfigCollection data);
    }
}