using Microsoft.Extensions.Configuration;

namespace TileWindow.Configuration
{
    public class TWConfigurationSource : FileConfigurationSource
    {
        public override IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            EnsureDefaults(builder);
            return new TWConfigurationProvider(this);
        }
    }
}