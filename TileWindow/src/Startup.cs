using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using TileWindow.Dto;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using TileWindow.Handlers;
using System.IO;
using TileWindow.Nodes.Creaters;
using TileWindow.Nodes;
using TileWindow.Trackers;

namespace TileWindow
{
    public static class Startup
    {
		public static MessageParser.MHSignal ParserSignal = new MessageParser.MHSignal();
		
		private static readonly Object lock32 = new Object();
		private static readonly Object lock64 = new Object();
		private static uint handler32;
		private static uint handler64;
		private static void UpdateHandler32(uint i)
		{
			lock(lock32)
			{
				handler32 = i;
			}
		}
		
		private static uint GetHandler32()
		{
			uint i;
			lock(lock32)
			{
				i = handler32;
			}
			
			return i;
		}
		
		private static void UpdateHandler64(uint i)
		{
			lock(lock64)
			{
				handler64 = i;
			}
		}
		
		private static uint GetHandler64()
		{
			uint i;
			lock(lock64)
			{
				i = handler64;
			}
			
			return i;
		}
		
		private static void ConfigureServices(IServiceCollection services, IConfiguration config)
		{
			var appConfig = config.Get<AppConfig>();
			services.AddLogging(log => log.AddSerilog());
			services.Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Trace);
			services.AddTransient<IFocusTracker, FocusTracker>();
			services.AddSingleton<AppConfig>(_ => appConfig);
			services.AddSingleton<IScreens, Screens>();
			services.AddSingleton<IVirtualDesktopCollection, VirtualDesktopCollection>(serv => new VirtualDesktopCollection(10));
			services.AddSingleton<VirtualDesktopCollection, VirtualDesktopCollection>();
			services.AddSingleton<IPInvokeHandler, PInvokeHandler>();
			services.AddSingleton<ISignalHandler, SignalHandler>();
			services.AddSingleton<ConcurrentQueue<PipeMessage>>(_ => new ConcurrentQueue<PipeMessage>());
			services.AddSingleton<ICommandHelper, CommandHelper>();
			services.AddSingleton<IFocusHandler, FocusHandler>();
			services.AddSingleton<IWindowTracker, WindowTracker>();
			services.AddSingleton<IContainerNodeCreater, ContainerNodeCreater>();
			services.AddTransient<CreateWindowNode, CreateWindowNode>(serv => (rect, hWnd, direction) => {
				try
				{
					return new WindowNode(
						serv.GetRequiredService<IFocusHandler>(),
						serv.GetRequiredService<IWindowEventHandler>(),
						serv.GetRequiredService<IWindowTracker>(),
						serv.GetRequiredService<IPInvokeHandler>(),
						rect, hWnd, direction);
				}
				catch(Exception ex)
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
				serv.GetRequiredService<IKeyHandler>(),
				serv.GetRequiredService<IWindowEventHandler>(),
				serv.GetRequiredService<IStartupHandler>()
			));
			services.AddTransient<MessageParser, MessageParser>();
		}

		[STAThread]
        static void Main()
        {
			var rootDir  = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
			var tw32Path = Path.Combine(rootDir, "twhandler32.exe");
			var tw64Path = Path.Combine(rootDir, "twhandler64.exe");

			Log.Logger = new LoggerConfiguration()
				.WriteTo.File(Path.Combine(rootDir, "logs", "tilewindow.log"), Serilog.Events.LogEventLevel.Verbose)
				.WriteTo.Console()
				.CreateLogger();

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			
			bool isFirstInstance;
			using (Mutex mtx = new Mutex(true, "TileWindow", out isFirstInstance))
			{
				var thParserDone = new AutoResetEvent(false);
				var serviceCollection = new ServiceCollection();

				IConfiguration config = new ConfigurationBuilder()
						.AddJsonFile("appsettings.json", true, true)
						.Build();

				ConfigureServices(serviceCollection, config);
				var serviceProvider = serviceCollection.BuildServiceProvider();
				var pinvokeHandler = serviceProvider.GetRequiredService<IPInvokeHandler>();
				var signalHandler = serviceProvider.GetRequiredService<ISignalHandler>();

				try
				{
					if (File.Exists(tw32Path) == false)
						throw new FileNotFoundException($"twhandler32.exe not found. (path: \"{tw32Path}\") this is needed to listen on 32-bit programs.");
					if (File.Exists(tw64Path) == false)
						throw new FileNotFoundException($"twhandler64.exe not found. (path: \"{tw64Path}\") this is needed to listen on 64-bit programs.");

					if (isFirstInstance)
					{
						var appConfig = serviceProvider.GetService<AppConfig>() ?? new AppConfig();
						var queue = serviceProvider.GetRequiredService<ConcurrentQueue<PipeMessage>>();

						using(var thread32 = new TWHandler(tw32Path, "tilewindowpipe32", ref queue, appConfig, pinvokeHandler, signalHandler))
						using(var thread64 = new TWHandler(tw64Path, "tilewindowpipe64", ref queue, appConfig, pinvokeHandler, signalHandler))
						using(var sessionHandler = new SessionChangeHandler(serviceProvider.GetRequiredService<IPInvokeHandler>()))
						{
							/*
							Func<string> getDesktopName = () => {
								var desktop = pinvokeHandler.OpenInputDesktop(0, false, 0);
								if (desktop == IntPtr.Zero)	
									return "(null) desktop handler";

								var b = new byte[1024];
								uint l = 0;
								var str = "";
								var uir = pinvokeHandler.GetUserObjectInformation(desktop, 2, b, (uint)1024, out l);
								if (uir && l > 1)
									str = System.Text.Encoding.ASCII.GetString(b, 0, (int)l-1);
								pinvokeHandler.CloseDesktop(desktop.ToInt32());
								return str;
							};
							Log.Information($"START OF CODE! (Desktop: \"{getDesktopName()}\")");
							*/

							sessionHandler.MachineLocked += (sender, arg) => {
								thread32.Stop();
								thread64.Stop();
							};
							sessionHandler.MachineUnlocked += (sender, arg) => {
								ParserSignal.SignalRestartThreads();
							};

							ParserSignal.RestartThreads += (sender, arg) => {
								thread32.Stop();
								thread64.Stop();
								thread32.Start();
								thread64.Start();
							};

							var parser = new Thread(() => MessageParser.StartThread(ref thParserDone, serviceProvider));
							thread32.Start();
							thread64.Start();
							parser.Start();
							
							using(var noti = new NotificationIcon(appConfig, new AppResource()))
							{
								noti.InitNotification();
								//frmStartup.Hide();
								Application.Run();
							}

							ParserSignal.Done = true;
							thread32.Stop();
							thread64.Stop();
						}
					} 
					else
					{
						MessageBox.Show("Just one instance of TileWindow please!", "TileWindow", MessageBoxButtons.OK);
					}
				}
				catch(Exception ex)
				{
					Log.Fatal(ex, "Unhandled exception occured!");
					throw;
				}
				finally
				{
					ParserSignal.Done = true;
					ParserSignal.SignalNewMessage();
					thParserDone.WaitOne();
				}
			}
        }
    }
}
