namespace PerfTap.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.InteropServices;
	using Microsoft.Win32;
	using NLog;
	using PerfTap.Counter;

	public class PdhHelper
		: IDisposable
	{
		private struct CounterHandleNInstance
		{
			public IntPtr CounterHandle { get; set; }
			public string InstanceName { get; set; }
		}

		private struct RawCounterSample
		{
			public PerformanceCounterSample PerformanceCounterSample { get; set; }
			public PDH_RAW_COUNTER RawCounter { get; set; }
		}

		private static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly Dictionary<string, CounterHandleNInstance> _consumerPathToHandleAndInstanceMap
			= new Dictionary<string, CounterHandleNInstance>();
		private PdhSafeDataSourceHandle _safeDataSourceHandle;
		private PdhSafeQueryHandle _safeQueryHandle;
		private readonly bool _isPreVista;
		private bool _isLastSampleBad;
		private readonly bool _ignoreBadStatusCodes = true;
		private readonly Lazy<bool> _isQueryOpen = new Lazy<bool>();

		public PdhHelper(IEnumerable<string> counters)
			: this(Environment.OSVersion.Version.Major < 6, new string[0], counters, true)
		{ }

		public PdhHelper(IEnumerable<string> computerNames, IEnumerable<string> counters, bool ignoreBadStatusCodes)
			: this(Environment.OSVersion.Version.Major < 6, computerNames, counters, ignoreBadStatusCodes)
		{ }

		//TODO: keeping this *only* for testing purposes -- but not sure that even makes sense
		internal PdhHelper(bool isPreVista, IEnumerable<string> computerNames, IEnumerable<string> counters, bool ignoreBadStatusCodes)
		{
			this._isPreVista = isPreVista;
			this._ignoreBadStatusCodes = ignoreBadStatusCodes;
			ConnectToDataSource();

			List<string> validPaths = ParsePaths(counters.PrefixWithComputerNames(computerNames)).ToList();

			if (validPaths.Count == 0)
				throw new Exception(string.Format(CultureInfo.CurrentCulture, GetEventResources.CounterPathIsInvalid, new object[] { string.Empty }));

			OpenQuery();
			AddCounters(validPaths);
		}

		private IEnumerable<string> ParsePaths(IEnumerable<string> allCounterPaths)
		{
			foreach (string counterPath in allCounterPaths)
			{
				var expandedPaths = ExpandWildCardPath(counterPath);
				if (null == expandedPaths)
				{
					_log.Debug(() => string.Format("Could not expand path {0}", counterPath));
					continue;
				}

				foreach (string expandedPath in expandedPaths)
				{
					if (!IsPathValid(expandedPath))
					{
						throw new Exception(string.Format(CultureInfo.CurrentCulture, GetEventResources.CounterPathIsInvalid, new object[] { counterPath }));
					}

					yield return expandedPath;
				}
			}
		}

		private void ConnectToDataSource()
		{
			if ((this._safeDataSourceHandle != null) && !this._safeDataSourceHandle.IsInvalid)
			{
				this._safeDataSourceHandle.Dispose();
			}

			uint returnCode = Apis.PdhBindInputDataSource(out this._safeDataSourceHandle, null);
			if (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA)
			{
				throw BuildException(returnCode);
			}
		}

		private void AddCounters(IEnumerable<string> validPaths)
		{
			uint resultCode = (uint)PdhResults.PDH_CSTATUS_VALID_DATA;
			foreach (string validPath in validPaths)
			{
				IntPtr counterPointer;
				resultCode = Apis.PdhAddCounter(this._safeQueryHandle, validPath, IntPtr.Zero, out counterPointer);
				if (resultCode == PdhResults.PDH_CSTATUS_VALID_DATA)
				{
					CounterHandleNInstance instance = new CounterHandleNInstance() { CounterHandle = counterPointer, InstanceName = null };
					PDH_COUNTER_PATH_ELEMENTS pCounterPathElements = PdhHelper.ParsePath(validPath);

					if (pCounterPathElements.InstanceName != null)
					{
						instance.InstanceName = pCounterPathElements.InstanceName.ToLower(CultureInfo.InvariantCulture);
					}
					if (!this._consumerPathToHandleAndInstanceMap.ContainsKey(validPath.ToLower(CultureInfo.InvariantCulture)))
					{
						this._consumerPathToHandleAndInstanceMap.Add(validPath.ToLower(CultureInfo.InvariantCulture), instance);
					}
				}
			}
		}

		private static Exception BuildException(uint failedReturnCode)
		{
			string message = Win32Messages.FormatMessageFromModule(failedReturnCode, "pdh.dll");
			if (string.IsNullOrEmpty(message))
			{
				message = string.Format(CultureInfo.InvariantCulture, GetEventResources.CounterApiError, new object[] { failedReturnCode });
			}

			return new Exception(message);
		}

		public void Dispose()
		{
			if ((this._safeDataSourceHandle != null) && !this._safeDataSourceHandle.IsInvalid)
			{
				this._safeDataSourceHandle.Dispose();
			}
			if ((this._safeQueryHandle != null) && !this._safeQueryHandle.IsInvalid)
			{
				this._safeQueryHandle.Dispose();
			}
			GC.SuppressFinalize(this);
		}

		private string[] ExpandWildCardPath(string path)
		{
			IntPtr pathListLength = new IntPtr(0);
			uint resultCode = Apis.PdhExpandWildCardPathH(this._safeDataSourceHandle, path, IntPtr.Zero, ref pathListLength, PdhWildcardPathFlags.None);
			if (resultCode == PdhResults.PDH_MORE_DATA)
			{
				IntPtr expandedPathList = Marshal.AllocHGlobal(pathListLength.ToInt32() * 2);
				try
				{
					resultCode = Apis.PdhExpandWildCardPathH(this._safeDataSourceHandle, path, expandedPathList, ref pathListLength, PdhWildcardPathFlags.None);
					if (resultCode == PdhResults.PDH_CSTATUS_VALID_DATA)
					{
						return PdhHelper.ParsePdhMultiStringBuffer(ref expandedPathList, pathListLength.ToInt32());
					}
				}
				finally
				{
					Marshal.FreeHGlobal(expandedPathList);
				}
			}
			return null;
		}

		private static CounterInfo GetCounterInfo(IntPtr counterHandle)
		{
			CounterInfo info = new CounterInfo();

			IntPtr counterBufferSize = new IntPtr(0);
			uint returnCode = Apis.PdhGetCounterInfo(counterHandle, false, ref counterBufferSize, IntPtr.Zero);

			if (returnCode != PdhResults.PDH_MORE_DATA)
			{
				return info;
			}

			IntPtr bufferPointer = Marshal.AllocHGlobal(counterBufferSize.ToInt32());
			try
			{
				if ((Apis.PdhGetCounterInfo(counterHandle, false, ref counterBufferSize, bufferPointer) == PdhResults.PDH_CSTATUS_VALID_DATA) && (bufferPointer != IntPtr.Zero))
				{
					info.Type = (uint)Marshal.ReadInt32(bufferPointer, 4);
					info.DefaultScale = (uint)Marshal.ReadInt32(bufferPointer, 20);
				}
			}
			finally
			{
				Marshal.FreeHGlobal(bufferPointer);
			}

			ulong timeBase;
			Apis.PdhGetCounterTimeBase(counterHandle, out timeBase);
			info.TimeBase = timeBase;

			return info;
		}

		private bool IsPathValid(string path)
		{
			return this._isPreVista ? (Apis.PdhValidatePath(path) == Win32Results.ERROR_SUCCESS) :
				(Apis.PdhValidatePathEx(this._safeDataSourceHandle, path) == Win32Results.ERROR_SUCCESS);
		}

		public static string LookupPerfNameByIndex(string machineName, uint index)
		{
			if (index < 0)
			{
				throw new Exception(string.Format(CultureInfo.CurrentCulture, GetEventResources.CounterPathTranslationFailed, 0));
			}

			int perfNameBufferSize = 256;
			IntPtr perfNamePointer = Marshal.AllocHGlobal(perfNameBufferSize * 2);

			try
			{
				uint returnCode = Apis.PdhLookupPerfNameByIndex(machineName, index, perfNamePointer, ref perfNameBufferSize);
				if (returnCode == PdhResults.PDH_MORE_DATA)
				{
					Marshal.FreeHGlobal(perfNamePointer);
					perfNamePointer = Marshal.AllocHGlobal(perfNameBufferSize * 2);
					returnCode = Apis.PdhLookupPerfNameByIndex(machineName, index, perfNamePointer, ref perfNameBufferSize);
				}

				if (returnCode == PdhResults.PDH_CSTATUS_VALID_DATA)
				{
					return Marshal.PtrToStringUni(perfNamePointer);
				}

				throw new Exception(string.Format(CultureInfo.CurrentCulture, GetEventResources.CounterPathTranslationFailed, returnCode));
			}
			finally
			{
				Marshal.FreeHGlobal(perfNamePointer);
			}
		}

		private static string MakePath(PDH_COUNTER_PATH_ELEMENTS pathElements, bool isWildcard)
		{
			IntPtr sizeOfCounterPath = new IntPtr(0);
			if (isWildcard)
			{
				pathElements.InstanceIndex = 0;
				pathElements.InstanceName = "*";
				pathElements.ParentInstance = null;
			}
			uint resultCode = Apis.PdhMakeCounterPath(ref pathElements, IntPtr.Zero, ref sizeOfCounterPath, PdhMakeCounterPathFlags.PDH_PATH_STANDARD_FORMAT);
			if (resultCode == PdhResults.PDH_MORE_DATA)
			{
				IntPtr fullPathBufferPointer = Marshal.AllocHGlobal(sizeOfCounterPath.ToInt32() * 2);
				try
				{
					resultCode = Apis.PdhMakeCounterPath(ref pathElements, fullPathBufferPointer, ref sizeOfCounterPath, PdhMakeCounterPathFlags.PDH_PATH_STANDARD_FORMAT);
					if (resultCode == PdhResults.PDH_CSTATUS_VALID_DATA)
					{
						return Marshal.PtrToStringUni(fullPathBufferPointer);
					}

				}
				finally
				{
					Marshal.FreeHGlobal(fullPathBufferPointer);
				}
			}

			throw new Exception(string.Format(CultureInfo.CurrentCulture, GetEventResources.CounterPathTranslationFailed, resultCode));
		}

		private static PDH_COUNTER_PATH_ELEMENTS ParsePath(string fullPath)
		{
			IntPtr bufferSize = new IntPtr(0);
			uint returnCode = Apis.PdhParseCounterPath(fullPath, IntPtr.Zero, ref bufferSize, 0);
			if (returnCode == PdhResults.PDH_MORE_DATA || returnCode == PdhResults.PDH_CSTATUS_VALID_DATA)
			{
				IntPtr counterPathBuffer = Marshal.AllocHGlobal(bufferSize.ToInt32());
				try
				{
					returnCode = Apis.PdhParseCounterPath(fullPath, counterPathBuffer, ref bufferSize, 0);	//flags must always be zero
					if (returnCode == PdhResults.PDH_CSTATUS_VALID_DATA)
					{
						return (PDH_COUNTER_PATH_ELEMENTS)Marshal.PtrToStructure(counterPathBuffer, typeof(PDH_COUNTER_PATH_ELEMENTS));
					}
				}
				finally
				{
					Marshal.FreeHGlobal(counterPathBuffer);
				}
			}

			throw new Exception(string.Format(CultureInfo.CurrentCulture, GetEventResources.CounterPathTranslationFailed, returnCode));
		}

		private void OpenQuery()
		{
			uint returnCode;
			if (!_isQueryOpen.IsValueCreated)
			{
				returnCode = Apis.PdhOpenQueryH(this._safeDataSourceHandle, IntPtr.Zero, out this._safeQueryHandle);
				if (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA)
				{
					throw BuildException(returnCode);
				}
			}
		}

		public PerformanceCounterSampleSet ReadNextSet()
		{
			OpenQuery();

			long fileTimeStamp = 0;
			uint returnCode = this._isPreVista ? Apis.PdhCollectQueryData(this._safeQueryHandle)
				: Apis.PdhCollectQueryDataWithTime(this._safeQueryHandle, ref fileTimeStamp);

			if (this._isLastSampleBad)
			{
				return null;
			}
			if ((returnCode != PdhResults.PDH_CSTATUS_VALID_DATA) && (returnCode != PdhResults.PDH_NO_DATA))
			{
				//this makes sure next call to ReadNextSet doesn't examine the data, and just returns null
				this._isLastSampleBad = true;
				return null;
			}

			DateTime now = (_isPreVista || returnCode == PdhResults.PDH_NO_DATA) ? DateTime.Now :
				new DateTime(DateTime.FromFileTimeUtc(fileTimeStamp).Ticks, DateTimeKind.Local);

			PerformanceCounterSample[] counterSamples = new PerformanceCounterSample[this._consumerPathToHandleAndInstanceMap.Count];
			int samplesRead = 0;

			foreach (string key in this._consumerPathToHandleAndInstanceMap.Keys)
			{
				IntPtr counterHandle = this._consumerPathToHandleAndInstanceMap[key].CounterHandle;
				CounterInfo info = PdhHelper.GetCounterInfo(counterHandle);

				var sample = GetRawCounterSample(counterHandle, key, info, now);
				var performanceSample = sample.PerformanceCounterSample ?? GetFormattedCounterSample(counterHandle, key, info, sample.RawCounter);

				if (!this._ignoreBadStatusCodes && (performanceSample.Status != 0))
				{
					throw BuildException(performanceSample.Status);
				}

				if (performanceSample.Status != 0)
				{
					_log.Info(() => string.Format("Status {0:x} ignored for counter {1}", performanceSample.Status, key));
					continue;
				}
				counterSamples[samplesRead++] = performanceSample;
			}

			this._isLastSampleBad = false;
			//in the event we skipped bad data
			if (samplesRead < counterSamples.Length)
			{
				Array.Resize(ref counterSamples, samplesRead);	
			}
			return new PerformanceCounterSampleSet(this._isPreVista ? counterSamples[samplesRead].Timestamp : now, counterSamples);
		}

		private RawCounterSample GetRawCounterSample(IntPtr counterHandle, string key, CounterInfo info, DateTime? now)
		{
			IntPtr counterType = new IntPtr(0);
			PDH_RAW_COUNTER rawCounter;
			uint returnCode = Apis.PdhGetRawCounterValue(counterHandle, out counterType, out rawCounter);
			//vista and above, any non-valid result is returned like this
			if ((!_isPreVista && (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA)) ||
				//below vista, return data for no_data and invalid_data, otherwise throw 
				(_isPreVista && (returnCode == PdhResults.PDH_INVALID_DATA || returnCode == PdhResults.PDH_NO_DATA)))
			{
				DateTime timeStamp = this._isPreVista ? DateTime.Now : now.Value;
				return new RawCounterSample()
				{
					PerformanceCounterSample = new PerformanceCounterSample(key, this._consumerPathToHandleAndInstanceMap[key].InstanceName,
					0.0, 0L, 0L, 0,
						//TODO: original code just uses pdh_raw_counter.CStatus when _isPreVista
						//TODO: not sure if its useful to stuff a bad returnCode in for status, as VerifySamples will throw when status isn't 0... not sure that's useful
					PerformanceCounterType.RawBase, info.DefaultScale, info.TimeBase, timeStamp, (ulong)timeStamp.ToFileTime(), (rawCounter.CStatus == 0) ? returnCode : rawCounter.CStatus)
				};
			}
			else if (_isPreVista && (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA))
			{
				throw BuildException(returnCode);
			}

			//this is only used when return code is 0 -- PdhResults.PDH_CSTATUS_VALID_DATA
			return new RawCounterSample() { RawCounter = rawCounter };
		}

		private PerformanceCounterSample GetFormattedCounterSample(IntPtr counterHandle, string name, CounterInfo info, PDH_RAW_COUNTER rawCounter)
		{
			IntPtr counterType = new IntPtr(0);
			long fileTime = (rawCounter.TimeStamp.dwHighDateTime << 0x20) + ((long)((ulong)rawCounter.TimeStamp.dwLowDateTime));
			DateTime timeStamp2 = new DateTime(DateTime.FromFileTimeUtc(fileTime).Ticks, DateTimeKind.Local);
			PDH_FMT_COUNTERVALUE_DOUBLE doubleFormattedCounter;
			uint returnCode = Apis.PdhGetFormattedCounterValue(counterHandle, PdhFormat.PDH_FMT_NOCAP100 + PdhFormat.PDH_FMT_DOUBLE, out counterType, out doubleFormattedCounter);

			//vista and above, any non-valid result is returned like this			
			if ((!_isPreVista && (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA)) ||
				//below vista, return data for no_data and invalid_data, otherwise throw 
				(_isPreVista && (returnCode == PdhResults.PDH_INVALID_DATA || returnCode == PdhResults.PDH_NO_DATA)))
			{
				return new PerformanceCounterSample(name, this._consumerPathToHandleAndInstanceMap[name].InstanceName,
					0.0, (ulong)rawCounter.FirstValue, (ulong)rawCounter.SecondValue, rawCounter.MultiCount,
					//TODO: not sure if its useful to stuff a bad returnCode in for status, as VerifySamples will throw when status isn't 0... not sure that's useful
					//TODO: original code just uses pdh_raw_counter.CStatus when _isPreVista
					(PerformanceCounterType)info.Type, info.DefaultScale, info.TimeBase, timeStamp2, (ulong)fileTime, (doubleFormattedCounter.CStatus == 0) ? returnCode : rawCounter.CStatus);
			}
			else if (_isPreVista && (returnCode != PdhResults.PDH_CSTATUS_VALID_DATA))
			{
				throw BuildException(returnCode);
			}

			return new PerformanceCounterSample(name, this._consumerPathToHandleAndInstanceMap[name].InstanceName,
				doubleFormattedCounter.doubleValue, (ulong)rawCounter.FirstValue, (ulong)rawCounter.SecondValue, rawCounter.MultiCount,
				(PerformanceCounterType)counterType.ToInt32(), info.DefaultScale, info.TimeBase, timeStamp2, (ulong)fileTime, doubleFormattedCounter.CStatus);
		}

		private static string[] ParsePdhMultiStringBuffer(ref IntPtr nativeStringPointer, int strSize)
		{
			int bufferIndex = 0;
			string outputString = string.Empty;
			while (bufferIndex <= ((strSize * 2) - 4))
			{
				int characterCode = Marshal.ReadInt32(nativeStringPointer, bufferIndex);
				if (characterCode == 0)
				{
					break;
				}
				outputString += ((char)characterCode);
				bufferIndex += 2;
			}

			return outputString.TrimEnd(new char[1])
				.Split(new char[1]);
		}

		public static string TranslateLocalCounterPath(string englishPath)
		{
			PDH_COUNTER_PATH_ELEMENTS counterPathElements = PdhHelper.ParsePath(englishPath);

			string counterName = counterPathElements.CounterName.ToLower(CultureInfo.InvariantCulture),
				objectName = counterPathElements.ObjectName.ToLower(CultureInfo.InvariantCulture);

			string[] counterNames = (string[])Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Perflib\009").GetValue("Counter");

			int counterIndex = -1, objectIndex = -1;

			for (int i = 1; i < counterNames.Length; i++)
			{
				string currentCounter = counterNames[i];
				if (currentCounter.ToLower(CultureInfo.InvariantCulture) == counterName)
				{
					counterIndex = Convert.ToInt32(counterNames[i - 1], CultureInfo.InvariantCulture);
					if ((counterIndex != -1) && (objectIndex != -1))
					{
						break;
					}

					continue;
				}
				if (currentCounter.ToLower(CultureInfo.InvariantCulture) == objectName)
				{
					objectIndex = Convert.ToInt32(counterNames[i - 1], CultureInfo.InvariantCulture);
				}

				if ((counterIndex != -1) && (objectIndex != -1))
				{
					break;
				}
			}

			counterPathElements.ObjectName = PdhHelper.LookupPerfNameByIndex(counterPathElements.MachineName, (uint)objectIndex);
			counterPathElements.CounterName = PdhHelper.LookupPerfNameByIndex(counterPathElements.MachineName, (uint)counterIndex);

			return PdhHelper.MakePath(counterPathElements, false);
		}
	}
}