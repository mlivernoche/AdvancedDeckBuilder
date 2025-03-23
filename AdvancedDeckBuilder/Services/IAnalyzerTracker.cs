using System;
using System.Text;
using System.Threading.Tasks;

namespace AdvancedDeckBuilder.Services
{
    public interface IAnalyzerTracker
    {
        void AddProcess(int processId);
        void RemoveProcess(int processId);
        void CloseAllProcesses();
    }
}
