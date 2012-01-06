namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Collections.ObjectModel;

	public class PerformanceCounterSampleSet
	{
		public PerformanceCounterSampleSet(DateTime timeStamp, PerformanceCounterSample[] counterSamples)
		{
			this.Timestamp = timeStamp;
			this.CounterSamples = new ReadOnlyCollection<PerformanceCounterSample>(counterSamples);
		}

		public ReadOnlyCollection<PerformanceCounterSample> CounterSamples { get; private set; }
		public DateTime Timestamp { get; private set; }
	}
}