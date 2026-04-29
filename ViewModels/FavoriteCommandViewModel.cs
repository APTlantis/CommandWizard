using System;
using System.Collections.ObjectModel;
using CommandWizard.Models;

namespace CommandWizard.ViewModels
{
    public sealed class FavoriteCommandViewModel : ViewModelBase
    {
        private string? _tool;
        private string? _action;
        private string? _task;
        private string? _command;
        private DateTime _createdAtUtc;

        public ObservableCollection<SchemaTag> Tags { get; } = new();

        public string? Tool
        {
            get => _tool;
            set
            {
                if (_tool == value) return;
                _tool = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToolActionLabel));
            }
        }

        public string? Action
        {
            get => _action;
            set
            {
                if (_action == value) return;
                _action = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ToolActionLabel));
            }
        }

        public string? Task
        {
            get => _task;
            set
            {
                if (_task == value) return;
                _task = value;
                OnPropertyChanged();
            }
        }

        public string? Command
        {
            get => _command;
            set
            {
                if (_command == value) return;
                _command = value;
                OnPropertyChanged();
            }
        }

        public DateTime CreatedAtUtc
        {
            get => _createdAtUtc;
            set
            {
                if (_createdAtUtc == value) return;
                _createdAtUtc = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CreatedAtLocal));
            }
        }

        public string ToolActionLabel
        {
            get
            {
                var tool = Tool ?? string.Empty;
                var action = Action ?? string.Empty;
                if (string.IsNullOrWhiteSpace(tool) && string.IsNullOrWhiteSpace(action)) return string.Empty;
                if (string.IsNullOrWhiteSpace(action)) return tool;
                if (string.IsNullOrWhiteSpace(tool)) return action;
                return $"{tool} • {action}";
            }
        }

        public string CreatedAtLocal
        {
            get
            {
                if (CreatedAtUtc == default) return string.Empty;
                return CreatedAtUtc.ToLocalTime().ToString("g");
            }
        }

        public void SetTags(System.Collections.Generic.IEnumerable<SchemaTag> tags)
        {
            Tags.Clear();
            foreach (var tag in tags)
            {
                Tags.Add(new SchemaTag
                {
                    Name = tag.Name,
                    Color = tag.Color
                });
            }
        }
    }
}
