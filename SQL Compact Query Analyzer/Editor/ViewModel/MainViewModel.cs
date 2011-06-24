using System;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Controls;
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
        private static bool queryExecuting;

        public MainViewModel()
        {
            Query = new TextDocument();
            ResultSetXml = new TextDocument();

            var args = Environment.GetCommandLineArgs();
            LaunchedWithArgument = args.Length == 2;

            LoadSqlSyntaxHighlighter();
        }

        public bool LaunchedWithArgument { get; private set; }

        #region Data Binding

        private bool databaseIsBusy;
        private bool queryIsBusy;
        private bool tableDataIsBusy;
        private bool queryStringIsBusy;
        private bool analyzingTablesIsBusy;
        private int currentResultsTabIndex;
        private int currentMainTabIndex;
        private int tableDataCount;
        private string text;
        private string status;
        private string resultSetErrors;
        private string resultSetMessages;
        private TextDocument query;
        private IHighlightingDefinition sqlSyntaxHighlighting;
        private ObservableCollection<Table> tables;
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

        public TextDocument ResultSetXml { get; private set; }

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

        public bool DatabaseIsBusy
        {
            get { return databaseIsBusy; }
            set
            {
                databaseIsBusy = value;
                RaisePropertyChanged("DatabaseIsBusy");
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

        public int TableDataCount
        {
            get { return tableDataCount; }
            set
            {
                tableDataCount = value;
                RaisePropertyChanged("TableDataCount");
            }
        }

        public ResultsContainer ResultsContainer { get; set; }

        public DataGridViewEx TableDataGrid { get; set; }

        #endregion

        public void Load()
        {
            DisplayResultsInGrid = Settings.Default.DisplayResultsInGrid;
            DisplayResultsAsXml = Settings.Default.DisplayResultsAsXml;
        }

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

                ResetTableData();
                Query.Text = string.Empty;
                RaisePropertyChanged("Query");
                CurrentMainTabIndex = 0;
            }
        }

        private void ResetTableData()
        {
            var table = TableDataGrid.DataSource as DataTable;
            if (table != null)
                table.Dispose();
            TableDataCount = 0;
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

                    Text = "SQL Compact Query Analyzer" + " - " + dataSource;

                    var collection = new ObservableCollection<Table>();
                    foreach (var table in database.Tables)
                        collection.Add(table);
                    Tables = collection;
                }
                catch (Exception e)
                {
                    ResultSetErrors = e.ToString();
                    CurrentResultsTabIndex = 3;
                }
                finally
                {
                    AnalyzingTablesIsBusy = false;
                }
            });
        }

        public void ExecuteQuery(string sql = null)
        {
            if (queryExecuting)
                return;

            Task.Factory.StartNew(() =>
            {
                var errors = new StringBuilder();
                var messages = new StringBuilder();

                try
                {
                    queryExecuting = true;
                    QueryStringIsBusy = QueryIsBusy = true;
                    ResultSetMessages = "Executing query...";
                    ResultSetErrors = string.Empty;

                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        if (string.IsNullOrEmpty(sql))
                            sql = Query.Text;

                        ResultSetXml.Text = string.Empty;
                        ResultsContainer.Clear();
                    });

                    var result = database.ExecuteQuery(sql, errors, messages) as DataSet;
                    if (result == null)
                        return;

                    if (DisplayResultsInGrid)
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            foreach (DataTable table in result.Tables)
                                ResultsContainer.Add(new DataGridViewEx { DataSource = table });
                        });

                    if (DisplayResultsAsXml)
                    {
                        var sb = new StringBuilder();
                        using (var writer = new StringWriter(sb))
                        using (var xml = new XmlTextWriter(writer) { Formatting = Formatting.Indented })
                        {
                            result.WriteXml(xml);
                            writer.WriteLine(string.Empty);
                        }
                        Application.Current.Dispatcher.Invoke((Action)delegate
                        {
                            ResultSetXml.Text = sb.ToString();
                            RaisePropertyChanged("ResultSetXml");
                        });
                    }

                    CurrentResultsTabIndex = result.Tables.Count > 0 ? 0 : 2;
                }
                catch (Exception e)
                {
                    ResultSetErrors = e.ToString();
                    CurrentResultsTabIndex = 3;
                }
                finally
                {
                    ResultSetMessages = messages.ToString();
                    ResultSetErrors = errors.ToString();
                    QueryStringIsBusy = QueryIsBusy = false;
                    queryExecuting = false;
                }
            });
        }

        public void LoadTableData(Table table)
        {
            if (table == null)
                return;

            ResetTableData();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    TableDataIsBusy = true;
                    var dataTable = database.GetTableData(table) as DataTable;
                    Application.Current.Dispatcher.Invoke((Action)delegate { TableDataGrid.DataSource = dataTable; });
                    TableDataCount = dataTable != null ? dataTable.Rows.Count : 0;
                }
                catch (Exception e)
                {
                    ResultSetErrors = e.ToString();
                    CurrentResultsTabIndex = 3;
                }
                finally
                {
                    TableDataIsBusy = false;
                }
            });
        }

        public void SaveTableDataChanges()
        {
            var dataTable = TableDataGrid.DataSource as DataTable;
            database.SaveTableDataChanges(dataTable);
            TableDataCount = dataTable != null ? dataTable.Rows.Count : 0;
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
            if (filePaths == null || filePaths.Length <= 0)
                return;

            var filePath = filePaths[0];
            if (filePath == null)
                return;

            var ext = Path.GetExtension(filePath);
            if (ext != null)
                ext = ext.ToLower();
            if (string.Compare(ext, ".sdf", true) != 0)
                return;

            dataSource = filePaths[0];
            AnalyzeDatabase();
        }

        public void ShrinkDatabase()
        {
            try
            {
                DatabaseIsBusy = true;

                var fi = new FileInfo(dataSource);
                var previous = fi.Length;
                database.Shrink();
                var current = fi.Length;

                ResultSetMessages = string.Format("Database shrinked to {0:0,0.0} from {1:0,0.0} bytes", previous, current);
                CurrentResultsTabIndex = 2;
            }
            catch (Exception e)
            {
                ResultSetErrors = e.ToString();
                CurrentResultsTabIndex = 3;
                DatabaseIsBusy = false;
            }
        }

        public void CompactDatabase()
        {
            try
            {
                DatabaseIsBusy = true;

                var fi = new FileInfo(dataSource);
                var previous = fi.Length;
                database.Compact();
                var current = fi.Length;

                ResultSetMessages = string.Format("Database compacted to {0:0,0.0} from {1:0,0.0} bytes", previous, current);
                CurrentResultsTabIndex = 2;
            }
            catch (Exception e)
            {
                ResultSetErrors = e.ToString();
                CurrentResultsTabIndex = 3;
                DatabaseIsBusy = false;
            }
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
                catch (Exception e)
                {
                    ResultSetErrors = e.ToString();
                    CurrentResultsTabIndex = 3;
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
                catch (Exception e)
                {
                    ResultSetErrors = e.ToString();
                    CurrentResultsTabIndex = 3;
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
                catch (Exception e)
                {
                    ResultSetErrors = e.ToString();
                    CurrentResultsTabIndex = 3;
                }
                finally
                {
                    QueryStringIsBusy = false;
                }
            });
        }
    }
}