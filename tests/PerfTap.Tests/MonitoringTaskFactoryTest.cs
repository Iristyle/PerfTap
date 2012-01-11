// -----------------------------------------------------------------------
// <copyright file="MonitoringTaskFactoryTest.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Tests
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Net.Sockets;
	using System.Threading;
	using System.Threading.Tasks;
	using PerfTap.Configuration;
	using Xunit;
	using PerfTap.Interop;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class MonitoringTaskFactoryTest
	{
		private int port;
		ICounterConfiguration counterConfig = new Configuration.CounterConfiguration()
		{
			CounterDefinitions = new List<string>() { @"\network interface(*)\bytes total/sec", 
				@"\processor(_total)\% processor time", 
				@"\memory\% committed bytes in use", 
				@"\memory\cache faults/sec", 
				@"\physicaldisk(_total)\% disk time", 
				@"\physicaldisk(_total)\current disk queue length" },
			SampleInterval = TimeSpan.FromSeconds(1)
		};

		IReportingConfiguration reportingConfig = new Configuration.ReportingConfiguration()
		{
			Key = "test",
			Server = "localhost"
		};

		public MonitoringTaskFactoryTest()
		{
			port = new Random(DateTime.Now.Second).Next(8500, 10000);
			((ReportingConfiguration)reportingConfig).Port = port; //bad bad
		}

		private Task<byte[]> StartListeningForBytes()
		{
			var receiveTask = new Task<byte[]>(() =>
				{
					using (var listener = new UdpClient(port))
					{
						IPEndPoint groupEndpoint = new IPEndPoint(IPAddress.Any, port);
						return listener.Receive(ref groupEndpoint);
					}
				});
			receiveTask.Start();

			return receiveTask;
		}

		[Fact]
		public void CreateTask_WritesToGivenUdpPort()
		{			
			var udpListenerTask = StartListeningForBytes();

			using (var cancellationTokenSource = new CancellationTokenSource())
			{
				new MonitoringTaskFactory(counterConfig, reportingConfig)
				.CreateTask(cancellationTokenSource.Token, 1)
				.RunSynchronously();
			}

			if (!udpListenerTask.Wait(500))
			{
				Assert.True(false, "Timed out while waiting for response from task");
			}

			Assert.True(udpListenerTask.Result.Length > 0);
		}
	}
}