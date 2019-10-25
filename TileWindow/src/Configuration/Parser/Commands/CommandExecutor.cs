using System.Linq;

namespace TileWindow.Configuration.Parser.Commands
{
    public interface ICommandExecutor
    {
        /// <summary>
        /// If an command is found it gets executed and result is return
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        bool? Execute(string cmd, ICommandHandler handler);

        /// <summary>
        /// If an command is found it gets executed and result is return
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="context"></param>
        /// <param name="handler"></param>
        /// <returns></returns>
        bool? Execute(string cmd, ConfigCollection context, ICommandHandler handler);
    }

    public class CommandExecutor : ICommandExecutor
    {
        private readonly IParseCommandBuilder builder;

        public CommandExecutor(IParseCommandBuilder builder)
        {
            this.builder = builder;
        }

        public bool? Execute(string cmd, ICommandHandler handler)
        {
            return Execute(cmd, null, handler);
        }

        public bool? Execute(string cmd, ConfigCollection context, ICommandHandler handler)
        {
            var commands = builder.Build();
            builder.VariableFinder.PushContext(context);
            var command = commands.FirstOrDefault(c => c.Validate(cmd));
            var result = command?.Execute(handler, cmd);
            builder.VariableFinder.PopContext();
            return result;
        }
    }
}