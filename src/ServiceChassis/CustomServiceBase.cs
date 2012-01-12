namespace ServiceChassis
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.ServiceProcess;
	using NLog;

	public abstract class CustomServiceBase : ServiceBase
	{
		protected static readonly Logger _log = LogManager.GetCurrentClassLogger();
		private System.ComponentModel.IContainer components = null;
		private CustomServiceBase() {}

		protected CustomServiceBase(string serviceName)
		{
			this.AutoLog = true;
			this.ServiceName = serviceName;
		}

		protected override void OnStart(string[] args)
		{
			_log.Info(() => string.Format("Service {0} starting in response to [START]", ServiceName));
			StartService();
			_log.Info(() => string.Format("Service {0} started", ServiceName));
		}
		protected override void OnStop()
		{
			_log.Info(() => string.Format("Service {0} stopping in response to [STOP]", ServiceName));
			StopService();
			_log.Info(() => string.Format("Service {0} stopped", ServiceName));
		}
		protected override void OnShutdown()
		{
			_log.Info(() => string.Format("Service {0} stopping in response to [STOP]", ServiceName));
			StopService();
			_log.Info(() => string.Format("Service {0} stopped", ServiceName));
		}
		protected override void OnPause()
		{
			_log.Info(() => string.Format("Service {0} pausing in response to [PAUSE]", ServiceName));
			PauseService();
			_log.Info(() => string.Format("Service {0} paused", ServiceName));
		}
		protected override void OnContinue()
		{
			_log.Info(() => string.Format("Service {0} starting in response to [CONTINUE]", ServiceName));
			//with polling interval {1} -- _config.Interval
			StartService();
			_log.Info(() => string.Format("Service {0} started", ServiceName));
		}

		protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
		{
			switch (powerStatus)
			{
				case PowerBroadcastStatus.BatteryLow:
					_log.Info(() => string.Format("Service {0} pausing in response to [BATTERY LOW]", ServiceName));
					PauseService();
					_log.Info(() => string.Format("Service {0} paused", ServiceName));
					return true;

				//no clue as to what this OEM event is -- let's not do anything
				case PowerBroadcastStatus.OemEvent:
				//useless since we don't know if we've gone to battery, or A/C... could also be battery low
				//which will result in a shutdown
				case PowerBroadcastStatus.PowerStatusChange:
					return false;

				//turn off our timer process
				case PowerBroadcastStatus.QuerySuspend:
					_log.Info(() => string.Format("Service {0} pausing in response to [SUSPEND REQUEST]", ServiceName));
					PauseService();
					_log.Info(() => string.Format("Service {0} paused", ServiceName));
					return true;

				case PowerBroadcastStatus.QuerySuspendFailed:
					_log.Info(() => string.Format("Service {0} starting in response to [SUSPEND REQUEST FAIL]", ServiceName));
					return true;

				//don't do anything yet as we will get a ResumeSuspend
				//of course that might not make sense as a server
				case PowerBroadcastStatus.ResumeAutomatic:
					return true;
				case PowerBroadcastStatus.ResumeCritical:
					//recreate our resources
					_log.Info(() => string.Format("Service {0} starting in response to [RESUME FROM CRITICAL]", ServiceName));
					StartService();
					_log.Info(() => string.Format("Service {0} started", ServiceName));
					return true;

				//resuming our service, so start synchronizing again
				case PowerBroadcastStatus.ResumeSuspend:
					_log.Info(() => string.Format("Service {0} starting in response to [RESUME FROM SUSPEND]", ServiceName));
					StartService();
					_log.Info(() => string.Format("Service {0} started", ServiceName));
					return true;

				//we already paused our service in QuerySuspend
				case PowerBroadcastStatus.Suspend:
					return true;
			}

			return false;
		}

		protected abstract void StartService();
		protected abstract void StopService();
		protected abstract void PauseService();
	}
}