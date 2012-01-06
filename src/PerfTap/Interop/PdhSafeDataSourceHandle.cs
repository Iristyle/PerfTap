namespace PerfTap.Interop
{
	using System;
	using System.Runtime.ConstrainedExecution;
	using System.Runtime.InteropServices;

	internal sealed class PdhSafeDataSourceHandle : SafeHandle
	{
		private PdhSafeDataSourceHandle()
			: base(IntPtr.Zero, true)
		{ }

		[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
		protected override bool ReleaseHandle()
		{
			return (Apis.PdhCloseLog(base.handle, 0) == 0);
		}

		public override bool IsInvalid
		{
			get { return (base.handle == IntPtr.Zero); }
		}
	}
}