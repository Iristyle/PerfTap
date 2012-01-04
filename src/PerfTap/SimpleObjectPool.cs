// -----------------------------------------------------------------------
// <copyright file="SimpleObjectPool.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace PerfTap
{
	using System;
	using System.Collections.Generic;

	public sealed class SimpleObjectPool<T>
		where T : class
	{
		private Stack<T> pool;
		public SimpleObjectPool(int capacity, Func<SimpleObjectPool<T>, T> constructor)
		{
			this.pool = new Stack<T>(capacity);
			for (int i = 0; i < capacity; ++i)
			{
				this.pool.Push(constructor(this));
			}
		}


		//TODO: consider using a millisecond timeout here
		public T Pop()
		{
			lock (this.pool)
			{
				if (this.pool.Count > 0) { return this.pool.Pop(); }
				else { return null; }
			}
		}

		public void Push(T item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("Items added to a SimpleObjectPool cannot be null");
			}
			lock (this.pool)
			{
				this.pool.Push(item);
			}
		}
	}
}