// -----------------------------------------------------------------------
// <copyright file="GetCounterCommand.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Globalization;
	using System.Linq;
	using System.Reflection;
	using System.Resources;
	using System.Threading;
	using NLog;
	using PerfTap.Interop;

	public class PerfmonCounterReader : IDisposable
	{
		private readonly Logger _log = LogManager.GetCurrentClassLogger();
		private readonly PdhHelper _pdhHelper = new PdhHelper(Environment.OSVersion.Version.Major < 6);
		private static readonly ResourceManager _resourceMgr = new ResourceManager("GetEventResources", Assembly.GetExecutingAssembly());

		private string[] _computerName = new string[0];
		private const int KEEP_ON_SAMPLING = -1;
		private bool _disposed;

		public PerfmonCounterReader()
		{			
			this._pdhHelper.ConnectToDataSource();
		}

		/// <summary>	Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. </summary>
		/// <remarks>	12/28/2011. </remarks>
		public void Dispose()
		{
			if (!this._disposed)
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>	Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. </summary>
		/// <remarks>	12/28/2011. </remarks>
		/// <param name="disposing">	true if resources should be disposed, false if not. </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				_pdhHelper.Dispose();
				this._disposed = true;
			}
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
			List<string> allCounterPaths = counters.PrefixWithComputerNames(this._computerName);
			List<string> validPaths = ParsePaths(allCounterPaths).ToList();

			if (validPaths.Count == 0)
				yield break;

			this._pdhHelper.OpenQuery();
			this._pdhHelper.AddCounters(validPaths, true);

			bool lastSampleBad = true;
			uint returnCode,
				samplesRead = 0;

			do
			{
				PerformanceCounterSampleSet set;
				returnCode = this._pdhHelper.ReadNextSet(out set, lastSampleBad);
				switch ((long)returnCode)
				{
					case PdhResults.PDH_CSTATUS_VALID_DATA:
						if (!lastSampleBad)
						{
							this.VerifySamples(set);
							yield return set; 
							samplesRead++;
						}
						lastSampleBad = false;
						break;

					case PdhResults.PDH_NO_DATA:
					case PdhResults.PDH_INVALID_DATA:
						//TODO: log this?
						//PdhHelper.BuildException(returnCode, false);
						lastSampleBad = true;
						samplesRead++;
						break;

					default:
						throw PdhHelper.BuildException(returnCode);
				}
			}

			while (((maxSamples == KEEP_ON_SAMPLING) || (samplesRead < maxSamples)) && !token.WaitHandle.WaitOne(sampleInterval, true));
		}

		private IEnumerable<string> GetDefaultCounters()
		{
			return new string[] { @"\network interface(*)\bytes total/sec", 
@"\processor(_total)\% processor time", 
@"\memory\% committed bytes in use", 
@"\memory\cache faults/sec", 
@"\physicaldisk(_total)\% disk time", 
@"\physicaldisk(_total)\current disk queue length" }.Select(path =>
			{

				return PdhHelper.TranslateLocalCounterPath(path);
			});
		}

		private IEnumerable<string> ParsePaths(IEnumerable<string> allCounterPaths)
		{
			foreach (string counterPath in allCounterPaths)
			{				
				var expandedPaths = _pdhHelper.ExpandWildCardPath(counterPath);
				if (null == expandedPaths)
				{
					this._log.Debug(() => string.Format("Could not expand path {0}", counterPath));
				}
				else
				{
					foreach (string expandedPath in expandedPaths)
					{
						if (!this._pdhHelper.IsPathValid(expandedPath))
						{
							throw new Exception(string.Format(CultureInfo.CurrentCulture, _resourceMgr.GetString("CounterPathIsInvalid"), new object[] { counterPath }));
						}

						yield return expandedPath;
					}
				}
			}
		}

		private void VerifySamples(PerformanceCounterSampleSet set)
		{
			if (set.CounterSamples.Any(sample => sample.Status != 0))
			{
				throw new Exception(string.Format(CultureInfo.InvariantCulture, _resourceMgr.GetString("CounterSampleDataInvalid"), new object[0]));
			}
		}

		// Properties
		//AllowEmptyCollection, ValidateNotNull, 
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Scope = "member", Target = "Microsoft.PowerShell.Commands.GetCounterCommand.ComputerName", Justification = "A string[] is required here because that is the type Powershell supports")]
		//, Parameter(ValueFromPipeline = false, ValueFromPipelineByPropertyName = false, HelpMessageBaseName = "GetEventResources", HelpMessageResourceId = "ComputerNameParamHelp")
		public string[] ComputerName
		{
			get { return this._computerName; }
			set { this._computerName = value; }
		}
	}
}