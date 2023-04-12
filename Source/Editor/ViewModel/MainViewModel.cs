using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Xml;
using System.Xml.Serialization;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Controls;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Misc;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Properties;
using ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.View;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using Application = System.Windows.Application;
using DataFormats = System.Windows.DataFormats;
using IDataObject = System.Windows.IDataObject;
using MenuItem = System.Windows.Controls.MenuItem;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private readonly string platform = $"{IntPtr.Size * 8}-bit";
        private string dataSource;
        private ISqlCeDatabase database;
        private static bool queryExecuting;

        private Table lastSelectedTable;
        private string password;

        public MainViewModel()
        {
            Query = new TextDocument();
            ResultSetXml = new TextDocument();
            RecentFiles = new ObservableCollection<System.Windows.Controls.Control>();

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
        private bool tablePropertiesIsBusy;
        private int currentResultsTabIndex;
        private int currentMainTabIndex;
        private int tableDataCount;
        private string text;
        private string status;
        private string resultSetErrors;
        private string resultSetMessages;
        private TextDocument query;
        private IHighlightingDefinition sqlSyntaxHighlighting;
        private TimeSpan? tableDataExecutionTime;
        private bool displayResultsInGrid;
        private bool displayResultsAsXml;
        private int queryResultCount;
        private TimeSpan? queryExecutionTime;

        public ObservableCollection<TreeViewItem> Tree { get; private set; }

        public ObservableCollection<System.Windows.Controls.Control> RecentFiles { get; set; }

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

        public bool TablePropertiesIsBusy
        {
            get { return tablePropertiesIsBusy; }
            set
            {
                tablePropertiesIsBusy = value;
                RaisePropertyChanged("TablePropertiesIsBusy ");
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

        public int QueryResultCount
        {
            get { return queryResultCount; }
            set
            {
                queryResultCount = value;
                RaisePropertyChanged("QueryResultCount");
            }
        }

        public TimeSpan? QueryExecutionTime
        {
            get { return queryExecutionTime; }
            set
            {
                queryExecutionTime = value;
                RaisePropertyChanged("QueryExecutionTime");
            }
        }

        public TimeSpan? TableDataExecutionTime
        {
            get { return tableDataExecutionTime; }
            set
            {
                tableDataExecutionTime = value;
                RaisePropertyChanged("TableDataExecutionTime");
            }
        }

        public ResultsContainer ResultsContainer { get; set; }

        public DataGridViewEx TableDataGrid { get; set; }

        public DataGridViewEx TablePropertiesGrid { get; set; }

        public ICommand OpenDatabaseCommand
        {
            get { return new SafeRelayCommand(OpenDatabase); }
        }

        public ICommand ExitCommand
        {
            get { return new SafeRelayCommand(Application.Current.Shutdown); }
        }

        public ICommand ExecuteQueryCommand
        {
            get { return new SafeRelayCommand(ExecuteQuery); }
        }

        public ICommand ShrinkCommand
        {
            get { return new SafeRelayCommand(ShrinkDatabase); }
        }

        public ICommand CompactCommand
        {
            get { return new SafeRelayCommand(CompactDatabase); }
        }

        public ICommand ScriptSchemaCommand
        {
            get { return new SafeRelayCommand(GenerateSchemaScript); }
        }

        public ICommand ScriptDataCommand
        {
            get { return new SafeRelayCommand(GenerateDataScript); }
        }

        public ICommand AboutCommand
        {
            get { return new SafeRelayCommand(() => new AboutBox(Application.Current.MainWindow).ShowDialog()); }
        }

        public ICommand AnalyzeDatabaseCommand
        {
            get { return new SafeRelayCommand(AnalyzeDatabase); }
        }

        public ICommand NewDatabaseCommand
        {
            get { return new SafeRelayCommand(CreateNewDatabase); }
        }

        public ICommand ClearRecentFilesCommand
        {
            get { return new SafeRelayCommand(ClearRecentFiles); }
        }

        public ICommand ScriptSchemaAndDataCommand
        {
            get { return new SafeRelayCommand(GenerateSchemaAndDataScript); }
        }

        public ICommand ExportDataCommand
        {
            get { return new SafeRelayCommand(ExportData); }
        }

        #endregion

        public void Load()
        {
            DisplayResultsInGrid = Settings.Default.DisplayResultsInGrid;
            DisplayResultsAsXml = Settings.Default.DisplayResultsAsXml;

            LoadRecentFiles();
        }

        public void OpenDatabase()
        {
            Text = $"SQL Compact Query Analyzer ({platform})";

            using (var dialog = new OpenFileDialog())
            {
                //dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                dialog.Filter = "Database files (*.sdf)|*.sdf";
                if (dialog.ShowDialog() == DialogResult.Cancel)
                    return;

                ResetFields();

                dataSource = dialog.FileName;
                AnalyzeDatabase();
            }
        }

        private void ResetFields()
        {
            Tree = null;
            ResetTableData();
            ResultsContainer.Clear();
            ResultSetMessages = ResultSetErrors = ResultSetXml.Text = Query.Text = string.Empty;
            RaisePropertyChanged("Tree");
            RaisePropertyChanged("Query");
            RaisePropertyChanged("ResultSetXml");
            CurrentMainTabIndex = 0;
            lastSelectedTable = null;
            dataSource = null;
            database = null;
            password = null;
        }

        private void ResetTableData()
        {
            var table = TableDataGrid.DataSource as DataTable;
            TableDataGrid.DataSource = null;
            if (table != null)
                table.Dispose();

            table = TablePropertiesGrid.DataSource as DataTable;
            TablePropertiesGrid.DataSource = null;
            if (table != null)
                table.Dispose();

            TableDataCount = 0;
            TableDataExecutionTime = null;
        }

        private void AnalyzeDatabase()
        {
            Task.Factory.StartNew(() =>
            {
                try
                {
                    if (!File.Exists(dataSource))
                        throw new InvalidOperationException("Unable to find " + dataSource);

                    Text = $"SQL Compact Query Analyzer ({platform}) - {dataSource}";

                    var fileInfo = new FileInfo(dataSource);
                    fileInfo.Attributes &= ~FileAttributes.ReadOnly;

                    database = SqlCeDatabaseFactory.Create(GetConnectionString());
                    while (!database.VerifyConnectionStringPassword())
                    {
                        var recentFiles = GetRecentFiles();
                        var savedFileInfo = recentFiles?.FirstOrDefault(x =>
                            x.FilePath.ToLowerInvariant() == dataSource.ToLowerInvariant());
                        if (savedFileInfo != null)
                        {
                            password = savedFileInfo.Password;
                        }
                        else
                        {
                            bool? result = null;
                            PasswordWindow window = null;
                            Application.Current.Dispatcher.Invoke((Action) delegate
                            {
                                window = new PasswordWindow();
                                result = window.ShowDialog();
                            });
                            if (result != true)
                                return;
                            password = window.Password;
                        }                        
                        database.ConnectionString = GetConnectionString(4091);
                    }

                    AnalyzingTablesIsBusy = true;

                    var stopwatch = Stopwatch.StartNew();

                    database.AnalyzeDatabase();

                    ResultSetMessages = "Analyzed database in " + stopwatch.Elapsed;
                    stopwatch.Stop();

                    Application.Current.Dispatcher.Invoke((Action)PopulateTables);

                    AddRecentFile(dataSource, password);
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

        private string GetConnectionString(int? maxDatabaseSize = null)
        {
            var connstr = string.Format("Data Source={0}; Password={1};", dataSource, password);
            if (maxDatabaseSize.HasValue)
                connstr += string.Format(" Max Database Size={0};", maxDatabaseSize.Value);
            return connstr;
        }

        public void PopulateTables()
        {
            var tablesNode = new TreeViewItem { Header = "Tables" };
            tablesNode.Selected += OnTreeViewItemSelected;
            tablesNode.ExpandSubtree();

            foreach (var item in database.Tables)
            {
                var table = new TreeViewItem { Header = item.DisplayName, Tag = item /*, FontWeight = FontWeights.Bold*/ };
                table.Selected += OnTreeViewItemSelected;
                //table.ExpandSubtree();
                tablesNode.Items.Add(table);

                var columns = new TreeViewItem { Header = "Columns", FontWeight = FontWeights.Normal };
                //columns.ExpandSubtree();
                table.Items.Add(columns);

                foreach (var column in item.Columns)
                {
                    var columnNode = new TreeViewItem();
                    columnNode.Selected += OnTreeViewItemSelected;
                    columnNode.Header = column.Value.DisplayName;
                    //columnNode.Tag = new KeyValuePair<string, string>(item.Name, column.Value.Name);
                    if (column.Value.IsPrimaryKey)
                        columnNode.Items.Add("Primary Key");
                    if (column.Value.IdentitySeed.HasValue)
                        columnNode.Items.Add("Identity Seed: " + column.Value.IdentitySeed.GetValueOrDefault(1));
                    if (column.Value.IdentityIncrement.HasValue)
                        columnNode.Items.Add("Identity Increment: " + column.Value.IdentityIncrement.GetValueOrDefault(1));
                    if (column.Value.IsForeignKey)
                        columnNode.Items.Add("Foreign Key");
                    columnNode.Items.Add("Ordinal Position:  " + column.Value.Ordinal);
                    columnNode.Items.Add(new TreeViewItem { Header = "Database Type:  " + column.Value.DatabaseType });
                    columnNode.Items.Add(new TreeViewItem { Header = ".NET CLR Type:  " + column.Value.ManagedType });
                    columnNode.Items.Add(new TreeViewItem { Header = "Allows Null:  " + column.Value.AllowsNull });
                    if (column.Value.ManagedType == typeof(string))
                        columnNode.Items.Add("Max Length:  " + column.Value.MaxLength);
                    columns.Items.Add(columnNode);
                }

                var indexes = new TreeViewItem { Header = "Indexes", FontWeight = FontWeights.Normal };
                table.Items.Add(indexes);

                if (item.Indexes != null)
                {
                    foreach (var index in item.Indexes)
                    {
                        var indexNode = new TreeViewItem { Header = index.Name };
                        indexNode.Items.Add(new TreeViewItem { Header = "Column:  " + index.Column.DisplayName });
                        indexNode.Items.Add(new TreeViewItem { Header = "Unique:  " + index.Unique });
                        indexNode.Items.Add(new TreeViewItem { Header = "Clustered:  " + index.Clustered });
                        indexes.Items.Add(indexNode);
                    }
                }
            }

            if (Tree == null)
                Tree = new ObservableCollection<TreeViewItem>();

            Tree.Clear();
            Tree.Add(GetDatabaseInformationTree());
            Tree.Add(tablesNode);
            RaisePropertyChanged("Tree");
        }

        void OnTreeViewItemSelected(object sender, RoutedEventArgs e)
        {
            var item = (TreeViewItem)sender;
            if (item.Tag == null)
            {
                lastSelectedTable = null;
                ResetTableData();
                return;
            }

            var table = item.Tag as Table;
            LoadTableDataAndProperties(table);
        }

        private TreeViewItem GetDatabaseInformationTree()
        {
            try
            {
                var fileInfo = new FileInfo(dataSource);

                var propertiesNode = new TreeViewItem { Header = "Database Information" };
                propertiesNode.Selected += OnTreeViewItemSelected;
                propertiesNode.Items.Add(new TreeViewItem { Header = "File name:  " + fileInfo.Name });
                propertiesNode.Items.Add(new TreeViewItem { Header = "Date Created:  " + fileInfo.CreationTime });
                propertiesNode.Items.Add(new TreeViewItem { Header = "Last Modified:  " + fileInfo.LastWriteTime });
                propertiesNode.Items.Add(new TreeViewItem { Header = "Version:  " + SqlCeDatabaseFactory.GetRuntimeVersion(dataSource) });
                propertiesNode.Items.Add(new TreeViewItem { Header = "Password Protected:  " + !string.IsNullOrEmpty(password) });
                propertiesNode.Items.Add(new TreeViewItem { Header = string.Format(new FileSizeFormatProvider(), "File size:  {0:fs}", fileInfo.Length) });
                propertiesNode.ExpandSubtree();

                var schemaSummaryNode = new TreeViewItem { Header = "Schema Summary" };
                schemaSummaryNode.Selected += OnTreeViewItemSelected;
                schemaSummaryNode.Items.Add(new TreeViewItem { Header = "Tables:  " + database.Tables.Count });
                schemaSummaryNode.Items.Add(new TreeViewItem { Header = "Columns:  " + database.Tables.Sum(c => c.Columns.Count) });
                schemaSummaryNode.Items.Add(new TreeViewItem { Header = "Primary keys:  " + database.Tables.Count(c => !string.IsNullOrEmpty(c.PrimaryKeyColumnName)) });
                //schemaSummaryNode.Items.Add(new TreeViewItem { Header = "Foreign keys:  " + database.Tables.Sum(c => c.Columns.Where(x => x.Value.IsForeignKey).Count()) });
                schemaSummaryNode.Items.Add(new TreeViewItem { Header = "Identity fields:  " + database.Tables.Sum(c => c.Columns.Count(x => x.Value.IdentityIncrement.HasValue)) });
                schemaSummaryNode.Items.Add(new TreeViewItem { Header = "Nullable fields:  " + database.Tables.Sum(c => c.Columns.Count(x => x.Value.AllowsNull)) });
                propertiesNode.Items.Add(schemaSummaryNode);

                var schemaInformationNode = new TreeViewItem { Header = "Schema Information" };
                schemaInformationNode.Items.Add(CreateSchemaInformationNode("Columns", "INFORMATION_SCHEMA.COLUMNS"));
                schemaInformationNode.Items.Add(CreateSchemaInformationNode("Indexes", "INFORMATION_SCHEMA.INDEXES"));
                schemaInformationNode.Items.Add(CreateSchemaInformationNode("Key Column Usage", "INFORMATION_SCHEMA.KEY_COLUMN_USAGE"));
                schemaInformationNode.Items.Add(CreateSchemaInformationNode("Tables", "INFORMATION_SCHEMA.TABLES"));
                schemaInformationNode.Items.Add(CreateSchemaInformationNode("Table Constraints", "INFORMATION_SCHEMA.TABLE_CONSTRAINTS"));
                schemaInformationNode.Items.Add(CreateSchemaInformationNode("Provider Types", "INFORMATION_SCHEMA.PROVIDER_TYPES"));
                schemaInformationNode.Items.Add(CreateSchemaInformationNode("Referential Constraints", "INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS"));
                propertiesNode.Items.Add(schemaInformationNode);

                return propertiesNode;
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);

                return new TreeViewItem
                {
                    Header = "Unable to retrieve database information",
                    FontStyle = FontStyles.Italic
                };
            }
        }

        private TreeViewItem CreateSchemaInformationNode(string header, string viewName)
        {
            var item = new TreeViewItem { Header = header, Tag = new Table { Name = viewName } };
            item.Selected += (sender, e) => LoadTableDataAndProperties(((TreeViewItem)sender).Tag as Table, true, true, false);
            return item;
        }

        public void ExecuteQuery()
        {
            ExecuteQuery(null);
        }

        public void ExecuteQuery(string sql)
        {
            if (database == null || queryExecuting)
                return;

            Task.Factory.StartNew(() =>
            {
                var errors = new StringBuilder();
                var messages = new StringBuilder();
                var stopwatch = Stopwatch.StartNew();

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

                    int resultCount;
                    var result = database.ExecuteQuery(sql, errors, messages, out resultCount) as DataSet;
                    QueryResultCount = resultCount;

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
                    QueryExecutionTime = stopwatch.Elapsed;
                    ResultSetMessages = messages.ToString();
                    ResultSetErrors = errors.ToString();
                    QueryStringIsBusy = QueryIsBusy = false;
                    queryExecuting = false;

                    if (!string.IsNullOrEmpty(ResultSetErrors) && ResultsContainer.Count == 0)
                        CurrentResultsTabIndex = 3;

                    sql = sql.ToLower();
                    if (sql.Contains("create") || sql.Contains("alter") || sql.Contains("drop") ||
                        sql.Contains("insert") || sql.Contains("update") || sql.Contains("delete"))
                        AnalyzeDatabase();
                }
            });
        }

        public void LoadTableDataAndProperties(Table table, bool readOnly = false, bool resizeColumns = false, bool displaySchemaInfo = true)
        {
            if (database == null || table == null || lastSelectedTable == table) return;
            lastSelectedTable = table;

            ResetTableData();

            Task.Factory.StartNew(() =>
            {
                try
                {
                    TableDataIsBusy = true;
                    var sw = Stopwatch.StartNew();
                    var dataTable = database.GetTableData(table) as DataTable;
                    Application.Current.Dispatcher.Invoke((Action)delegate
                    {
                        TableDataGrid.DataSource = dataTable;
                        TableDataGrid.ReadOnly = readOnly;
                        if (resizeColumns)
                            TableDataGrid.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
                    });
                    //CurrentMainTabIndex = 1;
                    TableDataCount = dataTable != null ? dataTable.Rows.Count : 0;
                    TableDataExecutionTime = sw.Elapsed;

                    if (displaySchemaInfo)
                    {
                        var propertiesTable = database.GetTableProperties(table) as DataTable;
                        Application.Current.Dispatcher.Invoke((Action)delegate { TablePropertiesGrid.DataSource = propertiesTable; });
                    }
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
            const string name = "ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Resources.SQL-Mode.xshd";
            using (var stream = Assembly.GetAssembly(GetType()).GetManifestResourceStream(name))
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
                if (String.Compare(ext, ".sdf", StringComparison.OrdinalIgnoreCase) == 0)
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
            ext = ext.ToLower();
            if (String.Compare(ext, ".sdf", StringComparison.OrdinalIgnoreCase) != 0)
                return;

            dataSource = filePaths[0];
            AnalyzeDatabase();
        }

        public void ShrinkDatabase()
        {
            if (database == null)
                return;

            try
            {
                DatabaseIsBusy = true;
                CurrentResultsTabIndex = 2;
                //ResultSetMessages = "Shrinking database...";

                //var fileInfo = new FileInfo(dataSource);
                //var previous = fileInfo.Length;

                database.Shrink();

                //fileInfo.Refresh();
                //var current = fileInfo.Length;

                //ResultSetMessages = string.Format("Database shrinked to {0:0,0.0} from {1:0,0.0} bytes", previous, current);
                AnalyzeDatabase();
            }
            catch (Exception e)
            {
                ResultSetMessages = string.Empty;
                ResultSetErrors = e.ToString();
                CurrentResultsTabIndex = 3;
            }
            finally
            {
                DatabaseIsBusy = false;
            }
        }

        public void CompactDatabase()
        {
            if (database == null)
                return;

            try
            {
                DatabaseIsBusy = true;
                CurrentResultsTabIndex = 2;
                //ResultSetMessages = "Compacting database...";

                //var fileInfo = new FileInfo(dataSource);
                //var previous = fileInfo.Length;

                database.Compact();

                //fileInfo.Refresh();
                //var current = fileInfo.Length;

                //ResultSetMessages = string.Format("Database compacted to {0:0,0.0} from {1:0,0.0} bytes", previous, current);
                AnalyzeDatabase();
            }
            catch (Exception e)
            {
                ResultSetMessages = string.Empty;
                ResultSetErrors = e.ToString();
                CurrentResultsTabIndex = 3;
            }
            finally
            {
                DatabaseIsBusy = false;
            }
        }

        public void GenerateSchemaScript()
        {
            if (database == null)
                return;

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
            if (database == null)
                return;

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
            if (database == null)
                return;

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

        private void ExportData()
        {
            MessageBox.Show("This feature is yet to be implemented...", "Coming soon");
        }

        /*
                public void RenameObject(object treeViewItem)
                {
                    var window = new RenameObjectWindow();

                    if (treeViewItem is Table)
                        window.ViewModel = new RenameObjectViewModel(database, treeViewItem as Table);
                    else if (treeViewItem is Column)
                        window.ViewModel = new RenameObjectViewModel(database, treeViewItem as Column);

                    if (window.ViewModel != null)
                        window.ShowDialog();
                }
        */

        private void CreateNewDatabase()
        {
            var window = new CreateDatabaseWindow();
            var dialogResult = window.ShowDialog();
            if (!dialogResult.GetValueOrDefault())
                return;

            var viewModel = window.DataContext as CreateDatabaseViewModel;
            if (viewModel == null)
                return;

            Text = $"SQL Compact Query Analyzer ({platform})";
            Tree = null;
            ResetTableData();
            ResultsContainer.Clear();
            ResultSetMessages = ResultSetErrors = ResultSetXml.Text = Query.Text = string.Empty;
            RaisePropertyChanged("Tree");
            RaisePropertyChanged("Query");
            RaisePropertyChanged("ResultSetXml");
            CurrentMainTabIndex = 0;
            lastSelectedTable = null;
            database = null;

            dataSource = viewModel.Filename;
            password = viewModel.Password;
            AnalyzeDatabase();
        }

        #region Recent Files

        private List<SavedFileInfo> GetRecentFiles()
        {
            List<SavedFileInfo> recentFiles = null;

            var filename = GetRecentsXmlFile();
            if (File.Exists(filename))
            {
                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    //var serializer = new XmlSerializer(typeof(List<string>));
                    var serializer = XmlSerializer.FromTypes(new[] { typeof(List<SavedFileInfo>) })[0];

                    recentFiles = serializer.Deserialize(stream) as List<SavedFileInfo>;
                }
            }

            return recentFiles;
        }

        private void LoadRecentFiles()
        {
            var recentFiles = GetRecentFiles();

            PopulateRecentFiles(recentFiles);
        }

        private void PopulateRecentFiles(IEnumerable<SavedFileInfo> recentFiles)
        {
            Application.Current.Dispatcher.Invoke(new Action(() => PopulateRecentFilesMethod(recentFiles)));
        }

        private void PopulateRecentFilesMethod(IEnumerable<SavedFileInfo> recentFiles)
        {
            RecentFiles.Clear();

            if (recentFiles != null)
            {
                foreach (var file in recentFiles)
                {
                    var fileInfo = new FileInfo(file.FilePath);
                    var menuItem = new MenuItem { Header = fileInfo.FullName, Tag = file.FilePath };
                    menuItem.Click += OnRecentFileClick;
                    RecentFiles.Add(menuItem);
                }
            }

            RecentFiles.Add(new Separator());
            var clearListMenuItem = new MenuItem { Header = "Clear Recent File List" };
            clearListMenuItem.Click += (sender, args) => ClearRecentFiles();
            RecentFiles.Add(clearListMenuItem);

            RaisePropertyChanged("RecentFiles");
        }

        private static string GetRecentsXmlFile()
        {
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SQL Compact Query Analyzer");
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);

            var filename = Path.Combine(path, "RecentEx.xml");
            return filename;
        }

        private void OnRecentFileClick(object sender, RoutedEventArgs args)
        {
            ResetFields();
            dataSource = ((MenuItem)sender).Tag.ToString();
            AnalyzeDatabase();
        }

        private void ClearRecentFiles()
        {
            var filename = GetRecentsXmlFile();
            if (File.Exists(filename))
                File.Delete(filename);
            LoadRecentFiles();
        }

        private void AddRecentFile(string file, string password)
        {
            var filename = GetRecentsXmlFile();
            var serializer = new XmlSerializer(typeof(List<SavedFileInfo>));
            var newSavedFileInfo = new SavedFileInfo(file, password);
            if (!File.Exists(filename))
            {
                using (var stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    serializer.Serialize(stream, new List<SavedFileInfo>{ newSavedFileInfo });
                }
            }
            else
            {
                List<SavedFileInfo> recentFiles;
                using (var stream = new FileStream(filename, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    recentFiles = serializer.Deserialize(stream) as List<SavedFileInfo>;
                    if (recentFiles == null) return;
                }
                using (var stream = new FileStream(filename, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    var existingEntry = recentFiles.FirstOrDefault(x => x.FilePath.ToLowerInvariant() == file.ToLowerInvariant());
                    if (existingEntry != null) 
                        recentFiles.Remove(existingEntry);
                    recentFiles.Insert(0, newSavedFileInfo);
                    serializer.Serialize(stream, recentFiles);
                    PopulateRecentFiles(recentFiles);
                }
            }
        }

        #endregion
    }
}