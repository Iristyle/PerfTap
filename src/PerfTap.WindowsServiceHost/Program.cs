namespace PerfTap.WindowsServiceHost
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Threading;
	using NLog;
	using PerfTap.Configuration;
	using ServiceChassis;
	using ServiceChassis.Configuration;
	using PerfTap.Interop;

	static class Program
	{
		private static readonly Logger log = LogManager.GetCurrentClassLogger();
		//TODO: replace with code that reads from Xml config
		private static ICounterConfiguration counterConfig = new CounterConfiguration()
		{
			SampleInterval = TimeSpan.FromSeconds(2),
			DefinitionPaths = new List<string>() { "CounterDefinitions\\system.counters" }
		};
		private static IReportingConfiguration reportingConfig = new ReportingConfiguration()
		{
			Key = "Test",
			Server = "metrics.eastpointsystems.com",
			Port = 8125
		};

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		static void Main(string[] args)
		{
			try
			{
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
				PerfmonPrivileges.Set();
#if (DEBUG)
				// Debug code: this allows the process to run as a non-service.
				// It will kick off the service start point, but never kill it.
				// Shut down the debugger to exit
				//TODO: this factory needs to be registered in a container to make this more general purpose 
				using (var service = new TaskService(cancellation => new MonitoringTaskFactory(counterConfig, reportingConfig).CreateContinuousTask(cancellation)))
				{
					// Put a breakpoint in OnStart to catch it
					typeof(CustomServiceBase).GetMethod("OnStart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(service, new object[] { null });
					//make sure we don't release the instance yet ;0
					Thread.Sleep(Timeout.Infinite);
				}
#else
                    ServiceBase.Run(new[] { new TaskService(cancellation => new MonitoringTaskFactory(counterConfig, reportingConfig).CreateContinuousTask(cancellation)) });
#endif
			}
			catch (Exception ex)
			{
				log.Fatal(String.Format("An unhandled error occurred in the PerfTap Service on [{0}]",
					Environment.MachineName), ex);
			}
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			log.Fatal(String.Format("An unhandled error occurred in the PerfTap Service on [{0}]",
					Environment.MachineName), e.ExceptionObject as Exception);
		}
	}
}