# PerfTap
A Win32 PerfMon monitoring service that publishes a set of counters to a StatsD compatible Udp listener.  Essentially, this publishes Windows performance data so that it can be graphed and analyzed in [Graphite](http://graphite.wikidot.com/)

Think of it as a super simplified version of [collectd](http://collectd.org/) for Windows, without plugins and with the limitation of publishing counter information only over Udp -- no other output options such as disk logging.

We searched high and low for something Windows based, but it would appear that there isn't anything else out there.  [NSClient++](http://www.nsclient.org/nscp/) is great for longer polling times to publish less detailed / simpler uptime for status / alerting purpose to our [OpsView](http://http://www.opsview.com) server.  However, it's not squarely targeted at real-time performance metrics like Graphite is. (NOTE: It would appear that the NSClient++ folks are working on some sort of direct Graphite publishing code, but it's not yet live.)

So we built our own.

Designed for compatibility with a StatsD compatible listener, such as:

* [StatsD](https://github.com/etsy/statsd) - The Etsy original, built with node.js
* [statsite](https://github.com/kiip/statsite) - Built with Python

## Installation

### Requirements

* .NET Framework 4+
* Windows
* Admin rights for installing services (the service is setup to run as NETWORK SERVICE)
* Powershell v2 required to user the one-line installer

Sorry Mono, this is the Win32 only club -- besides, Linux distros already have better tools for this!

### Local Installation Script

At a PowerShell admin prompt, installation is as easy as this (long) one-liner, which will prompt for the server name and assume all other defaults.

    icm $executioncontext.InvokeCommand.NewScriptBlock((New-Object Net.WebClient).DownloadString('https://raw.github.com/Iristyle/PerfTap/master/InstallPerfTap.ps1'))

Alternatively, specify any or all of the configuration options.

    icm $executioncontext.InvokeCommand.NewScriptBlock((New-Object Net.WebClient).DownloadString('https://raw.github.com/Iristyle/PerfTap/master/InstallPerfTap.ps1')) -Args @{HostName='server.addr';Port=8125;Key='Key';SampleInterval='00:00:01';Format='StatSite';DefinitionPaths='CounterDefinitions\system.counters','CounterDefinitions\webservice.counters';CounterNames='\Processor(*)\% Processor Time';}

Or if readability is your thing:

    $config = @{
        HostName = 'server.addr'; 
        Port = 8125; 
        Key = 'Key'; 
        SampleInterval = '00:00:01'; 
        Format = 'StatSite';
        DefinitionPaths = 'CounterDefinitions\system.counters','CounterDefinitions\webservice.counters'; 
        CounterNames = '\Processor(*)\% Processor Time';
    }
    $args = @{
        ScriptBlock = $executioncontext.InvokeCommand.NewScriptBlock((new-object Net.WebClient).DownloadString('https://raw.github.com/Iristyle/PerfTap/master/InstallPerfTap.ps1'));
        ArgumentList = $config
    }
    Invoke-Command @args

Since PowerShell binds parameters passed in -ArgumentList by order, a single hash object is specified as the lone parameter.

For hash values not supplied the following defaults are used. HostName and Format are required.  Format can be either 'StatsD' or 'StatSite'

* Port - 8125
* Key - (empty)
* SampleInterval - 00:00:05
* DefinitionPaths - CounterDefinitions\system.counters
* CounterNames - (empty)

### Simple Remote Installation via WinRM

Alternatively, a WinRM based installation method is supported, so that many machines may be installed to at once.

    TODO: 



### Manual Installation

The latest binaries may simply be dropped in a folder, and installutil can be run against them.

As an administrator

    %SystemRoot%\Microsoft.NET\Framework\v4.0.30319\installutil installationdir\PerfTap.WindowsServiceHost.exe

### Configuration

A simple XML file controls what counters are enabled, how often they're sampled, and where the statistics are published to.  Paths may be absolute, relative to the current working directory of the application, or relative to the current directory of where the binaries are installed.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<configuration>
  <configSections>
    <section name="perfTapCounterSampling" type="PerfTap.Configuration.CounterSamplingConfiguration, PerfTap" />
    <section name="perfTapPublishing" type="PerfTap.Configuration.MetricPublishingConfiguration, PerfTap"/>
  </configSections>
  <perfTapCounterSampling sampleInterval="00:00:01">
    <definitionFilePaths>
      <definitionFile path="CounterDefinitions\\system.counters" />
      <!-- <definitionFile path="CounterDefinitions\\aspnet.counters" /> -->
      <!-- <definitionFile path="CounterDefinitions\\dotnet.counters" /> -->
      <!-- <definitionFile path="CounterDefinitions\\sqlserver.counters" /> -->
      <!-- <definitionFile path="CounterDefinitions\\webservice.counters" /> -->
    </definitionFilePaths>
    <!--
    <counterNames>
      <counter name="\network interface(*)\bytes total/sec" />
    </counterNames>
    -->
  </perfTapCounterSampling>
  <perfTapPublishing prefixKey="PerfTap"
                    port="8125"
                    hostName="foo.bar.com"
                    format="StatSite"
    />
</configuration>
```

#### Counter Definitions in the box

<table>
<thead><tr><td>File</td><td>Purpose</td></tr></thead>
<tr>
	<td>system.counters</td>
	<td>standard Windows counters for CPU, memory and paging, disk IO and NIC</td>
</tr>
<tr>
	<td>dotnet.counters</td>
	<td>the most critical .NET performance counters - exceptions, logical and physical threads, heap bytes, time in GC, committed bytes, pinned objects, etc.  System totals are returned, as well as stats for all managed processes, as counters are specified with wildcards.</td>
</tr>
<tr>
	<td>aspnet.counters</td>
	<td>information about requests, errors, sessions, worker processes</td>
</tr>
<tr>
	<td>sqlserver.counters</td>
	<td>the kitchen sink for things that are important to SQL server (including some overlap with system.counters) - CPU time for SQL processes, data access performance counters, memory manager, user database size and performance, buffer manager and memory performance, workload (compiles, recompiles), users, locks and latches, and some mention in the comments of red herrings.  This list of counters was heavily researched.</td>
</tr>
<tr>
	<td>webservice.counters</td>
	<td>wild card counters for current connections, isapi extension requests, total method requests and bytes</td>
</tr>
</table>

#### Extra Counter Definitions

One-off counters may be added to the configuration file as shown in the example above.  Counter files may also be created to group things together.  Blank lines and lines prefixed with the # character are ignored.

The names of all counters are combined together from all the configured files and individually specified names.  However, these names have not yet been wildcard expanded.  So, if for instance, both the name "\processor(*)\% processor time" and "\processor(_total)\% processor time" have been specified, "\processor(_total)\% processor time" will be read twice.

### Logging

NLog is used for logging, and the default configuration ships with just file logging enabled.  The logs are dumped to %ALLUSERSPROFILE%\PerfTap\logs.  Generally speaking, on modern Windows installations, this will be C:\ProgramData\PerfTap\logs.  Obviously this can be modified to do whatever you want per the NLog [documentation](http://nlog-project.org/wiki/Configuration_File).


## Implementation Details

To keep down memory utilization, and improve performance, a handful of tricks are employed.

* Win32 Pdh API calls are used for fine grained control over unmanaged resources, counter calls / lifetime.
* As counters are read, they are streamed over an infinite IEnumerable, so should be kept in scope only until they've been converted to a byte array for sending over Udp
* Metrics are grouped together into max 512 byte packets to prevent packet fragmentation
* Asynchronous sockets are used with the [SendPacketsAsync](http://msdn.microsoft.com/en-us/library/system.net.sockets.socket.sendpacketsasync.aspx) API in fire'n'forget fashion.  This API uses a simple object pool to cut down significantly on temporal object constructions, including IAsyncResult instances.

## Other Historical Notes

The first take at this application was written in PowerShell.  The thought was that there would be no binaries to ship around, it would be easy to maintain, and easy to setup as a scheduled task that ran forever.  The original rationale was reasonable, but this turned out to be an awful decision, as the end result performed horrendously.  Memory utilization grew wildly out of control over a period of hours.  It would peg the CPU roughly every second or two.  You're probably thinking this was user error, but about a half dozen variations on the PowerShell tack were taken.  Get-Counter was used with -Continous and results were streamed and transformed as they moved through the pipeline, .NET timers that started background Jobs were attempted.  Grouping together results before parsing and sending was attempted.  String parsing was rewritten from more expensive regex which created extraneous string copies, to working within StringBuilder buffers.  Ultimately PowerShell was abandoned as there wasn't enough fine-grained control of resource allocation / deallocation.

As it turned out, the PowerShell code does hide quite a bit of useful heavy lifting under [Get-Counter](http://technet.microsoft.com/en-us/library/dd367892.aspx).  Reflector'd Pcode from Microsoft.PowerShell.Commands.Diagnostics.dll (can I say that?) served as a rough inspiration for the counter reading pieces of this code.  More than anything, it served as a bit of a shortcut for understanding what Win32 API calls were required to query the local system.  The code however bears no resemblance to their original code -- their implementation was written from a very Win32 API / C++ style slant, littered with HRESULT return codes and lots of yucky out and ref parameters.  This code base use modern language features to make the code simpler / more readable / maintainable going forward.

## Future Improvements

* This probably would have been a good application to build in F# - oops.
* Configurable sample intervals for counters on an individual basis.
* Add a raw Graphite publisher that doesn't go through StatsD first
* Configuration options for packet batching
* Better configuration file error checking
* Potentially break out the ServiceChassis piece to a separate NuGet package - or integrate with what the [TopShelf](http://topshelf-project.com/) guys have done.
* Provide uninstall and upgrade (that won't modify config file) option through script

## Contributing

Fork the code, and submit a pull request!  

Any useful changes are welcomed.  If you have an idea you'd like to see implemented that strays far from the simple spirit of the application, ping us first so that we're on the same page.
