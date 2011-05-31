using System;
using System.Windows;
using System.Windows.Input;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.View;

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
            //dataGrid.Visible = false;
        }

        private MainViewModel ViewModel
        {
            get { return ((MainViewModel)DataContext); }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (ViewModel.LaunchedWithArgument)
                ViewModel.OpenDatabase();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
        }

        private void ExecuteQuery_Click(object sender, RoutedEventArgs e)
        {
            //dataGrid.DataSource = 
            ViewModel.ExecuteQuery();
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            //if (e.Key == Key.F5)
            //    //dataGrid.DataSource = 
            //    ViewModel.ExecuteQuery();

            switch (e.Key)
            {
                case Key.F5:
                    ViewModel.ExecuteQuery();
                    break;
                case Key.F1:
                    new AboutBox(this).ShowDialog();
                    break;
            }
        }

        private void dataGrid_DataSourceChanged(object sender, EventArgs e)
        {
            //if (dataGrid.DataSource == null)
            //    dataGrid.DataBindings.Clear();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new AboutBox(this).ShowDialog();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenDatabase();
        }
    }
}
