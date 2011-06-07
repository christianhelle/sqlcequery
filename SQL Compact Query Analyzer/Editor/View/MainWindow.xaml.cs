using System;
using System.Windows;
using System.Windows.Input;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.View;
using System.Windows.Controls;
using System.Data;
using System.Windows.Shapes;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool dirty;

        public MainWindow()
        {
            InitializeComponent();
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

        private void ExecuteQuery_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ExecuteQuery(editor.SelectedText);
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.F5:
                    ViewModel.ExecuteQuery(editor.SelectedText);
                    break;
                case Key.F1:
                    new AboutBox(this).ShowDialog();
                    break;
            }
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            new AboutBox(this).ShowDialog();
        }

        private void Open_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.OpenDatabase();
        }

        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            dirty = true;
        }

        private void DataGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            if (!dirty) return;
            ViewModel.SaveTableDataChanges();
            dirty = false;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            ViewModel.LoadDroppedFile(e.Data);
        }

        private void Shrink_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ShrinkDatabase();
        }

        private void Compact_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CompactDatabase();
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
