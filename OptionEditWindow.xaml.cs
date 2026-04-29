using System.Windows;

namespace CommandWizard
{
    public partial class OptionEditWindow : Window
    {
        public OptionEditWindow()
        {
            InitializeComponent();
            DataContext = this;
        }

        public string Flag { get; set; } = string.Empty;
        public string LongFlag { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        private void OnCancelClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnSaveClicked(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
