namespace PerfTap.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;

	internal class Structures
	{
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

		[StructLayout(LayoutKind.Sequential)]
		public struct PDH_FMT_COUNTERVALUE_DOUBLE
		{
			public uint CStatus;
			public double doubleValue;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PDH_RAW_COUNTER
		{
			public uint CStatus;
			public System.Runtime.InteropServices.ComTypes.FILETIME TimeStamp;
			public long FirstValue;
			public long SecondValue;
			public uint MultiCount;
		}
	}
}