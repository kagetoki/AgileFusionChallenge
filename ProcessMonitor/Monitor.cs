using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualBasic.Devices;
using ThreadUtils;
using System.Collections.Concurrent;

namespace ProcessMonitor
{
    public static class Monitor
    {
        private static object _sync = new object();
        public static event EventHandler<ResourceUsageEventArgs> HighLoadHappend;
        public static event EventHandler<ResourceUsageEventArgs> ResourceSnapshot;
        public static event Action Started;
        public static event Action Stopped;
        public static SystemResourceConsumptionModel Current { get; private set; }
        public static ComputerInfo ComputerInfo = new ComputerInfo();
        public static bool IsRunning { get { return _worker.IsRunning; } }
        private static double CpuThreshold = 80d;
        private static long RamThreshold = (long)(ComputerInfo.TotalPhysicalMemory * 0.8);
        private static AsyncIterationWorker<SystemResourceConsumptionModel> _worker = 
            new AsyncIterationWorker<SystemResourceConsumptionModel>(GatherInfo, 1000);
        private static PerformanceCounter _totalResourceCounter;
        private static IDictionary<int, PerformanceCounter> _perfomanceCounters = new ConcurrentDictionary<int, PerformanceCounter>();
        public static IList<ProcessModel> GetProcesses()
        {
            var processes = Process.GetProcesses();
            var result = new List<ProcessModel>(processes.Length);
            try
            {
                foreach (var process in processes)
                {
                    try
                    {
                        using (process)                        
                        {
                            PerformanceCounter perfomanceCounter;
                            if (!_perfomanceCounters.TryGetValue(process.Id, out perfomanceCounter))
                            {
                                perfomanceCounter = new PerformanceCounter("Process", "% Processor Time", process.ProcessName, true);
                                _perfomanceCounters.Add(process.Id, perfomanceCounter);
                            }
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
                    catch (InvalidOperationException)
                    {
                        Stop();
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
            if (!IsRunning) { return Current; }
            var result = new SystemResourceConsumptionModel();
            result.Processes = GetProcesses();

            var value = _totalResourceCounter.NextValue();
            result.CpuUsage = value;
            result.RamUsage = result.Processes.Sum(p => p.MemoryConsumption);

            return result;
        }

        public static void Start()
        {
            lock (_sync)
            {
                if (!_worker.IsStarted)
                {
                    _worker.IterationCompleted += (s, e) => { Current = e.Result; };
                    _totalResourceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total", true);
                }
                _worker.Start();
                Started?.Invoke();
            }
        }
        
        private static SystemResourceConsumptionModel GatherInfo()
        {
            var result = GetSystemResourceConsumption();
            var eventArgs = new ResourceUsageEventArgs(result.CpuUsage, result.RamUsage, result.Processes);
            ResourceSnapshot?.Invoke(null, eventArgs);
            if (result.CpuUsage >= CpuThreshold || result.RamUsage >= RamThreshold)
            {
                HighLoadHappend?.Invoke(null, eventArgs);
            }
            return result;
        }
        public static void Stop()
        {
            lock (_sync)
            {
                try
                {
                    _worker.Stop();
                    Stopped?.Invoke();
                }
                finally
                {
                    if (_totalResourceCounter != null)
                    {
                        _totalResourceCounter.Dispose();
                        _totalResourceCounter = null;
                    }
                    foreach(var counter in _perfomanceCounters.Values)
                    {
                        counter.Dispose();
                    }
                    _perfomanceCounters.Clear();
                }
            }
        }
    }
}
