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
	public class CounterSamplingConfiguration : ConfigurationSection, ICounterSamplingConfiguration
	{
		public static CounterSamplingConfiguration FromConfig(string section = "perfTapCounterSampling")
		{
			return (CounterSamplingConfiguration)ConfigurationManager.GetSection(section);
		}

		[ConfigurationProperty("sampleInterval", DefaultValue="00:00:05", IsRequired=false)]
		[TimeSpanValidator(MinValueString="00:00:01",ExcludeRange=false)]
		public TimeSpan SampleInterval 
		{
			get { return (TimeSpan)this["sampleInterval"]; } 
			set { this["sampleInterval"] = value; }
		}

		[ConfigurationProperty("definitionFilePaths", IsDefaultCollection = true, IsRequired = false)]
		[ConfigurationCollection(typeof(CounterDefinitionsFilePathConfigurationCollection), AddItemName = "definitionFile")]
		public CounterDefinitionsFilePathConfigurationCollection DefinitionFilePaths
		{
			get { return (CounterDefinitionsFilePathConfigurationCollection)this["definitionFilePaths"]; }
			set { this["definitionFilePaths"] = value; }
		}

		ReadOnlyCollection<ICounterDefinitionsFilePath> ICounterSamplingConfiguration.DefinitionFilePaths
		{
			get { return new ReadOnlyCollection<ICounterDefinitionsFilePath>(DefinitionFilePaths.OfType<ICounterDefinitionsFilePath>().ToList() 
				?? (IList<ICounterDefinitionsFilePath>)new ICounterDefinitionsFilePath[0]); }
		}

		[ConfigurationProperty("counterNames", IsDefaultCollection = true, IsRequired = false)]
		[ConfigurationCollection(typeof(CounterNameConfigurationCollection), AddItemName = "counter")]
		public CounterNameConfigurationCollection CounterDefinitions
		{
			get { return (CounterNameConfigurationCollection)this["counterNames"]; }
			set { this["counterNames"] = value; }
		}

		ReadOnlyCollection<ICounterName> ICounterSamplingConfiguration.CounterNames
		{
			get { return new ReadOnlyCollection<ICounterName>(CounterDefinitions.OfType<ICounterName>().ToList() ?? (IList<ICounterName>)new ICounterName[0]); }
		}

		//TODO: 1-9-2012 -- add error handling to ensure that there's always at least a set of definition paths OR counter definitions supplied by the user
	}
}