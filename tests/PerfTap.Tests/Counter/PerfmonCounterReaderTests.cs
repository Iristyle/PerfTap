// -----------------------------------------------------------------------
// <copyright file="PerfmonCounterReaderTests.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Counter.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using Xunit;
	using Xunit.Extensions;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class PerfmonCounterReaderTests
	{
		[Fact]
		public void GetCounterSamples_IsCancellable()
		{
			var reader = new PerfmonCounterReader();
			using (var cancellationTokenSource = new CancellationTokenSource())
			{
				List<PerformanceCounterSampleSet> sampleSets = new List<PerformanceCounterSampleSet>();
				foreach (var sampleSet in reader.GetCounterSamples(TimeSpan.FromSeconds(5), 20, cancellationTokenSource.Token))
				{
					sampleSets.Add(sampleSet);
					cancellationTokenSource.Cancel();
				}
				Assert.Single(sampleSets);
			}
		}

		[Fact]
		public void GetCounterSamples_ReturnsExpectedSampleCount()
		{
			var reader = new PerfmonCounterReader();
			using (var cancellationTokenSource = new CancellationTokenSource())
			{
				Assert.Single(reader.GetCounterSamples(new[] { @"\processor(_total)\% processor time" }, TimeSpan.FromSeconds(1), 1, cancellationTokenSource.Token)
					.SelectMany(set => set.CounterSamples));
			}
		}

		[Theory]
		[InlineData(@"\processor(_total)\% processor time", PerformanceCounterType.Timer100NsInverse, 1, 100, "_total", 10000000UL)]
		[InlineData(@"\System\Context Switches/sec", PerformanceCounterType.RateOfCountsPerSecond32, 1000, 10000, null, 3579545UL)]
		public void GetCounterSamples_ProcessorTime_HasReasonableValues(string counter, PerformanceCounterType counterType, double minValue, double maxValue, string instanceName, ulong timeBase)
		{
			var reader = new PerfmonCounterReader();
			using (var cancellationTokenSource = new CancellationTokenSource())
			{
				var sample = reader.GetCounterSamples(new[] { counter }, TimeSpan.FromSeconds(1), 1, cancellationTokenSource.Token)
					.SelectMany(set => set.CounterSamples)
					.First();

				//TODO: this is bad form - replace with an object comparer 
				Assert.Equal(counterType, sample.CounterType);
				Assert.InRange(sample.CookedValue, minValue, maxValue);
				Assert.Equal(instanceName, sample.InstanceName);
				Assert.Equal(timeBase, sample.TimeBase);
				Assert.True(sample.Path.ToLower().EndsWith(counter.ToLower()));
			}
		}
	}
}
