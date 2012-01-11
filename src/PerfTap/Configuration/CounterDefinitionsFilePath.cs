namespace PerfTap.Configuration
{
	using System;
	using System.Configuration;

	public class CounterDefinitionsFilePath : ConfigurationElement, ICounterDefinitionsFilePath
	{
		[ConfigurationProperty("path", IsRequired = true)]
		public string Path
		{
			get { return (string)this["path"]; }
			set { this["path"] = value; }
		}
	}
}