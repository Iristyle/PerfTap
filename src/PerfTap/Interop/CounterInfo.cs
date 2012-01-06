namespace PerfTap.Interop
{
	using System;

	public struct CounterInfo
	{
		public uint Type { get; set; }
		public uint DefaultScale { get; set; }
		public ulong TimeBase { get; set; }
	}
}