namespace PerfTap.Net
{
	using System;
	using System.Net.Sockets;

	public sealed class PoolAwareSocketAsyncEventArgs : SocketAsyncEventArgs
	{
		private SimpleObjectPool<SocketAsyncEventArgs> _parentPool;

		/// <summary>
		/// Initializes a new instance of the PooledSocketAsyncEventArgs class.
		/// </summary>
		/// <param name="parentPool"></param>
		public PoolAwareSocketAsyncEventArgs(SimpleObjectPool<SocketAsyncEventArgs> parentPool)
		{
			_parentPool = parentPool;
		}

		protected override void OnCompleted(SocketAsyncEventArgs e)
		{
			base.OnCompleted(e);
			_parentPool.Push(this);
		}
	}
}