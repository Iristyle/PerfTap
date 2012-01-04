// -----------------------------------------------------------------------
// <copyright file="Counter.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace ServiceChassis.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;

    /// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class PollingConfiguration : ConfigurationElement, IPollingConfiguration
	{
		[ConfigurationProperty("pollingInterval", DefaultValue=5, IsRequired=false)]
		[PositiveTimeSpanValidator()]
		[TimeSpanValidator(MinValueString="00:00:01",ExcludeRange=false)]
		public TimeSpan Interval { get; set; }

		[ConfigurationProperty("concurrency", DefaultValue=PollingTaskConcurrency.AllowMultipleTasks, IsRequired=false)]
		public PollingTaskConcurrency Concurrency { get; set; }
	}
}