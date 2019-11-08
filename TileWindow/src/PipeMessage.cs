using System;
using System.Runtime.InteropServices;

namespace TileWindow
{
		/// <summary>
		/// Description of SysMessage.
		/// </summary>
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
		public struct PipeMessage
		{
			public long msg;
			public ulong wParam;
			public long lParam;
		}

		public struct PipeMessageEx
		{
			public long msg;
			public ulong wParam;
			public long lParam;
			public string from;

			public PipeMessageEx(PipeMessage message, string fromName)
			{
				msg = message.msg;
				wParam = message.wParam;
				lParam = message.lParam;
				from = fromName;
			}
		}
}
