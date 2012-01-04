// -----------------------------------------------------------------------
// <copyright file="UdpClient.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap.Net
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net.Sockets;

	/// <summary>
	/// TODO: Update summary.
	/// </summary>
	public class UdpMessenger : IDisposable
	{
		private readonly static SimpleObjectPool<SocketAsyncEventArgs> _eventArgsPool 
			= new SimpleObjectPool<SocketAsyncEventArgs>(30, pool => new PoolAwareSocketAsyncEventArgs(pool));
		private readonly string _hostname;
		private readonly int _port;
		private readonly UdpClient _client;
		private bool _disposed;

		/// <summary>
		/// Initializes a new instance of the UdpMessenger class.
		/// </summary>
		/// <param name="server"></param>
		/// <param name="port"></param>
		public UdpMessenger(string hostname, int port)
		{
			_hostname = hostname;
			_port = port;
			_client = new UdpClient(hostname, port);
			_client.Client.SendBufferSize = 0;
		}

		/// <summary>	Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. </summary>
		/// <remarks>	12/28/2011. </remarks>
		public void Dispose()
		{
			if (!this._disposed)
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>	Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. </summary>
		/// <remarks>	12/28/2011. </remarks>
		/// <param name="disposing">	true if resources should be disposed, false if not. </param>
		protected virtual void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (null != _client)
				{
					_client.Close();
				}
				this._disposed = true;
			}
		}

		private void HealConnection()
		{
			//heal connection if necessary
			if (!_client.Client.Connected)
			{
				try { _client.Connect(_hostname, _port); }
				catch { }
			}
		}

		//TODO: write a separate version that instead processes each packet individually inside the loop
		public void SendMetrics(IEnumerable<string> metrics)
		{
			var data = _eventArgsPool.Pop();
			//firehose alert! -- keep it moving!
			if (null == data) { return; }

			try
			{
				data.SendPacketsElements = metrics.ToMaximumBytePackets()
					.Select(bytes => new SendPacketsElement(bytes, 0, bytes.Length, true))
					.ToArray();

				HealConnection();

				//SendAsync is superior to BeginSend / EndSend
				//_client.Client.SendAsync(eventArgs);
				//_client.Client.NoDelay = true;
				_client.Client.SendPacketsAsync(data);

				//Write-Debug "Wrote $(byteBlock.length) bytes to $server:$port"
			}
			//fire and forget, so just eat intermittent failures / exceptions
			catch
			{ }
		}
	}
}