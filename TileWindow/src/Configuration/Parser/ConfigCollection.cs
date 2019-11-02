using System.Collections.Generic;

namespace TileWindow.Configuration.Parser
{
    public class ConfigCollection
    {
        public string Name { get; }
        public Dictionary<string, string> Variables { get; }
        public Dictionary<string, string> KeyBinds { get; }
        public Dictionary<string, string> Data { get; }
        public Dictionary<string, ConfigCollection> Modes { get; }

        public ConfigCollection Parent { get; }
        public ConfigCollection(string name, ConfigCollection parent = null)
        {
            Name = name;
            Parent = parent;
            Variables = new Dictionary<string, string>();
            KeyBinds = new Dictionary<string, string>();
            Data = new Dictionary<string, string>();
            Modes = new Dictionary<string, ConfigCollection>();
        }

        public void AddVariable(string variableName, string variableData)
        {
            if (Variables.ContainsKey(variableName))
                Variables[variableName] = variableData;
            else
                Variables.Add(variableName, variableData);
        }

        public void AddData<T>(string variableName, T variableData)
        {
            Data.Add(variableName, variableData.ToString());
        }
    }
}