// -----------------------------------------------------------------------
// <copyright file="PerfmonConverter.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Text;
	using System.Text.RegularExpressions;
	using NanoTube.Support;
	using NanoTube.Core;
	using NanoTube;
	using PerfTap.Counter;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class PerformanceCounterSamplesExtensions
	{
		private const string _keyValue = "kv",
			_timer = "ms",
			_performanceCounter = "c",
			_separator = "\n",
			_badChars = " ;:/.()*";

		public static IEnumerable<IMetric> ToMetrics(this IEnumerable<PerformanceCounterSample> performanceCounters, bool addInstance)
		{
			if (null == performanceCounters)
			{ throw new ArgumentNullException("performanceCounter"); }


			var metricName = new StringBuilder(150);
			IMetric metric;
			foreach (var counter in performanceCounters)
			{
				//http://msdn.microsoft.com/en-us/library/system.diagnostics.performancecountertype(v=VS.85).aspx
				switch (counter.CounterType)
				{
					//#these types of counters are not usable
					case PerformanceCounterType.AverageBase:
					case PerformanceCounterType.CounterMultiBase:
					case PerformanceCounterType.RawBase:
					case PerformanceCounterType.SampleBase:
						yield break;

					//record as simple key value pairs
					case PerformanceCounterType.AverageCount64:
					case PerformanceCounterType.CounterDelta32:
					case PerformanceCounterType.CounterDelta64:
					case PerformanceCounterType.CounterMultiTimer:
					case PerformanceCounterType.CounterMultiTimer100Ns:
					case PerformanceCounterType.CounterMultiTimer100NsInverse:
					case PerformanceCounterType.CounterMultiTimerInverse:
					case PerformanceCounterType.CounterTimer:
					case PerformanceCounterType.CounterTimerInverse:
					case PerformanceCounterType.CountPerTimeInterval32:
					case PerformanceCounterType.CountPerTimeInterval64:
					case PerformanceCounterType.NumberOfItems32:
					case PerformanceCounterType.NumberOfItems64:
					case PerformanceCounterType.NumberOfItemsHEX32:
					case PerformanceCounterType.NumberOfItemsHEX64:
					case PerformanceCounterType.RateOfCountsPerSecond32:
					case PerformanceCounterType.RateOfCountsPerSecond64:
					case PerformanceCounterType.RawFraction:
					case PerformanceCounterType.SampleCounter:
					case PerformanceCounterType.SampleFraction:
					case PerformanceCounterType.Timer100Ns:
					case PerformanceCounterType.Timer100NsInverse:
					default:
						var newMetric = new KeyValue();
						newMetric.Value = counter.CookedValue;
						newMetric.Timestamp = counter.Timestamp;
						metric = newMetric;
						break;

					//timers
					case PerformanceCounterType.AverageTimer32:
					case PerformanceCounterType.ElapsedTime:
						var newTiming = new Timing();
						newTiming.Duration = counter.CookedValue;
						metric = newTiming;
						break;
				}
				metricName.Remove(0, metricName.Length);	//clear the buffer
				var path = counter.Path.ToLower();
				if (!addInstance)
				{
					path = path.Substring(path.IndexOf('\\', 2) + 1);
				}
				metricName.Append(path);

				metricName.Replace(@"\\", string.Empty);

				if (null != counter.InstanceName)
				{
					string instanceName = counter.InstanceName.ToLower();
					metricName.Replace(String.Format(@"({0})\", instanceName), String.Format(@"\{0}\", instanceName));
				}

				for (int i = 0; i < metricName.Length; ++i)
				{
					if (_badChars.Contains(metricName[i])) { metricName[i] = '_'; }
				}

				metricName.Replace('\\', '.');
				metricName.Replace("#", "num");
				metricName.Replace("%", "pct");

				metric.Key = metricName.ToString();
				yield return metric;
			}
		}
	}
}