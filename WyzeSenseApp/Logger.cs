using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseApp
{
    public class Logger: WyzeSenseCore.IWyzeSenseLogger
    {
        public void Log(string level, string message)
        {
            Console.WriteLine("[{0}] {1} {2}", level, DateTime.Now, message);
        }
        public void LogTrace(string message) => Log("Trace", message);
        public void LogInformation(string message) => Log("Information", message);
        public void LogDebug(string message) => Log("Debug", message);
        public void LogWarning(string message) => Log("Warning", message);
        public void LogError(string message) => Log("Error", message);


    }
}
