using System;

namespace WyzeSense
{
    public static class Logger
    {
        public static void Error(string message) => Write(message, "Error");
        public static void Info(string message) => Write(message, "Info");
        public static void Debug(string message) => Write(message, "Debug");
        public static void Trace(string message) => Write(message, "Trace");

        private static void Write(string message, string type)
        {
            Console.WriteLine("[{2}]{0} - {1}", type, message, DateTime.Now);
        }
    }
}