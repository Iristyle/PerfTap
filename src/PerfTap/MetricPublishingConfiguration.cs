using System;
using System.Configuration;
using System.Globalization;
using System.Threading;

namespace PerfTap
{
    public class MetricPublishingConfiguration
    {
        public string HostName { get; set; }
        public int Port { get; set; }
        public string PrefixKey { get; set; }
        public CultureInfo CultureInfo { get; set; }

        public static MetricPublishingConfiguration FromConfig()
        {
            return new MetricPublishingConfiguration
            {
                CultureInfo = Thread.CurrentThread.CurrentCulture,
                PrefixKey = ConfigurationManager.AppSettings["prefix"],
                Port = Convert.ToInt32(ConfigurationManager.AppSettings["port"]),
                HostName = ConfigurationManager.AppSettings["host"]
            };
        }
    }
}
