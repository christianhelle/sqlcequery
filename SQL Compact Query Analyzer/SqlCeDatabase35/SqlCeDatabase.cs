using System;
using System.Linq;
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
        {
            ConnectionString = connectionString;
        }

        public void Verify()
        {
            try
            {
                using (var engine = new SqlCeEngine(ConnectionString))
                    engine.Verify();
                using (var connection = new SqlCeConnection(ConnectionString))
                    connection.Open();
            }
            catch (SqlCeInvalidDatabaseFormatException)
            {
                MessageBox.Show("The version of the SQL Server Compact Edition database loaded is currently not supported", "Unsupported Database Version");
                //Upgrade();
            }
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
            var file = new FileInfo(new SqlConnectionStringBuilder(ConnectionString).DataSource);
            var appData = Path.Combine(Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "SQLCE Code Generator");
            if (!Directory.Exists(appData))
                Directory.CreateDirectory(appData);
            var newFile = file.CopyTo(Path.Combine(appData, file.Name), true);

            var newConnString = new SqlConnectionStringBuilder(ConnectionString);
            newConnString.DataSource = newFile.FullName;

            var firstIdx = newConnString.ToString().IndexOf("\"", 0);
            var lastIdx = newConnString.ToString().LastIndexOf("\"");
            var connStr = new StringBuilder(newConnString.ToString());
            connStr[firstIdx] = '\'';
            connStr[lastIdx] = '\'';

            ConnectionString = connStr.ToString();
            using (var engine = new SqlCeEngine(ConnectionString))
                engine.Upgrade();

            using (var connection = new SqlCeConnection(ConnectionString))
                connection.Open();
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

        public object GetTableProperties(Table table)
        {
            using (var conn = new SqlCeConnection(ConnectionString))
            using (var adapter = new SqlCeDataAdapter(string.Format("SELECT COLUMN_NAME AS Name, IS_NULLABLE AS [Allows Null], DATA_TYPE AS [Data Type], CHARACTER_MAXIMUM_LENGTH AS [Max Length] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='{0}'", table.DisplayName), conn))
            {
                var dataTable = new DataTable(table.DisplayName);
                adapter.Fill(dataTable);
                return dataTable;
            }
        }

        public object ExecuteQuery(string query, StringBuilder errors, StringBuilder messages)
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

                    int affectedRows = 0;
                    var split = query.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

                    var tables = new DataSet();
                    using (var command = conn.CreateCommand())
                    {
                        conn.Open();
                        foreach (var sql in split)
                        {
                            try
                            {
                                if (sql.Trim().StartsWith("select", StringComparison.InvariantCultureIgnoreCase))
                                {
                                    using (var adapter = new SqlCeDataAdapter(sql, conn))
                                    {
                                        var table = new DataTable();
                                        affectedRows += adapter.Fill(table);
                                        tables.Tables.Add(table);
                                        messages.AppendLine(string.Format("Retrieved {0} row(s)", table.Rows.Count));
                                    }
                                }
                                else
                                {
                                    command.CommandText = sql;
                                    affectedRows += command.ExecuteNonQuery();
                                }
                            }
                            catch (SqlCeException e)
                            {
                                foreach (SqlCeError error in e.Errors)
                                    errors.AppendLine(error.Message);
                            }
                        }
                        messages.AppendLine();
                        messages.AppendLine(string.Format("Total affected row(s): {0}", affectedRows));
                        return tables;
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

        public List<Table> Tables { get; set; }
        public string ConnectionString { get; set; }

        public bool VerifyConnectionStringPassword()
        {
            try
            {
                using (var conn = new SqlCeConnection(ConnectionString))
                    conn.Open();
                return true;
            }
            catch (SqlCeException e)
            {
                // SSCE_M_INVALIDPASSWORD - The specified password does not match the database password
                if (e.NativeError == 25028)
                    return false;
                throw;
            }
        }

        public void AnalyzeDatabase()
        {
            var engine = new SqlCeEngine(ConnectionString);
            var tables = engine.GetTables();
            if (tables == null) return;

            var tableList = GetTableInformation(ConnectionString, tables);
            Tables = new List<Table>(tableList.Values);
            FetchPrimaryKeys();
            FetchIndexes();
        }

        private void FetchPrimaryKeys()
        {
            foreach (var table in Tables)
            {
                using (var conn = new SqlCeConnection(ConnectionString))
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
                        adapter.Fill(0, 1, columnDescriptions);

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

        private void FetchIndexes()
        {
            foreach (var table in Tables)
            {
                using (var conn = new SqlCeConnection(ConnectionString))
                using (var cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = @"SELECT COLUMN_NAME, INDEX_NAME, [UNIQUE], [CLUSTERED] FROM INFORMATION_SCHEMA.INDEXES WHERE PRIMARY_KEY = 0 AND TABLE_NAME='" + table.DisplayName + "' ORDER BY TABLE_NAME, COLUMN_NAME, INDEX_NAME";

                    var dataTable = new DataTable();
                    using (var adapter = new SqlCeDataAdapter(cmd))
                        adapter.Fill(dataTable);

                    if (dataTable.Rows.Count == 0)
                        continue;

                    table.Indexes = new List<Index>(dataTable.Rows.Count);
                    foreach (DataRow row in dataTable.Rows)
                    {
                        var index = new Index
                        {
                            Name = row.Field<string>("INDEX_NAME"),
                            Unique = row.Field<bool>("UNIQUE"),
                            Clustered = row.Field<bool>("CLUSTERED"),
                            Column = table.Columns.Values.Where(c => c.DisplayName == row.Field<string>("COLUMN_NAME")).FirstOrDefault()
                        };
                        table.Indexes.Add(index);
                    }
                }
            }
        }
        
        public void Rename(Table table, string newName)
        {
            throw new NotImplementedException();
        }

        public void Rename(Column column, string newName)
        {
            throw new NotImplementedException();
        }
    }
}
