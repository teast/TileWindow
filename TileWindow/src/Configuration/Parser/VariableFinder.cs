using System.Collections.Generic;

namespace TileWindow.Configuration.Parser
{
    public interface IVariableFinder
    {
        (bool result, string value) GetValue(string variable);
        void PushContext(ConfigCollection context);
        void PopContext();
        string ParseAll(string text);
    }

    public class VariableFinder : IVariableFinder
    {
        private readonly Stack<ConfigCollection> context;

        public VariableFinder()
        {
            context = new Stack<ConfigCollection>();
        }

        private (int index, char spit) IndexOf(string str, params char[] splits)
        {
            var index = int.MaxValue;
            var split = ' ';
            foreach (var s in splits)
            {
                var i = str.IndexOf(s);
                if (i != -1 && i < index)
                {
                    index = i;
                    split = s;
                }
            }

            if (index == int.MaxValue)
            {
                index = -1;
            }

            return (index, split);
        }

        public string ParseAll(string text)
        {

            var result = "";
            var remaining = text;
            int i;
            char split;
            (i, split) = IndexOf(remaining, ' ', '+', ';');
            while (remaining.Length > 0 && i != -1)
            {
                var word = remaining.Substring(0, i);
                remaining = remaining.Substring(i + 1);

                if (word.StartsWith('$'))
                {
                    var varResult = GetValue(word);
                    if (varResult.result == false)
                    {
                        throw new System.FormatException($"Unknown variable \"{word}\"");
                    }

                    result += varResult.value + split;
                }
                else
                {
                    result += word + split;
                }
                
                (i, split) = IndexOf(remaining, ' ', '+', ';');
            }

            return result + remaining;
        }

        public (bool result, string value) GetValue(string variable)
        {
            var p = context.Peek();
            while (p != null)
            {
                var result = GetValueFromCollection(p, variable);
                if (result.result)
                {
                    return result;
                }

                p = p.Parent;
            }

            return (false, null);
        }

        public void PushContext(ConfigCollection context)
        {
            this.context.Push(context);
        }

        public void PopContext()
        {
            this.context.Pop();
        }

        private (bool result, string value) GetValueFromCollection(ConfigCollection collection, string variable)
        {
            return (collection.Variables.TryGetValue(variable, out string value), value);
        }

    }
}