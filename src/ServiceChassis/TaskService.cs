namespace ServiceChassis
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;

	public class TaskService : CustomServiceBase
	{
		private readonly Func<CancellationToken, Task> _taskFactory;
		private CancellableTask _currentTask;
		private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

		public TaskService(Func<CancellationToken, Task> taskFactory)
			: base("TaskService")
		{
			this._taskFactory = taskFactory;
		}

		protected override void StartService()
		{
			try
			{
				lock (_taskFactory)
				{
					if (null == _currentTask)
					{
						_currentTask = new CancellableTask(_taskFactory(_cancellationTokenSource.Token), _cancellationTokenSource);
						_currentTask.Task.Start();
					}
				}
			}
			catch (Exception ex)
			{
				_log.Fatal(String.Format("Service {0} - Unexpected Error - Failed to start", ServiceName), ex);

				if (null != _currentTask)
				{
					_currentTask.Cancel();
					_currentTask = null;
				}

				//make sure that Windows knows that we're not running -- with the service messed, we can't
				throw;
			}
		}

		protected override void StopService()
		{
			//user asked for a stop, so cancel it -- and then punch them in the face!!
			_currentTask.CancelAndWait();
		}

		protected override void PauseService()
		{
			//TODO: this doesn't really make sense.... if this is a short-lived task, we should wait for it to finish
			 _currentTask.Task.Wait();
			//otherwise, we should cancel it
			//_currentTask.CancelAndWait();
		}
	}
}