using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseCore
{
    public interface IWyzeSenseLogger
    {
        public void Log(string level, string message);
        public void LogTrace(string message);
        public void LogDebug(string message);
        public void LogInformation(string message);
        public void LogWarning(string message);
        public void LogError(string message);
    }
}
