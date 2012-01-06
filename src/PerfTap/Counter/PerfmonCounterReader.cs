	// -----------------------------------------------------------------------
// <copyright file="GetCounterCommand.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Reflection;
	using System.Resources;
	using System.Threading;
	using PerfTap.Interop;

	public class PerfmonCounterReader
	{
		private static readonly ResourceManager _resourceManager = new ResourceManager("GetEventResources", Assembly.GetExecutingAssembly());
		private readonly IEnumerable<string> _computerNames = new string[0];
		private const int INFINITIY = -1;

		public PerfmonCounterReader(IEnumerable<string> computerNames)
		{			
			if (null == computerNames) { throw new ArgumentNullException("computerNames"); }

			this._computerNames = computerNames;
		}
		
		public PerfmonCounterReader()
		{
			this._computerNames = new string[0];
		}

		public IEnumerable<PerformanceCounterSampleSet> GetCounterSamples(TimeSpan sampleInterval, int count, CancellationToken token)
		{
			if (count <= 0) { throw new ArgumentOutOfRangeException("count", "must be greater than zero"); }
			if (null == token) { throw new ArgumentNullException("token"); }
 
			return ProcessGetCounter(GetDefaultCounters(), sampleInterval, count, token);	
		}
		public IEnumerable<PerformanceCounterSampleSet> StreamCounterSamples(TimeSpan sampleInterval, CancellationToken token)
		{
			if (null == token) { throw new ArgumentNullException("token"); }

			return ProcessGetCounter(GetDefaultCounters(), sampleInterval, INFINITIY, token);
		}

		public IEnumerable<PerformanceCounterSampleSet> GetCounterSamples(IEnumerable<string> counters, TimeSpan sampleInterval, int count, CancellationToken token)
		{
			if (null == counters) { throw new ArgumentNullException("counters"); }
			if (count <= 0) { throw new ArgumentOutOfRangeException("count", "must be greater than zero"); }
			if (null == token) { throw new ArgumentNullException("token"); }

			return ProcessGetCounter(counters, sampleInterval, count, token);
		}

		public IEnumerable<PerformanceCounterSampleSet> StreamCounterSamples(IEnumerable<string> counters, TimeSpan sampleInterval, CancellationToken token)
		{
			if (null == counters) { throw new ArgumentNullException("counters"); }
			if (null == token) { throw new ArgumentNullException("token"); }

			return ProcessGetCounter(counters, sampleInterval, INFINITIY, token);
		}

		private IEnumerable<PerformanceCounterSampleSet> ProcessGetCounter(IEnumerable<string> counters, TimeSpan sampleInterval, int maxSamples, CancellationToken token)
		{
			using (PdhHelper helper = new PdhHelper(this._computerNames, counters))
			{
				int samplesRead = 0;

				do
				{
					PerformanceCounterSampleSet set = helper.ReadNextSet();
					if (null != set)
					{
						this.VerifySamples(set);
						yield return set;
					}
					//TODO: log a null set like this?
					//PdhHelper.BuildException(returnCode, false);
					samplesRead++;
				}
				while (((maxSamples == INFINITIY) || (samplesRead < maxSamples)) && !token.WaitHandle.WaitOne(sampleInterval, true));
			}
		}

		public static List<string> DefaultCounters
		{
			get 
			{ 
				return new List<string>() { @"\network interface(*)\bytes total/sec", 
@"\processor(_total)\% processor time", 
@"\memory\% committed bytes in use", 
@"\memory\cache faults/sec", 
@"\physicaldisk(_total)\% disk time", 
@"\physicaldisk(_total)\current disk queue length" };
			}
		}

		private IEnumerable<string> GetDefaultCounters()
		{
			return DefaultCounters.Select(path =>
			{
				return PdhHelper.TranslateLocalCounterPath(path);
			});
		}

		private void VerifySamples(PerformanceCounterSampleSet set)
		{
			if (set.CounterSamples.Any(sample => sample.Status != 0))
			{
				throw new Exception(string.Format(CultureInfo.InvariantCulture, _resourceManager.GetString("CounterSampleDataInvalid"), new object[0]));
			}
		}
	}
}