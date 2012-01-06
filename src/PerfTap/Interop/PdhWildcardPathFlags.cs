namespace PerfTap.Interop
{
	using System;

	internal static class PdhWildcardPathFlags
	{
		public const uint None = 0;
		public const uint PDH_NOEXPANDCOUNTERS = 1;
		public const uint PDH_NOEXPANDINSTANCES = 2;
		public const uint PDH_REFRESHCOUNTERS = 4;
	}
}