// -----------------------------------------------------------------------
// <copyright file="Privileges.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Interop
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Runtime.InteropServices;
	using System.Security.Principal;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public static class PerfmonPrivileges
	{
		[DllImport("advapi32.dll", SetLastError = true)]
		static extern bool GetTokenInformation(IntPtr TokenHandle, TOKEN_INFORMATION_CLASS TokenInformationClass,
			IntPtr TokenInformation, uint TokenInformationLength, out uint ReturnLength);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

		[DllImport("advapi32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, [MarshalAs(UnmanagedType.Bool)] bool DisableAllPrivileges,
			ref TOKEN_PRIVILEGES NewState, uint BufferLengthInBytes, IntPtr nullPreviousState, IntPtr nullReturnLengthByetes);
		//ref TOKEN_PRIVILEGES PreviousState, out uint ReturnLengthInBytes);

		private const int ANYSIZE_ARRAY = 1;
		private const int SE_PRIVILEGE_ENABLED = 0x00000002;
		private const int ERROR_NOT_ALL_ASSIGNED = 1300;

		private enum TOKEN_INFORMATION_CLASS
		{
			TokenUser = 1,
			TokenGroups,
			TokenPrivileges,
			TokenOwner,
			TokenPrimaryGroup,
			TokenDefaultDacl,
			TokenSource,
			TokenType,
			TokenImpersonationLevel,
			TokenStatistics,
			TokenRestrictedSids,
			TokenSessionId,
			TokenGroupsAndPrivileges,
			TokenSessionReference,
			TokenSandBoxInert,
			TokenAuditPolicy,
			TokenOrigin
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct LUID
		{
			public uint LowPart;
			public int HighPart;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct LUID_AND_ATTRIBUTES
		{
			public LUID Luid;
			public int Attributes;
		};

		[StructLayout(LayoutKind.Sequential)]
		private struct TOKEN_PRIVILEGES
		{
			public int PrivilegeCount;
			[MarshalAs(UnmanagedType.ByValArray, SizeConst = ANYSIZE_ARRAY)]
			public LUID_AND_ATTRIBUTES[] Privileges;
		};

		private static Exception BuildException(int failedReturnCode)
		{
			string message = Win32Messages.FormatMessageFromModule((uint)failedReturnCode, "advapi32.dll");
			if (string.IsNullOrEmpty(message))
			{
				message = string.Format(CultureInfo.InvariantCulture, GetEventResources.PrivilegesApiError, new object[] { failedReturnCode });
			}

			return new Exception(message);
		}

		private static TOKEN_PRIVILEGES ReadTokenInformation()
		{
			uint tokenBufferSize = 0;
			IntPtr userToken = WindowsIdentity.GetCurrent().Token;

			// first call gets length of buffer
			GetTokenInformation(userToken, TOKEN_INFORMATION_CLASS.TokenPrivileges, IntPtr.Zero, tokenBufferSize, out tokenBufferSize);

			IntPtr privilegesPointer = IntPtr.Zero;

			try
			{
				privilegesPointer = Marshal.AllocHGlobal((int)tokenBufferSize);

				if (!GetTokenInformation(userToken, TOKEN_INFORMATION_CLASS.TokenPrivileges, privilegesPointer, tokenBufferSize, out tokenBufferSize))
				{
					throw BuildException(Marshal.GetLastWin32Error());
				}

				var privileges = (TOKEN_PRIVILEGES)Marshal.PtrToStructure(privilegesPointer, typeof(TOKEN_PRIVILEGES));
				IntPtr extraPrivilegeData = new IntPtr(privilegesPointer.ToInt32() + Marshal.SizeOf(privileges));
				//read all the extra 
				Type localIdType = typeof(LUID_AND_ATTRIBUTES);
				int sizeOfRecord = Marshal.SizeOf(localIdType);
				privileges.Privileges = privileges.Privileges.Union(
					Enumerable.Range(1, privileges.PrivilegeCount - 1).Select(index =>
							(LUID_AND_ATTRIBUTES)Marshal.PtrToStructure(extraPrivilegeData + ((index - 1) * sizeOfRecord), localIdType)))
				.ToArray();

				return privileges;
			}
			finally
			{
				Marshal.FreeHGlobal(privilegesPointer);
			}
		}

		public static void Set()
		{
			var user = ReadTokenInformation();

			TOKEN_PRIVILEGES tokenPrivileges;
			LUID systemProfileLuid;

			if (!LookupPrivilegeValue(null, PrivilegeConstants.SE_SYSTEM_PROFILE_NAME, out systemProfileLuid))
			{
				throw BuildException(Marshal.GetLastWin32Error());
			}

			//see if we already have the perfmon reading user privilege set
			//TODO: does this particular privilege vary by OS?
			if (user.Privileges.Any(p => (p.Luid.HighPart == systemProfileLuid.HighPart && p.Luid.LowPart == systemProfileLuid.LowPart)))
			{
				return;
			}

			tokenPrivileges.PrivilegeCount = 1;
			tokenPrivileges.Privileges = new[]
				{
					new LUID_AND_ATTRIBUTES() { Luid = systemProfileLuid, Attributes = SE_PRIVILEGE_ENABLED }
				};

			IntPtr userToken = WindowsIdentity.GetCurrent(TokenAccessLevels.AdjustPrivileges | TokenAccessLevels.Query).Token;

			if (!AdjustTokenPrivileges(userToken, false, ref tokenPrivileges, 0, IntPtr.Zero, IntPtr.Zero))
			{
				throw BuildException(Marshal.GetLastWin32Error());
			}

			if (Marshal.GetLastWin32Error() == ERROR_NOT_ALL_ASSIGNED)
			{
				throw BuildException(ERROR_NOT_ALL_ASSIGNED);
			}
		}
	}
}