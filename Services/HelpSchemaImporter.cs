using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CommandWizard.Models;

namespace CommandWizard.Services
{
    public static class HelpSchemaImporter
    {
        public static bool TryImportFromCommand(string commandName, string helpArgs, out ToolSchema schema, out string error)
        {
            return TryImportFromCommand(commandName, helpArgs, useAdvancedSources: false, out schema, out error);
        }

        public static bool TryImportFromCommand(string commandName, string helpArgs, bool useAdvancedSources, out ToolSchema schema, out string error)
        {
            return HelpImportPipeline.TryImportFromCommand(commandName, helpArgs, useAdvancedSources, out schema, out error);
        }

        public static bool TryImportFromHelpText(string commandName, string helpText, out ToolSchema schema, out string error)
        {
            return TryImportFromHelpText(commandName, helpText, useAdvancedSources: false, out schema, out error);
        }

        public static bool TryImportFromHelpText(string commandName, string helpText, bool useAdvancedSources, out ToolSchema schema, out string error)
        {
            return HelpImportPipeline.TryImportFromHelpText(commandName, helpText, useAdvancedSources, out schema, out error);
        }

        internal static string? CaptureHelpText(string commandName, string helpArgs, out string error)
        {
            error = string.Empty;
            var args = string.IsNullOrWhiteSpace(helpArgs) ? "--help" : helpArgs;
            var text = RunProcess(commandName, args, out error);
            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            if (!string.IsNullOrWhiteSpace(helpArgs))
            {
                return null;
            }

            var fallbackArgs = new[] { "-h", "/?" };
            foreach (var fallback in fallbackArgs)
            {
                text = RunProcess(commandName, fallback, out error);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return text;
                }
            }

            return null;
        }

        private static string? RunProcess(string commandName, string args, out string error)
        {
            error = string.Empty;
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = commandName,
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    error = "Failed to start process.";
                    return null;
                }

                var stdOut = process.StandardOutput.ReadToEnd();
                var stdErr = process.StandardError.ReadToEnd();

                if (!process.WaitForExit(8000))
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    error = "Help command timed out.";
                    return null;
                }

                var combined = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(stdOut))
                {
                    combined.AppendLine(stdOut.Trim());
                }
                if (!string.IsNullOrWhiteSpace(stdErr))
                {
                    if (combined.Length > 0) combined.AppendLine();
                    combined.AppendLine(stdErr.Trim());
                }

                var output = combined.ToString().Trim();
                if (string.IsNullOrWhiteSpace(output))
                {
                    error = $"Help output was empty for '{commandName} {args}'.";
                }

                return output;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                AppLogger.Error($"Help capture exception: {commandName} {args}", ex);
                return null;
            }
        }

        internal static string ResolveExecutablePath(string commandName)
        {
            if (File.Exists(commandName))
            {
                return Path.GetFullPath(commandName);
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = "where.exe",
                    Arguments = commandName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return string.Empty;
                }

                var output = process.StandardOutput.ReadToEnd();
                process.WaitForExit(3000);

                var first = output
                    .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .FirstOrDefault();
                return first ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
