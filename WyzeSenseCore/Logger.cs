using System;

namespace WyzeSenseCore
{
    public static class Logger
    {
        public static uint LogLevel = 4;
        public static void Error(string message) => Write(message, "Error");
        public static void Info(string message) { if (LogLevel > 0) Write(message, "Info"); }
        public static void Debug(string message) { if (LogLevel > 1) Write(message, "Debug"); }
        public static void Trace(string message) { if(LogLevel > 2) Write(message, "Trace"); }

        private static void Write(string message, string type)
        {
            Console.WriteLine("[{2}]{0} - {1}", type, message, DateTime.Now);
        }
    }
}