using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TileWindow.Configuration.Parser;
using TileWindow.Configuration.Parser.Commands;
using TileWindow.Dto;
using TileWindow.Handlers;
using TileWindow.Nodes;
using TileWindow.Nodes.Creaters;
using TileWindow.Trackers;

namespace TileWindow
{
    public static class Ioc
    {
        public static ServiceCollection Init(IConfiguration config, ConcurrentQueue<PipeMessageEx> queue)
        {
            var services = new ServiceCollection();

            var appConfig = config.Get<AppConfig>() ?? new AppConfig();
            services.AddLogging(log => log.AddSerilog());
            services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);
            services.AddTransient<IFocusTracker, FocusTracker>();
            services.AddSingleton<AppConfig>(_ => appConfig);
            services.AddSingleton<IScreens, Screens>();
            services.AddSingleton<IVirtualDesktopCollection, VirtualDesktopCollection>(serv => new VirtualDesktopCollection(10));
            services.AddSingleton<VirtualDesktopCollection, VirtualDesktopCollection>();
            services.AddSingleton<IPInvokeHandler, PInvokeHandler>();
            services.AddSingleton<ISignalHandler, SignalHandler>();
            services.AddSingleton<ConcurrentQueue<PipeMessageEx>>(_ => queue);
            services.AddSingleton<IVariableFinder, VariableFinder>();
            services.AddSingleton<IParseCommandBuilder, ParseCommandBuilder>();
            services.AddSingleton<ICommandExecutor, CommandExecutor>();
            services.AddSingleton<ICommandHelper, CommandHelper>();
            services.AddSingleton<IFocusHandler, FocusHandler>();
            services.AddSingleton<IDragHandler, DragHandler>();
            services.AddSingleton<IWindowTracker, WindowTracker>();
            services.AddSingleton<IContainerNodeCreater, ContainerNodeCreater>();
            services.AddTransient<CreateWindowNode, CreateWindowNode>(serv => (rect, hWnd, direction) =>
            {
                try
                {
                    return new WindowNode(
                        serv.GetRequiredService<IDragHandler>(),
                        serv.GetRequiredService<IFocusHandler>(),
                        serv.GetRequiredService<ISignalHandler>(),
                        serv.GetRequiredService<IWindowEventHandler>(),
                        serv.GetRequiredService<IWindowTracker>(),
                        serv.GetRequiredService<IPInvokeHandler>(),
                        rect, hWnd, direction);
                }
                catch (Exception ex)
                {
                    Log.Fatal(ex, $"Could not create an window node for {hWnd} (from Startup.cs)");
                }

                return null;
            });
            services.AddSingleton<IWindowNodeCreater, WindowNodeCreater>();
            services.AddSingleton<IScreenNodeCreater, ScreenNodeCreater>();
            services.AddSingleton<IVirtualDesktopCreater, VirtualDesktopCreater>(serv => new VirtualDesktopCreater(serv));
            services.AddSingleton<IStartupHandler, StartupHandler>();
            services.AddSingleton<IKeyHandler, KeyHandler>();
            services.AddSingleton<IWindowEventHandler, WindowEventHandler>();
            services.AddSingleton<MessageHandlerCollection>(serv => new MessageHandlerCollection(
                serv.GetRequiredService<IFocusHandler>(),
                serv.GetRequiredService<IDragHandler>(),
                serv.GetRequiredService<IKeyHandler>(),
                serv.GetRequiredService<IWindowEventHandler>(),
                serv.GetRequiredService<IStartupHandler>()
            ));
            services.AddTransient<MessageParser, MessageParser>();


            return services;
        }
    }
}