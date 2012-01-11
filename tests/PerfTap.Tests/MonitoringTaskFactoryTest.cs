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

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class MonitoringTaskFactoryTest
	{
		private int port;
		ICounterSamplingConfiguration counterConfig = new Configuration.CounterSamplingConfiguration()
		{
			CounterDefinitions = new CounterNameConfigurationCollection(new [] {@"\network interface(*)\bytes total/sec", 
				@"\processor(_total)\% processor time", 
				@"\memory\% committed bytes in use", 
				@"\memory\cache faults/sec", 
				@"\physicaldisk(_total)\% disk time", 
				@"\physicaldisk(_total)\current disk queue length" }),
			SampleInterval = TimeSpan.FromSeconds(1)
		};

		IMetricPublishingConfiguration reportingConfig = new Configuration.MetricPublishingConfiguration()
		{
			PrefixKey = "test",
			Server = "localhost"
		};

		public MonitoringTaskFactoryTest()
		{
			port = new Random(DateTime.Now.Second).Next(8500, 10000);
			((MetricPublishingConfiguration)reportingConfig).Port = port; //bad bad
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