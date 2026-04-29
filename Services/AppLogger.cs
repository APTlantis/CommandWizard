using System;
using System.IO;

namespace CommandWizard.Services
{
    public static class AppLogger
    {
        private static readonly object Sync = new();

        public static string LogPath => Path.Combine(AppPaths.ResolveDataRoot(), "commandwizard.log");

        public static void Info(string message) => Write("INFO", message, null);

        public static void Warn(string message) => Write("WARN", message, null);

        public static void Error(string message, Exception? ex = null) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception? ex)
        {
            try
            {
                var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{level}] {message}";
                if (ex != null)
                {
                    line += $" | {ex.GetType().Name}: {ex.Message}";
                }

                lock (Sync)
                {
                    File.AppendAllText(LogPath, line + Environment.NewLine);
                }
            }
            catch
            {
                // Logging should never crash the app.
            }
        }
    }
}
