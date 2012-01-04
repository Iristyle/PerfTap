# PerfTap

PerfTap is a Win32 PerfMon monitoring service that publishes all given counters to a remote Udp listener

At the moment, it is designed to publish data to [Graphite](http://graphite.wikidot.com/)

The format being used to push to Graphite is the one established by Etsy with [statsd](https://github.com/etsy/statsd)

In house, we are using the Python based [statsite](https://github.com/kiip/statsite) listener