// -----------------------------------------------------------------------
// <copyright file="MonitoringTaskFactory.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

using JustEat.StatsD;
using PerfTap.Configuration;
using PerfTap.Counter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace PerfTap
{

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class MonitoringTaskFactory
    {
        private readonly ICounterSamplingConfiguration _counterSamplingConfig;
        private readonly MetricPublishingConfiguration _metricPublishingConfig;
        private readonly List<string> _counterPaths;

        /// <summary>
        /// Initializes a new instance of the MonitoringTaskFactory class.
        /// </summary>
        /// <param name="counterSamplingConfig"></param>
        public MonitoringTaskFactory(ICounterSamplingConfiguration counterSamplingConfig, MetricPublishingConfiguration metricPublishingConfig)
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

                using (IStatsDPublisher publisher = new StatsDImmediatePublisher(
                    _metricPublishingConfig.CultureInfo,
                    _metricPublishingConfig.HostName,
                    _metricPublishingConfig.Port,
                    _metricPublishingConfig.PrefixKey))
                {
                    foreach (var metrics in reader.StreamCounterSamples(_counterPaths, _counterSamplingConfig.SampleInterval, cancellationToken))
                    {
                        foreach (var metric in metrics.CounterSamples)
                        {
                            WriteMetric(publisher, metric);
                        }

                    }
                }
            }, cancellationToken);
        }

        public Task CreateTask(CancellationToken cancellationToken, int maximumSamples)
        {
            var cultureInfo = Thread.CurrentThread.CurrentCulture;
            return new Task(() =>
                {
                    var reader = new PerfmonCounterReader();

                    using (IStatsDPublisher publisher = new StatsDImmediatePublisher(cultureInfo, _metricPublishingConfig.HostName, _metricPublishingConfig.Port))
                    {
                        foreach (var metrics in reader.GetCounterSamples(_counterPaths, _counterSamplingConfig.SampleInterval, maximumSamples, cancellationToken))
                        {
                            foreach (var metric in metrics.CounterSamples)
                            {
                                WriteMetric(publisher, metric);
                            }
                        }
                    }
                }, cancellationToken);
        }


        private static void WriteMetric(IStatsDPublisher publisher, PerformanceCounterSample metric)
        {
            publisher.Gauge(Convert.ToInt32(metric.CookedValue), metric.Path, DateTime.Now);
        }

    }
}