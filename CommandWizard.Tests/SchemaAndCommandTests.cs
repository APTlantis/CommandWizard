using System;
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
    }
}
