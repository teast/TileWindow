﻿using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Extensions.Configuration;
using TileWindow.Dto;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using System.IO;
using TileWindow.Nodes;
using TileWindow.Configuration;
using TileWindow.Forms;
using System.Runtime.InteropServices;

namespace TileWindow
{
    public static class Startup
    {
        private static ConcurrentQueue<PipeMessageEx> queue = new ConcurrentQueue<PipeMessageEx>();
        public static MessageParser.MHSignal ParserSignal = new MessageParser.MHSignal(ref queue);

        private static readonly Object lock32 = new Object();
        private static readonly Object lock64 = new Object();
        private static uint handler32;
        private static uint handler64;
        private static void UpdateHandler32(uint i)
        {
            lock (lock32)
            {
                handler32 = i;
            }
        }

        private static uint GetHandler32()
        {
            uint i;
            lock (lock32)
            {
                i = handler32;
            }

            return i;
        }

        private static void UpdateHandler64(uint i)
        {
            lock (lock64)
            {
                handler64 = i;
            }
        }

        private static uint GetHandler64()
        {
            uint i;
            lock (lock64)
            {
                i = handler64;
            }

            return i;
        }

        [STAThread]
        static void Main()
        {
            var rootDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var tw32Path = Path.Combine(rootDir, "twhandler32.exe");
            var tw64Path = Path.Combine(rootDir, "twhandler64.exe");

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.File(Path.Combine(rootDir, "logs", "tilewindow.log"), Serilog.Events.LogEventLevel.Verbose)
                .WriteTo.Console(Serilog.Events.LogEventLevel.Verbose)
                .CreateLogger();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isFirstInstance;
            using (Mutex mtx = new Mutex(true, "TileWindow", out isFirstInstance))
            {
                var thParserDone = new AutoResetEvent(false);

                IConfiguration config = new ConfigurationBuilder()
                        .AddTWConfigFile("tilewindow.config", true, true)
                        .Build();

                var serviceProvider = Ioc.Init(config, queue).BuildServiceProvider();
                var pinvokeHandler = serviceProvider.GetRequiredService<IPInvokeHandler>();
                var signalHandler = serviceProvider.GetRequiredService<ISignalHandler>();
                var desktops = serviceProvider.GetRequiredService<IVirtualDesktopCollection>();

                try
                {
                    if (File.Exists(tw32Path) == false)
                    {
                        throw new FileNotFoundException($"twhandler32.exe not found. (path: \"{tw32Path}\") this is needed to listen on 32-bit programs.");
                    }

                    if (File.Exists(tw64Path) == false)
                    {
                        throw new FileNotFoundException($"twhandler64.exe not found. (path: \"{tw64Path}\") this is needed to listen on 64-bit programs.");
                    }

                    if (isFirstInstance)
                    {
                        var appConfig = serviceProvider.GetService<AppConfig>() ?? new AppConfig();
                        var queue = serviceProvider.GetRequiredService<ConcurrentQueue<PipeMessageEx>>();

                        using (var thread32 = new TWHandler(tw32Path, "tilewindowpipe32", ref queue, appConfig, pinvokeHandler, signalHandler))
                        using (var thread64 = new TWHandler(tw64Path, "tilewindowpipe64", ref queue, appConfig, pinvokeHandler, signalHandler))
                        using (var sessionHandler = new SessionChangeHandler(serviceProvider.GetRequiredService<IPInvokeHandler>()))
                        {
                            sessionHandler.MachineLocked += (sender, arg) =>
                            {
                                thread32.Stop();
                                thread64.Stop();
                            };
                            sessionHandler.MachineUnlocked += (sender, arg) =>
                            {
                                ParserSignal.SignalRestartThreads();
                            };

                            ParserSignal.RestartThreads += (sender, arg) =>
                            {
                                thread32.Stop();
                                thread64.Stop();
                                thread32.Start();
                                thread64.Start();
                            };

                            var parser = new Thread(() => MessageParser.StartThread(ref thParserDone, serviceProvider));

                            IntPtr appbarHandle = IntPtr.Zero;
                            var appbar = new Thread(() =>
                            {
                                try
                                {
                                    var _form = new FormAppBar(appConfig, pinvokeHandler, signalHandler.WMC_SHOWNODE);
                                    appbarHandle = _form.Handle;
                                    Application.Run(_form);
                                }
                                catch (Exception ex)
                                {
                                    Log.Fatal(ex, $"Unhandled exception with appbar");
                                }
                            });

                            if (appConfig.Bar != null)
                            {
                                appbar.Start();
                                desktops.DesktopChange += (sender, arg) =>
                                {
                                    if (appbarHandle == IntPtr.Zero)
                                    {
                                        return;
                                    }
                                    var HWnd = new HWnd(appbarHandle);
                                    var lParam = arg.Visible || arg.Focus ? 1 : 0; // focus == visible
                                    if (arg.Focus) { lParam += 2; }
                                    pinvokeHandler.PostMessage(new HandleRef(HWnd, HWnd.Hwnd), signalHandler.WMC_SHOWNODE, new IntPtr(arg.Index), new IntPtr(lParam));
                                };
                            }

                            thread32.Start();
                            thread64.Start();
                            parser.Start();

                            using (var noti = new NotificationIcon(appConfig, new AppResource()))
                            {
                                noti.InitNotification();
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
                catch (Exception ex)
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
