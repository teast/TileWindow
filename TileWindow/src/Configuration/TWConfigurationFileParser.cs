using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using TileWindow.Configuration.Parser;
using TileWindow.Configuration.Parser.Commands;
using TileWindow.Configuration.Parser.Instructions;

namespace TileWindow.Configuration
{
    public class TWConfigurationFileParser
    {
        private readonly IParseInstructionBuilder _instructionBuilder;
        private readonly IDictionary<string, string> _data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _context = new Stack<string>();
        private string _currentPath;
        private readonly IDictionary<string, string> _contextTranslation = new Dictionary<string, string>
        {
            { "disable_win_key", "DisableWinKey" }
        };

        public TWConfigurationFileParser(IParseInstructionBuilder instructionBuilder)
        {
            _data = new Dictionary<string, string>();
            this._instructionBuilder = instructionBuilder;
        }

        public static IDictionary<string, string> Parse(Stream input)
        {
            var variableParser = new VariableFinder();
            var cmdBuilder = new ParseCommandBuilder(variableParser);
            var executor = new CommandExecutor(cmdBuilder);
            var tester = new BlankCommandHandler();
            var parser = new TWConfigurationFileParser(new ParseInstructionBuilder(variableParser, executor, tester));
            
            return parser.ParseStream(input);
        }

        private IDictionary<string, string> ParseStream(Stream input)
        {
            _data.Clear();

            using (var parser = new FileParser(_instructionBuilder))
            {
                parser.Parse(input);
                VisitMode(parser.Data);
            }

            return _data;
        }

        private void VisitMode(ConfigCollection mode)
        {
            foreach(var data in mode.Data)
            {
                EnterContext(data.Key);
                VisitValue(data.Value);
                ExitContext();
            }

            EnterContext("KeyBinds");
            foreach(var key in mode.KeyBinds)
            {
                EnterContext(key.Key);
                VisitValue(key.Value);
                ExitContext();
            }
            ExitContext();

            foreach(var cmode in mode.Modes)
            {
                EnterContext(cmode.Key);
                VisitMode(cmode.Value);
                ExitContext();
            }
        }

        private void VisitValue(string value)
        {
            var key = _currentPath;
            if (_data.ContainsKey(key))
            {
                throw new FormatException($"Duplicate values found \"{key}\"");
            }
            _data[key] = value;
        }

         private void EnterContext(string context)
        {
            _context.Push(TranslateContext(context));
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private void ExitContext()
        {
            _context.Pop();
            _currentPath = ConfigurationPath.Combine(_context.Reverse());
        }

        private string TranslateContext(string context)
        {
            if (!_contextTranslation.TryGetValue(context, out string newContext))
                newContext = context;
            return newContext;
        }
    }
}