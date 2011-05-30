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
using System.Windows.Navigation;
using System.Windows.Shapes;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            dataGrid.Visible = false;
        }

        private MainViewModel ViewModel
        {
            get { return ((MainViewModel)DataContext); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.NewDataSource();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.NewDataSource();
        }

        private void ExecuteQuery_Click(object sender, RoutedEventArgs e)
        {
            dataGrid.DataSource = ViewModel.ExecuteQuery();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F5)
                dataGrid.DataSource = ViewModel.ExecuteQuery();
        }

        private void dataGrid_DataSourceChanged(object sender, EventArgs e)
        {
            if (dataGrid.DataSource == null)
                dataGrid.DataBindings.Clear();
        }
    }
}
