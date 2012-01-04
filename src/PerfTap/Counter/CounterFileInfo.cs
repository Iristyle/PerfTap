namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public class CounterFileInfo
	{
		internal CounterFileInfo()
		{
			this.OldestRecord = DateTime.MinValue;
			this.NewestRecord = DateTime.MaxValue;
		}

		internal CounterFileInfo(DateTime oldestRecord, DateTime newestRecord, uint sampleCount)
		{
			this.OldestRecord = DateTime.MinValue;
			this.NewestRecord = DateTime.MaxValue;
			this.OldestRecord = oldestRecord;
			this.NewestRecord = newestRecord;
			this.SampleCount = sampleCount;
		}

		public DateTime NewestRecord { get; private set; }
		public DateTime OldestRecord { get; private set; }
		public uint SampleCount { get; private set; }
	}
}