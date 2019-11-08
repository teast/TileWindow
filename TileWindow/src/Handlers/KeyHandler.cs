using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Serilog;
using TileWindow.Dto;

namespace TileWindow.Handlers
{
    public interface IKeyHandler: IHandler
    {
 		bool GetKeyCombination(string s, out ulong[] keys);
		bool keysOk(ulong[] s, bool specificKeys, out int kw);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="keyCombo"></param>
		/// <param name="callback">Should return true to clear key press (so no more trigger will happen on it, note that propagate will still happen, that is all other listeners will be call for this instance)</param>
		/// <returns></returns>
		Guid AddListener(ulong[] keyCombo, Func<ulong[], bool> callback);
		void RemoveListener(Guid id);
		Guid AddOnKeyChangeListener(Action<ulong, bool> listener);
		void RemoveKeyChangeListener(Guid id);
    }

    public class KeyHandler: IKeyHandler
	{
        private const ulong MAX_KEYS = 512;
    	private bool[] Keystate { get; } = new bool[MAX_KEYS];

		private Timer _repeater = null;

        private readonly ISignalHandler signal;

		private List<Tuple<ulong[], Guid, Func<ulong[], bool>>> _listeners;

		private List<Tuple<Guid, Action<ulong, bool>>> _onKeyChangeListeners;

		private int _repeateMs;
 
        public KeyHandler(ISignalHandler signal)
        {
			for(int i = 0; i < Keystate.Length; i++)
				Keystate[i] = false;

            this.signal = signal;
			this._listeners = new List<Tuple<ulong[], Guid, Func<ulong[], bool>>>();
			_onKeyChangeListeners = new List<Tuple<Guid, Action<ulong, bool>>>();
        }

        public void ReadConfig(AppConfig config)
        {
			_repeateMs = Math.Max(config.KeyRepeatInMs, 125);
        }

        public void Init()
        {
			Startup.ParserSignal.RestartThreads += (sender, args) => {
				AbortTimer();
				for(int i = 0; i < Keystate.Length; i++)
					Keystate[i] = false;
			};
        }

        public void Quit()
        {
        }
		
		public Guid AddListener(ulong[] keyCombo, Func<ulong[], bool> callback)
		{
			var id = Guid.NewGuid();
			_listeners.Add(Tuple.Create(keyCombo, id, callback));
			return id;
		}

		public void RemoveListener(Guid id)
		{
			_listeners = _listeners.Where(l => l.Item2 != id).ToList();
		}

		public Guid AddOnKeyChangeListener(Action<ulong, bool> listener)
		{
			var id = Guid.NewGuid();
			_onKeyChangeListeners.Add(Tuple.Create(id, listener));
			return id;
		}

		public void RemoveKeyChangeListener(Guid id)
		{
			_onKeyChangeListeners = _onKeyChangeListeners.Where(l => l.Item1 != id).ToList();
		}

		public void HandleMessage(PipeMessageEx msg)
		{
			if (msg.msg == signal.WMC_KEYDOWN)
			{
				HandleKeyDown(msg);
			}
			else if (msg.msg == signal.WMC_KEYUP)
			{
				HandleKeyUp(msg);
			}
		}

 		public bool GetKeyCombination(string s, out ulong[] keys)
		{
			var ret = new List<ulong>();
			keys = ret.ToArray();
			if(string.IsNullOrWhiteSpace(s))
				return false;
			
			var errors = false;
			foreach(var key in s.Trim().Split(new [] { '+' }, StringSplitOptions.RemoveEmptyEntries))
			{
				ulong found = 0;
				if (key.Length > 1)
				{
					switch(key.ToLowerInvariant())
					{
						case "alt":
							found = 0x12;
							break;
						case "lalt":
							found = 0xa4;
							break;
						case "ralt":
							found = 0xa5;
							break;
						case "shift":
							found = 0x10;
							break;
						case "lshift":
							found = 0xa0;
							break;
						case "rshift":
							found = 0xa1;
							break;
						case "ctrl":
							found = 0x11;
							break;
						case "lctrl":
							found = 0xa2;
							break;
						case "rctrl":
							found = 0xa3;
							break;
						case "left":
							found = 37;
							break;
						case "up":
							found = 38;
							break;
						case "right":
							found = 39;
							break;
						case "down":
							found = 40;
							break;
						case "lwin":
						case "win":
							found = 0x5b;
							break;
						case "rwin":
							found = 0x5c;
							break;
						case "space":
							found = 32;
							break;
					}
				}
				else
				{
					found = Convert.ToUInt64((int)key.ToUpperInvariant()[0]);
				}
				
				if(found > 0)
					ret.Add(found);
				else
				{
					Log.Warning($"Unknown key: \"{key}\"");
					errors = true;
				}
			}
			
			keys = ret.ToArray();
			return !errors;
		}

		public bool keysOk(ulong[] s, bool specificKeys, out int kw)
		{
			if (specificKeys)
			{
				// TODO: Could move this out and make this calculation ones on every key change... might optimize some
				var tmp = Keystate
						.Select((Keystate, keyIndex) => new { keyState = Keystate, keyIndex = (ulong)keyIndex })
						.Where(k => k.keyState == true).Select(k => k.keyIndex).ToList();

				if (tmp.Count < s.Length)
				{
					kw = 0;
					return false;
				}

				var okKeys = s.Select(ss => Tuple.Create(extendedKeys(ss), false)).ToList();
				var result = true;
				foreach(var keyIndex in tmp)
				{
					var hit = false;
					for (var i = 0; i < okKeys.Count; i++)
					{
						hit = okKeys[i].Item1.Any(k => k == keyIndex);
						if (hit && okKeys[i].Item2 == false)
						{
							okKeys[i] = Tuple.Create(okKeys[i].Item1, true);
						}

						if (hit)
							break;
					}

					if (!hit)
					{
						result = false;
						break;
					}
				}

				// If all key sequence got hit on them
				result = result && okKeys.All(k => k.Item2);

				kw = result ? s.Length : 0;
				return result;
			}

			kw = 0;
			if(s == null || s.Length == 0)
				return true;

			foreach(var k in s)
			{
				kw++;
				if(!Keystate[k])
					return false;
			}
			
			return true;
		}

		/// <summary>
		/// Abort the internal timer for repeating key press
		/// </summary>
		private void AbortTimer()
		{
			if (_repeater != null)
			{
				var waiter = new AutoResetEvent(false);
				_repeater.Dispose(waiter);
				_repeater = null;
				waiter.WaitOne();
			}
		}

		/// <summary>
		/// Starts the internal timer for repeating key press
		/// </summary>
		private void StartTimer()
		{
			_repeater = new Timer(stateInfo => {
				HandleKeyHits();
			}, null, _repeateMs, _repeateMs);
		}

		/// <summary>
		/// Calls all listeners that matches current keypress
		/// </summary>
		/// <returns>true if any hit and all of the hits return true</returns>
		private bool HandleKeyHits()
		{
			var anyHit = false;
			var repeatKeys = true;
			foreach(var l in _listeners)
			{
				if (keysOk(l.Item1, true, out _))
				{
					anyHit = true;
					repeatKeys = l.Item3(l.Item1) && repeatKeys;
				}
			}

			return anyHit && repeatKeys;
		}

		private void HandleKeyDown(PipeMessageEx msg)
		{
			if(msg.wParam > MAX_KEYS)
			{
				Log.Warning("HandleKeyDown - wParam > MAX_KEYS (wParam: " + msg.wParam + ")");
				//Trace.WriteLine("HandleKeyDown - wParam > MAX_KEYS (wParam: " + msg.wParam + ")");
				return;
			}
			
			var pre = Keystate[msg.wParam];
			
			Keystate[msg.wParam] = true;


			if (pre != true)
			{
				AbortTimer();

				foreach(var l in _onKeyChangeListeners)
					l.Item2(msg.wParam, true);

				//var tmp = keyListener.Keystate.Select((keyVal, keyIndex) => new { key = keyVal, index = keyIndex }).Where(k => k.key).Select(k => k.index).ToList();
				//Trace.WriteLine($"Keydown: {msg.wParam} {msg.lParam} ({msg.lParam:x}) \"{Convert.ToString(msg.lParam, 2)}\" Pressed keys: {string.Join(", ", tmp)}");
				if (HandleKeyHits())
					StartTimer();
			}
		}
		
		private void HandleKeyUp(PipeMessageEx msg)
		{
			if(msg.wParam > MAX_KEYS)
			{
				Log.Error($"{nameof(KeyHandler)}.{nameof(HandleKeyUp)} wParam > MAX_KEYS (wParam: " + msg.wParam + ")");
				return;
			}
			
			var pre = Keystate[msg.wParam];
			Keystate[msg.wParam] = false;

			//if (pre != false)
			//	Trace.WriteLine($"keyup: {msg.wParam} {msg.lParam} ({msg.lParam:x}) \"{Convert.ToString(msg.lParam, 2)}\"");

			if(pre != false)
			{
				AbortTimer();

				foreach(var l in _onKeyChangeListeners)
					l.Item2(msg.wParam, false);
			}
		}

		/// <summary>
		/// Some keys can represent one of multiple keys, for example ctrl can represent both lctrl and rctrl
		/// so this method will return a list of keys that any of one should be a hit
		/// </summary>
		/// <param name="key">key to retrieve special extended key array for</param>
		/// <returns>array of possible hit for given key</returns>
		private ulong[] extendedKeys(ulong key)
		{
			if (key == 0x12) // alt
				return new ulong[] { 0x12, 0xa4, 0xa5 };
			if (key == 0x10) // shift
				return new ulong[] { 0x10, 0xa0, 0xa1 };
			if (key == 0x11) // ctrl
				return new ulong[] { 0x11, 0xa2, 0xa3 };

			if (key == 0xa4) // lalt
				return new ulong[] { 0x12, 0xa4 };
			if (key == 0xa0) // lshift
				return new ulong[] { 0x10, 0xa0 };
			if (key == 0xa2) // lctrl
				return new ulong[] { 0x11, 0xa2 };

			if (key == 0xa5) // ralt
				return new ulong[] { 0x12, 0xa5 };
			if (key == 0xa1) // rshift
				return new ulong[] { 0x10, 0xa1 };
			if (key == 0xa3) // rctrl
				return new ulong[] { 0x11, 0xa3 };
			
			return new[]{ key };
		}

        public void DumpDebug()
        {
            
        }

        public void Dispose()
        {
         }
    }
}