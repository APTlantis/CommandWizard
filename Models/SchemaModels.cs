using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CommandWizard.Models
{
    public sealed class ToolSchema
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SourcePath { get; set; } = string.Empty;
        public string ExecutablePath { get; set; } = string.Empty;
        public string InstalledName { get; set; } = string.Empty;
        public ObservableCollection<SchemaAction> Actions { get; set; } = new();
        public ObservableCollection<SchemaArgument> Arguments { get; set; } = new();
        public ObservableCollection<SchemaParameter> Parameters { get; set; } = new();
    }

    public sealed class SchemaAction : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _description = string.Empty;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class SchemaArgument : INotifyPropertyChanged
    {
        private string _flag = string.Empty;
        private string _long = string.Empty;
        private string _description = string.Empty;
        private string _type = "boolean";

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Flag
        {
            get => _flag;
            set
            {
                if (_flag == value) return;
                _flag = value;
                OnPropertyChanged();
            }
        }

        public string Long
        {
            get => _long;
            set
            {
                if (_long == value) return;
                _long = value;
                OnPropertyChanged();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                if (_description == value) return;
                _description = value;
                OnPropertyChanged();
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type == value) return;
                _type = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public sealed class SchemaParameter : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _type = "string";
        private bool _required = true;

        public event PropertyChangedEventHandler? PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                if (_name == value) return;
                _name = value;
                OnPropertyChanged();
            }
        }

        public string Type
        {
            get => _type;
            set
            {
                if (_type == value) return;
                _type = value;
                OnPropertyChanged();
            }
        }

        public bool Required
        {
            get => _required;
            set
            {
                if (_required == value) return;
                _required = value;
                OnPropertyChanged();
            }
        }

        private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
