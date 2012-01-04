// -----------------------------------------------------------------------
// <copyright file="DateTimeConversions.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class DateTimeExtensions
	{
		private static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

		public static double AsUnixTime(this DateTime dateTime)
		{
			return Math.Round(dateTime.ToUniversalTime().Subtract(epoch).TotalSeconds);
		}
	}
}