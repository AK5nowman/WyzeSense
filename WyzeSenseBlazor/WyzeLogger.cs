using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WyzeSenseBlazor
{
    public class WyzeLogger : WyzeSenseCore.IWyzeSenseLogger
    {
        ILogger _logger;
        public WyzeLogger(ILogger<WyzeLogger> Logger)
        {
            _logger = Logger;
        }
        public void Log(string level, string message)
        {
            switch(level)
            {
                case "Debug": _logger.LogDebug(message); break;
                case "Error": _logger.LogError(message); break;
                case "Trace": _logger.LogTrace(message); break;
                case "Warning": _logger.LogWarning(message); break;
                case "Info":
                case "Information":
                default:
                    _logger.LogInformation(message); break;
            }
        }

        public void LogDebug(string message)
        {
            Log("Debug", message);
        }

        public void LogError(string message)
        {
            Log("Error", message);
        }

        public void LogInformation(string message)
        {
            Log("Information", message);
        }

        public void LogTrace(string message)
        {
            Log("Trace", message);
        }

        public void LogWarning(string message)
        {
            Log("Warning", message);
        }
    }
}
