namespace ProcessMonitor
{
    public class ProcessModel
    {
        public string Name { get; set; }
        public long MemoryConsumption { get; set; }
        public double CpuUsage { get; set; }
        public int PID { get; set; }
        public override string ToString()
        {
            return $"{Name} | {PID} | Memory used: {MemoryConsumption} | CPU used: {CpuUsage} |";
        }
    }
}
