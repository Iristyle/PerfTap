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
                PrefixKey = "WNDMTRX",
                Port = 8125,
                HostName = "192.168.99.100"
            };
        }
    }
}
