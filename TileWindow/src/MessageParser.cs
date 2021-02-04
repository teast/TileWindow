using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using TileWindow.Dto;
using TileWindow.Handlers;

namespace TileWindow
{
    public class MessageParser : IDisposable
    {
        private ConcurrentQueue<PipeMessageEx> queue;
        private AppConfig _appConfig;
        private readonly ISignalHandler signal;
        private readonly MessageHandlerCollection handlers;

        public MessageParser(AppConfig appConfig, ISignalHandler signal, MessageHandlerCollection handlers, ConcurrentQueue<PipeMessageEx> queue)
        {
            _appConfig = appConfig;
            this.signal = signal;
            this.handlers = handlers ?? new MessageHandlerCollection();
            this.queue = queue;
        }

        public void HandleMessages()
        {
            while (true)
            {
                PipeMessageEx msg;

                if (Startup.ParserSignal.ReloadConfig)
                {
                    this.ReadConfig();
                    // Clear the stack
                    while (queue.TryDequeue(out msg)) ;

                    // Signal done
                    Startup.ParserSignal.ReloadConfig = false;
                }

                if (!queue.TryDequeue(out msg))
                    break;

                if (msg.msg == signal.WMC_EXTRATRACK)
                {
                    Log.Information($"Message: {signal.SignalToString((uint)msg.msg)} ({msg.from}) wParam: {msg.wParam}, lParam: {msg.lParam}");
                }
				
                foreach (var handler in handlers)
                {
                    handler.HandleMessage(msg);
                }
            }
        }

        public void ReadConfig()
        {
            foreach (var handler in handlers)
                handler.ReadConfig(_appConfig);
        }

        public void PostInit()
        {
            foreach (var handler in handlers)
                handler.Init();
        }

        public void Quit()
        {
            foreach (var handler in handlers)
                handler.Quit();
        }
        public static void StartThread(ref AutoResetEvent thDone, ServiceProvider services)
        {
            try
            {
                using (var parser = services.GetRequiredService<MessageParser>())
                {
                    try
                    {
                        parser.ReadConfig();
                        parser.PostInit();
                    }
                    catch (Exception ex)
                    {
                        Log.Fatal(ex, "Unhandled exception occured when init MessageParser!");
                        Startup.ParserSignal.Done = true;
                    }

                    while (!Startup.ParserSignal.Done)
                    {
                        try
                        {
                            parser.HandleMessages();
                            Startup.ParserSignal.WaitNewMessage();
                        }
                        catch (Exception ex)
                        {
                            Log.Fatal(ex, "Unhandled exception occured in MessageParser!");

                            try
                            {
                                parser.DumpDebug();
                            }
                            catch (Exception ex2)
                            {
                                Log.Fatal(ex2, "Exception occured while dumping debug information!");
                            }

                            Startup.ParserSignal.Done = true;
                        }
                    }

                    parser.Quit();
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unhandled exception occured in initialize phase for MessageParser");
            }
            finally
            {
                thDone.Set();
            }
        }

        public void DumpDebug()
        {
            foreach (var handler in handlers)
                handler.DumpDebug();
        }

        public void Dispose()
        {
            foreach (var handler in handlers)
                handler.Dispose();
        }

        public class MHSignal
        {
            private object locker = new object();
            private bool reloadConfig;
            private bool stopHandlingMessages;
            private AutoResetEvent msgSignal = new AutoResetEvent(false);
            private ConcurrentQueue<PipeMessageEx> queue;
            public event EventHandler RestartThreads;
            public bool Done { get; set; }

            /// <summary>
            /// Set to true to signal MessageParser to reload configuration
            /// Read false to know when thread is done with reloading config
            /// </summary>
            public bool ReloadConfig
            {
                get
                {
                    return this.UseLocker(() => this.reloadConfig);
                }
                set
                {
                    this.UseLockerA(() => this.reloadConfig = value);
                }
            }

            /// <summary>
            /// Set to true to signal to stop handling messages, set to false to signal to start handling messages again
            /// </summary>
            public bool StopHandlingMessages
            {
                get
                {
                    return this.UseLocker(() => this.stopHandlingMessages);
                }
                set
                {
                    this.UseLockerA(() => this.stopHandlingMessages = value);
                }
            }

            public MHSignal(ref ConcurrentQueue<PipeMessageEx> queue)
            {
                this.queue = queue;
            }

            public void QueuePipeMessage(PipeMessageEx message)
            {
                queue.Enqueue(message);
                SignalNewMessage();
            }

            /// <summary>
            /// Flag that there are new messages to parse
            /// </summary>
            public void SignalNewMessage()
            {
                msgSignal.Set();
            }

            public void SignalRestartThreads()
            {
                RestartThreads?.Invoke(this, null);
            }

            /// <summary>
            /// Wait until there are any more new messages to parse
            /// </summary>
            public void WaitNewMessage()
            {
                msgSignal.WaitOne();
            }

            /// <summary>
            /// Helper method to use lock object
            /// </summary>
            /// <param name="doer">function to execute inside the lock</param>
            /// <returns>result from doer</returns>
            private T UseLocker<T>(Func<T> doer)
            {
                T result;
                lock (this.locker)
                {
                    result = doer();
                }

                return result;
            }

            /// <summary>
            /// Helper method to use lock object
            /// </summary>
            /// <param name="doer">action to execute inside the lock</param>
            private void UseLockerA(Action doer)
            {
                lock (this.locker)
                {
                    doer();
                }
            }
        }
    }
}
