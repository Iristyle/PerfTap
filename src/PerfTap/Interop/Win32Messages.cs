namespace PerfTap.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Text;

	internal static class Win32Messages
	{
		private const uint FORMAT_SUCCESS = 0;
		private const uint FORMAT_MESSAGE_FROM_HMODULE = 0x800;
		private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x1000;
		private const uint FORMAT_MESSAGE_IGNORE_INSERTS = 0x200;
		private const uint LOAD_LIBRARY_AS_DATAFILE = 2;

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint FormatMessage(uint dwFlags, IntPtr lpSource, uint dwMessageId, uint dwLanguageId, 
			[MarshalAs(UnmanagedType.LPWStr)] StringBuilder lpBuffer, uint nSize, IntPtr Arguments);
		[DllImport("kernel32.dll")]
		private static extern bool FreeLibrary(IntPtr hModule);
		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern ushort GetUserDefaultLangID();
		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern IntPtr LoadLibraryEx([MarshalAs(UnmanagedType.LPWStr)] string lpFileName, IntPtr hFile, uint dwFlags);

		public static uint FormatMessageFromModule(uint lastError, string moduleName, out string msg)
		{
			msg = string.Empty;
			IntPtr zero = LoadLibraryEx(moduleName, IntPtr.Zero, LOAD_LIBRARY_AS_DATAFILE);

			if (zero == IntPtr.Zero)
			{
				return (uint)Marshal.GetLastWin32Error();
			}

			try
			{
				uint userDefaultLangID = GetUserLangID();

				StringBuilder messageBuffer = new StringBuilder(1024);
				if (FormatMessage(FORMAT_MESSAGE_FROM_SYSTEM + FORMAT_MESSAGE_FROM_HMODULE + FORMAT_MESSAGE_IGNORE_INSERTS, zero,
				lastError, userDefaultLangID, messageBuffer, (uint)messageBuffer.Capacity, IntPtr.Zero) == FORMAT_SUCCESS)
				{
					return (uint)Marshal.GetLastWin32Error();
				}
				msg = messageBuffer.ToString();				
				if (msg.EndsWith(Environment.NewLine, StringComparison.Ordinal))
				{
					msg = msg.Substring(0, msg.Length - 2);
				}
			}
			finally
			{
				FreeLibrary(zero);
			}
			return FORMAT_SUCCESS;
		}

		public static uint GetUserLangID()
		{
			uint userDefaultLangID = GetUserDefaultLangID();

			return (Marshal.GetLastWin32Error() != 0) ? 0 : userDefaultLangID;
		}
	}
}