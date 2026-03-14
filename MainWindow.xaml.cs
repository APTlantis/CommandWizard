using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using CommandWizard.ViewModels;

namespace CommandWizard
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static RoutedCommand CopyCommand { get; } = new RoutedCommand();
        public static RoutedCommand GenerateCommand { get; } = new RoutedCommand();
        public static RoutedCommand HelpCommand { get; } = new RoutedCommand();

        private readonly MainViewModel _viewModel;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        private void OnCopyExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _viewModel.CopyCommand();
        }

        private void OnGenerateExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            _viewModel.GenerateCommand();
        }

        private void OnHelpExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            var helpPath = Path.GetFullPath(Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "CommandBuilder",
                "HELP.md"));

            if (!File.Exists(helpPath))
            {
                MessageBox.Show(
                    $"Help file not found at: {helpPath}",
                    "Help",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = helpPath,
                UseShellExecute = true
            });
        }

        private void OnCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                e.CanExecute = !string.IsNullOrWhiteSpace(vm.CommandPreview);
                return;
            }

            e.CanExecute = false;
        }

        private void OnSaveSchemaClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.SaveSelectedSchema();
        }

        private void OnNewSchemaClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.AddNewSchema();
        }

        private void OnAddActionClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.AddAction();
        }

        private void OnRemoveActionClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveAction();
        }

        private void OnAddArgumentClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.AddArgument();
        }

        private void OnRemoveArgumentClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveArgument();
        }

        private void OnAddParameterClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.AddParameter();
        }

        private void OnRemoveParameterClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.RemoveParameter();
        }

        private void OnCopyToolClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.CopyToolToAppDirectory();
        }

        private void OnAddToolsDirToPathClicked(object sender, RoutedEventArgs e)
        {
            _viewModel.AddToolsDirectoryToPath();
        }

        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OnAboutClicked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Aptlantis Command Wizard\nSchema-driven command builder.",
                "About",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
