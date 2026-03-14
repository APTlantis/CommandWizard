using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using CommandWizard.Models;
using CommandWizard.Services;

namespace CommandWizard.ViewModels
{
    public sealed class MainViewModel : ViewModelBase
    {
        private readonly string _schemaDirectory;
        private readonly string _historyPath;
        private ToolSchemaViewModel? _selectedTool;
        private SchemaAction? _selectedAction;
        private string _taskLabel = string.Empty;
        private string _commandPreview = string.Empty;
        private SchemaAction? _selectedActionForEdit;
        private SchemaArgument? _selectedArgumentForEdit;
        private SchemaParameter? _selectedParameterForEdit;

        public MainViewModel()
        {
            _schemaDirectory = Path.Combine(AppContext.BaseDirectory, "schemas");
            _historyPath = Path.Combine(AppContext.BaseDirectory, "history.json");
            Tools = new ObservableCollection<ToolSchemaViewModel>();
            LoadSchemas();
        }

        internal MainViewModel(System.Collections.Generic.IEnumerable<ToolSchema> schemas)
        {
            _schemaDirectory = Path.Combine(AppContext.BaseDirectory, "schemas");
            _historyPath = Path.Combine(AppContext.BaseDirectory, "history.json");
            Tools = new ObservableCollection<ToolSchemaViewModel>(
                schemas.Select(schema => new ToolSchemaViewModel(schema)));
            SelectedTool = Tools.FirstOrDefault();
        }

        public ObservableCollection<ToolSchemaViewModel> Tools { get; }

        public ToolSchemaViewModel? SelectedTool
        {
            get => _selectedTool;
            set
            {
                if (_selectedTool == value) return;
                DetachToolHandlers(_selectedTool);
                _selectedTool = value;
                AttachToolHandlers(_selectedTool);
                SelectedAction = _selectedTool?.Actions.FirstOrDefault();
                OnPropertyChanged();
                UpdateCommandPreview();
            }
        }

        public SchemaAction? SelectedAction
        {
            get => _selectedAction;
            set
            {
                if (_selectedAction == value) return;
                _selectedAction = value;
                OnPropertyChanged();
                UpdateCommandPreview();
            }
        }

        public SchemaAction? SelectedActionForEdit
        {
            get => _selectedActionForEdit;
            set
            {
                if (_selectedActionForEdit == value) return;
                _selectedActionForEdit = value;
                OnPropertyChanged();
            }
        }

        public SchemaArgument? SelectedArgumentForEdit
        {
            get => _selectedArgumentForEdit;
            set
            {
                if (_selectedArgumentForEdit == value) return;
                _selectedArgumentForEdit = value;
                OnPropertyChanged();
            }
        }

        public SchemaParameter? SelectedParameterForEdit
        {
            get => _selectedParameterForEdit;
            set
            {
                if (_selectedParameterForEdit == value) return;
                _selectedParameterForEdit = value;
                OnPropertyChanged();
            }
        }

        public string TaskLabel
        {
            get => _taskLabel;
            set
            {
                if (_taskLabel == value) return;
                _taskLabel = value;
                OnPropertyChanged();
            }
        }

        public string CommandPreview
        {
            get => _commandPreview;
            private set
            {
                if (_commandPreview == value) return;
                _commandPreview = value;
                OnPropertyChanged();
            }
        }

        public void CopyCommand()
        {
            if (string.IsNullOrWhiteSpace(CommandPreview))
            {
                MessageBox.Show("No command to copy yet.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            Clipboard.SetText(CommandPreview);
            AppendHistory(CommandPreview);
            MessageBox.Show("Command copied to clipboard.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void GenerateCommand()
        {
            if (string.IsNullOrWhiteSpace(CommandPreview))
            {
                MessageBox.Show("No command to generate yet.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AppendHistory(CommandPreview);
            MessageBox.Show("Command saved to history.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void UpdateCommandPreview()
        {
            if (SelectedTool == null)
            {
                CommandPreview = string.Empty;
                return;
            }

            CommandPreview = CommandBuilder.BuildCommand(SelectedTool, SelectedAction);
        }

        public void AddNewSchema()
        {
            var schema = new ToolSchema
            {
                Name = "new-tool",
                Description = "",
                SourcePath = ""
            };
            var vm = new ToolSchemaViewModel(schema);
            Tools.Add(vm);
            SelectedTool = vm;
        }

        public void AddAction()
        {
            if (SelectedTool == null) return;
            var action = new SchemaAction { Name = "action", Description = "" };
            SelectedTool.Actions.Add(action);
            SelectedActionForEdit = action;
        }

        public void RemoveAction()
        {
            if (SelectedTool == null || SelectedActionForEdit == null) return;
            SelectedTool.Actions.Remove(SelectedActionForEdit);
            if (SelectedAction == SelectedActionForEdit)
            {
                SelectedAction = SelectedTool.Actions.FirstOrDefault();
            }
            SelectedActionForEdit = null;
            UpdateCommandPreview();
        }

        public void AddArgument()
        {
            if (SelectedTool == null) return;
            var argument = new SchemaArgument
            {
                Flag = "--flag",
                Long = "",
                Description = "",
                Type = "boolean"
            };
            SelectedTool.Schema.Arguments.Add(argument);
            var optionVm = new OptionItemViewModel(argument);
            optionVm.PropertyChanged += OptionOnPropertyChanged;
            SelectedTool.Options.Add(optionVm);
            SelectedArgumentForEdit = argument;
        }

        public void RemoveArgument()
        {
            if (SelectedTool == null || SelectedArgumentForEdit == null) return;
            SelectedTool.Schema.Arguments.Remove(SelectedArgumentForEdit);
            var optionVm = SelectedTool.Options.FirstOrDefault(o => o.Argument == SelectedArgumentForEdit);
            if (optionVm != null)
            {
                optionVm.PropertyChanged -= OptionOnPropertyChanged;
                SelectedTool.Options.Remove(optionVm);
            }
            SelectedArgumentForEdit = null;
            UpdateCommandPreview();
        }

        public void AddParameter()
        {
            if (SelectedTool == null) return;
            var parameter = new SchemaParameter { Name = "param", Type = "string", Required = true };
            SelectedTool.Schema.Parameters.Add(parameter);
            var paramVm = new ParameterItemViewModel(parameter);
            paramVm.PropertyChanged += ParameterOnPropertyChanged;
            SelectedTool.Parameters.Add(paramVm);
            SelectedParameterForEdit = parameter;
        }

        public void RemoveParameter()
        {
            if (SelectedTool == null || SelectedParameterForEdit == null) return;
            SelectedTool.Schema.Parameters.Remove(SelectedParameterForEdit);
            var paramVm = SelectedTool.Parameters.FirstOrDefault(p => p.Parameter == SelectedParameterForEdit);
            if (paramVm != null)
            {
                paramVm.PropertyChanged -= ParameterOnPropertyChanged;
                SelectedTool.Parameters.Remove(paramVm);
            }
            SelectedParameterForEdit = null;
            UpdateCommandPreview();
        }

        public void SaveSelectedSchema()
        {
            if (SelectedTool == null)
            {
                MessageBox.Show("Select a tool to save.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedTool.ToolName))
            {
                MessageBox.Show("Tool name is required.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var filePath = SelectedTool.SourcePath;
            if (string.IsNullOrWhiteSpace(filePath))
            {
                var safeName = new string(SelectedTool.ToolName
                    .ToLowerInvariant()
                    .Select(ch => char.IsLetterOrDigit(ch) ? ch : '-')
                    .ToArray())
                    .Trim('-');
                if (string.IsNullOrWhiteSpace(safeName))
                {
                    safeName = "tool";
                }
                filePath = Path.Combine(_schemaDirectory, $"{safeName}.toml");
                SelectedTool.SourcePath = filePath;
            }

            var toml = SchemaSerialization.Serialize(SelectedTool.Schema);
            File.WriteAllText(filePath, toml);
            MessageBox.Show("Schema saved.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void CopyToolToAppDirectory()
        {
            if (SelectedTool == null)
            {
                MessageBox.Show("Select a tool first.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedTool.ExecutablePath))
            {
                MessageBox.Show("Provide an executable path to copy.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!File.Exists(SelectedTool.ExecutablePath))
            {
                MessageBox.Show("Executable path not found.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var toolsDir = Path.Combine(AppContext.BaseDirectory, "tools");
            Directory.CreateDirectory(toolsDir);
            var fileName = Path.GetFileName(SelectedTool.ExecutablePath);
            var destination = Path.Combine(toolsDir, fileName);
            File.Copy(SelectedTool.ExecutablePath, destination, overwrite: true);

            SelectedTool.ExecutablePath = destination;
            if (string.IsNullOrWhiteSpace(SelectedTool.InstalledName))
            {
                SelectedTool.InstalledName = Path.GetFileNameWithoutExtension(fileName);
            }

            MessageBox.Show($"Copied to '{destination}'.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void AddToolsDirectoryToPath()
        {
            var toolsDir = Path.Combine(AppContext.BaseDirectory, "tools");
            Directory.CreateDirectory(toolsDir);

            var current = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            var parts = current.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Any(p => string.Equals(p, toolsDir, StringComparison.OrdinalIgnoreCase)))
            {
                MessageBox.Show("Tools directory is already on PATH (User).", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var updated = string.IsNullOrWhiteSpace(current) ? toolsDir : $"{current};{toolsDir}";
            Environment.SetEnvironmentVariable("PATH", updated, EnvironmentVariableTarget.User);

            MessageBox.Show("Added tools directory to PATH (User). Restart shells to pick it up.", "Command Wizard", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadSchemas()
        {
            if (!Directory.Exists(_schemaDirectory))
            {
                Directory.CreateDirectory(_schemaDirectory);
                MessageBox.Show(
                    $"Created schema folder at '{_schemaDirectory}'. Add .toml schema files to begin.",
                    "Command Wizard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var files = Directory.GetFiles(_schemaDirectory, "*.toml");
            if (files.Length == 0)
            {
                MessageBox.Show(
                    $"No schemas found in '{_schemaDirectory}'. Add at least one .toml file.",
                    "Command Wizard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            foreach (var file in files)
            {
                try
                {
                    var schema = SchemaSerialization.Parse(File.ReadAllText(file));
                    schema.SourcePath = file;
                    var vm = new ToolSchemaViewModel(schema);
                    Tools.Add(vm);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Failed to load schema '{Path.GetFileName(file)}': {ex.Message}",
                        "Command Wizard",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }
            }

            SelectedTool = Tools.FirstOrDefault();
        }


        private void AppendHistory(string command)
        {
            var record = new HistoryRecord
            {
                Tool = SelectedTool?.Schema.Name ?? "",
                Action = SelectedAction?.Name ?? "",
                Task = TaskLabel,
                Command = command,
                CreatedAtUtc = DateTime.UtcNow
            };

            try
            {
                var history = new System.Collections.Generic.List<HistoryRecord>();
                if (File.Exists(_historyPath))
                {
                    var json = File.ReadAllText(_historyPath);
                    var loaded = JsonSerializer.Deserialize<System.Collections.Generic.List<HistoryRecord>>(json);
                    if (loaded != null)
                    {
                        history = loaded;
                    }
                }

                history.Add(record);
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(_historyPath, JsonSerializer.Serialize(history, options));
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Failed to write history: {ex.Message}",
                    "Command Wizard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private void AttachToolHandlers(ToolSchemaViewModel? tool)
        {
            if (tool == null) return;
            tool.PropertyChanged += ToolOnPropertyChanged;
            foreach (var option in tool.Options)
            {
                option.PropertyChanged += OptionOnPropertyChanged;
            }
            foreach (var parameter in tool.Parameters)
            {
                parameter.PropertyChanged += ParameterOnPropertyChanged;
            }
        }

        private void DetachToolHandlers(ToolSchemaViewModel? tool)
        {
            if (tool == null) return;
            tool.PropertyChanged -= ToolOnPropertyChanged;
            foreach (var option in tool.Options)
            {
                option.PropertyChanged -= OptionOnPropertyChanged;
            }
            foreach (var parameter in tool.Parameters)
            {
                parameter.PropertyChanged -= ParameterOnPropertyChanged;
            }
        }

        private void OptionOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateCommandPreview();
        }

        private void ParameterOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateCommandPreview();
        }

        private void ToolOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            UpdateCommandPreview();
        }

        private sealed class HistoryRecord
        {
            public string Tool { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string Task { get; set; } = string.Empty;
            public string Command { get; set; } = string.Empty;
            public DateTime CreatedAtUtc { get; set; }
        }
    }
}
