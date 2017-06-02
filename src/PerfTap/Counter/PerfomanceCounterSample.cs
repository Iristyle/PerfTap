// -----------------------------------------------------------------------
// <copyright file="PerfomanceCounterSample.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PerfTap.Counter
{
    public class PerformanceCounterSample
    {
        public PerformanceCounterSample(string path, string instanceName, double cookedValue, ulong rawValue, ulong secondValue, uint multiCount, PerformanceCounterType counterType, uint defaultScale, ulong timeBase, DateTime timeStamp, ulong timeStamp100nSec, uint status)
        {
            this.Path = path;
            this.InstanceName = instanceName;
            this.CookedValue = cookedValue;
            this.RawValue = rawValue;
            this.SecondValue = secondValue;
            this.MultipleCount = multiCount;
            this.CounterType = counterType;
            this.DefaultScale = defaultScale;
            this.TimeBase = timeBase;
            this.Timestamp = timeStamp;
            this.Timestamp100NSec = timeStamp100nSec;
            this.Status = status;
        }

        public double CookedValue { get; private set; }
        public PerformanceCounterType CounterType { get; private set; }
        public uint DefaultScale { get; private set; }
        public string InstanceName { get; private set; }
        public uint MultipleCount { get; private set; }
        public string Path { get; private set; }
        public ulong RawValue { get; private set; }
        public ulong SecondValue { get; private set; }
        public uint Status { get; private set; }
        public ulong TimeBase { get; private set; }
        public DateTime Timestamp { get; private set; }
        public ulong Timestamp100NSec { get; private set; }
    }
}