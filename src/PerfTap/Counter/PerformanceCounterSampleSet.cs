namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;
	using System.Reflection;
	using System.Resources;

	public class PerformanceCounterSampleSet
	{
		// Fields
		private ResourceManager _resourceMgr;

		// Methods
		internal PerformanceCounterSampleSet()
		{
			this.Timestamp = DateTime.MinValue;
			this._resourceMgr = new ResourceManager("GetEventResources", Assembly.GetExecutingAssembly());
		}

		internal PerformanceCounterSampleSet(DateTime timeStamp, PerformanceCounterSample[] counterSamples, bool firstSet)
			: this()
		{
			this.Timestamp = timeStamp;
			this.CounterSamples = counterSamples;
		}

		// Properties
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "Microsoft.PowerShell.Commands.GetCounter.PerformanceCounterSample.CounterSamples", Justification = "A string[] is required here because that is the type Powershell supports")]
		public PerformanceCounterSample[] CounterSamples { get; set; }
		public DateTime Timestamp { get; set; }
	}
}