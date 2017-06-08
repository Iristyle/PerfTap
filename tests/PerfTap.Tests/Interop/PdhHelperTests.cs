// -----------------------------------------------------------------------
// <copyright file="PdhHelper.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Interop.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Xunit;
	using PerfTap.Counter;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class PdhHelperTests
	{
		[Fact]
		public void Constructor_DoesNotThrow()
		{
			//Assert.DoesNotThrow(() => new PdhHelper(PerfmonCounterReader.DefaultCounters));
		}

		[Fact]
		public void Constructor_WithComputersOverload_DoesNotThrow()
		{
			//Assert.DoesNotThrow(() => new PdhHelper(new string[] { Environment.MachineName}, PerfmonCounterReader.DefaultCounters, true));
		}

		[Fact]
		public void ReadNextSet_DefaultCounters_RetrievesAll()
		{			
			using (var pdhHelper = new PdhHelper(PerfmonCounterReader.DefaultCounters))
			{
				var counters = pdhHelper.ReadNextSet();
				Assert.True(counters.CounterSamples.Count >= 6);
			}
		}

		[Fact]
		public void ReadNextSet_WithComputersOverload_DefaultCounters_RetrievesAll()
		{
			using (var pdhHelper = new PdhHelper(new string[] { Environment.MachineName }, PerfmonCounterReader.DefaultCounters, true))
			{
				var counters = pdhHelper.ReadNextSet();
				Assert.True(counters.CounterSamples.Count >= 6);
			}
		}

		[Fact]
		public void ReadNextSet_DefaultCounters_DataHasApproximatelyCorrectTimestamps()
		{
			using (var pdhHelper = new PdhHelper(PerfmonCounterReader.DefaultCounters))
			{
				DateTime now = DateTime.Now;
				var counters = pdhHelper.ReadNextSet();
				Assert.True(counters.CounterSamples.All(sample => 
					Math.Abs((sample.Timestamp - now).TotalSeconds) <= 4));
			}
		}
	}
}