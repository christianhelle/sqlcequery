using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using GalaSoft.MvvmLight;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Windows.Forms;
using System.IO;
using ChristianHelle.DatabaseTools.SqlCe.CodeGenCore;
using System.Diagnostics;
using System.Data.SqlServerCe;

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

        #endregion

        public void NewDataSource()
        {
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
                Text = "SQL Compact Query Analyzer - Untitled";
            }
        }

        private void AnalyzeDatabase()
        {
            Status = "Analyzing Database...";
            database = new SqlCeDatabase("Data Source=" + dataSource);

            Status = string.Format("Found {0} tables", database.Tables.Count);
            PopulateTables(database.Tables);
        }

        public DataTable ExecuteQuery()
        {
            try
            {
                using (var conn = new SqlCeConnection(database.ConnectionString))
                using (var adapter = new SqlCeDataAdapter(Query.Text, conn))
                {
                    if (ResultSet != null)
                    {
                        ResultSet.Dispose();
                        ResultSet = null;
                    }

                    var dataTable = new DataTable();
                    adapter.Fill(dataTable);
                    return ResultSet = dataTable;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void PopulateTables(IEnumerable<Table> list)
        {
            var tablesNode = new TreeViewItem { Header = "Tables" };
            tablesNode.ExpandSubtree();

            foreach (var item in list)
            {
                var node = new TreeViewItem { Header = item.DisplayName };
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
        }
    }
}
