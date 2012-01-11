// -----------------------------------------------------------------------
// <copyright file="LinqExtensionsTests.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Linq.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using Xunit;
	using Xunit.Extensions;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class LinqExtensionsTests
	{		
		[Theory]
		[InlineData(4, 2)]
		[InlineData(9, 2)]
		[InlineData(9, 1)]
		public void Chunk_CreatesCorrectlySizedGroups(int items, int groupSize)
		{
			int expectedGroups = items / groupSize + (items % groupSize != 0 ? 1 : 0);
			Assert.Equal(expectedGroups,
				Enumerable.Repeat("a", items)
					.Chunk(groupSize)
					.ToList().Count);
		}

		[Theory]
		[InlineData(6, 3)]
		[InlineData(8, 3)]
		[InlineData(4, 1)]
		public void Chunk_ReconstitutingReturnsAllOriginalElements(int items, int groupSize)
		{
			Assert.Equal(items,
				Enumerable.Repeat("a", items)
					.Chunk(groupSize)
					.SelectMany(list => list)
					.Count());
		}
	}
}