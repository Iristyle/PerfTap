namespace PerfTap.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Reflection;
	using System.Resources;
	using System.Runtime.InteropServices;
	using Microsoft.Win32;
	using PerfTap.Counter;

	internal class PdhHelper : IDisposable
	{
		private static readonly ResourceManager _resourceMgr = new ResourceManager("GetEventResources", Assembly.GetExecutingAssembly());
		private readonly Dictionary<string, CounterHandleNInstance> _consumerPathToHandleAndInstanceMap
			= new Dictionary<string, CounterHandleNInstance>();
		private bool _firstReading = true;
		private PdhSafeDataSourceHandle _hDataSource;
		private PdhSafeQueryHandle _hQuery;
		private readonly bool _isPreVista;

		// Methods
		public PdhHelper(bool isPreVista)
		{
			this._isPreVista = isPreVista;
		}

		public void AddCounters(IEnumerable<string> validPaths, bool bFlushOldCounters)
		{
			if (bFlushOldCounters)
			{
				this._consumerPathToHandleAndInstanceMap.Clear();
			}

			uint resultCode = (uint)PdhResults.PDH_CSTATUS_VALID_DATA;
			foreach (string str in validPaths)
			{
				IntPtr ptr;
				resultCode = Apis.PdhAddCounter(this._hQuery, str, IntPtr.Zero, out ptr);
				if (resultCode == PdhResults.PDH_CSTATUS_VALID_DATA)
				{
					CounterHandleNInstance instance = new CounterHandleNInstance() { hCounter = ptr, InstanceName = null };
					Structures.PDH_COUNTER_PATH_ELEMENTS pCounterPathElements = PdhHelper.ParsePath(str);

					if (pCounterPathElements.InstanceName != null)
					{
						instance.InstanceName = pCounterPathElements.InstanceName.ToLower(CultureInfo.InvariantCulture);
					}
					if (!this._consumerPathToHandleAndInstanceMap.ContainsKey(str.ToLower(CultureInfo.InvariantCulture)))
					{
						this._consumerPathToHandleAndInstanceMap.Add(str.ToLower(CultureInfo.InvariantCulture), instance);
					}
				}
			}
		}

		public static Exception BuildException(uint res)
		{
			string message;
			if (Win32Messages.FormatMessageFromModule(res, "pdh.dll", out message) != 0)
			{
				message = string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterApiError"), new object[] { res });
			}

			return new Exception(message);
		}

		public void ConnectToDataSource()
		{
			if ((this._hDataSource != null) && !this._hDataSource.IsInvalid)
			{
				this._hDataSource.Dispose();
			}

			uint returnCode = Apis.PdhBindInputDataSource(out this._hDataSource, null);
			if (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA)
			{
				throw BuildException(returnCode);
			}
		}

		public void Dispose()
		{
			if ((this._hDataSource != null) && !this._hDataSource.IsInvalid)
			{
				this._hDataSource.Dispose();
			}
			if ((this._hQuery != null) && !this._hQuery.IsInvalid)
			{
				this._hQuery.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		public string[] ExpandWildCardPath(string path)
		{
			IntPtr pcchPathListLength = new IntPtr(0);
			uint resultCode = Apis.PdhExpandWildCardPathH(this._hDataSource, path, IntPtr.Zero, ref pcchPathListLength, PdhWildcardPathFlags.None);
			if (resultCode == PdhResults.PDH_MORE_DATA)
			{
				IntPtr mszExpandedPathList = Marshal.AllocHGlobal(pcchPathListLength.ToInt32() * 2);
				try
				{
					resultCode = Apis.PdhExpandWildCardPathH(this._hDataSource, path, mszExpandedPathList, ref pcchPathListLength, PdhWildcardPathFlags.None);
					if (resultCode == PdhResults.PDH_CSTATUS_VALID_DATA)
					{
						return PdhHelper.ReadPdhMultiString(ref mszExpandedPathList, pcchPathListLength.ToInt32());
					}
				}
				finally
				{
					Marshal.FreeHGlobal(mszExpandedPathList);
				}
			}
			return null;
		}

		private static uint GetCounterInfoPlus(IntPtr hCounter, out uint counterType, out uint defaultScale, out ulong timeBase)
		{
			counterType = 0;
			defaultScale = 0;
			timeBase = 0L;

			IntPtr pdwBufferSize = new IntPtr(0);
			uint returnCode = Apis.PdhGetCounterInfo(hCounter, false, ref pdwBufferSize, IntPtr.Zero);

			if (returnCode != PdhResults.PDH_MORE_DATA)
			{
				return returnCode;
			}

			IntPtr lpBuffer = Marshal.AllocHGlobal(pdwBufferSize.ToInt32());
			try
			{
				if ((Apis.PdhGetCounterInfo(hCounter, false, ref pdwBufferSize, lpBuffer) == PdhResults.PDH_CSTATUS_VALID_DATA) && (lpBuffer != IntPtr.Zero))
				{
					counterType = (uint)Marshal.ReadInt32(lpBuffer, 4);
					defaultScale = (uint)Marshal.ReadInt32(lpBuffer, 20);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(lpBuffer);
			}
			return Apis.PdhGetCounterTimeBase(hCounter, out timeBase);
		}

		public bool IsPathValid(string path)
		{
			return this._isPreVista ? (Apis.PdhValidatePath(path) == Win32Results.ERROR_SUCCESS) :
				(Apis.PdhValidatePathEx(this._hDataSource, path) == Win32Results.ERROR_SUCCESS);
		}

		public static string LookupPerfNameByIndex(string machineName, uint index)
		{
			int pcchNameBufferSize = 256;
			IntPtr szNameBuffer = Marshal.AllocHGlobal(pcchNameBufferSize * 2);

			try
			{
				uint returnCode = Apis.PdhLookupPerfNameByIndex(machineName, index, szNameBuffer, ref pcchNameBufferSize);
				if (returnCode == PdhResults.PDH_MORE_DATA)
				{
					Marshal.FreeHGlobal(szNameBuffer);
					szNameBuffer = Marshal.AllocHGlobal(pcchNameBufferSize * 2);
					returnCode = Apis.PdhLookupPerfNameByIndex(machineName, index, szNameBuffer, ref pcchNameBufferSize);
				}

				if (returnCode == PdhResults.PDH_CSTATUS_VALID_DATA)
				{
					return Marshal.PtrToStringUni(szNameBuffer);
				}
								
				throw new Exception(string.Format(CultureInfo.CurrentCulture, _resourceMgr.GetString("CounterPathTranslationFailed"), returnCode));
			}
			finally
			{
				Marshal.FreeHGlobal(szNameBuffer);
			}
		}

		private static string MakePath(Structures.PDH_COUNTER_PATH_ELEMENTS pathElts, bool bWildcardInstances)
		{
			IntPtr pcchBufferSize = new IntPtr(0);
			if (bWildcardInstances)
			{
				pathElts.InstanceIndex = 0;
				pathElts.InstanceName = "*";
				pathElts.ParentInstance = null;
			}
			uint resultCode = Apis.PdhMakeCounterPath(ref pathElts, IntPtr.Zero, ref pcchBufferSize, PdhMakeCounterPathFlags.PDH_PATH_STANDARD_FORMAT);
			if (resultCode == PdhResults.PDH_MORE_DATA)
			{
				IntPtr szFullPathBuffer = Marshal.AllocHGlobal(pcchBufferSize.ToInt32() * 2);
				try
				{
					resultCode = Apis.PdhMakeCounterPath(ref pathElts, szFullPathBuffer, ref pcchBufferSize, PdhMakeCounterPathFlags.PDH_PATH_STANDARD_FORMAT);
					if (resultCode == PdhResults.PDH_CSTATUS_VALID_DATA)
					{
						return Marshal.PtrToStringUni(szFullPathBuffer);
					}

				}
				finally
				{
					Marshal.FreeHGlobal(szFullPathBuffer);
				}
			}

			throw new Exception(string.Format(CultureInfo.CurrentCulture, _resourceMgr.GetString("CounterPathTranslationFailed"), resultCode));
		}

		public void OpenQuery()
		{
			uint returnCode = Apis.PdhOpenQueryH(this._hDataSource, IntPtr.Zero, out this._hQuery);
			if (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA)
			{
				throw BuildException(returnCode);
			}
		}

		private static Structures.PDH_COUNTER_PATH_ELEMENTS ParsePath(string fullPath)
		{
			IntPtr pdwBufferSize = new IntPtr(0);
			uint returnCode = Apis.PdhParseCounterPath(fullPath, IntPtr.Zero, ref pdwBufferSize, 0);
			switch ((long)returnCode)
			{
				case PdhResults.PDH_MORE_DATA:
				case PdhResults.PDH_CSTATUS_VALID_DATA:
					{
						IntPtr ptr2 = Marshal.AllocHGlobal(pdwBufferSize.ToInt32());
						try
						{
							returnCode = Apis.PdhParseCounterPath(fullPath, ptr2, ref pdwBufferSize, 0);	//flags must always be zero
							if (returnCode == PdhResults.PDH_CSTATUS_VALID_DATA)
							{
								return (Structures.PDH_COUNTER_PATH_ELEMENTS)Marshal.PtrToStructure(ptr2, typeof(Structures.PDH_COUNTER_PATH_ELEMENTS));
							}
						}
						finally
						{
							Marshal.FreeHGlobal(ptr2);
						}
						break;
					}
			}

			throw new Exception(string.Format(CultureInfo.CurrentCulture, _resourceMgr.GetString("CounterPathTranslationFailed"), returnCode));
		}

		public uint ReadNextSet(out PerformanceCounterSampleSet nextSet, bool bSkipReading)
		{
			if (this._isPreVista)
			{
				return this.ReadNextSetPreVista(out nextSet, bSkipReading);
			}
			nextSet = null;
			long pllTimeStamp = 0;
			uint returnCode = Apis.PdhCollectQueryDataWithTime(this._hQuery, ref pllTimeStamp);
			if (bSkipReading)
			{
				return returnCode;
			}
			if ((returnCode != PdhResults.PDH_CSTATUS_VALID_DATA) && (returnCode != PdhResults.PDH_NO_DATA))
			{
				return returnCode;
			}

			DateTime now = (returnCode == PdhResults.PDH_NO_DATA) ? DateTime.Now :
				new DateTime(DateTime.FromFileTimeUtc(pllTimeStamp).Ticks, DateTimeKind.Local);

			PerformanceCounterSample[] counterSamples = new PerformanceCounterSample[this._consumerPathToHandleAndInstanceMap.Count];
			uint samplesRead = 0, validSamples = 0, lastErrorReturnCode = 0;

			foreach (string str in this._consumerPathToHandleAndInstanceMap.Keys)
			{
				Structures.PDH_RAW_COUNTER pdh_raw_counter;
				IntPtr lpdwType = new IntPtr(0);
				uint counterType,
					defaultScale = 0;
				ulong timeBase = 0L;
				IntPtr hCounter = this._consumerPathToHandleAndInstanceMap[str].hCounter;
				PdhHelper.GetCounterInfoPlus(hCounter, out counterType, out defaultScale, out timeBase);
				returnCode = Apis.PdhGetRawCounterValue(hCounter, out lpdwType, out pdh_raw_counter);
				if (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA)
				{
					counterSamples[samplesRead++] = new PerformanceCounterSample(str, this._consumerPathToHandleAndInstanceMap[str].InstanceName, 0.0, 0L, 0L, 0, PerformanceCounterType.RawBase, defaultScale, timeBase, now, (ulong)now.ToFileTime(), (pdh_raw_counter.CStatus == 0) ? returnCode : pdh_raw_counter.CStatus);
					validSamples++;
					lastErrorReturnCode = returnCode;
				}
				else
				{
					Structures.PDH_FMT_COUNTERVALUE_DOUBLE pdh_fmt_countervalue_double;
					long fileTime = (pdh_raw_counter.TimeStamp.dwHighDateTime << 0x20) + ((long)((ulong)pdh_raw_counter.TimeStamp.dwLowDateTime));
					DateTime timeStamp = new DateTime(DateTime.FromFileTimeUtc(fileTime).Ticks, DateTimeKind.Local);
					returnCode = Apis.PdhGetFormattedCounterValue(hCounter, PdhFormat.PDH_FMT_NOCAP100 + PdhFormat.PDH_FMT_DOUBLE, out lpdwType, out pdh_fmt_countervalue_double);
					if (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA)
					{
						counterSamples[samplesRead++] = new PerformanceCounterSample(str, this._consumerPathToHandleAndInstanceMap[str].InstanceName, 0.0, (ulong)pdh_raw_counter.FirstValue, (ulong)pdh_raw_counter.SecondValue, pdh_raw_counter.MultiCount, (PerformanceCounterType)counterType, defaultScale, timeBase, timeStamp, (ulong)fileTime, (pdh_fmt_countervalue_double.CStatus == 0) ? returnCode : pdh_raw_counter.CStatus);
						validSamples++;
						lastErrorReturnCode = returnCode;
						continue;
					}
					counterSamples[samplesRead++] = new PerformanceCounterSample(str, this._consumerPathToHandleAndInstanceMap[str].InstanceName, pdh_fmt_countervalue_double.doubleValue, (ulong)pdh_raw_counter.FirstValue, (ulong)pdh_raw_counter.SecondValue, pdh_raw_counter.MultiCount, (PerformanceCounterType)lpdwType.ToInt32(), defaultScale, timeBase, timeStamp, (ulong)fileTime, pdh_fmt_countervalue_double.CStatus);
				}
			}

			nextSet = new PerformanceCounterSampleSet(now, counterSamples, this._firstReading);
			this._firstReading = false;
			if (validSamples == counterSamples.Length)
			{
				return lastErrorReturnCode;
			}
			return (uint)PdhResults.PDH_CSTATUS_VALID_DATA;
		}

		public uint ReadNextSetPreVista(out PerformanceCounterSampleSet nextSet, bool bSkipReading)
		{
			nextSet = null;
			uint returnCode = Apis.PdhCollectQueryData(this._hQuery);
			if (bSkipReading)
			{
				return returnCode;
			}
			if ((returnCode != PdhResults.PDH_CSTATUS_VALID_DATA) && (returnCode != PdhResults.PDH_NO_DATA))
			{
				return returnCode;
			}
			PerformanceCounterSample[] counterSamples = new PerformanceCounterSample[this._consumerPathToHandleAndInstanceMap.Count];
			uint samplesRead = 0,
				validSamples = 0,
				lastErrorReturnCode = 0;

			DateTime now = DateTime.Now;

			foreach (string str in this._consumerPathToHandleAndInstanceMap.Keys)
			{
				Structures.PDH_RAW_COUNTER pdh_raw_counter;
				Structures.PDH_FMT_COUNTERVALUE_DOUBLE pdh_fmt_countervalue_double;
				IntPtr lpdwType = new IntPtr(0);
				uint counterType,
					defaultScale = 0;
				ulong timeBase = 0L;
				IntPtr hCounter = this._consumerPathToHandleAndInstanceMap[str].hCounter;
				PdhHelper.GetCounterInfoPlus(hCounter, out counterType, out defaultScale, out timeBase);
				returnCode = Apis.PdhGetRawCounterValue(hCounter, out lpdwType, out pdh_raw_counter);
				switch ((long)returnCode)
				{
					case PdhResults.PDH_INVALID_DATA:
					case PdhResults.PDH_NO_DATA:
						{
							counterSamples[samplesRead++] = new PerformanceCounterSample(str, this._consumerPathToHandleAndInstanceMap[str].InstanceName, 0.0, 0L, 0L, 0, PerformanceCounterType.RawBase, defaultScale, timeBase, DateTime.Now, (ulong)DateTime.Now.ToFileTime(), pdh_raw_counter.CStatus);
							validSamples++;
							lastErrorReturnCode = returnCode;
							continue;
						}
					case PdhResults.PDH_CSTATUS_VALID_DATA:
						break;

					default:
						return returnCode;
				}

				long fileTime = (pdh_raw_counter.TimeStamp.dwHighDateTime << 0x20) + ((long)((ulong)pdh_raw_counter.TimeStamp.dwLowDateTime));
				now = new DateTime(DateTime.FromFileTimeUtc(fileTime).Ticks, DateTimeKind.Local);
				returnCode = Apis.PdhGetFormattedCounterValue(hCounter, PdhFormat.PDH_FMT_NOCAP100 + PdhFormat.PDH_FMT_DOUBLE, out lpdwType, out pdh_fmt_countervalue_double);
				switch ((long)returnCode)
				{
					case PdhResults.PDH_INVALID_DATA:
					case PdhResults.PDH_NO_DATA:
						{
							counterSamples[samplesRead++] = new PerformanceCounterSample(str, this._consumerPathToHandleAndInstanceMap[str].InstanceName, 0.0, (ulong)pdh_raw_counter.FirstValue, (ulong)pdh_raw_counter.SecondValue, pdh_raw_counter.MultiCount, (PerformanceCounterType)counterType, defaultScale, timeBase, now, (ulong)fileTime, pdh_fmt_countervalue_double.CStatus);
							validSamples++;
							lastErrorReturnCode = returnCode;
							continue;
						}
					case PdhResults.PDH_CSTATUS_VALID_DATA:
						break;

					default:
						return returnCode;
				}
				counterSamples[samplesRead++] = new PerformanceCounterSample(str, this._consumerPathToHandleAndInstanceMap[str].InstanceName, pdh_fmt_countervalue_double.doubleValue, (ulong)pdh_raw_counter.FirstValue, (ulong)pdh_raw_counter.SecondValue, pdh_raw_counter.MultiCount, (PerformanceCounterType)lpdwType.ToInt32(), defaultScale, timeBase, now, (ulong)fileTime, pdh_fmt_countervalue_double.CStatus);
			}
			nextSet = new PerformanceCounterSampleSet(now, counterSamples, this._firstReading);
			this._firstReading = false;
			if (validSamples == counterSamples.Length)
			{
				return lastErrorReturnCode;
			}
			return (uint)PdhResults.PDH_CSTATUS_VALID_DATA;
		}

		private static string[] ReadPdhMultiString(ref IntPtr strNative, int strSize)
		{
			int bufferIndex = 0;
			string outputString = string.Empty;
			while (bufferIndex <= ((strSize * 2) - 4))
			{
				int characterCode = Marshal.ReadInt32(strNative, bufferIndex);
				if (characterCode == 0)
				{
					break;
				}
				outputString = outputString + ((char)characterCode);
				bufferIndex += 2;
			}

			return outputString.TrimEnd(new char[1])
				.Split(new char[1]);
		}

		public static string TranslateLocalCounterPath(string englishPath)
		{
			Structures.PDH_COUNTER_PATH_ELEMENTS pCounterPathElements = PdhHelper.ParsePath(englishPath);

			string counterName = pCounterPathElements.CounterName.ToLower(CultureInfo.InvariantCulture),
				objectName = pCounterPathElements.ObjectName.ToLower(CultureInfo.InvariantCulture);

			string[] counterNames = (string[])Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009").GetValue("Counter");
			int num2 = -1, num3 = -1;

			for (uint i = 1; i < counterNames.Length; i++)
			{
				string currentCounter = counterNames[i];
				if (currentCounter.ToLower(CultureInfo.InvariantCulture) == counterName)
				{
					num2 = Convert.ToInt32(counterNames[(int)((IntPtr)(i - 1))], CultureInfo.InvariantCulture);
					if ((num2 != -1) && (num3 != -1))
					{
						break;
					}

					continue;
				}
				if (currentCounter.ToLower(CultureInfo.InvariantCulture) == objectName)
				{
					num3 = Convert.ToInt32(counterNames[(int)((IntPtr)(i - 1))], CultureInfo.InvariantCulture);
				}

				if ((num2 != -1) && (num3 != -1))
				{
					break;
				}
			}

			if ((num2 == -1) || (num3 == -1))
			{
				throw new Exception(string.Format(CultureInfo.CurrentCulture, _resourceMgr.GetString("CounterPathTranslationFailed"), 0));
			}

			pCounterPathElements.ObjectName = PdhHelper.LookupPerfNameByIndex(pCounterPathElements.MachineName, (uint)num3);
			pCounterPathElements.CounterName = PdhHelper.LookupPerfNameByIndex(pCounterPathElements.MachineName, (uint)num2);

			return PdhHelper.MakePath(pCounterPathElements, false);
		}
	}
}