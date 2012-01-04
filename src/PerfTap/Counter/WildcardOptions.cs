namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	[Flags]
	public enum WildcardOptions
	{
		Compiled = 1,
		CultureInvariant = 4,
		IgnoreCase = 2,
		None = 0
	}
}