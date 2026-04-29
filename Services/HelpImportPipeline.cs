using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using CommandWizard.Models;

namespace CommandWizard.Services
{
    public static class HelpImportPipeline
    {
        private static readonly string[] CompletionProbeArgs =
        {
            "completion powershell",
            "completion bash",
            "completion zsh",
            "completion fish",
            "completions powershell",
            "--generate-completions powershell",
            "--completion powershell"
        };

        public static bool TryImportFromCommand(string commandName, string helpArgs, bool useAdvancedSources, out ToolSchema schema, out string error)
        {
            schema = new ToolSchema();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(commandName))
            {
                error = "Command name is required.";
                return false;
            }

            AppLogger.Info($"Capture help output: {commandName} {helpArgs}");
            var helpText = HelpSchemaImporter.CaptureHelpText(commandName, helpArgs, out error);
            if (string.IsNullOrWhiteSpace(helpText))
            {
                AppLogger.Error($"Help capture failed: {commandName} {helpArgs}", string.IsNullOrWhiteSpace(error) ? null : new InvalidOperationException(error));
                return false;
            }

            schema = HelpTextParser.Parse(commandName, helpText);
            schema.InstalledName = commandName.Trim();
            schema.ExecutablePath = HelpSchemaImporter.ResolveExecutablePath(commandName.Trim());

            if (!useAdvancedSources)
            {
                return true;
            }

            MergeCompletionData(schema, commandName);
            return true;
        }

        public static bool TryImportFromHelpText(string commandName, string helpText, bool useAdvancedSources, out ToolSchema schema, out string error)
        {
            schema = new ToolSchema();
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(commandName))
            {
                error = "Command name is required.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(helpText))
            {
                error = "Help text is required.";
                return false;
            }

            AppLogger.Info($"Parse pasted help text: {commandName} (len={helpText.Length})");
            schema = HelpTextParser.Parse(commandName, helpText);
            schema.InstalledName = commandName.Trim();
            schema.ExecutablePath = HelpSchemaImporter.ResolveExecutablePath(commandName.Trim());

            if (!useAdvancedSources)
            {
                return true;
            }

            MergeCompletionData(schema, commandName);
            return true;
        }

        public static string GetArgumentKey(SchemaArgument argument)
        {
            if (!string.IsNullOrWhiteSpace(argument.Long))
            {
                return argument.Long.Trim();
            }

            var flag = argument.Flag?.Trim() ?? string.Empty;
            return flag;
        }

        private static void MergeCompletionData(ToolSchema schema, string commandName)
        {
            var completion = CaptureCompletionScript(commandName);
            if (string.IsNullOrWhiteSpace(completion))
            {
                AppLogger.Info($"Completion probe skipped (no output): {commandName}");
                return;
            }

            ApplyCompletionText(schema, completion);
        }

        public static void ApplyCompletionText(ToolSchema schema, string completionText)
        {
            var parsed = CompletionScriptParser.Parse(completionText);
            if (parsed.Arguments.Count == 0 && parsed.Actions.Count == 0)
            {
                AppLogger.Info("Completion parse empty.");
                return;
            }

            MergeArguments(schema, parsed.Arguments);
            MergeActions(schema, parsed.Actions);
            AppLogger.Info($"Completion merge: +{parsed.Arguments.Count} args, +{parsed.Actions.Count} actions");
        }

        private static void MergeArguments(ToolSchema schema, IReadOnlyCollection<SchemaArgument> completionArgs)
        {
            var byKey = schema.Arguments.ToDictionary(GetArgumentKey, StringComparer.OrdinalIgnoreCase);

            foreach (var completionArg in completionArgs)
            {
                var key = GetArgumentKey(completionArg);
                if (string.IsNullOrWhiteSpace(key)) continue;

                if (!byKey.TryGetValue(key, out var existing))
                {
                    schema.Arguments.Add(completionArg);
                    byKey[key] = completionArg;
                    continue;
                }

                if (string.IsNullOrWhiteSpace(existing.Long) && !string.IsNullOrWhiteSpace(completionArg.Long))
                {
                    existing.Long = completionArg.Long;
                }

                if (string.IsNullOrWhiteSpace(existing.Flag) && !string.IsNullOrWhiteSpace(completionArg.Flag))
                {
                    existing.Flag = completionArg.Flag;
                }

                if (!string.IsNullOrWhiteSpace(completionArg.Description))
                {
                    if (string.IsNullOrWhiteSpace(existing.Description))
                    {
                        existing.Description = completionArg.Description;
                    }
                    else if (!string.Equals(existing.Description.Trim(), completionArg.Description.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        AppLogger.Warn($"Completion description ignored for {key}: '{completionArg.Description}'");
                    }
                }
            }
        }

        private static void MergeActions(ToolSchema schema, IReadOnlyCollection<SchemaAction> completionActions)
        {
            var byName = schema.Actions.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var completionAction in completionActions)
            {
                if (string.IsNullOrWhiteSpace(completionAction.Name)) continue;
                if (!byName.TryGetValue(completionAction.Name, out var existing))
                {
                    schema.Actions.Add(completionAction);
                    byName[completionAction.Name] = completionAction;
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(completionAction.Description))
                {
                    if (string.IsNullOrWhiteSpace(existing.Description))
                    {
                        existing.Description = completionAction.Description;
                    }
                    else if (!string.Equals(existing.Description.Trim(), completionAction.Description.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        AppLogger.Warn($"Completion action description ignored for {completionAction.Name}: '{completionAction.Description}'");
                    }
                }
            }
        }

        private static string? CaptureCompletionScript(string commandName)
        {
            foreach (var args in CompletionProbeArgs)
            {
                AppLogger.Info($"Completion probe: {commandName} {args}");
                var output = RunProcess(commandName, args, out var error);
                if (!string.IsNullOrWhiteSpace(output))
                {
                    AppLogger.Info($"Completion probe success: {commandName} {args}");
                    return output;
                }

                if (!string.IsNullOrWhiteSpace(error))
                {
                    AppLogger.Warn($"Completion probe failed: {commandName} {args} | {error}");
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

                if (!process.WaitForExit(4000))
                {
                    try { process.Kill(entireProcessTree: true); } catch { }
                    error = "Completion command timed out.";
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
                    error = $"Completion output was empty for '{commandName} {args}'.";
                }

                return output;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                AppLogger.Error($"Completion capture exception: {commandName} {args}", ex);
                return null;
            }
        }
    }
}
