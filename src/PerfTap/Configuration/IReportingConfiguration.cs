namespace PerfTap.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public interface IReportingConfiguration
	{
		string Server { get; }
		int Port { get; }
		string Key { get; }
	}
}