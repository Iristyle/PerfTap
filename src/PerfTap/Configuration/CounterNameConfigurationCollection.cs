namespace PerfTap.Configuration
{
	using System;
	using System.Configuration;
	using System.Collections.Generic;

	public class CounterNameConfigurationCollection : ConfigurationElementCollection
	{
		/// <summary>
		/// Initializes a new instance of the CounterNameConfigurationCollection class.
		/// </summary>
		public CounterNameConfigurationCollection()
		{ }

		public CounterNameConfigurationCollection(IEnumerable<string> names)
		{
			foreach (var name in names)
			{
				this.BaseAdd(new CounterName() { Name = name });
			}			
		}

		protected override ConfigurationElement CreateNewElement()
		{
			return new CounterName();
		}

		protected override object GetElementKey(ConfigurationElement element)
		{
			return ((CounterName)element).Name;
		}
	}
}