namespace PerfTap.Interop
{
	using System;

	internal static class PdhFormat
	{
		public const uint PDH_FMT_1000 = 0x2000;
		public const uint PDH_FMT_ANSI = 0x20;
		public const uint PDH_FMT_DOUBLE = 0x200;
		public const uint PDH_FMT_LARGE = 0x400;
		public const uint PDH_FMT_LONG = 0x100;
		public const uint PDH_FMT_NOCAP100 = 0x8000;
		public const uint PDH_FMT_NODATA = 0x4000;
		public const uint PDH_FMT_NOSCALE = 0x1000;
		public const uint PDH_FMT_RAW = 0x10;
		public const uint PDH_FMT_UNICODE = 0x40;
		public const uint PERF_DETAIL_COSTLY = 0x10000;
		public const uint PERF_DETAIL_STANDARD = 0xffff;
	}
}