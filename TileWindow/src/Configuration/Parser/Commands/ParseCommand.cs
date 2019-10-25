namespace TileWindow.Configuration.Parser.Commands
{
    public abstract class ParseCommand
    {
        protected readonly IVariableFinder variableFinder;

        public abstract string Instruction { get; }

        public ParseCommand(IVariableFinder variableFinder)
        {
            this.variableFinder = variableFinder;
        }
        
        public abstract bool Execute(ICommandHandler handler, string data);

        public bool Validate(string data) => (data?.StartsWith(Instruction) ?? false) && DoValidate(data);

        protected abstract bool DoValidate(string line);

        protected string GetVariable(string variable)
        {
            var reuslt = variableFinder.GetValue(variable);
            if (reuslt.result)
                return reuslt.value;

            throw new System.FormatException($"Missing variable \"{variable}\"");
        }
    }
}
