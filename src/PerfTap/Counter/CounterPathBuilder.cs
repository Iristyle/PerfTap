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
					//already has computer name?
					if (counter.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
					{
						return new[] { counter };
					}

					return computerNames.Select(computerName =>
						{
							string format = computerName.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase) ? @"{0}\{1}"
								: counter.StartsWith(@"\", StringComparison.OrdinalIgnoreCase) ? @"\\{0}{1}" :
								@"\\{0}\{1}";

							return String.Format(format, computerName, counter);
						});
				}).SelectMany(c => c).ToList();
		}
	}
}