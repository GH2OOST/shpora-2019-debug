using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Top
{
    public class MetricsCollector
    {
        public event EventHandler<MetricsCollection> MetricsAvailable = delegate { };

        private Dictionary<int, long> times = new Dictionary<int, long>();
    
        public void Run(TimeSpan period)
        {   
            while (true)
            {
                Collect(period);
                Thread.Sleep(period);
            }
        }
    
        private unsafe void Collect(TimeSpan period)
        {
            var processes = new List<ProcessMetrics>();
            var newTimes = new Dictionary<int, long>();
            

            NtUtility.VisitProcesses(info => Visit(info, newTimes, processes, period));

            times = newTimes;
            
            var metricsCollection = new MetricsCollection
            {
                ProcessMetricsCollection = processes.ToArray(),
                Timestamp = DateTime.UtcNow,
                MachineName = Environment.MachineName
            };
            Task.Run(() => MetricsAvailable(this, metricsCollection));
        }

        private unsafe void Visit(SystemProcessInformation* info, Dictionary<int, long> newTimes, List<ProcessMetrics> processes, TimeSpan period)
        {
            var pid = (int)info->UniqueProcessId;
            var currentTimes = info->KernelTime + info->UserTime;
            var prevTimes = times.TryGetValue(pid, out var t) ? t : currentTimes;
            newTimes[pid] = currentTimes;

            processes.Add(new ProcessMetrics
            {
                BasePriority = info->BasePriority,
                HandlesCount = (int)info->HandleCount,
                Name = GetProcessName(info->NamePtr),
                PeakPrivateBytes = (long)info->PeakPagefileUsage,
                WorkingSet = (long)info->WorkingSetSize,
                PrivateBytes = (long)info->PrivatePageCount,
                ProcessId = (int)info->UniqueProcessId,
                SessionId = (int)info->SessionId,
                PeakWorkingSet = (long)info->PeakWorkingSetSize,
                ThreadsCount = (int)info->NumberOfThreads,
                Cpu = (currentTimes - prevTimes) / (double)period.Ticks / Environment.ProcessorCount
            });
        }

        private static unsafe string GetProcessName(IntPtr namePtr)
        {
            var name = new string((char*) namePtr);
            return !string.IsNullOrEmpty(name) ? name : "System Idle Process";
        }
    }
}