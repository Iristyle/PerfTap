// -----------------------------------------------------------------------
// <copyright file="PdhHelper.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Interop.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Xunit;
	using PerfTap.Counter;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class PdhHelperTests
	{
		[Fact]
		public void Foo()
		{			
			using (var pdhHelper = new PdhHelper(PerfmonCounterReader.DefaultCounters))
			{
				pdhHelper.ReadNextSet(false);
			}

			Assert.True(true);
		}

	}
}
