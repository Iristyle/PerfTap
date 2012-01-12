// -----------------------------------------------------------------------
// <copyright file="PerformanceCounterSamplesExtensionsTests.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Counter.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
using Xunit;
	using PerfTap;
	using System.Diagnostics;
	using Xunit.Extensions;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class PerformanceCounterSamplesExtensionsTests
	{
		[Theory]
		[InlineData("ju!nk")]
		[InlineData("^")]
		[InlineData("#")]
		[InlineData("$")]
		[InlineData(" foo")]
		[InlineData(";")]
		[InlineData("test:key")]
		[InlineData("key/check")]
		[InlineData("my.key")]
		[InlineData("my(key)")]
		[InlineData("this\\key\\happy")]
		[InlineData("%")]
		public void ToGraphiteString_ThrowsOnInvalidKey(string key)
		{
			var samples = new [] { new PerformanceCounterSample(@"\\machine-name\memory\% committed bytes in use", null, 36.41245914, 818220, 2247088, 1, PerformanceCounterType.RawFraction, 0, 3579545, DateTime.Now, (ulong)DateTime.Now.ToFileTime(), 0) };
			Assert.Throws<ArgumentException>(() => samples.ToGraphiteString(key).ToList());
		}

		public static IEnumerable<object[]> ExpectedMetricConversions
		{
			get
			{
				DateTime now = DateTime.Now;
				yield return new object[] { 
					"key",
					new PerformanceCounterSample(@"\\machine-name\memory\% committed bytes in use", null, 36.41245914, 818220, 2247088, 1, PerformanceCounterType.RawFraction, 0, 3579545, now, (ulong)now.ToFileTime(), 0), 
					String.Format(@"key.machine-name.memory.pct_committed_bytes_in_use:36.412|kv|@{0}", now.AsUnixTime()) };

				yield return new object[] { 
					null,
					new PerformanceCounterSample(@"\\machine-name\processor(0)\% processor time", "0", 1.6925116, 3559922343750, 129708662189268994, 1, PerformanceCounterType.Timer100NsInverse, 0, 10000000, now, (ulong)now.ToFileTime(), 0), 
					String.Format(@"machine-name.processor_0_.pct_processor_time:1.693|kv|@{0}", now.AsUnixTime()) };

				yield return new object[] { 
					"foo",
					new PerformanceCounterSample(@"\\machine-name\processor(_total)\% processor time", "_total", 1.6925116, 3559922343750, 129708662189268994, 1, PerformanceCounterType.Timer100NsInverse, 0, 10000000, now, (ulong)now.ToFileTime(), 0), 
					String.Format(@"foo.machine-name.processor__total_.pct_processor_time:1.693|kv|@{0}", now.AsUnixTime()) };

				yield return new object[] { 
					"junk",
					new PerformanceCounterSample(@"\\machine-name\System\Context Switches/sec", null, 3019.49343554114, 155783595, 149122840759, 1, PerformanceCounterType.RateOfCountsPerSecond32, 4294967294, 3579545, now, (ulong)now.ToFileTime(), 0), 
					String.Format(@"junk.machine-name.system.context_switches_sec:3019.493|kv|@{0}", now.AsUnixTime()) };

				yield return new object[] { 
					"super-key",
					new PerformanceCounterSample(@"\\machine-name\physicaldisk(_total)\avg. disk sec/write", "_total", 0.000599983144971405, 840816148, 181869, 1, PerformanceCounterType.AverageTimer32, 3, 3579545, now, (ulong)now.ToFileTime(), 0), 
					String.Format(@"super-key.machine-name.physicaldisk__total_.avg__disk_sec_write:0.001|ms", now.AsUnixTime()) };
			}
		}

		[Theory]
		[PropertyData("ExpectedMetricConversions")]
		public void ToGraphiteString_GeneratesExpectedMetrics(string key, PerformanceCounterSample sample, string expected)
		{			
			string converted = new [] { sample }.ToGraphiteString(key).First();
			Assert.Equal(expected, converted);
		}
	}
}
