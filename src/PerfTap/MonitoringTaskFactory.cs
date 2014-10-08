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
	using NanoTube.Configuration;
	using NanoTube.Linq;
	using NanoTube;
	using PerfTap.Configuration;
	using PerfTap.Counter;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class MonitoringTaskFactory
	{
		private readonly ICounterSamplingConfiguration _counterSamplingConfig;
		private readonly IMetricPublishingConfiguration _metricPublishingConfig;
		private readonly List<string> _counterPaths;

		/// <summary>
		/// Initializes a new instance of the MonitoringTaskFactory class.
		/// </summary>
		/// <param name="counterSamplingConfig"></param>
		public MonitoringTaskFactory(ICounterSamplingConfiguration counterSamplingConfig, IMetricPublishingConfiguration metricPublishingConfig)
		{
			if (null == counterSamplingConfig) { throw new ArgumentNullException("counterSamplingConfig"); }
			if (null == metricPublishingConfig) { throw new ArgumentNullException("metricPublishingConfig"); }

			_counterSamplingConfig = counterSamplingConfig;
			_counterPaths = counterSamplingConfig.DefinitionFilePaths
				.SelectMany(path => CounterFileParser.ReadCountersFromFile(path.Path))
				.Union(_counterSamplingConfig.CounterNames.Select(name => name.Name.Trim()))
				.Distinct(StringComparer.CurrentCultureIgnoreCase)
				.ToList();
			_metricPublishingConfig = metricPublishingConfig;
		}

		public Task CreateContinuousTask(CancellationToken cancellationToken)
		{
			return new Task(() =>
			{
				var reader = new PerfmonCounterReader();
				using (var messenger = new MetricClient(_metricPublishingConfig))
				{
					foreach (var metricBatch in reader.StreamCounterSamples(_counterPaths, _counterSamplingConfig.SampleInterval, cancellationToken)
						.SelectMany(set => set.CounterSamples.ToMetrics())
						.Chunk(10))
					{
						messenger.Send(metricBatch);
					}
				}
			}, cancellationToken);
		}

		public Task CreateTask(CancellationToken cancellationToken, int maximumSamples)
		{
			return new Task(() => 
				{
					var reader = new PerfmonCounterReader();

					using (var messenger = new MetricClient(_metricPublishingConfig))
					{
						foreach (var metricBatch in reader.GetCounterSamples(_counterPaths, _counterSamplingConfig.SampleInterval, maximumSamples, cancellationToken)
							.SelectMany(set => set.CounterSamples.ToMetrics())
							.Chunk(10))
						{
							messenger.Send(metricBatch);
						}
					}
				}, cancellationToken);
		}
	}
}