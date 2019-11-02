using System;
using System.Collections.Generic;
using System.IO;
using TileWindow.Configuration.Parser.Instructions;

namespace TileWindow.Configuration.Parser
{
    public delegate void AddErrorDelegate(string error, bool withLineNumbers = true);

    public enum FileParserStateResult
    {
        None,
        OpenBracket,
        CloseBracket
    }

    public interface IFileParser: IDisposable
    {
        ConfigCollection Data { get; }
        List<Tuple<int, string>> Errors { get; }
        void Parse(Stream stream);
    }

    public class FileParser: IFileParser
    {
        private readonly List<IParseInstruction> _parseInstruction;
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
            Data = new ConfigCollection("Default");
            _data = Data;
            Errors = new List<Tuple<int, string>>();
            
            _parseInstruction = instructionBuilder.Build(new AddErrorDelegate(AddError));
        }

        public void Parse(Stream stream)
        {
            _sr = new StreamReader(stream);

            string line = "";
            var prevName = "";
            while ((line = NextLine(line)) != null)
            {
                var result = ParseLine(line);
                if (result?.Status == FileParserStateResult.OpenBracket)
                {
                    _blocks++;
                    var modeName = result?.Instruction?.Instruction ?? prevName;
                    var old = _data;
                    _data = new ConfigCollection(modeName, old);
                    old.Modes.Add(modeName, _data);
                }
                else if (result?.Status == FileParserStateResult.CloseBracket)
                {
                    _blocks--;
                    _data = _data.Parent;
                }

                prevName = result?.Instruction?.Instruction ?? "";
                line = result?.Remaining ?? "";
            }

            if (_blocks != 0)
            {
                AddError("Missing closing Brackets", false);
            }
            else if (_data.Name != "Default")
            {
                AddError($"Unknown error. ended up in mode \"{_data.Name}\" but it should be \"Default\".", false);
            }
        }

        public void Dispose()
        {
            if (_sr != null)
                _sr.Dispose();
        }

        protected virtual ParseInstructionResult ParseLine(string line)
        {
            // Trim
            line = line.Trim(new [] { ' ', '\t' });

            // Comment or empty line, ignore
            if (line.StartsWith('#') || string.IsNullOrEmpty(line))
                return new ParseInstructionResult(FileParserStateResult.None, "", null);
            
            if (line.StartsWith('{'))
            {
                return new ParseInstructionResult(FileParserStateResult.OpenBracket, line.Substring(1), null);
            }

            if (line.StartsWith('}'))
            {
                return new ParseInstructionResult(FileParserStateResult.CloseBracket, line.Substring(1), null);
            }

            return ParseLineParticles(line);
        }

        protected virtual ParseInstructionResult ParseLineParticles(string line)
        {
            var particles = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            // Wrong number of particles (minimum 2)
            if (particles.Length < 2)
            {
                AddError("Invalid instruction");
                return null;
            }

            foreach(var parser in _parseInstruction)
            {
                if (parser.Parse(particles, ref _data))
                    return parser.FetchResult;
            }
            
            AddError($"Unknown instruction \"{particles[0]}\"");
            return null;
        }

        protected virtual string NextLine(string remaining)
        {
            if ((remaining?.Length ?? 0) > 0)
                return remaining;

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