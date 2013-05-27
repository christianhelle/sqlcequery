using System;
using System.Linq;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlServerCe;
using System.Text;
using System.Diagnostics;

namespace ChristianHelle.DatabaseTools.SqlCe
{
    public class SqlCeDatabase : ISqlCeDatabase
    {
        public SqlCeDatabase()
        {

        }

        public SqlCeDatabase(string connectionString)
        {
            ConnectionString = connectionString + " Max Database Size=4091;";
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

        public object GetTableProperties(Table table)
        {
            using (var conn = new SqlCeConnection(ConnectionString))
            using (var adapter = new SqlCeDataAdapter(string.Format("SELECT COLUMN_NAME AS Name, IS_NULLABLE AS [Allows Null], DATA_TYPE AS [Data Type], CHARACTER_MAXIMUM_LENGTH AS [Max Length], AUTOINC_SEED AS [Identity Seed], AUTOINC_INCREMENT AS [Identity Increment] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='{0}'", table.DisplayName), conn))
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
                    var split = query.Split(new[] { ";" + Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

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

        public void CreateDatabase(string filename, string password)
        {
            using (var engine = new SqlCeEngine("Data Source=" + filename + "; Password=" + password))
                engine.CreateDatabase();
        }

        public void SaveTableDataChanges(DataTable tableData)
        {
            if (tableData == null)
                return;

            using (var conn = new SqlCeConnection(ConnectionString))
            using (var adapter = new SqlCeDataAdapter("SELECT * FROM " + tableData.TableName, conn))
            using (var commands = new SqlCeCommandBuilder(adapter))
                adapter.Update(tableData);
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
            using (var engine = new SqlCeEngine(ConnectionString))
            {
                var schema = engine.GetSchemaInformationViews();

                var tables = schema.Tables["INFORMATION_SCHEMA.TABLES"].AsEnumerable().Select(c => c.Field<string>("TABLE_NAME")).ToList();
                if (tables.Count == 0) return;

                var tableList = GetTableInformation(tables, schema);
                Tables = new List<Table>(tableList.Values);

                FetchPrimaryKeys(schema);
                FetchIndexes(schema);
            }
        }

        private static Dictionary<string, Table> GetTableInformation(ICollection<string> tables, DataSet schema)
        {
            var tableList = new Dictionary<string, Table>(tables.Count);
            foreach (var tableName in tables)
            {
                string table = tableName;
                Trace.WriteLine("Analyazing column information for " + table);

                table = string.Format("[{0}]", table);
                var columns = schema.Tables["INFORMATION_SCHEMA.COLUMNS"]
                    .AsEnumerable()
                    .Where(c => c.Field<string>("TABLE_NAME") == tableName)
                    .CopyToDataTable();

                var item = new Table
                {
                    Name = table,
                    DisplayName = tableName,
                    ClassName = tableName.Replace(" ", string.Empty),
                    Columns = new Dictionary<string, Column>(columns.Rows.Count)
                };

                foreach (DataRow row in columns.Rows)
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
                        ManagedType = GetManagedType(row.Field<string>("DATA_TYPE")),
                        AllowsNull = (String.Compare(row.Field<string>("IS_NULLABLE"), "YES", StringComparison.OrdinalIgnoreCase) == 0),
                        IdentityIncrement = row.Field<long?>("AUTOINC_INCREMENT"),
                        IdentitySeed = row.Field<long?>("AUTOINC_SEED"),
                        Ordinal = row.Field<int>("ORDINAL_POSITION")
                    };
                    item.Columns.Add(name, column);
                }

                tableList.Add(table, item);
            }

            return tableList;
        }

        private static Type GetManagedType(string dataType)
        {
            switch (dataType)
            {
                case "bit":
                    return typeof(bool);
                case "varbinary":
                case "image":
                    return typeof(byte[]);
                case "tinyint":
                    return typeof(byte);
                case "datetime":
                    return typeof(DateTime);
                case "numeric":
                    return typeof(decimal);
                case "float":
                    return typeof(double);
                case "uniqueidentifier":
                    return typeof(Guid);
                case "smallint":
                    return typeof(short);
                case "int":
                case "integer":
                    return typeof(int);
                case "uint32":
                    return typeof(uint);
                case "bigint":
                    return typeof(long);
                case "uint64":
                    return typeof(UInt64);
                case "char":
                case "nchar":
                case "text":
                case "ntext":
                case "varchar":
                case "nvarchar":
                    return typeof(string);
                default:
                    return typeof(object);
            }
        }

        private void FetchPrimaryKeys(DataSet schema)
        {
            foreach (var table in Tables)
            {
                Trace.WriteLine("Analyzing primary keys for " + table);

                var primaryKeyColumnName = schema.Tables["INFORMATION_SCHEMA.INDEXES"]
                    .AsEnumerable()
                    .First(c => c.Field<string>("TABLE_NAME") == table.DisplayName && c.Field<bool>("PRIMARY_KEY"))
                    .Field<string>("COLUMN_NAME");

                if (primaryKeyColumnName != null)
                {
                    table.PrimaryKeyColumnName = primaryKeyColumnName;
                    if (primaryKeyColumnName.Contains(" "))
                        table.PrimaryKeyColumnName = string.Format("[{0}]", primaryKeyColumnName);
                    if (table.Columns.ContainsKey(table.PrimaryKeyColumnName))
                        table.Columns[table.PrimaryKeyColumnName].IsPrimaryKey = true;
                }

                var constraints = schema.Tables["INFORMATION_SCHEMA.TABLE_CONSTRAINTS"]
                    .AsEnumerable()
                    .Where(c => c.Field<string>("TABLE_NAME") == table.DisplayName && c.Field<string>("CONSTRAINT_TYPE") == "FOREIGN KEY")
                    .Select(c => c.Field<string>("CONSTRAINT_NAME"));

                foreach (var constraint in constraints)
                {
                    var columns = schema.Tables["INFORMATION_SCHEMA.KEY_COLUMN_USAGE"]
                        .AsEnumerable()
                        .Where(c => c.Field<string>("CONSTRAINT_NAME") == constraint)
                        .Select(c => c.Field<string>("COLUMN_NAME"));

                    foreach (var column in columns)
                    {
                        if (table.Columns.ContainsKey(column))
                            table.Columns[column].IsForeignKey = true;
                    }
                }
            }
        }

        private void FetchIndexes(DataSet schema)
        {
            foreach (var table in Tables)
            {
                Trace.WriteLine("Analyazing index information for " + table);

                var indexes = schema.Tables["INFORMATION_SCHEMA.INDEXES"]
                    .AsEnumerable()
                    .Where(c => !c.Field<bool>("PRIMARY_KEY") && c.Field<string>("TABLE_NAME") == table.DisplayName)
                    .OrderBy(c => c.Field<string>("TABLE_NAME"))
                    .ThenBy(c => c.Field<string>("COLUMN_NAME"))
                    .ThenBy(c => c.Field<string>("INDEX_NAME"))
                    .Select(row => new Index
                    {
                        Name = row.Field<string>("INDEX_NAME"),
                        Unique = row.Field<bool>("UNIQUE"),
                        Clustered = row.Field<bool>("CLUSTERED"),
                        Column = table.Columns.Values.FirstOrDefault(c => c.DisplayName == row.Field<string>("COLUMN_NAME"))
                    }).ToList();

                if (indexes.Count > 0)
                    table.Indexes = indexes;
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
