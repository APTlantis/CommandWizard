using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CommandWizard.Models;

namespace CommandWizard.Services
{
    public static class HelpTextParser
    {
        private static readonly Regex FlagLine = new(@"^\s*(?<flags>(?:[-/][^\s]+(?:\s*,\s*[-/][^\s]+)*)(?:\s+[-/][^\s]+(?:\s*,\s*[-/][^\s]+)*)*)\s+(?<desc>.+)$", RegexOptions.Compiled);
        private static readonly Regex SectionHeader = new(@"^\s*(?<header>[\w\s]+):\s*$", RegexOptions.Compiled);
        private static readonly Regex ActionLine = new(@"^\s*(?<cmds>[\w-]+(?:\s*,\s*[\w-]+)*)\s+(?<desc>.+)$", RegexOptions.Compiled);
        private static readonly Regex GitCommandsIntro = new(@"common\s+git\s+commands", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly HashSet<string> CommandSectionHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "commands",
            "subcommands"
        };
        private static readonly HashSet<string> OptionSectionHeaders = new(StringComparer.OrdinalIgnoreCase)
        {
            "options",
            "global options",
            "flags",
            "global flags",
            "arguments"
        };

        public static ToolSchema Parse(string commandName, string helpText)
        {
            var schema = new ToolSchema
            {
                Name = string.IsNullOrWhiteSpace(commandName) ? "tool" : commandName.Trim(),
                Description = "",
                InstalledName = commandName.Trim()
            };

            var lines = helpText
                .Split(new[] { "\r\n", "\n" }, StringSplitOptions.None)
                .Select(line => line.TrimEnd())
                .ToList();

            schema.Description = InferDescription(lines);

            var actions = ParseActions(lines);
            foreach (var action in actions)
            {
                schema.Actions.Add(action);
            }

            var parameters = ParseParametersFromUsage(commandName, lines, actions);
            foreach (var parameter in parameters)
            {
                schema.Parameters.Add(parameter);
            }

            var arguments = ParseArguments(lines);
            foreach (var argument in arguments)
            {
                schema.Arguments.Add(argument);
            }

            return schema;
        }

        private static string InferDescription(IReadOnlyList<string> lines)
        {
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (string.IsNullOrWhiteSpace(trimmed)) continue;
                if (trimmed.StartsWith("Usage", StringComparison.OrdinalIgnoreCase)) continue;
                if (trimmed.EndsWith(":", StringComparison.Ordinal)) continue;
                return trimmed;
            }

            return string.Empty;
        }

        private static IEnumerable<SchemaAction> ParseActions(IReadOnlyList<string> lines)
        {
            var actions = new List<SchemaAction>();
            var inCommands = false;

            foreach (var raw in lines)
            {
                var line = raw.TrimEnd();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                if (IsCommandsIntro(line))
                {
                    inCommands = true;
                    continue;
                }

                if (!inCommands && GitCommandsIntro.IsMatch(line))
                {
                    inCommands = true;
                    continue;
                }

                if (inCommands && IsOptionsIntro(line))
                {
                    inCommands = false;
                    continue;
                }

                var section = SectionHeader.Match(line);
                if (section.Success)
                {
                    var header = section.Groups["header"].Value.Trim().ToLowerInvariant();
                    if (CommandSectionHeaders.Contains(header))
                    {
                        inCommands = true;
                        continue;
                    }

                    if (inCommands && OptionSectionHeaders.Contains(header))
                    {
                        inCommands = false;
                    }
                    continue;
                }

                if (!inCommands) continue;

                var trimmed = line.Trim();
                if (trimmed.StartsWith("-", StringComparison.Ordinal)) continue;
                if (trimmed.StartsWith("usage:", StringComparison.OrdinalIgnoreCase)) continue;

                var actionMatch = ActionLine.Match(trimmed);
                if (!actionMatch.Success)
                {
                    AppLogger.Warn($"Unparsed command line: '{trimmed}'");
                    var parts = trimmed.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 0) continue;

                    var fallbackName = parts[0].Trim().TrimEnd(',');
                    if (string.IsNullOrWhiteSpace(fallbackName)) continue;

                    var fallbackDescription = trimmed.Length > fallbackName.Length
                        ? trimmed.Substring(fallbackName.Length).Trim()
                        : string.Empty;

                    actions.Add(new SchemaAction
                    {
                        Name = fallbackName,
                        Description = fallbackDescription
                    });
                    continue;
                }

                var cmds = actionMatch.Groups["cmds"].Value;
                var description = actionMatch.Groups["desc"].Value.Trim();
                var aliases = cmds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
                if (aliases.Count == 0) continue;

                var name = aliases[0];
                if (aliases.Count > 1)
                {
                    var aliasText = string.Join(", ", aliases.Skip(1));
                    description = string.IsNullOrWhiteSpace(description)
                        ? $"Aliases: {aliasText}"
                        : $"{description} (Aliases: {aliasText})";
                }

                actions.Add(new SchemaAction
                {
                    Name = name,
                    Description = description
                });
            }

            return actions;
        }

        private static bool IsCommandsIntro(string line)
        {
            var trimmed = line.Trim();
            return trimmed.Contains("commands are available", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.Contains("available commands", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("Commands:", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOptionsIntro(string line)
        {
            var trimmed = line.Trim();
            return trimmed.Contains("options are available", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("Options:", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("Global Options:", StringComparison.OrdinalIgnoreCase) ||
                   trimmed.StartsWith("Flags:", StringComparison.OrdinalIgnoreCase);
        }

        private static IEnumerable<SchemaArgument> ParseArguments(IReadOnlyList<string> lines)
        {
            var arguments = new List<SchemaArgument>();
            var byKey = new Dictionary<string, SchemaArgument>(StringComparer.OrdinalIgnoreCase);
            SchemaArgument? lastArgument = null;

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    lastArgument = null;
                    continue;
                }

                if (SectionHeader.IsMatch(line))
                {
                    lastArgument = null;
                    continue;
                }

                var match = FlagLine.Match(line);
                if (!match.Success)
                {
                    if (lastArgument != null && IsIndented(line))
                    {
                        var continuation = line.Trim();
                        if (!string.IsNullOrWhiteSpace(continuation))
                        {
                            lastArgument.Description = string.IsNullOrWhiteSpace(lastArgument.Description)
                                ? continuation
                                : $"{lastArgument.Description} {continuation}";
                        }
                    }
                    else if (line.TrimStart().StartsWith("-", StringComparison.Ordinal) || line.TrimStart().StartsWith("/", StringComparison.Ordinal))
                    {
                        AppLogger.Warn($"Unparsed option line: '{line.Trim()}'");
                    }
                    continue;
                }

                var flagsPart = match.Groups["flags"].Value;
                var description = match.Groups["desc"].Value.Trim();
                var flagTokens = flagsPart
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .SelectMany(token => token.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Select(f => f.Trim())
                    .Where(f => !string.IsNullOrWhiteSpace(f))
                    .ToList();

                var shortFlag = flagTokens.FirstOrDefault(f => f.StartsWith("-", StringComparison.Ordinal) && !f.StartsWith("--", StringComparison.Ordinal));
                var longFlag = flagTokens.FirstOrDefault(f => f.StartsWith("--", StringComparison.Ordinal));

                var primary = longFlag ?? shortFlag ?? flagTokens.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(primary)) continue;

                var type = InferArgumentType(flagTokens);
                var key = longFlag ?? primary;

                if (byKey.TryGetValue(key, out var existing))
                {
                    if (string.IsNullOrWhiteSpace(existing.Long) && !string.IsNullOrWhiteSpace(longFlag))
                    {
                        existing.Long = longFlag;
                    }

                    if (string.IsNullOrWhiteSpace(existing.Flag) && !string.IsNullOrWhiteSpace(shortFlag))
                    {
                        existing.Flag = shortFlag;
                    }

                    if (!string.IsNullOrWhiteSpace(description) &&
                        (string.IsNullOrWhiteSpace(existing.Description) || description.Length > existing.Description.Length))
                    {
                        existing.Description = description;
                    }

                    lastArgument = existing;
                    continue;
                }

                var argument = new SchemaArgument
                {
                    Flag = shortFlag ?? longFlag ?? primary,
                    Long = shortFlag != null && longFlag != null ? longFlag : "",
                    Description = description,
                    Type = type
                };

                byKey[key] = argument;
                arguments.Add(argument);
                lastArgument = argument;
            }

            return arguments;
        }

        private static string InferArgumentType(IEnumerable<string> flags)
        {
            foreach (var flag in flags)
            {
                if (flag.Contains("<", StringComparison.Ordinal) ||
                    flag.Contains("=", StringComparison.Ordinal) ||
                    flag.Contains("[", StringComparison.Ordinal))
                {
                    return "string";
                }
            }

            return "boolean";
        }

        private static IEnumerable<SchemaParameter> ParseParametersFromUsage(string commandName, IReadOnlyList<string> lines, IEnumerable<SchemaAction> actions)
        {
            var parameters = new List<SchemaParameter>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var actionNames = new HashSet<string>(actions.Select(action => action.Name), StringComparer.OrdinalIgnoreCase);
            var inUsageBlock = false;

            foreach (var raw in lines)
            {
                if (string.IsNullOrWhiteSpace(raw))
                {
                    inUsageBlock = false;
                    continue;
                }

                var trimmed = raw.TrimStart();
                if (trimmed.StartsWith("usage:", StringComparison.OrdinalIgnoreCase))
                {
                    inUsageBlock = true;
                    var usageText = trimmed.Substring("usage:".Length).Trim();
                    ExtractUsageTokens(commandName, usageText, parameters, seen, actionNames);
                    continue;
                }

                if (inUsageBlock && IsIndented(raw) && !SectionHeader.IsMatch(raw))
                {
                    ExtractUsageTokens(commandName, trimmed, parameters, seen, actionNames);
                    continue;
                }

                inUsageBlock = false;
            }

            return parameters;
        }

        private static void ExtractUsageTokens(string commandName, string usageText, ICollection<SchemaParameter> parameters, ISet<string> seen, ISet<string> actionNames)
        {
            var tokens = TokenizeUsage(usageText);
            foreach (var token in tokens)
            {
                var name = token.Text;
                if (string.IsNullOrWhiteSpace(name)) continue;
                if (name.StartsWith("-", StringComparison.Ordinal)) continue;
                if (string.Equals(name, "options", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name, "option", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(commandName) &&
                    string.Equals(name, commandName, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (actionNames.Contains(name))
                {
                    continue;
                }

                if (seen.Contains(name)) continue;
                seen.Add(name);

                parameters.Add(new SchemaParameter
                {
                    Name = name,
                    Type = "string",
                    Required = !token.IsOptional
                });
            }
        }

        private static IEnumerable<UsageToken> TokenizeUsage(string usageText)
        {
            var tokens = new List<UsageToken>();
            if (string.IsNullOrWhiteSpace(usageText)) return tokens;

            var current = string.Empty;
            var bracketDepth = 0;

            void FlushToken(bool? optionalOverride = null)
            {
                if (string.IsNullOrWhiteSpace(current)) return;
                var raw = current;
                current = string.Empty;

                foreach (var part in raw.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var cleaned = part.Trim().Trim('[', ']', '<', '>', '(', ')', ',', ';');
                    if (string.IsNullOrWhiteSpace(cleaned)) continue;
                    var isOptional = optionalOverride ?? (bracketDepth > 0);
                    tokens.Add(new UsageToken(cleaned, isOptional));
                }
            }

            foreach (var ch in usageText)
            {
                if (char.IsWhiteSpace(ch))
                {
                    FlushToken();
                    continue;
                }

                if (ch == '[')
                {
                    bracketDepth++;
                    continue;
                }

                if (ch == ']')
                {
                    if (!string.IsNullOrWhiteSpace(current))
                    {
                        FlushToken(true);
                    }
                    bracketDepth = Math.Max(0, bracketDepth - 1);
                    continue;
                }

                current += ch;
            }

            FlushToken();
            return tokens;
        }

        private static bool IsIndented(string line)
        {
            return !string.IsNullOrEmpty(line) && char.IsWhiteSpace(line[0]);
        }

        private readonly record struct UsageToken(string Text, bool IsOptional);
    }
}
