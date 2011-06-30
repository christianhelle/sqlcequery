using System.Windows;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.View
{
    /// <summary>
    /// Interaction logic for PasswordWindow.xaml
    /// </summary>
    public partial class PasswordWindow : Window
    {
        public PasswordWindow()
        {
            InitializeComponent();
            passwordBox.Focus();
        }

        public string Password { get; private set; }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            Password = passwordBox.Password;
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
