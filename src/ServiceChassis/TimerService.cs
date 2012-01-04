namespace ServiceChassis
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
	using System.Threading;
	using System.Threading.Tasks;
	using System.Timers;
	using ServiceChassis.Configuration;

	public class TimerService : CustomServiceBase
	{		
		private System.Timers.Timer _pollTimer;
		private TaskPoolManager _taskPoolManager = new TaskPoolManager();
		private IPollingConfiguration _config;
		private Func<CancellationToken, Task> _taskFactory;
		
		public TimerService(Func<CancellationToken, Task> taskFactory, IPollingConfiguration config)
			: base("TimerService")
		{
			this._taskFactory = taskFactory;
			this._config = config;
		}

		protected override void StartService()
		{
			try
			{
				//create and fire off a timer
				//simply start our timer -- which will automatically kick off synchronizing
				//if (_taskPoolManager != null)
				//	_taskPoolManager.ProgressChanged -= synchronizer_ProgressChanged;
				//_taskPoolManager.ProgressChanged += synchronizer_ProgressChanged;

				_pollTimer = new System.Timers.Timer()
				{
					Enabled = true,
					Interval = _config.Interval.TotalMilliseconds
				};
				_pollTimer.Elapsed += pollTimer_Elapsed;

				_pollTimer.Start();
			}
			catch (Exception ex)
			{
				_log.Fatal(String.Format("Service {0} - Unexpected Error - Failed to start", ServiceName), ex);

				if (null != _pollTimer) _pollTimer.Stop();
				if (null != _taskPoolManager)
				{
					try { _taskPoolManager.CancelAll(); }
					catch (Exception) { };
				}

				//make sure that Windows knows that we're not running -- with the service messed, we can't
				throw;
			}
		}

		protected override void StopService()
		{
			try
			{
				ShutdownTimer();

				//user asked for a stop, so cancel it -- and then punch them in the face!!
				_taskPoolManager.CancelAllAndWait();
			}
			catch (Exception ex)
			{
				_log.Error(String.Format("Service {0} - Unexpected Error - Failed to Stop", ServiceName), ex);
			}
		}

		protected override void PauseService()
		{
			try
			{
				ShutdownTimer();

				//don't kill any ongoing tasks -- wait for them to stop on their own
				_taskPoolManager.WaitUntilComplete();
			}
			catch (Exception ex)
			{
				_log.Error(String.Format("Service {0} - Unexpected Error - Failed to Pause", ServiceName), ex);
			}
		}

		private void ShutdownTimer()
		{
			//make sure no more events are fired -- pollTimer can be null on an unhandled exception above
			if (null != _pollTimer)
			{
				_pollTimer.Stop();
				_pollTimer.Elapsed -= pollTimer_Elapsed;
			}
		}

		void pollTimer_Elapsed(object sender, ElapsedEventArgs e)
		{
			//TODO: make this configurable -- overlapping tasks is something that a user should decide on
			//we only kick off a new synchronization when one isn't running --otherwise wait until the next opportunity
			//very simple implementation
			_log.Debug(() => string.Format("Service {0} - timer tripped at - {1} - starting synchronizing", ServiceName, DateTime.Now));
			RunTaskImpl();
		}

		private void RunTaskImpl()
		{
			try
			{
				Stopwatch timer = new Stopwatch();
				timer.Start();
				_log.Debug(() => string.Format("Service {0} - starting task at {1}", ServiceName, DateTime.Now));
				var token = new CancellationTokenSource();
				var task = new CancellableTask(_taskFactory(token.Token), token);
				_taskPoolManager.EnqueTask(task);
				task.Task.Wait();
				timer.Stop();
				_log.Debug(() => String.Format("Service {0} - task completed at {1} - elapsed time {2}", ServiceName, DateTime.Now, timer.Elapsed));
			}
			catch (Exception ex)
			{
				_log.Error(String.Format("Service {0} - Unexpected Error - couldn't start task", ServiceName), ex);
			}
		}

		/*
		void synchronizer_ProgressChanged(object sender, TaskProgressChangedEventArgs e)
		{
			if (e.Details.Status == TaskStatus.Error)
				_log.Error("Synchronization Progress Update - Unexpected Error: {0}", e.Details.Message);
			else
			{
				_log.Debug("Synchronization Progress Update {0}% - Status {1}: {2}", e.ProgressPercentage, e.Details.Status, e.Details.Message);
			}
		}
		*/
	}
}