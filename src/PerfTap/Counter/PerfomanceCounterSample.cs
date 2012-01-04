// -----------------------------------------------------------------------
// <copyright file="PerfomanceCounterSample.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;

	public class PerformanceCounterSample
	{
		internal PerformanceCounterSample()
		{
			this.Path = "";
			this.InstanceName = "";
			this.Timestamp = DateTime.MinValue;
		}

		internal PerformanceCounterSample(string path, string instanceName, double cookedValue, ulong rawValue, ulong secondValue, uint multiCount, PerformanceCounterType counterType, uint defaultScale, ulong timeBase, DateTime timeStamp, ulong timeStamp100nSec, uint status)
		{
			this.Path = "";
			this.InstanceName = "";
			this.Timestamp = DateTime.MinValue;
			this.Path = path;
			this.InstanceName = instanceName;
			this.CookedValue = cookedValue;
			this.RawValue = rawValue;
			this.SecondValue = secondValue;
			this.MultipleCount = multiCount;
			this.CounterType = counterType;
			this.DefaultScale = defaultScale;
			this.TimeBase = timeBase;
			this.Timestamp = timeStamp;
			this.Timestamp100NSec = timeStamp100nSec;
			this.Status = status;
		}

		public double CookedValue { get; set; }
		public PerformanceCounterType CounterType { get; set; }
		public uint DefaultScale { get; set; }
		public string InstanceName { get; set; }
		public uint MultipleCount { get; set; }
		public string Path { get; set; }
		public ulong RawValue { get; set; }
		public ulong SecondValue { get; set; }
		public uint Status { get; set; }
		public ulong TimeBase { get; set; }
		public DateTime Timestamp { get; set; }
		public ulong Timestamp100NSec { get; set; }
	}
}