using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WyzeSenseApp
{
    public class Logger: ILogger
    {
        public IDisposable BeginScope<TState>(TState state)
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            throw new NotImplementedException();
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (logLevel == LogLevel.Trace) return;

            Console.WriteLine("[{0}]({1}) - {2}", logLevel.ToString().PadRight(11, ' '), DateTime.Now, state);
            if(exception != null)
                Console.WriteLine(exception.ToString());
        }
    }
}
