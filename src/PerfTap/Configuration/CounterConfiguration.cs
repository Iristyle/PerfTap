// -----------------------------------------------------------------------
// <copyright file="Counter.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Configuration;
	using System.Linq;
	using System.Collections.ObjectModel;

    /// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class CounterConfiguration : ConfigurationElement, ICounterConfiguration
	{
		[ConfigurationProperty("pollingFrequency", DefaultValue=5, IsRequired=false)]
		[PositiveTimeSpanValidator()]
		[TimeSpanValidator(MinValueString="00:00:01",ExcludeRange=false)]
		public TimeSpan SampleInterval { get; set; }

		[ConfigurationProperty("definitionPaths", IsRequired=true)]
		public List<string> DefinitionPaths { get; set; }

		ReadOnlyCollection<string> ICounterConfiguration.DefinitionPaths
		{
			get { return new ReadOnlyCollection<string>(DefinitionPaths); }
		}
	}
}