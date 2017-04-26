using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.View
{
    /// <summary>
    /// Interaction logic for RenameObjectWindow.xaml
    /// </summary>
    public partial class RenameObjectWindow : Window
    {
        public RenameObjectWindow()
        {
            InitializeComponent();
        }

        public RenameObjectViewModel ViewModel
        {
            get { return DataContext as RenameObjectViewModel; }
            set { DataContext = value; }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void btnRename_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.Rename();
            Close();
        }
    }
}
