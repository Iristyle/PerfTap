namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	public static class CounterPathBuilder
	{
		public static List<string> PrefixWithComputerNames(this IEnumerable<string> counterNames, IEnumerable<string> computerNames)
		{
			if (null == counterNames) { throw new ArgumentNullException("counterNames"); }

			if (null == computerNames || computerNames.Count() == 0)
			{
				return counterNames.ToList();
			}

			return counterNames.Select(counter =>
				{
					if (counter.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
					{
						return new[] { counter };
					}

					return computerNames.Select(computerName =>
						{
							if (computerName.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
							{
								return String.Format(@"{0}\{1}", computerName, counter);
							}
							return String.Format(@"\\{0}\{1}", computerName, counter);
						});
				}).SelectMany(c => c).ToList();
		}
	}
}