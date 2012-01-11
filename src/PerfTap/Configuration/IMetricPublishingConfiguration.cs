namespace PerfTap.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public interface IMetricPublishingConfiguration
	{
		string Server { get; }
		int Port { get; }
		string PrefixKey { get; }
	}
}