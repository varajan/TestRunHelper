using System;
using System.IO;

namespace TestRunHelper.Helpers
{
    public static class Logger
    {
        public static void Info(string message) => Log("info", message);
        public static void Error(string message) => Log("error", message);
        public static void Error(Exception exception) => Log("error", exception.Message);

        private static void Log(string type, string message)
        {
            File.AppendAllText("log.txt", $@"{DateTime.Now:yyyy-MM-dd hh:mm:ss.fff} - {type.ToUpper()} - {message}{Environment.NewLine}");
        }
    }
}
