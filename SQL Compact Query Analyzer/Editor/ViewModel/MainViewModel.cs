using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using GalaSoft.MvvmLight;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
using IDataObject = System.Windows.IDataObject;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Properties;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private string dataSource;
        private ISqlCeDatabase database;

        public MainViewModel()
        {
            Query = new TextDocument();
            ResultSetXml = new TextDocument();

            var args = Environment.GetCommandLineArgs();
            LaunchedWithArgument = args.Length == 2;

            LoadSqlSyntaxHighlighter();
        }

        public bool LaunchedWithArgument { get; private set; }

        public void Load()
        {
            DisplayResultsInGrid = Settings.Default.DisplayResultsInGrid;
            DisplayResultsAsXml = Settings.Default.DisplayResultsAsXml;
        }

        #region Data Binding

        private bool analyzingTablesIsBusy;
        private int currentResultsTabIndex;
        private int currentMainTabIndex;
        private TextDocument query;
        private bool queryIsBusy;
        private bool queryStringIsBusy;
        private ObservableCollection<DataTable> resultSet;
        private string resultSetErrors;
        private string resultSetMessages;
        private IHighlightingDefinition sqlSyntaxHighlighting;
        private string status;
        private DataTable tableData;
        private bool tableDataIsBusy;
        private ObservableCollection<Table> tables;
        private string text;
        private bool displayResultsInGrid;
        private bool displayResultsAsXml;

        public ObservableCollection<Table> Tables
        {
            get { return tables; }
            set
            {
                tables = value;
                RaisePropertyChanged("Tables");
            }
        }

        public TextDocument Query
        {
            get { return query; }
            set
            {
                query = value;
                RaisePropertyChanged("Query");
            }
        }

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

        public DataTable TableData
        {
            get { return tableData; }
            set
            {
                tableData = value;
                RaisePropertyChanged("TableData");
            }
        }

        public IHighlightingDefinition SqlSyntaxHighlighting
        {
            get { return sqlSyntaxHighlighting; }
            set
            {
                sqlSyntaxHighlighting = value;
                RaisePropertyChanged("SqlSyntaxHighlighting");
            }
        }

        public string Text
        {
            get { return text; }
            set
            {
                text = value;
                RaisePropertyChanged("Text");
            }
        }

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                RaisePropertyChanged("Status");
            }
        }

        public string ResultSetMessages
        {
            get { return resultSetMessages; }
            set
            {
                resultSetMessages = value;
                RaisePropertyChanged("ResultSetMessages");
            }
        }

        public string ResultSetErrors
        {
            get { return resultSetErrors; }
            set
            {
                resultSetErrors = value;
                RaisePropertyChanged("ResultSetErrors");
            }
        }

        public int CurrentResultsTabIndex
        {
            get { return currentResultsTabIndex; }
            set
            {
                currentResultsTabIndex = value;
                RaisePropertyChanged("CurrentResultsTabIndex");
            }
        }

        public int CurrentMainTabIndex
        {
            get { return currentMainTabIndex; }
            set
            {
                currentMainTabIndex = value;
                RaisePropertyChanged("CurrentMainTabIndex");
            }
        }

        public bool QueryIsBusy
        {
            get { return queryIsBusy; }
            set
            {
                queryIsBusy = value;
                RaisePropertyChanged("QueryIsBusy");
            }
        }

        public bool QueryStringIsBusy
        {
            get { return queryStringIsBusy; }
            set
            {
                queryStringIsBusy = value;
                RaisePropertyChanged("QueryStringIsBusy");
            }
        }

        public bool TableDataIsBusy
        {
            get { return tableDataIsBusy; }
            set
            {
                tableDataIsBusy = value;
                RaisePropertyChanged("TableDataIsBusy");
            }
        }

        public bool AnalyzingTablesIsBusy
        {
            get { return analyzingTablesIsBusy; }
            set
            {
                analyzingTablesIsBusy = value;
                RaisePropertyChanged("AnalyzingTablesIsBusy");
            }
        }

        public bool DisplayResultsInGrid
        {
            get { return displayResultsInGrid; }
            set
            {
                Settings.Default.DisplayResultsInGrid = displayResultsInGrid = value;
                Settings.Default.Save();
                RaisePropertyChanged("DisplayResultsInGrid");
            }
        }

        public bool DisplayResultsAsXml
        {
            get { return displayResultsAsXml; }
            set
            {
                Settings.Default.DisplayResultsAsXml = displayResultsAsXml = value;
                Settings.Default.Save();
                RaisePropertyChanged("DisplayResultsAsXml");
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

                dataSource = dialog.FileName;
                AnalyzeDatabase();

                TableData = null;
                Query.Text = string.Empty;
                RaisePropertyChanged("Query");
                CurrentMainTabIndex = 0;
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

                    var collection = new ObservableCollection<Table>();
                    foreach (var table in database.Tables)
                        collection.Add(table);
                    Tables = collection;
                }
                finally
                {
                    AnalyzingTablesIsBusy = false;
                }
            });
        }

        public void ExecuteQuery(string sql = null)
        {
            Task.Factory.StartNew(() =>
            {
                var errors = new StringBuilder();
                var messages = new StringBuilder();

                try
                {
                    QueryIsBusy = true;
                    ResultSetXml = null;
                    ResultSet = null;

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (string.IsNullOrEmpty(sql))
                            sql = Query.Text;
                    });

                    var result = database.ExecuteQuery(sql, errors, messages) as IEnumerable<DataTable>;
                    if (result == null)
                        return;

                    var counter = 1;
                    var sb = new StringBuilder();
                    var dataTables = new ObservableCollection<DataTable>();
                    foreach (var table in result)
                    {
                        dataTables.Add(table);

                        if (string.IsNullOrEmpty(table.TableName))
                            table.TableName = "ResultSet" + counter++;

                        if (!DisplayResultsAsXml) continue;
                        using (var writer = new StringWriter(sb))
                        using (var xml = new XmlTextWriter(writer) { Formatting = Formatting.Indented })
                        {
                            table.WriteXml(xml);
                            writer.WriteLine(string.Empty);
                        }
                    }

                    if (DisplayResultsInGrid)
                        ResultSet = dataTables;

                    if (DisplayResultsAsXml)
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            ResultSetXml.Text = sb.ToString();
                            RaisePropertyChanged("ResultSetXml");
                        });
                }
                finally
                {
                    CurrentResultsTabIndex = ResultSet != null && ResultSet.Count > 0 ? 0 : 2;
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
            const string NAME = "ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Resources.SQL-Mode.xshd";
            using (var stream = Assembly.GetAssembly(GetType()).GetManifestResourceStream(NAME))
                if (stream != null)
                    using (var reader = new XmlTextReader(stream))
                    {
                        var xshd = HighlightingLoader.LoadXshd(reader);
                        SqlSyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                    }
        }

        public void ProcessCommandLineArguments()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 2)
            {
                LaunchedWithArgument = true;
                dataSource = args[1];

                var ext = Path.GetExtension(dataSource);
                if (string.Compare(ext, ".sdf", true) == 0)
                    AnalyzeDatabase();
            }
        }

        public void LoadDroppedFile(IDataObject data)
        {
            if (!data.GetDataPresent(DataFormats.FileDrop))
                return;
            var filePaths = (string[])(data.GetData(DataFormats.FileDrop));
            if (filePaths != null && filePaths.Length > 0)
            {
                var filePath = filePaths[0];
                if (filePath != null)
                {
                    var ext = Path.GetExtension(filePath);
                    if (ext != null)
                        ext = ext.ToLower();
                    if (string.Compare(ext, ".sdf", true) == 0)
                    {
                        dataSource = filePaths[0];
                        AnalyzeDatabase();
                    }
                }
            }
        }

        public void ShrinkDatabase()
        {
            var previous = new FileInfo(dataSource).Length;

            database.Shrink();

            var current = new FileInfo(dataSource).Length;

            ResultSetMessages = string.Format("Database shrinked to {0:0,0.0} from {1:0,0.0} bytes", previous, current);
            CurrentResultsTabIndex = 2;
        }

        public void CompactDatabase()
        {
            var previous = new FileInfo(dataSource).Length;

            database.Compact();

            var current = new FileInfo(dataSource).Length;

            ResultSetMessages = string.Format("Database compacted to {0:0,0.0} from {1:0,0.0} bytes", previous, current);
            CurrentResultsTabIndex = 2;
        }

        public void GenerateSchemaScript()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    QueryStringIsBusy = true;

                    var builder = new StringBuilder();
                    database.Tables.ForEach(table => builder.AppendLine(table.GenerateSchemaScript()));
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Query.Text = builder.ToString();
                        RaisePropertyChanged("Query");
                    });
                    CurrentMainTabIndex = 0;
                }
                finally
                {
                    QueryStringIsBusy = false;
                }
            });
        }

        public void GenerateDataScript()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    QueryStringIsBusy = true;

                    var result = database.Tables.GenerateDataScript(database);
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Query.Text = result;
                        RaisePropertyChanged("Query");
                    });
                    CurrentMainTabIndex = 0;
                }
                finally
                {
                    QueryStringIsBusy = false;
                }
            });
        }

        public void GenerateSchemaAndDataScript()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    QueryStringIsBusy = true;

                    var result = database.Tables.GenerateSchemaAndDataScript(database);
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        Query.Text = result;
                        RaisePropertyChanged("Query");
                    });
                    CurrentMainTabIndex = 0;
                }
                finally
                {
                    QueryStringIsBusy = false;
                }
            });
        }
    }
}