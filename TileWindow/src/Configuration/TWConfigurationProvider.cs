using System.IO;
using Microsoft.Extensions.Configuration;

namespace TileWindow.Configuration
{
    public class TWConfigurationProvider : FileConfigurationProvider
    {
        public TWConfigurationProvider(TWConfigurationSource source) : base(source) { }

        public override void Load(Stream stream)
        {
            Data = TWConfigurationFileParser.Parse(stream);
        }
    }
}