namespace PerfTap.Counter
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Diagnostics;
	using System.Collections.Specialized;

	public class CounterSet
	{
		// Fields
		private Dictionary<string, string[]> _counterInstanceMapping;
		private string _counterSetName = "";
		private PerformanceCounterCategoryType _counterSetType;
		private string _description = "";
		private string _machineName = ".";

		// Methods
		internal CounterSet(string setName, string machineName, PerformanceCounterCategoryType categoryType, string setHelp, ref Dictionary<string, string[]> counterInstanceMapping)
		{
			this._counterSetName = setName;
			if ((machineName == null) || (machineName.Length == 0))
			{
				machineName = ".";
			}
			else
			{
				this._machineName = machineName;
				if (!this._machineName.StartsWith(@"\\", StringComparison.OrdinalIgnoreCase))
				{
					this._machineName = @"\\" + this._machineName;
				}
			}
			this._counterSetType = categoryType;
			this._description = setHelp;
			this._counterInstanceMapping = counterInstanceMapping;
		}

		// Properties
		internal Dictionary<string, string[]> CounterInstanceMapping
		{
			get { return this._counterInstanceMapping; }
		}

		public string CounterSetName
		{
			get { return this._counterSetName; }
		}

		public PerformanceCounterCategoryType CounterSetType
		{
			get { return this._counterSetType; }
		}

		public string Description
		{
			get { return this._description; }
		}

		public string MachineName
		{
			get { return this._machineName; }
		}

		public StringCollection Paths
		{
			get
			{
				StringCollection strings = new StringCollection();
				foreach (string str in this.CounterInstanceMapping.Keys)
				{
					string str2;
					if (this.CounterInstanceMapping[str].Length != 0)
					{
						str2 = (this._machineName == ".") ? (@"\" + this._counterSetName + @"(*)\" + str) : (this._machineName + @"\" + this._counterSetName + @"(*)\" + str);
					}
					else
					{
						str2 = (this._machineName == ".") ? (@"\" + this._counterSetName + @"\" + str) : (this._machineName + @"\" + this._counterSetName + @"\" + str);
					}
					strings.Add(str2);
				}
				return strings;
			}
		}

		public StringCollection PathsWithInstances
		{
			get
			{
				StringCollection strings = new StringCollection();
				foreach (string str in this.CounterInstanceMapping.Keys)
				{
					foreach (string str2 in this.CounterInstanceMapping[str])
					{
						string str3 = (this._machineName == ".") ? (@"\" + this._counterSetName + "(" + str2 + @")\" + str) : (this._machineName + @"\" + this._counterSetName + "(" + str2 + @")\" + str);
						strings.Add(str3);
					}
				}
				return strings;
			}
		}
	}
}