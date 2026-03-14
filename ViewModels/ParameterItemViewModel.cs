using CommandWizard.Models;

namespace CommandWizard.ViewModels
{
    public sealed class ParameterItemViewModel : ViewModelBase
    {
        private string _value = string.Empty;

        public ParameterItemViewModel(SchemaParameter parameter)
        {
            Parameter = parameter;
        }

        public SchemaParameter Parameter { get; }

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
    }
}
