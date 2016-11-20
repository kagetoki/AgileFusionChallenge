using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ProcessMonitor;
using System.Linq;
using System.Threading;
using Monitor = ProcessMonitor.Monitor;

namespace ProcessMonitorTests
{
    [TestClass]
    public class MonitorTests
    {
        [TestMethod]
        public void ShouldGetAllProcesses()
        {
            var infos = Monitor.GetProcesses();
            foreach(var pi in infos)
            {
                Console.WriteLine(pi.ToString());
            }
        }

        [TestMethod]
        public void ShoudGetTotalInfo()
        {
            var totalInfo = Monitor.GetSystemResourceConsumption();
            Console.WriteLine($"CPU usage: {totalInfo.CpuUsage}; RAM usage: {totalInfo.RamUsage}");
            var notZeroCpu = totalInfo.Processes.Where(p => p.CpuUsage != 0);
            foreach(var p in notZeroCpu)
            {
                Console.WriteLine(p);
            }
        }

        [TestMethod]
        public void ShouldStartAndStop()
        {
            Monitor.ResourceSnapshot += (s, e) => Console.WriteLine(e);
            Monitor.Start();
            Thread.Sleep(10 * 1000);
            Monitor.Stop();
        }
    }
}
