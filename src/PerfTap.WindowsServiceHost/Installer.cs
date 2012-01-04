using System;
using System.ComponentModel;
using System.ServiceProcess;

namespace PerfTap.WindowsServiceHost
{
	[RunInstaller(true)]
	public partial class Installer : System.Configuration.Install.Installer
	{
		public Installer()
		{
			InitializeComponent();

			var processInstaller = new ServiceProcessInstaller { Account = ServiceAccount.NetworkService };
			var serviceInstaller = new ServiceInstaller()
			{
				StartType = ServiceStartMode.Automatic,
				DisplayName = "PerfTap - Synchronize PerfMon to StatsD compatible Graphite listener",
				ServiceName = "PerfTap",
				Description = "Reads particular metrics data from PerfMon and relays it to Graphite via the simple StatsD protocol.  Edit .config and nlog.config in the installation directory to modify service settings, then restart the service.",
			};

			Installers.Add(serviceInstaller);
			Installers.Add(processInstaller);
		}
	}
}