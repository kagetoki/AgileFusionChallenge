using ProcessMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            Monitor.ResourceSnapshot += (s, e) => Console.WriteLine(e);
            Monitor.Start();
            Console.ReadLine();
            Monitor.Stop();
        }
    }
}
