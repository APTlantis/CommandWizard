using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media;
using CommandWizard.Models;

namespace CommandWizard.ViewModels
{
    public sealed class ToolSchemaViewModel : ViewModelBase
    {
        public ToolSchemaViewModel(ToolSchema schema)
        {
            Schema = schema;
            Actions = schema.Actions;
            Options = new ObservableCollection<OptionItemViewModel>();
            Parameters = new ObservableCollection<ParameterItemViewModel>();

            foreach (var arg in schema.Arguments)
            {
                Options.Add(new OptionItemViewModel(arg));
            }

            foreach (var param in schema.Parameters)
            {
                Parameters.Add(new ParameterItemViewModel(param));
            }
        }

        public ToolSchema Schema { get; }
        public ObservableCollection<SchemaAction> Actions { get; }
        public ObservableCollection<OptionItemViewModel> Options { get; }
        public ObservableCollection<ParameterItemViewModel> Parameters { get; }

        public string ToolName
        {
            get => Schema.Name;
            set
            {
                if (Schema.Name == value) return;
                Schema.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public string ToolDescription
        {
            get => Schema.Description;
            set
            {
                if (Schema.Description == value) return;
                Schema.Description = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayName));
            }
        }

        public string SourcePath
        {
            get => Schema.SourcePath;
            set
            {
                if (Schema.SourcePath == value) return;
                Schema.SourcePath = value;
                OnPropertyChanged();
            }
        }

        public string ExecutablePath
        {
            get => Schema.ExecutablePath;
            set
            {
                if (Schema.ExecutablePath == value) return;
                Schema.ExecutablePath = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public string InstalledName
        {
            get => Schema.InstalledName;
            set
            {
                if (Schema.InstalledName == value) return;
                Schema.InstalledName = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CommandName));
                OnPropertyChanged(nameof(StatusText));
                OnPropertyChanged(nameof(StatusBrush));
            }
        }

        public string CommandName => string.IsNullOrWhiteSpace(InstalledName) ? ToolName : InstalledName;

        public string StatusText
        {
            get
            {
                var path = Schema.ExecutablePath;
                if (string.IsNullOrWhiteSpace(path))
                {
                    return IsOnPath(CommandName) ? "Installed" : "Not found";
                }

                if (!File.Exists(path))
                {
                    return IsOnPath(CommandName) ? "Installed" : "Not found";
                }

                return IsBundledPath(path) ? "Bundled" : "Installed";
            }
        }

        public Brush StatusBrush
        {
            get
            {
                return StatusText switch
                {
                    "Bundled" => Brushes.SteelBlue,
                    "Installed" => Brushes.SeaGreen,
                    _ => Brushes.DarkOrange
                };
            }
        }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(ToolDescription)
                ? ToolName
                : $"{ToolName} — {ToolDescription}";

        private static bool IsBundledPath(string path)
        {
            var toolsDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "tools"));
            var fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(toolsDir, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsOnPath(string commandName)
        {
            if (string.IsNullOrWhiteSpace(commandName))
            {
                return false;
            }

            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            var pathParts = pathVar.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (pathParts.Length == 0)
            {
                return false;
            }

            var extVar = Environment.GetEnvironmentVariable("PATHEXT") ?? ".EXE;.BAT;.CMD;.COM";
            var extensions = extVar.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var hasExt = Path.HasExtension(commandName);
            foreach (var dir in pathParts)
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                if (hasExt)
                {
                    var candidate = Path.Combine(dir, commandName);
                    if (File.Exists(candidate)) return true;
                    continue;
                }

                foreach (var ext in extensions)
                {
                    var candidate = Path.Combine(dir, commandName + ext);
                    if (File.Exists(candidate)) return true;
                }
            }

            return false;
        }
    }
}
