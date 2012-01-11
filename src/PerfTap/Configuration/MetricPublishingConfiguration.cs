namespace PerfTap.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;

	public class MetricPublishingConfiguration : ConfigurationSection, IMetricPublishingConfiguration
	{
		public static IMetricPublishingConfiguration FromConfig(string section = "perfTapPublishing")
		{
			return (MetricPublishingConfiguration)ConfigurationManager.GetSection(section);
		}

		[ConfigurationProperty("server", IsRequired = true)]
		public string Server 
		{
			get { return (string)this["server"]; }
			set { this["server"] = value; }
		}

		[ConfigurationProperty("port", DefaultValue = 8125, IsRequired = false)]
		public int Port 
		{
			get { return (int)this["port"]; }
			set { this["port"] = value; }
		}

		[ConfigurationProperty("prefixKey", DefaultValue = "", IsRequired = false)]
		[RegexStringValidator(@"^[^\s;:/\.\(\)\\#%\$\^]+$|^$")]
		public string PrefixKey 
		{
			get { return (string)this["prefixKey"]; }
			set { this["prefixKey"] = value; }
		}
	}
}