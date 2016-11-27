using System;
using System.Collections.Generic;

namespace ProcessMonitor
{
    public class ResourceUsageEventArgs
    {
        public double CpuUsage { get; private set; }
        public long RamUsage { get; private set; }
        public string MachineName { get; private set; }
        public IList<ProcessModel> Processes { get; private set; }
        public ResourceUsageEventArgs(double cpuUsage, long ramUsage, IList<ProcessModel> processes = null, string machineName = null)
        {
            CpuUsage = cpuUsage;
            RamUsage = ramUsage;
            MachineName = machineName ?? Environment.MachineName;
            Processes = processes;
        }

        public override string ToString()
        {
            return $"Machine name: {MachineName} | Memory used: {RamUsage} | CPU used: {CpuUsage} |";
        }
    }
}
