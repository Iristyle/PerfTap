namespace PerfTap.Interop
{
	using System;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	public struct PDH_COUNTER_PATH_ELEMENTS
	{
		[MarshalAs(UnmanagedType.LPWStr)]
		public string MachineName;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ObjectName;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string InstanceName;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string ParentInstance;
		public uint InstanceIndex;
		[MarshalAs(UnmanagedType.LPWStr)]
		public string CounterName;
	}
}