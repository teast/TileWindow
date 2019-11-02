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
        private readonly IDictionary<string, string> _data = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private readonly Stack<string> _context = new Stack<string>();
        private readonly IFileParser _fileParser;
        private string _currentPath;

        /// <summary>
        /// Instructions on how to transform various values
        /// </summary>
        /// <remarks>
        /// Key is built up by "currentPath" and context
        /// if "currentPath" is null then it will be matched on all pathes.
        /// </remarks>
        private readonly IDictionary<Tuple<string, string>, string> _contextTranslation = new Dictionary<Tuple<string, string>, string>
        {
            { Tuple.Create("", "disable_win_key"), "DisableWinKey" },
            { Tuple.Create("", "bar"), "Bar" },
            { Tuple.Create((string)null, "colors"), "Colors" },
            { Tuple.Create((string)null, "position"), "Position" },
            { Tuple.Create((string)null, "focused_workspace"), "FocusedWorkspace" },
            { Tuple.Create((string)null, "background"), "Background" },
            { Tuple.Create((string)null, "border"), "Border" },
            { Tuple.Create((string)null, "text"), "Text" }
        };

        public TWConfigurationFileParser(IFileParser fileParser)
        {
            _data = new Dictionary<string, string>();
            _fileParser = fileParser;
        }

        public static IDictionary<string, string> Parse(Stream input)
        {
            var variableParser = new VariableFinder();
            var cmdBuilder = new ParseCommandBuilder(variableParser);
            var executor = new CommandExecutor(cmdBuilder);
            var tester = new BlankCommandHandler();
            var instrBuilder = new ParseInstructionBuilder(variableParser, executor, tester);
            var fileParser = new FileParser(instrBuilder);
            var parser = new TWConfigurationFileParser(fileParser);
            
            return parser.ParseStream(input);
        }

        public IDictionary<string, string> ParseStream(Stream input)
        {
            _data.Clear();

            using (var parser = _fileParser)
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
            if (_contextTranslation.TryGetValue(Tuple.Create(_currentPath, context), out string newContext))
                return newContext;
            if (_contextTranslation.TryGetValue(Tuple.Create((string)null, context), out newContext))
                return newContext;
            return context;
        }
    }
}