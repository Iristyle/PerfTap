// -----------------------------------------------------------------------
// <copyright file="CounterFileParser.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class CounterFileParser
	{
		public static List<string> ReadCountersFromFile(string path)
		{
			return File.ReadAllLines(path)
				.Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
				.Distinct(StringComparer.CurrentCultureIgnoreCase)
				.OrderBy(line => line)
				.ToList();
		}
	}
}