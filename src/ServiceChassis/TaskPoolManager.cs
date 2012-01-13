namespace ServiceChassis
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;
	using NLog;

	public class TaskPoolManager
	{
		protected static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private ConcurrentBag<CancellableTask> _tasks = new ConcurrentBag<CancellableTask>();

		public TaskPoolManager()
		{ }

		public void EnqueTask(CancellableTask task)
		{
			this._tasks.Add(task);
			task.Start();
		}

		public void CancelAll()
		{
			CancelAllImpl();
		}

		public void CancelAllAndWait()
		{
			List<CancellableTask> tasks = CancelAllImpl();

			_log.Info(() => string.Format("Waiting synchronously on {0} tasks to cancel", tasks.Count));
			Task.WaitAll(tasks.Select(t => t.Task).ToArray());
		}

		private List<CancellableTask> CancelAllImpl()
		{
			var tasks = this._tasks.ToList();
			_log.Info(() => string.Format("Canceling {0} tasks", tasks.Count));
			foreach (var task in tasks)
			{
				task.Cancel();
			}
			return tasks;
		}
		
		//can be used to wait synchronously until tasks are completed
		//after firing off async -- (useful when the client is a service, and we're handling the 'Pause' event)
		public void WaitUntilComplete()
		{
			var tasks = this._tasks.ToList();
			if (tasks.Count == 0)
			{
				return;
			}
			
			_log.Info(() => string.Format("Waiting synchronously on {0} tasks to complete", tasks.Count));
			Task.WaitAll(tasks.Select(t => t.Task).ToArray());
		}
	}
}