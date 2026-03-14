using CommandWizard.Models;

namespace CommandWizard.ViewModels
{
    public sealed class OptionItemViewModel : ViewModelBase
    {
        private bool _isSelected;
        private string _value = string.Empty;

        public OptionItemViewModel(SchemaArgument argument)
        {
            Argument = argument;
            Argument.PropertyChanged += ArgumentOnPropertyChanged;
        }

        public SchemaArgument Argument { get; }

        public string DisplayName => string.IsNullOrWhiteSpace(Argument.Long) ? Argument.Flag : Argument.Long;

        public bool IsBoolean => string.Equals(Argument.Type, "boolean", System.StringComparison.OrdinalIgnoreCase);

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected == value) return;
                _isSelected = value;
                OnPropertyChanged();
            }
        }

        public string Value
        {
            get => _value;
            set
            {
                if (_value == value) return;
                _value = value;
                OnPropertyChanged();
            }
        }

        private void ArgumentOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(SchemaArgument.Long) || e.PropertyName == nameof(SchemaArgument.Flag))
            {
                OnPropertyChanged(nameof(DisplayName));
            }
            if (e.PropertyName == nameof(SchemaArgument.Type))
            {
                OnPropertyChanged(nameof(IsBoolean));
            }
        }
    }
}
