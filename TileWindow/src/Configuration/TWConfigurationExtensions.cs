using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace TileWindow.Configuration
{
    public static class TWConfigurationExtensions
    {
        public static IConfigurationBuilder AddTWConfigFile(this IConfigurationBuilder builder, string path)
        {
            return AddTWConfigFile(builder, provider: null, path: path, optional: false, reloadOnChange: false);
        }

        public static IConfigurationBuilder AddTWConfigFile(this IConfigurationBuilder builder, string path, bool optional)
        {
            return AddTWConfigFile(builder, provider: null, path: path, optional: optional, reloadOnChange: false);
        }

        public static IConfigurationBuilder AddTWConfigFile(this IConfigurationBuilder builder, string path, bool optional, bool reloadOnChange)
        {
            return AddTWConfigFile(builder, provider: null, path: path, optional: optional, reloadOnChange: reloadOnChange);
        }

        public static IConfigurationBuilder AddTWConfigFile(this IConfigurationBuilder builder, IFileProvider provider, string path, bool optional, bool reloadOnChange)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentException("Invalid file path", nameof(path));
            }

            return builder.AddTWConfigFile(s =>
            {
                s.FileProvider = provider;
                s.Path = path;
                s.Optional = optional;
                s.ReloadOnChange = reloadOnChange;
                s.ResolveFileProvider();
            });
        }

        public static IConfigurationBuilder AddTWConfigFile(this IConfigurationBuilder builder, Action<TWConfigurationSource> configureSource)
            => builder.Add(configureSource);
    }
}