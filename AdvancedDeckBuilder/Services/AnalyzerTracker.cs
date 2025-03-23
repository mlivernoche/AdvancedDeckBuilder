using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AdvancedDeckBuilder.Services
{
    public class AnalyzerTracker : IAnalyzerTracker
    {
        private HashSet<int> ProcessIds { get; } = new HashSet<int>();

        public void AddProcess(int processId)
        {
            ProcessIds.Add(processId);
        }

        public void CloseAllProcesses()
        {
            var processes = ProcessIds.ToArray();
            foreach(var processId in processes)
            {
                ProcessIds.Remove(processId);

                try
                {
                    var process = Process.GetProcessById(processId);
                    process.Kill();
                }
                catch { }
            }
        }

        public void RemoveProcess(int processId)
        {
            ProcessIds.Remove(processId);
        }
    }
}
