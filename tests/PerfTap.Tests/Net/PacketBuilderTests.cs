// -----------------------------------------------------------------------
// <copyright file="PacketBuilderTests.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Net.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using Xunit;

	public class PacketBuilderTests
	{
		[Fact]
		public void ToMaximumBytePackets_AdheresToMaximum()
		{
			var bytes = PacketBuilder.ToMaximumBytePackets(new [] { Enumerable.Repeat("a", 512).ToString() }).ToArray();
			Assert.InRange(bytes[0].Length, 1, 512);
		}
	}
}
