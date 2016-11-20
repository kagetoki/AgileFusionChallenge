using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualBasic.Devices;
using ThreadUtils;

namespace ProcessMonitor
{
    public static class Monitor
    {
        public static event EventHandler<ResourceUsageEventArgs> HighLoadHappend;
        public static event EventHandler<ResourceUsageEventArgs> ResourceSnapshot;
        public static SystemResourceConsumptionModel Current { get; private set; }
        public static ComputerInfo ComputerInfo = new ComputerInfo();
        public static bool IsRunning { get { return _worker.IsRunning; } }
        private static double CpuThreshold = 80d;
        private static long RamThreshold = (long)(ComputerInfo.TotalPhysicalMemory * 0.8);
        private static AsyncIterationWorker<SystemResourceConsumptionModel> _worker = new AsyncIterationWorker<SystemResourceConsumptionModel>(GatherInfo, 1000);

        public static IList<ProcessModel> GetProcesses()
        {
            var processes = Process.GetProcesses();
            var result = new List<ProcessModel>(processes.Length);
            try
            {
                foreach (var process in processes)
                {
                    using (process)
                    using (var perfomanceCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true))
                    {
                        perfomanceCounter.NextValue();
                        var model = new ProcessModel
                        {
                            Name = process.ProcessName,
                            MemoryConsumption = process.WorkingSet64,
                            PID = process.Id,
                            CpuUsage = perfomanceCounter.NextValue()
                        };
                        result.Add(model);
                    }
                }

                return result;
            }
            finally
            {
                if (processes != null)
                {
                    foreach (var process in processes)
                    {
                        process?.Dispose();
                    }
                }
            }
        }

        public static SystemResourceConsumptionModel GetSystemResourceConsumption()
        {
            var result = new SystemResourceConsumptionModel();
            result.Processes = GetProcesses();
            using (var cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true))
            {
                cpuCounter.NextValue();
                var value = cpuCounter.NextValue();
                result.CpuUsage = value;
                result.RamUsage = result.Processes.Sum(p => p.MemoryConsumption);
            }
            
            return result;
        }

        public static void Start()
        {
            if (!_worker.IsStarted)
            {
                _worker.IterationCompleted += (s, e) => { Current = e.Result; };
            }
            _worker.Start();
        }
        
        private static SystemResourceConsumptionModel GatherInfo()
        {
            var result = GetSystemResourceConsumption();
            var eventArgs = new ResourceUsageEventArgs(result.CpuUsage, result.RamUsage);
            ResourceSnapshot?.Invoke(null, eventArgs);
            if (result.CpuUsage >= CpuThreshold || result.RamUsage >= RamThreshold)
            {
                HighLoadHappend?.Invoke(null, eventArgs);
            }
            return result;
        }
        public static void Stop()
        {
            _worker.Stop();
        }
    }
}
