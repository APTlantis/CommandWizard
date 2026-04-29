using System;
using System.IO;
using System.Linq;
using CommandWizard.Models;
using CommandWizard.Services;
using CommandWizard.ViewModels;
using Xunit;

namespace CommandWizard.Tests
{
    public class SchemaAndCommandTests
    {
        [Fact]
        public void ParseSchema_ValidToml_LoadsTool()
        {
            var toml = """
            [tool]
            name = "rsync"
            description = "Fast file synchronization tool"

            [[arguments]]
            flag = "-a"
            long = "--archive"
            description = "archive mode"
            type = "boolean"

            [[parameters]]
            name = "source"
            type = "path"
            """;

            var schema = SchemaSerialization.Parse(toml);

            Assert.Equal("rsync", schema.Name);
            Assert.Single(schema.Arguments);
            Assert.Single(schema.Parameters);
        }

        [Fact]
        public void ParseSchema_InvalidToml_Throws()
        {
            var toml = """
            [tool]
            name = "rsync"
            [[arguments
            """;

            Assert.Throws<InvalidOperationException>(() => SchemaSerialization.Parse(toml));
        }

        [Fact]
        public void BuildCommand_UsesCanonicalOrder()
        {
            var schema = new ToolSchema
            {
                Name = "git",
                InstalledName = "git-local"
            };
            schema.Actions.Add(new SchemaAction { Name = "clone" });
            schema.Arguments.Add(new SchemaArgument { Flag = "--depth", Type = "string" });
            schema.Arguments.Add(new SchemaArgument { Flag = "--quiet", Type = "boolean" });
            schema.Parameters.Add(new SchemaParameter { Name = "repo" });

            var toolVm = new ToolSchemaViewModel(schema);
            toolVm.Options[0].Value = "1";
            toolVm.Options[1].IsSelected = true;
            toolVm.Parameters[0].Value = "https://example.com/repo.git";

            var command = CommandBuilder.BuildCommand(toolVm, schema.Actions[0]);

            Assert.Equal("git-local clone --depth 1 --quiet https://example.com/repo.git", command);
        }

        [Fact]
        public void OptionToggle_UpdatesPreview()
        {
            var schema = new ToolSchema { Name = "rsync" };
            schema.Arguments.Add(new SchemaArgument { Long = "--archive", Type = "boolean" });

            var vm = new MainViewModel(new[] { schema });
            var option = vm.SelectedTool?.Options[0];
            Assert.NotNull(option);

            Assert.Equal("rsync", vm.CommandPreview);
            option!.IsSelected = true;

            Assert.Equal("rsync --archive", vm.CommandPreview);
        }

        [Fact]
        public void HelpImport_Winget_ParsesActionsAndOptions()
        {
            var helpFile = @"A:\AptWeb\zypper-operations\Archive-Hasher\HelpLists.txt";
            Assert.True(File.Exists(helpFile));

            var content = File.ReadAllText(helpFile);
            var helpText = ExtractHelpSection(content, "winget --help");
            var schema = HelpTextParser.Parse("winget", helpText);

            Assert.Contains(schema.Actions, action => action.Name == "install");
            Assert.Contains(schema.Actions, action => action.Name == "search");
            Assert.Contains(schema.Actions, action => action.Name == "list");
            Assert.Contains(schema.Arguments, arg => arg.Flag == "--verbose" || arg.Long == "--verbose");
            Assert.Contains(schema.Arguments, arg => arg.Flag == "--no-proxy" || arg.Long == "--no-proxy");
        }

        [Fact]
        public void HelpImport_Node_ParsesParametersAndOptions()
        {
            var helpFile = @"A:\AptWeb\zypper-operations\Archive-Hasher\HelpLists.txt";
            Assert.True(File.Exists(helpFile));

            var content = File.ReadAllText(helpFile);
            var helpText = ExtractHelpSection(content, "node --help");
            var schema = HelpTextParser.Parse("node", helpText);

            Assert.Contains(schema.Parameters, param => string.Equals(param.Name, "script.js", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(schema.Parameters, param => string.Equals(param.Name, "arguments", StringComparison.OrdinalIgnoreCase));
            Assert.True(schema.Arguments.Count > 50);
        }

        [Fact]
        public void HelpImport_Cloudflared_ParsesNestedCommands()
        {
            var helpFile = @"A:\AptWeb\zypper-operations\Archive-Hasher\HelpLists.txt";
            Assert.True(File.Exists(helpFile));

            var content = File.ReadAllText(helpFile);
            var helpText = ExtractHelpSection(content, "cloudflared --help");
            var schema = HelpTextParser.Parse("cloudflared", helpText);

            Assert.Contains(schema.Actions, action => action.Name == "access");
            Assert.Contains(schema.Actions, action => action.Name == "tunnel");
        }

        [Fact]
        public void CompletionParser_PowerShell_ExtractsFlagsAndCommands()
        {
            var completion = """
            Register-ArgumentCompleter -CommandName foo -ScriptBlock {
                param($wordToComplete, $commandAst, $cursorPosition)
                $commands = @('init','deploy')
                $globalFlags = @('--help','--verbose','-v')
            }
            """;

            var parsed = CompletionScriptParser.Parse(completion);

            Assert.Contains(parsed.Actions, action => action.Name == "init");
            Assert.Contains(parsed.Actions, action => action.Name == "deploy");
            Assert.Contains(parsed.Arguments, arg => arg.Flag == "--help" || arg.Long == "--help");
            Assert.Contains(parsed.Arguments, arg => arg.Flag == "--verbose" || arg.Long == "--verbose");
            Assert.Contains(parsed.Arguments, arg => arg.Flag == "-v");
        }

        [Fact]
        public void CompletionParser_Bash_ExtractsFlags()
        {
            var completion = """
            _foo_completions() {
              local flags="--help --config -c --verbose"
              local commands="init deploy"
            }
            """;

            var parsed = CompletionScriptParser.Parse(completion);

            Assert.Contains(parsed.Arguments, arg => arg.Flag == "--help" || arg.Long == "--help");
            Assert.Contains(parsed.Arguments, arg => arg.Flag == "--config" || arg.Long == "--config");
            Assert.Contains(parsed.Arguments, arg => arg.Flag == "-c");
        }

        [Fact]
        public void CompletionMerge_AddsMissingFlags_WithoutOverwritingDescriptions()
        {
            var schema = new ToolSchema { Name = "demo" };
            schema.Arguments.Add(new SchemaArgument
            {
                Flag = "--help",
                Description = "Show help",
                Type = "boolean"
            });

            var completion = """
            Register-ArgumentCompleter -CommandName demo -ScriptBlock {
                $globalFlags = @('--help','--verbose')
            }
            """;

            HelpImportPipeline.ApplyCompletionText(schema, completion);

            var helpArg = schema.Arguments.First(arg => arg.Flag == "--help" || arg.Long == "--help");
            Assert.Equal("Show help", helpArg.Description);
            Assert.Contains(schema.Arguments, arg => arg.Flag == "--verbose" || arg.Long == "--verbose");
        }

        [Fact]
        public void CompletionParser_PairsShortAndLongFlags()
        {
            var completion = """
            Register-ArgumentCompleter -CommandName demo -ScriptBlock {
                $flags = @("-v, --verbose", "--config -c")
            }
            """;

            var parsed = CompletionScriptParser.Parse(completion);

            var verbose = parsed.Arguments.First(arg => arg.Flag == "-v" || arg.Long == "--verbose");
            Assert.Equal("--verbose", string.IsNullOrWhiteSpace(verbose.Long) ? verbose.Flag : verbose.Long);
            Assert.Equal("-v", verbose.Flag);

            var config = parsed.Arguments.First(arg => arg.Flag == "-c" || arg.Long == "--config");
            Assert.Equal("--config", string.IsNullOrWhiteSpace(config.Long) ? config.Flag : config.Long);
            Assert.Equal("-c", config.Flag);
        }

        private static string ExtractHelpSection(string content, string commandLine)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var startIndex = -1;
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];
                if (line.StartsWith("PS ", StringComparison.OrdinalIgnoreCase) &&
                    line.Contains($"> {commandLine}", StringComparison.OrdinalIgnoreCase))
                {
                    startIndex = i + 1;
                    break;
                }
            }

            if (startIndex < 0)
            {
                throw new InvalidOperationException($"Command line '{commandLine}' not found.");
            }

            var collected = lines
                .Skip(startIndex)
                .TakeWhile(line => !line.StartsWith("PS ", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return string.Join(Environment.NewLine, collected).Trim();
        }
    }
}
