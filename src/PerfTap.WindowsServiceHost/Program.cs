namespace PerfTap.WindowsServiceHost
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.ServiceProcess;
	using System.Threading;
	using NLog;
	using NanoTube.Configuration;
	using PerfTap.Configuration;
	using ServiceChassis;

	static class Program
	{
		private static readonly Logger _log = LogManager.GetCurrentClassLogger();

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		private static void Main(string[] args)
		{
			try
			{
				AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
				_log.Info(() => "PerfTap Started");

#if (DEBUG)
				// Debug code: this allows the process to run as a non-service.
				// It will kick off the service start point, but never kill it.
				// Shut down the debugger to exit
				//TODO: this factory needs to be registered in a container to make this more general purpose 
				using (var service = new TaskService(cancellation => new MonitoringTaskFactory(CounterSamplingConfiguration.FromConfig(), MetricPublishingConfiguration.FromConfig()).CreateContinuousTask(cancellation)))
				{
					// Put a breakpoint in OnStart to catch it
					typeof(CustomServiceBase).GetMethod("OnStart", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(service, new object[] { null });
					//make sure we don't release the instance yet ;0
					Thread.Sleep(Timeout.Infinite);
				}
#else
				ServiceBase.Run(new TaskService(cancellation => new MonitoringTaskFactory(CounterSamplingConfiguration.FromConfig(),
				MetricPublishingConfiguration.FromConfig()).CreateContinuousTask(cancellation)));
#endif
			}
			catch (Exception ex)
			{
				_log.FatalException(String.Format("An unhandled error occurred in the PerfTap Service on [{0}]",
				Environment.MachineName), ex);
				throw;
			}
		}

		private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			_log.FatalException(String.Format("An unhandled error occurred in the PerfTap Service on [{0}]",
			Environment.MachineName), e.ExceptionObject as Exception);
		}
	}
}