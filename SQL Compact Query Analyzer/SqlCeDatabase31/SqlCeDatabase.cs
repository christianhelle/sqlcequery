using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Text;
using System.Diagnostics;
using System.Data.SqlClient;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace ChristianHelle.DatabaseTools.SqlCe
{
    public class SqlCeDatabase : ISqlCeDatabase
    {
        public SqlCeDatabase(string connectionString)
            : this("SqlCeCodeGen", connectionString)
        {
        }

        public SqlCeDatabase(string defaultNamespace, string connectionString)
        {
            Namespace = defaultNamespace;
            ConnectionString = connectionString;

            //Verify();
            //AnalyzeDatabase();
        }

        public void Verify()
        {
            using (var engine = new SqlCeEngine(ConnectionString))
                engine.Verify();
        }

        public void Shrink()
        {
            using (var engine = new SqlCeEngine(ConnectionString))
                engine.Shrink();
        }

        public void Compact()
        {
            using (var engine = new SqlCeEngine(ConnectionString))
                engine.Compact(null);
        }

        public void Upgrade()
        {
            throw new NotImplementedException("This method is not supported in SQL Server CE 3.1");
        }

        public object GetTableData(Table table)
        {
            using (var conn = new SqlCeConnection(ConnectionString))
            using (var adapter = new SqlCeDataAdapter("SELECT * FROM " + table.Name, conn))
            {
                var dataTable = new DataTable(table.DisplayName);
                adapter.Fill(dataTable);
                return dataTable;
            }
        }

        public object GetTableData(string tableName, string columnName)
        {
            using (var conn = new SqlCeConnection(ConnectionString))
            using (var adapter = new SqlCeDataAdapter(string.Format("SELECT {0} FROM {1}", columnName, tableName), conn))
            {
                var dataTable = new DataTable(tableName);
                adapter.Fill(dataTable);
                return dataTable;
            }
        }

        public DataTable ExecuteQuery(string query, StringBuilder errors, StringBuilder messages)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                using (var conn = new SqlCeConnection(ConnectionString))
                {
                    conn.InfoMessage += (sender, e) =>
                    {
                        messages.AppendLine(e.Message);
                        foreach (SqlCeError error in e.Errors)
                            errors.AppendLine(error.ToString());
                    };

                    using (var adapter = new SqlCeDataAdapter(query, conn))
                    {
                        var dataTable = new DataTable();
                        adapter.Fill(dataTable);
                        messages.AppendLine(string.Format("Retrieved {0} row(s)", dataTable.Rows.Count));

                        return dataTable;
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
                messages.AppendLine();
                messages.AppendLine("Executed in " + stopwatch.Elapsed);
            }

            return null;
        }

        public void SaveTableDataChanges(DataTable TableData)
        {
            if (TableData == null)
                return;

            using (var conn = new SqlCeConnection(ConnectionString))
            using (var adapter = new SqlCeDataAdapter("SELECT * FROM " + TableData.TableName, conn))
            using (var commands = new SqlCeCommandBuilder(adapter))
                adapter.Update(TableData);
        }

        public string Namespace { get; set; }
        public List<Table> Tables { get; set; }
        public string ConnectionString { get; private set; }

        public void AnalyzeDatabase()
        {
            var engine = new SqlCeEngine(ConnectionString);
            var tables = engine.GetTables();
            if (tables == null) return;

            var tableList = GetTableInformation(ConnectionString, tables);
            FetchPrimaryKeys(tableList, ConnectionString);
        }

        private void FetchPrimaryKeys(Dictionary<string, Table> tableList, string connectionString)
        {
            Tables = new List<Table>(tableList.Values);
            foreach (var table in Tables)
            {
                using (var conn = new SqlCeConnection(connectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"SELECT COLUMN_NAME FROM INFORMATION_SCHEMA.INDEXES WHERE TABLE_NAME=@Name AND PRIMARY_KEY=1";
                    cmd.Parameters.AddWithValue("@Name", table.Name);
                    var primaryKeyColumnName = cmd.ExecuteScalar() as string;
                    if (primaryKeyColumnName != null)
                    {
                        table.PrimaryKeyColumnName = primaryKeyColumnName;
                        if (primaryKeyColumnName.Contains(" "))
                            table.PrimaryKeyColumnName = string.Format("[{0}]", primaryKeyColumnName);
                        if (table.Columns.ContainsKey(table.PrimaryKeyColumnName))
                            table.Columns[table.PrimaryKeyColumnName].IsPrimaryKey = true;
                    }

                    var constraints = new List<string>();
                    cmd.CommandText = @"SELECT CONSTRAINT_NAME from INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME=@Name AND CONSTRAINT_TYPE='FOREIGN KEY'";
                    using (var reader = cmd.ExecuteReader())
                        while (reader.Read())
                            constraints.Add(reader[0].ToString());

                    foreach (var constraint in constraints)
                    {
                        cmd.CommandText = @"SELECT COLUMN_NAME from INFORMATION_SCHEMA.KEY_COLUMN_USAGE WHERE CONSTRAINT_NAME=@Name";
                        cmd.Parameters["@Name"].Value = constraint;
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var key = reader.GetString(0);
                                if (table.Columns.ContainsKey(key))
                                    table.Columns[key].IsForeignKey = true;
                            }
                        }

                    }
                }
            }
        }

        private static Dictionary<string, Table> GetTableInformation(string connectionString, ICollection<string> tables)
        {
            var tableList = new Dictionary<string, Table>(tables.Count);
            foreach (var tableName in tables)
            {
                string table = tableName;
                Trace.WriteLine("Analyazing " + table);

                if (table.Contains(" "))
                    table = string.Format("[{0}]", table);
                var schema = new DataTable(table);

                using (var connection = new SqlCeConnection(connectionString))
                using (var command = connection.CreateCommand())
                {
                    connection.Open();

                    command.CommandText = "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='" + tableName + "'";
                    using (var adapter = new SqlCeDataAdapter(command))
                        adapter.Fill(schema);

                    var columnDescriptions = new DataTable();
                    command.CommandText = @"SELECT * FROM " + table;
                    using (var adapter = new SqlCeDataAdapter(command))
                        adapter.Fill(columnDescriptions);

                    var item = new Table
                    {
                        Name = table,
                        DisplayName = tableName,
                        ClassName = tableName.Replace(" ", string.Empty),
                        Columns = new Dictionary<string, Column>(schema.Rows.Count)
                    };

                    foreach (DataRow row in schema.Rows)
                    {
                        var displayName = row.Field<string>("COLUMN_NAME");
                        var name = displayName;
                        if (name.Contains(" "))
                            name = string.Format("[{0}]", name);
                        var column = new Column
                        {
                            Name = name,
                            DisplayName = displayName,
                            FieldName = displayName.Replace(" ", string.Empty),
                            DatabaseType = row.Field<string>("DATA_TYPE"),
                            MaxLength = row.Field<int?>("CHARACTER_MAXIMUM_LENGTH"),
                            ManagedType = columnDescriptions.Columns[displayName].DataType,
                            AllowsNull = (string.Compare(row.Field<string>("IS_NULLABLE"), "YES", true) == 0),
                            AutoIncrement = row.Field<long?>("AUTOINC_INCREMENT"),
                            AutoIncrementSeed = row.Field<long?>("AUTOINC_SEED"),
                            Ordinal = row.Field<int>("ORDINAL_POSITION")
                        };
                        item.Columns.Add(name, column);
                    }

                    tableList.Add(table, item);
                }
            }
            return tableList;
        }
    }
}
