using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessMonitor
{
    public class SystemResourceConsumptionModel
    {
        public IList<ProcessModel> Processes { get; set; }
        public double CpuUsage { get; set; }
        public long RamUsage { get; set; }
    }
}
