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
		private static readonly ResourceManager _resourceMgr = new ResourceManager("GetEventResources", Assembly.GetExecutingAssembly());

		private readonly IEnumerable<string> _computerNames = new string[0];
		private const int KEEP_ON_SAMPLING = -1;

		public PerfmonCounterReader(IEnumerable<string> computerNames)
		{			
			this._computerNames = computerNames;
		}
		
		public PerfmonCounterReader()
		{
			this._computerNames = new string[0];
		}

		public IEnumerable<PerformanceCounterSampleSet> GetCounterSamples(TimeSpan sampleInterval, int count)
		{
			return ProcessGetCounter(GetDefaultCounters(), sampleInterval, count, new CancellationToken());	
		}

		public IEnumerable<PerformanceCounterSampleSet> GetCounterSamples(IEnumerable<string> counters, TimeSpan sampleInterval, int count)
		{			
			return ProcessGetCounter(counters, sampleInterval, count, new CancellationToken());
		}

		public IEnumerable<PerformanceCounterSampleSet> StreamCounterSamples(IEnumerable<string> counters, TimeSpan sampleInterval, CancellationToken token)
		{
			return ProcessGetCounter(counters, sampleInterval, KEEP_ON_SAMPLING, token);
		}

		//KEEP_ON_SAMPLING is a valid int here
		private IEnumerable<PerformanceCounterSampleSet> ProcessGetCounter(IEnumerable<string> counters, TimeSpan sampleInterval, int maxSamples, CancellationToken token)
		{
			using (PdhHelper helper = new PdhHelper(this._computerNames, counters))
			{
				bool lastSampleBad = true;
				uint samplesRead = 0;

				do
				{
					PerformanceCounterSampleSet set = helper.ReadNextSet(lastSampleBad);
					if (null == set)
					{
						//TODO: log this?
						//PdhHelper.BuildException(returnCode, false);
						lastSampleBad = true;
						samplesRead++;
						continue;
					}

					if (!lastSampleBad)
					{
						this.VerifySamples(set);
						yield return set;
						samplesRead++;
					}
					lastSampleBad = false;
				}

				while (((maxSamples == KEEP_ON_SAMPLING) || (samplesRead < maxSamples)) && !token.WaitHandle.WaitOne(sampleInterval, true));
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
				throw new Exception(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterSampleDataInvalid"), new object[0]));
			}
		}
	}
}