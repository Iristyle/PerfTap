namespace PerfTap.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;

	public class ReportingConfiguration : ConfigurationElement, IReportingConfiguration
	{
		[ConfigurationProperty("server", IsRequired = true)]
		public string Server { get; set; }

		[ConfigurationProperty("port", DefaultValue = 8125, IsRequired = false)]
		public int Port { get; set; }

		[ConfigurationProperty("key", DefaultValue = "", IsRequired = false)]
		[RegexStringValidator(@"^[^\s;:/\.\(\)\\#%\$\^]+$")]
		public string Key { get; set; }
	}
}