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
}
