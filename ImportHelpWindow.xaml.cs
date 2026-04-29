using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using CommandWizard.Services;

namespace CommandWizard
{
    public partial class ImportHelpWindow : Window
    {
        public ImportHelpWindow()
        {
            InitializeComponent();
            HelpArgs = "--help";
            UsePasteText = false;
            UseAdvancedSources = true;
            DataContext = this;
            Loaded += OnLoaded;
        }

        public string CommandText { get; set; } = string.Empty;
        public string HelpArgs { get; set; } = string.Empty;
        public string HelpText { get; set; } = string.Empty;
        public bool UsePasteText { get; private set; }
        public bool UseAdvancedSources { get; set; }
        public ObservableCollection<ImportFlagOption> FlagOptions { get; } = new();

        public string[] ExcludedFlags =>
            FlagOptions.Where(option => !option.IsIncluded)
                .Select(option => option.Flag)
                .Where(flag => !string.IsNullOrWhiteSpace(flag))
                .ToArray();

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnImportClicked(object sender, RoutedEventArgs e)
        {
            if (UsePasteText && string.IsNullOrWhiteSpace(HelpText))
            {
                MessageBox.Show("Paste help text to import.", "Import Schema", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
        }

        private void OnPreviewFlagsClicked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(CommandText))
            {
                MessageBox.Show("Command name is required to preview flags.", "Import Schema", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (UsePasteText && string.IsNullOrWhiteSpace(HelpText))
            {
                MessageBox.Show("Paste help text to preview flags.", "Import Schema", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var ok = UsePasteText
                ? HelpSchemaImporter.TryImportFromHelpText(CommandText, HelpText, UseAdvancedSources, out var schema, out var error)
                : HelpSchemaImporter.TryImportFromCommand(CommandText, HelpArgs, UseAdvancedSources, out schema, out error);

            if (!ok)
            {
                var message = string.IsNullOrWhiteSpace(error) ? "Preview failed." : error;
                MessageBox.Show(message, "Import Schema", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            FlagOptions.Clear();
            foreach (var argument in schema.Arguments.OrderBy(arg => arg.Long).ThenBy(arg => arg.Flag))
            {
                var display = string.IsNullOrWhiteSpace(argument.Description)
                    ? BuildDisplayName(argument.Flag, argument.Long)
                    : $"{BuildDisplayName(argument.Flag, argument.Long)} — {argument.Description}";

                FlagOptions.Add(new ImportFlagOption(argument.Flag, argument.Long, display));
            }

            if (FlagOptions.Count == 0)
            {
                MessageBox.Show("No flags detected to preview.", "Import Schema", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetImportMode(UsePasteText);
        }

        private void OnRunModeChecked(object sender, RoutedEventArgs e)
        {
            SetImportMode(false);
        }

        private void OnPasteModeChecked(object sender, RoutedEventArgs e)
        {
            SetImportMode(true);
        }

        private void SetImportMode(bool usePaste)
        {
            UsePasteText = usePaste;
            if (RunCommandPanel == null || PasteTextPanel == null || HelpTextBox == null)
            {
                return;
            }

            RunCommandPanel.Visibility = usePaste ? Visibility.Collapsed : Visibility.Visible;
            PasteTextPanel.Visibility = usePaste ? Visibility.Visible : Visibility.Collapsed;
            HelpTextBox.Visibility = usePaste ? Visibility.Visible : Visibility.Collapsed;
        }

        public sealed class ImportFlagOption : ViewModels.ViewModelBase
        {
            private bool _isIncluded = true;

            public ImportFlagOption(string flag, string longFlag, string display)
            {
                Flag = string.IsNullOrWhiteSpace(longFlag) ? flag : longFlag;
                Display = display;
            }

            public string Flag { get; }
            public string Display { get; }

            public bool IsIncluded
            {
                get => _isIncluded;
                set
                {
                    if (_isIncluded == value) return;
                    _isIncluded = value;
                    OnPropertyChanged();
                }
            }
        }

        private static string BuildDisplayName(string flag, string longFlag)
        {
            if (string.IsNullOrWhiteSpace(longFlag)) return flag;
            if (string.IsNullOrWhiteSpace(flag)) return longFlag;
            return $"{flag}, {longFlag}";
        }
    }
}
