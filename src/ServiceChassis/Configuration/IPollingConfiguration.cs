namespace ServiceChassis.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public enum PollingTaskConcurrency
	{
		OnlyAllowSingleTask,
		AllowMultipleTasks
	}

	public interface IPollingConfiguration
	{
		TimeSpan Interval { get; }
		PollingTaskConcurrency Concurrency { get; }
	}
}