namespace PerfTap.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	internal static class PdhMakeCounterPathFlags
	{
		//Returns the path in the PDH format, for example, \\computer\object(parent/instance#index)\counter.
		public const uint PDH_PATH_STANDARD_FORMAT = 0;
		//Converts a PDH path to the WMI class and property name format.
		public const uint PDH_PATH_WBEM_RESULT = 1;
		//Converts the WMI class and property name to a PDH path.
		public const uint PDH_PATH_WBEM_INPUT = 2;
	}
}