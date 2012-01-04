namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.InteropServices;

	[StructLayout(LayoutKind.Sequential)]
	public struct SwitchParameter
	{
		private bool isPresent;
		public bool IsPresent
		{
			get { return this.isPresent; }
		}
		public static implicit operator bool(SwitchParameter switchParameter)
		{
			return switchParameter.IsPresent;
		}

		public static implicit operator SwitchParameter(bool value)
		{
			return new SwitchParameter(value);
		}

		public bool ToBool()
		{
			return this.isPresent;
		}

		public SwitchParameter(bool isPresent)
		{
			this.isPresent = isPresent;
		}

		public static SwitchParameter Present
		{
			get { return new SwitchParameter(true); }
		}
		
		public override bool Equals(object obj)
		{
			if (obj is bool)
			{
				return (this.isPresent == ((bool)obj));
			}
			if (obj is SwitchParameter)
			{
				SwitchParameter parameter = (SwitchParameter)obj;
				return (this.isPresent == parameter.IsPresent);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return this.isPresent.GetHashCode();
		}

		public static bool operator ==(SwitchParameter first, SwitchParameter second)
		{
			return first.Equals(second);
		}

		public static bool operator !=(SwitchParameter first, SwitchParameter second)
		{
			return !first.Equals(second);
		}

		public static bool operator ==(SwitchParameter first, bool second)
		{
			return first.Equals(second);
		}

		public static bool operator !=(SwitchParameter first, bool second)
		{
			return !first.Equals(second);
		}

		public static bool operator ==(bool first, SwitchParameter second)
		{
			return first.Equals((bool)second);
		}

		public static bool operator !=(bool first, SwitchParameter second)
		{
			return !first.Equals((bool)second);
		}

		public override string ToString()
		{
			return this.isPresent.ToString();
		}
	}
}