using System;
using System.Collections.Generic;
using System.IO;
using TileWindow.Configuration.Parser.Instructions;

namespace TileWindow.Configuration.Parser
{
    public delegate void AddErrorDelegate(string error, bool withLineNumbers = true);

    public class FileParser: IDisposable
    {
        private readonly Dictionary<string, IParseInstruction> _parseInstruction;
        private readonly IParseInstructionBuilder instructionBuilder;
        private StreamReader _sr;
        private int _blocks;
        //private int _dblQuotes;
        //private int _quotes;
        private int _lineNumber;
        private ConfigCollection _data;
        public ConfigCollection Data { get; }
        public List<Tuple<int, string>> Errors { get; private set;}

        public FileParser(IParseInstructionBuilder instructionBuilder)
        {
            this.instructionBuilder = instructionBuilder;
            _blocks = 0;
            //_dblQuotes = 0;
            //_quotes = 0;
            _lineNumber = 0;
            Data = new ConfigCollection();
            _data = Data;
            Errors = new List<Tuple<int, string>>();
            
            _parseInstruction = instructionBuilder.Build(new AddErrorDelegate(AddError));
        }

        public void Parse(Stream stream)
        {
            _sr = new StreamReader(stream);

            string line;
            while ((line = NextLine()) != null)
            {
                ParseLine(line, ParseLineInnerDefault);
            }
        }

        public void Dispose()
        {
            if (_sr != null)
                _sr.Dispose();
        }

        protected virtual void ParseLine(string line, Func<string, string> innerParse)
        {
            // Trim
            line = line.TrimStart(new [] { ' ', '\t' });

            // Comment line, ignore
            if (line.StartsWith('#'))
                return;

            var commands = line.Split(';', StringSplitOptions.RemoveEmptyEntries);
            foreach(var cmd in commands)
            {
                var left = cmd;
                while (left.Length > 0)
                {
                    left = innerParse(left);
                }
            }
        }

        protected virtual string ParseLineInnerDefault(string line)
        {
            var particles = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Wrong number of particles (minimum 2)
            if (particles.Length < 2)
            {
                AddError("Invalid instruction");
                return "";
            }

            if (_parseInstruction.TryGetValue(particles[0].ToLowerInvariant(), out IParseInstruction parser))
                return parser.Parse(particles, ref _data);
            
            AddError($"Unknown instruction \"{particles[0]}\"");
            return "";
        }

        protected virtual string ParseLineInnerCloseBlock(string line)
        {
            var close = line.IndexOf('}');
            var open = line.IndexOf('{');

            if (open != -1)
                _blocks++;

            if (close == -1)
                return "";
            
            if (close < open)
            {
                _blocks--;
            }

                return line.Substring(close);
        }

        protected virtual string NextLine()
        {
            if (_sr?.EndOfStream ?? true)
                return null;

            _lineNumber++;
            return _sr.ReadLine();
        }

        protected virtual void AddError(string error, bool withLineNumber = true)
        {
            Errors.Add(Tuple.Create(withLineNumber ? _lineNumber : -1, error));
        }
   }
}