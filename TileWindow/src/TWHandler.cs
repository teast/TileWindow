using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Threading;
using Serilog;
using TileWindow.Dto;

namespace TileWindow
{
	public class TWHandler: IDisposable
	{
		private string exec;
        private string pipeName;
		private NamedPipeServerStream pipe = null;
		private BinaryReader pipeReader = null;
		private Process proc = null;
		private ConcurrentQueue<PipeMessage> queue;
		
		private bool stopCalled = false;
		private bool disableWinKey;
        private readonly bool showHooks;
        private readonly IPInvokeHandler pinvokeHandler;
        private readonly ISignalHandler signalHandler;
        private AutoResetEvent pipeDone = new AutoResetEvent(false);

		public TWHandler(string exec, string pipeName, ref ConcurrentQueue<PipeMessage> queue, AppConfig appConfig, IPInvokeHandler pinvokeHandler, ISignalHandler signalHandler)
		{
			this.queue = queue;
			this.exec = exec;
			this.pipeName = pipeName;
			this.disableWinKey = appConfig?.DisableWinKey ?? false;
			this.showHooks = appConfig?.DebugShowHooks ?? true;
            this.pinvokeHandler = pinvokeHandler;
            this.signalHandler = signalHandler;
        }
		
		public void Start()
		{
			if (pipe != null || proc != null)
			{
//Log.Information($"{this} Start() but pipe or proc is not null so calling stop before");
				Stop();
			}

			this.InitNamedPipeServer();
//Log.Information($"{this} starting process...");
			this.StartProcess();
			stopCalled = false;
//Log.Information($"{this} Begin wait for connection...");
			pipe.BeginWaitForConnection(new AsyncCallback(HandlePipeConnection), pipe);
		}

		public void Stop()
		{
			stopCalled = true;
			if (proc == null)
				return;
			if (proc.HasExited == false)
			{
//Log.Information($"{this} Sending WM_CLOSE to {proc?.Threads?.Count} threads...");
			foreach(ProcessThread t in proc.Threads)
				pinvokeHandler.PostThreadMessage((uint)t.Id, PInvoker.WM_CLOSE, UIntPtr.Zero, IntPtr.Zero);
			}

//Log.Information($"{this} Waiting on pipe to be done...");
			pipeDone.WaitOne();

			pipeReader?.Dispose();
			pipe?.Dispose();
			proc?.Dispose();
			pipeReader = null;
			pipe = null;
			proc = null;
//Log.Information($"{this} All resources disposed...");
		}

		public override string ToString() => $"TWHandler({Path.GetFileName(exec)})";

		public void Dispose()
		{
			if (stopCalled == false)
			{
				Stop();
			}

			try
			{
				pipeReader.Dispose();
			}
			catch {}
			
			try
			{
				pipe.Dispose();
			}
			catch {}
			
			try
			{
				proc.Dispose();
			}
			catch {}
		}

		private void HandlePipeConnection(IAsyncResult iar)
		{
//Log.Information($"{this} Got pipe connection. entering message loop...");
			while(stopCalled == false && Startup.ParserSignal.Done == false)
			{
				try
				{
					WaitForMessage();
				}
				catch(Exception ex)
				{
					Log.Fatal(ex, $"Unhandled exception in {this}");
					break;
				}
			}

//Log.Information($"{this} Ending pipe connection and triggering pipeDone...");
			if (pipe.IsConnected)
				pipe.EndWaitForConnection(iar);
			pipeDone.Set();
		}

		private void InitNamedPipeServer()
		{
			if(pipeReader != null)
				pipeReader.Dispose();
			
			if(pipe != null)
				pipe.Dispose();
			
			pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 2);
			pipeReader = new BinaryReader(pipe);
		}
		
		private void proc_Exited(object sender, EventArgs e)
		{
			// TODO: Flag this somewhere so the program quits...
			//Log.Information($"{this} proc_Exited");
			pipeDone.Set();
		}
		
		private void StartProcess()
		{
			var start = new ProcessStartInfo();
			start.Arguments = disableWinKey ? "disablewinkey" : "";
			start.FileName = exec;
			start.WorkingDirectory = System.IO.Path.GetDirectoryName(exec);
			
			start.WindowStyle = showHooks ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
			start.CreateNoWindow = !showHooks;
			start.RedirectStandardOutput = !showHooks;
			start.RedirectStandardError = !showHooks;

			if(proc != null)
				proc.Dispose();
			
			proc = new Process { StartInfo = start, EnableRaisingEvents = !showHooks };
			proc.OutputDataReceived += (sender, arg) => {
				Log.Warning($"TWHandler Data: [{arg.Data}]");
			};
			proc.ErrorDataReceived += (sender, arg) => {
				Log.Fatal($"TWHandler Data: [{arg.Data}]");
			};

			proc.Exited += proc_Exited;
			proc.Start();
		}
		
		private void WaitForMessage()
		{
			if (pipe.IsConnected == false)
				return;

			pipe.WaitForPipeDrain();
			var msg = ByteToType<PipeMessage>(pipeReader);
			//Trace.WriteLine("Msg: " + msg.msg + " wParam: " + msg.wParam + " lParam: " + msg.lParam);
			//Log.Information($"{this} addind message to queue: {signalHandler.SignalToString((uint)msg.msg)}");
			queue.Enqueue(msg);
			Startup.ParserSignal.SignalNewMessage();
		}
		
		private static T ByteToType<T>(BinaryReader reader)
		{
		    byte[] bytes = reader.ReadBytes(Marshal.SizeOf(typeof(T)));
		
		    GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
		    T theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
		    handle.Free();
		
		    return theStructure;
		}
	}
}
