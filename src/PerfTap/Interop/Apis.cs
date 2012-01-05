namespace PerfTap.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;

	internal class Apis
	{
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhAddCounter(PdhSafeQueryHandle queryHandle, string counterPath, IntPtr userData, out IntPtr counterHandle);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhBindInputDataSource(out PdhSafeDataSourceHandle phDataSource, string szLogFileNameList);
		[DllImport("pdh.dll")]
		internal static extern uint PdhCloseLog(IntPtr logHandle, uint dwFlags);
		[DllImport("pdh.dll")]
		internal static extern uint PdhCloseQuery(IntPtr queryHandle);
		[DllImport("pdh.dll")]
		public static extern uint PdhCollectQueryData(PdhSafeQueryHandle queryHandle);
		[DllImport("pdh.dll")]
		public static extern uint PdhCollectQueryDataWithTime(PdhSafeQueryHandle queryHandle, ref long pllTimeStamp);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhExpandWildCardPathH(PdhSafeDataSourceHandle hDataSource, string szWildCardPath, IntPtr mszExpandedPathList, ref IntPtr pcchPathListLength, uint dwFlags);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhGetCounterInfo(IntPtr hCounter, [MarshalAs(UnmanagedType.U1)] bool bRetrieveExplainText, ref IntPtr pdwBufferSize, IntPtr lpBuffer);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhGetCounterTimeBase(IntPtr hCounter, out ulong pTimeBase);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhGetFormattedCounterValue(IntPtr counterHandle, uint dwFormat, out IntPtr lpdwType, out PDH_FMT_COUNTERVALUE_DOUBLE pValue);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhGetRawCounterValue(IntPtr hCounter, out IntPtr lpdwType, out PDH_RAW_COUNTER pValue);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhLookupPerfNameByIndex(string szMachineName, uint dwNameIndex, IntPtr szNameBuffer, ref int pcchNameBufferSize);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhMakeCounterPath(ref PDH_COUNTER_PATH_ELEMENTS pCounterPathElements, IntPtr szFullPathBuffer, ref IntPtr pcchBufferSize, uint dwFlags);
		[DllImport("pdh.dll")]
		public static extern uint PdhOpenQueryH(PdhSafeDataSourceHandle hDataSource, IntPtr dwUserData, out PdhSafeQueryHandle phQuery);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhParseCounterPath(string szFullPathBuffer, IntPtr pCounterPathElements, ref IntPtr pdwBufferSize, uint dwFlags);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhValidatePath(string szFullPathBuffer);
		[DllImport("pdh.dll", CharSet = CharSet.Unicode)]
		public static extern uint PdhValidatePathEx(PdhSafeDataSourceHandle hDataSource, string szFullPathBuffer);
	}
}