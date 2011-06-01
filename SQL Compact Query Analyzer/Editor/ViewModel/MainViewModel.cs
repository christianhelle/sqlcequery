using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlServerCe;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Xml;
using ChristianHelle.DatabaseTools.SqlCe.CodeGenCore;
using GalaSoft.MvvmLight;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Windows.Input;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        string dataSource;
        SqlCeDatabase database;

        public MainViewModel()
        {
            Tree = new ObservableCollection<TreeViewItem>();
            Query = new TextDocument();

            ProcessCommandLineArguments(Environment.GetCommandLineArgs());
            LoadSqlSyntaxHighlighter();
        }

        public bool LaunchedWithArgument { get; private set; }

        #region Data Binding

        public ObservableCollection<TreeViewItem> Tree { get; set; }

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

        private DataTable resultSet;
        public DataTable ResultSet
        {
            get { return resultSet; }
            set
            {
                resultSet = value;
                RaisePropertyChanged("ResultSet");
            }
        }

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

        private IHighlightingDefinition syntaxHighlighting;
        public IHighlightingDefinition SyntaxHighlighting
        {
            get { return syntaxHighlighting; }
            set
            {
                syntaxHighlighting = value;
                RaisePropertyChanged("SyntaxHighlighting");
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
                var sw = Stopwatch.StartNew();

                AnalyzeDatabase();

                Status = "Executed in " + sw.Elapsed;
            }
        }

        private void AnalyzeDatabase()
        {
            Status = "Analyzing Database...";
            database = new SqlCeDatabase("Data Source=" + dataSource);
            Text = "SQL Compact Query Analyzer" + " - " + new FileInfo(dataSource).Name;

            Status = string.Format("Found {0} tables", database.Tables.Count);
            PopulateTables(database.Tables);
        }

        public DataTable ExecuteQuery()
        {
            var errors = new StringBuilder();
            var messages = new StringBuilder();
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var conn = new SqlCeConnection(database.ConnectionString))
                {
                    conn.InfoMessage += (sender, e) =>
                    {
                        messages.AppendLine(e.Message);
                        foreach (SqlCeError error in e.Errors)
                            errors.AppendLine(error.ToString());
                    };
                    conn.Disposed += (sender, e) =>
                    {
                        if (errors.Length > 0) return;
                        messages.AppendLine();
                        messages.AppendLine("Executed in " + stopwatch.Elapsed);
                    };

                    using (var adapter = new SqlCeDataAdapter(Query.Text, conn))
                    {
                        if (ResultSet != null)
                        {
                            ResultSet.Dispose();
                            ResultSet = null;
                        }

                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        messages.AppendLine(string.Format("Retrieved {0} row(s)", dataTable.Rows.Count));

                        CurrentTabIndex = 0;
                        return ResultSet = dataTable;
                    }
                }
            }
            catch (SqlCeException e)
            {
                foreach (SqlCeError error in e.Errors)
                    errors.AppendLine(error.Message);
            }
            catch (Exception e)
            {
                errors.AppendLine(e.Message);
            }
            finally
            {
                ResultSetMessages = messages.ToString();
                ResultSetErrors = errors.ToString();
            }

            CurrentTabIndex = 2;
            return null;
        }

        public void PopulateTables(IEnumerable<Table> list)
        {
            var tablesNode = new TreeViewItem { Header = "Tables" };
            tablesNode.ExpandSubtree();

            foreach (var item in list)
            {
                var node = new TreeViewItem { Header = item.DisplayName };
                node.Selected += TableSelected;
                node.Tag = item;
                node.FontWeight = FontWeights.Bold;
                node.ExpandSubtree();
                tablesNode.Items.Add(node);

                var columns = new TreeViewItem { Header = "Columns" };
                columns.FontWeight = FontWeights.Normal;
                columns.ExpandSubtree();
                node.Items.Add(columns);

                foreach (var column in item.Columns)
                {
                    var columnNode = new TreeViewItem { Header = column.Value.DisplayName };
                    columnNode.Tag = new KeyValuePair<string, string>(item.Name, column.Value.Name);
                    //columnNode.Items.Add("Ordinal Position - " + column.Value.Ordinal);
                    if (column.Value.IsPrimaryKey)
                        columnNode.Items.Add("Primary Key");
                    if (column.Value.AutoIncrement.HasValue)
                        columnNode.Items.Add("Auto Increment");
                    if (column.Value.IsForeignKey)
                        columnNode.Items.Add("Foreign Key");
                    columnNode.Items.Add(new TreeViewItem { Header = "Database Type - " + column.Value.DatabaseType });
                    columnNode.Items.Add(new TreeViewItem { Header = "Managed Type - " + column.Value.ManagedType });
                    if (column.Value.ManagedType.Equals(typeof(string)))
                        columnNode.Items.Add("Max Length - " + column.Value.MaxLength);
                    columnNode.Items.Add(new TreeViewItem { Header = "Allows Null - " + column.Value.AllowsNull });
                    columns.Items.Add(columnNode);
                }
            }

            Tree.Clear();
            Tree.Add(tablesNode);
            RaisePropertyChanged("Tree");
        }

        private void TableSelected(object sender, RoutedEventArgs e)
        {
            var treeViewItem = (TreeViewItem)sender;
            LoadTableData(treeViewItem.Tag as Table);
        }

        public void LoadTableData(Table table)
        {
            using (var conn = new SqlCeConnection(database.ConnectionString))
            using (var adapter = new SqlCeDataAdapter("SELECT * FROM " + table.Name, conn))
            {
                if (TableData != null)
                {
                    TableData.Dispose();
                    TableData = null;
                }

                var dataTable = new DataTable(table.Name);
                adapter.Fill(dataTable);
                TableData = dataTable;
            }
        }

        public void SaveTableDataChanges()
        {
            if (TableData == null)
                return;

            using (var conn = new SqlCeConnection(database.ConnectionString))
            using (var adapter = new SqlCeDataAdapter("SELECT * FROM " + TableData.TableName, conn))
            using (var commands = new SqlCeCommandBuilder(adapter))
                adapter.Update(TableData);
        }

        public void LoadSqlSyntaxHighlighter()
        {
            using (var stream = Assembly.GetAssembly(GetType()).GetManifestResourceStream("ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.Resources.SQL-Mode.xshd"))
            using (var reader = new XmlTextReader(stream))
            {
                var xshd = HighlightingLoader.LoadXshd(reader);
                SyntaxHighlighting = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
            }
        }

        public void ProcessCommandLineArguments(string[] args)
        {
            if (args != null && args.Length == 1)
            {
                LaunchedWithArgument = true;
                dataSource = args[0];

                var ext = Path.GetExtension(dataSource);
                if (string.Compare(ext, ".sdf", true) == 0)
                {
                    AnalyzeDatabase();
                }
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
            database.Shrink();
        }

        public void CompactDatabase()
        {
            database.Compact();
        }
    }
}
