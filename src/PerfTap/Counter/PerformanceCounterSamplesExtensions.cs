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
	using PerfTap.Counter;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class PerformanceCounterSamplesExtensions
	{
		private static Regex _validKey = new Regex(@"^[^!\s;:/\.\(\)\\#%\$\^]+$", RegexOptions.Compiled);
		private const string _keyValue = "kv",
			_timer = "ms",
			_performanceCounter = "c",
			_separator = "\n",
			_badChars = " ;:/.()*";

		public static IEnumerable<string> ToGraphiteString(this IEnumerable<PerformanceCounterSample> performanceCounters, string key)
		{
			if (null == performanceCounters)
				{ throw new ArgumentNullException("performanceCounter"); }
			if (!string.IsNullOrEmpty(key) && !_validKey.IsMatch(key))
				{ throw new ArgumentException("Key contains invalid characters", "key"); }

			var metric = new StringBuilder(150);
			string prefix = string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim() + ".";
			string type = _keyValue;

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
						type = _keyValue;
						break;

					//timers
					case PerformanceCounterType.AverageTimer32:
					case PerformanceCounterType.ElapsedTime:
						type = _timer;
						break;
				}

				//https://github.com/kiip/statsite
				//key:value|type[|@flag]
				metric.Remove(0, metric.Length);	//clear the buffer
				metric.Append(counter.Path.ToLower());

				metric.Replace(@"\\", string.Empty);				

				for (int i = 0; i < metric.Length; ++i)
				{
					if (_badChars.Contains(metric[i])) { metric[i] = '_'; }
				}

				if (null != counter.InstanceName)
				{
					string instanceName = counter.InstanceName.ToLower();
					metric.Replace(String.Format(@"_{0}_\", instanceName), String.Format(@".{0}.", instanceName));
				}

				metric.Replace('\\', '.');
				metric.Replace("#", "num");
				metric.Replace("%", "pct");
				if (!string.IsNullOrEmpty(prefix)) { metric.Insert(0, prefix); }

				metric.AppendFormat(":{0:0.###}|{1}", counter.CookedValue, type);

				if (type == _keyValue)
				{
					metric.AppendFormat("|@{0}", counter.Timestamp.AsUnixTime());
				}

				yield return metric.ToString();
			}
		}
		//[String]::Join($Separator, $formatted)
	}
}