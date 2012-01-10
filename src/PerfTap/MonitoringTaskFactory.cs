// -----------------------------------------------------------------------
// <copyright file="MonitoringTaskFactory.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using PerfTap.Configuration;
	using PerfTap.Counter;
	using PerfTap.Linq;
	using PerfTap.Net;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class MonitoringTaskFactory
	{
		private readonly ICounterConfiguration _counterConfig;
		private readonly IReportingConfiguration _reportingConfig;
		private readonly List<string> _counterPaths;

		/// <summary>
		/// Initializes a new instance of the MonitoringTaskFactory class.
		/// </summary>
		/// <param name="counterConfig"></param>
		public MonitoringTaskFactory(ICounterConfiguration counterConfig, IReportingConfiguration reportingConfig)
		{
			if (null == counterConfig) { throw new ArgumentNullException("counterConfig"); }
			if (null == reportingConfig) { throw new ArgumentNullException("reportingConfig"); }

			_counterConfig = counterConfig;
			_counterPaths = counterConfig.DefinitionPaths
				.SelectMany(path => CounterFileParser.ReadCountersFromFile(path))
				.Union(_counterConfig.CounterDefinitions)
				.Distinct(StringComparer.CurrentCultureIgnoreCase)
				.ToList();
			_reportingConfig = reportingConfig;
		}

		public Task CreateContinuousTask(CancellationToken cancellationToken)
		{
			return new Task(() =>
			{
				var reader = new PerfmonCounterReader();
				using (var messenger = new UdpMessenger(_reportingConfig.Server, _reportingConfig.Port))
				{
					foreach (var metricBatch in reader.StreamCounterSamples(_counterPaths, _counterConfig.SampleInterval, cancellationToken)
						.SelectMany(set => set.CounterSamples.ToGraphiteString(_reportingConfig.Key))
						.Chunk(10))
					{
						messenger.SendMetrics(metricBatch);
					}
				}
			}, cancellationToken);
		}

		public Task CreateTask(CancellationToken cancellationToken, int maximumSamples)
		{
			return new Task(() => 
				{
					var reader = new PerfmonCounterReader();

					using (var messenger = new UdpMessenger(_reportingConfig.Server, _reportingConfig.Port))
					{
						foreach (var metricBatch in reader.GetCounterSamples(_counterPaths, _counterConfig.SampleInterval, maximumSamples, cancellationToken)
							.SelectMany(set => set.CounterSamples.ToGraphiteString(_reportingConfig.Key))
							.Chunk(10))
						{
							messenger.SendMetrics(metricBatch);
						}
					}
				}, cancellationToken);
			/*
			
		#when querying at a 10 second or less interval, batch into at least 10-second groups to cut down on cpu usage
		$maxSamples = 1
		if ($SecondFrequency -lt 10)
		{
			#TODO: should we prevent the timer from overlapping?
			$maxSamples = [Math]::Round(10 / $SecondFrequency)
		}
		*/
		}
	}
}