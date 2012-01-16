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
using System.Reflection;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class CounterFileParser
	{
		private static string relativePathRoot = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

		public static List<string> ReadCountersFromFile(string path)
		{
			string filePath = Path.IsPathRooted(path) ? path
				: File.Exists(path) ? path
				: Path.Combine(relativePathRoot, path);

			return File.ReadAllLines(filePath)
				.Where(line => !line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
				.Select(line => line.Trim())
				.Distinct(StringComparer.CurrentCultureIgnoreCase)
				.OrderBy(line => line)
				.ToList();
		}
	}
}