namespace PerfTap.Configuration
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Collections.ObjectModel;

	public interface ICounterSamplingConfiguration
	{
		ReadOnlyCollection<ICounterDefinitionsFilePath> DefinitionFilePaths { get; }
		ReadOnlyCollection<ICounterName> CounterNames { get; } 
		TimeSpan SampleInterval { get; }
	}
}