// -----------------------------------------------------------------------
// <copyright file="CounterFileParserTest.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.IO;
	using Xunit;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class CounterFileParserTest
	{
		[Fact]
		public void ReadCountersFromFile_ParsesTextCorrectly()
		{
			StringBuilder text = new StringBuilder();
			string filePath = Path.GetTempFileName();

			try
			{				
				for (int i = 0; i < 255; i++)
				{
					string line = (i % 16 == 0) ? "#foo" :
						(i % 50 == 0) ? string.Empty : ("foo" + string.Join(string.Empty, Enumerable.Repeat("a", i)));

					text.AppendLine(line);
				}
				File.WriteAllText(filePath, text.ToString());

				var lines = CounterFileParser.ReadCountersFromFile(filePath);

				Assert.Equal(255 - (256 / 16) - (256 / 50), lines.Count);
			}
			finally
			{
				File.Delete(filePath);
			}
		}

		[Fact]
		public void ReadCountersFromFile_IgnoresDuplicates()
		{
			StringBuilder text = new StringBuilder();
			string filePath = Path.GetTempFileName();

			try
			{
				for (int i = 0; i < 255; i++)
				{
					string line = (i % 16 == 0) ? "#foo" :
						(i % 7 == 0) ? string.Empty : "foo";

					text.AppendLine(line);
				}
				File.WriteAllText(filePath, text.ToString());

				var lines = CounterFileParser.ReadCountersFromFile(filePath);

				Assert.Equal(1, lines.Count);
			}
			finally
			{
				File.Delete(filePath);
			}
		}

	}
}
