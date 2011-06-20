using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml;
using GalaSoft.MvvmLight;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        string dataSource;
        ISqlCeDatabase database;

        public MainViewModel()
        {
            Query = new TextDocument();
            ResultSetXml = new TextDocument();

            var args = Environment.GetCommandLineArgs();
            LaunchedWithArgument = args != null && args.Length == 2;

            LoadSqlSyntaxHighlighter();
        }

        public bool LaunchedWithArgument { get; private set; }

        #region Data Binding

        private ObservableCollection<Table> tables;
        public ObservableCollection<Table> Tables
        {
            get { return tables; }
            set
            {
                tables = value;
                RaisePropertyChanged("Tables");
            }
        }

        private TextDocument query;
        public TextDocument Query
        {
            get { return query; }
            set
            {
                query = value;
                RaisePropertyChanged("Query");
            }
        }

        private ObservableCollection<DataTable> resultSet;
        public ObservableCollection<DataTable> ResultSet
        {
            get { return resultSet; }
            set
            {
                resultSet = value;
                RaisePropertyChanged("ResultSet");
            }
        }

        public TextDocument ResultSetXml { get; private set; }

        private DataTable tableData;
        public DataTable TableData
        {
            get { return tableData; }
            set
            {
                tableData = value;
                RaisePropertyChanged("TableData");
            }
        }

        private IHighlightingDefinition sqlSyntaxHighlighting;
        public IHighlightingDefinition SqlSyntaxHighlighting
        {
            get { return sqlSyntaxHighlighting; }
            set
            {
                sqlSyntaxHighlighting = value;
                RaisePropertyChanged("SqlSyntaxHighlighting");
            }
        }

        private string text;
        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                RaisePropertyChanged("Text");
            }
        }

        private string status;
        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                RaisePropertyChanged("Status");
            }
        }

        private string resultSetMessages;
        public string ResultSetMessages
        {
            get { return resultSetMessages; }
            set
            {
                resultSetMessages = value;
                RaisePropertyChanged("ResultSetMessages");
            }
        }

        private string resultSetErrors;
        public string ResultSetErrors
        {
            get { return resultSetErrors; }
            set
            {
                resultSetErrors = value;
                RaisePropertyChanged("ResultSetErrors");
            }
        }

        private int currentTabIndex;
        public int CurrentTabIndex
        {
            get { return currentTabIndex; }
            set
            {
                currentTabIndex = value;
                RaisePropertyChanged("CurrentTabIndex");
            }
        }

        private bool queryIsBusy;
        public bool QueryIsBusy
        {
            get { return queryIsBusy; }
            set
            {
                queryIsBusy = value;
                RaisePropertyChanged("QueryIsBusy");
            }
        }

        private bool tableDataIsBusy;
        public bool TableDataIsBusy
        {
            get { return tableDataIsBusy; }
            set
            {
                tableDataIsBusy = value;
                RaisePropertyChanged("TableDataIsBusy");
            }
        }

        private bool analyzingTablesIsBusy;
        public bool AnalyzingTablesIsBusy
        {
            get { return analyzingTablesIsBusy; }
            set
            {
                analyzingTablesIsBusy = value;
                RaisePropertyChanged("AnalyzingTablesIsBusy");
            }
        }

        #endregion

        public void OpenDatabase()
        {
            Text = "SQL Compact Query Analyzer";

            using (var dialog = new OpenFileDialog())
            {
                dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Filter = "Database files (*.sdf)|*.sdf";
                if (dialog.ShowDialog() == DialogResult.Cancel)
                    return;

                var fi = new FileInfo(dialog.FileName);
                fi.Attributes = FileAttributes.Normal;

                dataSource = dialog.FileName;
                AnalyzeDatabase();
            }
        }

        private void AnalyzeDatabase()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    AnalyzingTablesIsBusy = true;

                    if (!File.Exists(dataSource))
                        throw new InvalidOperationException("Unable to find " + dataSource);

                    database = SqlCeDatabaseFactory.Create("Data Source=" + dataSource);

                    Text = "SQL Compact Query Analyzer" + " - " + new FileInfo(dataSource).Name;

                    var tables = new ObservableCollection<Table>();
                    foreach (var table in database.Tables)
                        tables.Add(table);
                    Tables = tables;
                }
                finally
                {
                    AnalyzingTablesIsBusy = false;
                }
            });
        }

        public void ExecuteQuery(string query = null)
        {
            Task.Factory.StartNew(() =>
            {
                var errors = new StringBuilder();
                var messages = new StringBuilder();

                try
                {
                    QueryIsBusy = true;
                    var tables = new ObservableCollection<DataTable>();

                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (string.IsNullOrEmpty(query))
                            query = Query.Text;
                    });

                    var result = database.ExecuteQuery(query, errors, messages) as IEnumerable<DataTable>;
                    if (result == null)
                        return;

                    int counter = 1;
                    var sb = new StringBuilder();
                    foreach (var table in result)
                    {
                        tables.Add(table);

                        if (string.IsNullOrEmpty(table.TableName))
                            table.TableName = "ResultSet" + counter++;

                        using (var writer = new StringWriter(sb))
                        using (var xml = new XmlTextWriter(writer) { Formatting = Formatting.Indented })
                        {
                            table.WriteXml(writer);
                            writer.WriteLine(string.Empty);
                        }
                    }
                    ResultSet = tables;

                    System.Windows.Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        ResultSetXml.Text = sb.ToString();
                        RaisePropertyChanged("ResultSetXml");
                    });
                }
                finally
                {
                    CurrentTabIndex = ResultSet != null && ResultSet.Count > 0 ? 0 : 2;
                    ResultSetMessages = messages.ToString();
                    ResultSetErrors = errors.ToString();
                    QueryIsBusy = false;
                }
            });
        }

        public void LoadTableData(Table table)
        {
            if (table == null)
                return;

            Task.Factory.StartNew(() =>
            {
                try
                {
                    TableDataIsBusy = true;
                    if (TableData != null)
                    {
                        TableData.Dispose();
                        TableData = null;
                    }

                    var dataTable = database.GetTableData(table) as DataTable;
                    TableData = dataTable;
                }
                finally
                {
                    TableDataIsBusy = false;
                }
            });
        }

        public void SaveTableDataChanges()
        {
            database.SaveTableDataChanges(TableData);
        }

        public void LoadSqlSyntaxHighlighter()
        {
            using (var stream = Assembly.GetAssembly(GetType()).GetManifestResourceStream("ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Resources.SQL-Mode.xshd"))
            using (var reader = new XmlTextReader(stream))
            {
                var xshd = HighlightingLoader.LoadXshd(reader);
                SqlSyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
            }
        }

        public void ProcessCommandLineArguments()
        {
            var args = Environment.GetCommandLineArgs();
            if (args != null && args.Length == 2)
            {
                LaunchedWithArgument = true;
                dataSource = args[1];

                var ext = Path.GetExtension(dataSource);
                if (string.Compare(ext, ".sdf", true) == 0)
                    AnalyzeDatabase();
            }
        }

        public void LoadDroppedFile(System.Windows.IDataObject data)
        {
            if (!data.GetDataPresent(System.Windows.DataFormats.FileDrop))
                return;
            var filePaths = (string[])(data.GetData(System.Windows.DataFormats.FileDrop));
            var ext = Path.GetExtension(filePaths[0]).ToLower();
            if (string.Compare(ext, ".sdf", true) == 0)
            {
                dataSource = filePaths[0];
                AnalyzeDatabase();
            }
        }

        public void ShrinkDatabase()
        {
            var previous = new FileInfo(dataSource).Length;

            database.Shrink();

            var current = new FileInfo(dataSource).Length;
            
            ResultSetMessages = string.Format("Database shrinked to {0:0,0.0} from {1:0,0.0} bytes", previous, current);
            CurrentTabIndex = 2;
        }

        public void CompactDatabase()
        {
            var previous = new FileInfo(dataSource).Length;

            database.Compact();

            var current = new FileInfo(dataSource).Length;

            ResultSetMessages = string.Format("Database compacted to {0:0,0.0} from {1:0,0.0} bytes", previous, current);
            CurrentTabIndex = 2;
        }
    }
}
