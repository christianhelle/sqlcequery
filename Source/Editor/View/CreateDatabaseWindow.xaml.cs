using System.Windows;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.View
{
    /// <summary>
    /// Interaction logic for CreateDatabase.xaml
    /// </summary>
    public partial class CreateDatabaseWindow : Window
    {
        public CreateDatabaseWindow()
        {
            InitializeComponent();
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CreateDatabaseWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            tbFilename.Focus();
        }
    }
}
