// -----------------------------------------------------------------------
// <copyright file="CounterHandleNInstance.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	internal struct CounterHandleNInstance
	{
		public IntPtr hCounter;
		public string InstanceName;
	}
}