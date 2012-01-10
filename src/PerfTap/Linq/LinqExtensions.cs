// -----------------------------------------------------------------------
// <copyright file="LinqExtensions.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Linq
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class LinqExtensions
	{
		public static IEnumerable<IEnumerable<T>> Chunk<T>(this IEnumerable<T> list, int batchSize)
		{
			var batch = new List<T>(batchSize);

			foreach (var item in list)
			{
				batch.Add(item);
				if (batch.Count == batchSize)
				{
					yield return batch;
					batch = new List<T>(batchSize);
				}
			}

			if (batch.Count > 0)
				yield return batch;
		}
	}
}