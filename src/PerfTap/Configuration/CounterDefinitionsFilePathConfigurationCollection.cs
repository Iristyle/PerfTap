namespace PerfTap.Configuration
{
	using System;
	using System.Configuration;
	using System.Collections.Generic;

	public class CounterDefinitionsFilePathConfigurationCollection : ConfigurationElementCollection
	{
		public CounterDefinitionsFilePathConfigurationCollection()
		{ }

		public CounterDefinitionsFilePathConfigurationCollection(IEnumerable<string> paths)
		{
			foreach (var path in paths)
			{
				this.BaseAdd(new CounterDefinitionsFilePath() { Path = path });
			}			
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new CounterDefinitionsFilePath();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((CounterDefinitionsFilePath)element).Path;
		}
	}
}