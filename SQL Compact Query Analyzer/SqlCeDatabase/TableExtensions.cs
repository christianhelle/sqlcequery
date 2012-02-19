using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;

namespace ChristianHelle.DatabaseTools.SqlCe
{
    public static class TableExtensions
    {
        public static string GenerateSchemaScript(this Table table)
        {
            var builder = new StringBuilder();

            builder.Append("CREATE TABLE ");
            builder.Append(table.Name);
            builder.Append(" (");
            foreach (var column in table.Columns)
            {
                builder.AppendLine();
                builder.Append("\t");
                builder.Append(column.Value.Name);
                builder.Append(" ");
                builder.Append(column.Value.DatabaseType.ToUpper());
                if (column.Value.ManagedType == typeof(string) && !column.Value.DatabaseType.ToUpper().Contains("TEXT"))
                    builder.AppendFormat("({0})", column.Value.MaxLength);
                builder.Append(" ");
                if (column.Value.IsPrimaryKey || table.PrimaryKeyColumnName == column.Value.Name)
                    builder.Append("PRIMARY KEY ");
                builder.Append(column.Value.AllowsNull ? "NULL" : "NOT NULL");
                builder.Append(",");
            }
            builder.Remove(builder.Length - 1, 1);
            builder.AppendLine();
            builder.AppendLine(");");

            return builder.ToString();
        }

        public static string GenerateDataScript(this Table table, ISqlCeDatabase database)
        {
            var data = database.GetTableData(table) as DataTable;
            if (data == null || data.Rows.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            foreach (DataRow row in data.Rows)
            {
                builder.AppendLine();
                builder.AppendFormat("INSERT INTO {0}", table.Name);

                builder.Append(" (");
                foreach (var column in table.Columns.Where(column => column.Value.ManagedType != typeof(byte[])))
                    builder.AppendFormat("{0},", column.Value.Name);
                builder.Remove(builder.Length - 1, 1);

                builder.Append(") VALUES (");
                foreach (var column in table.Columns.Where(column => column.Value.ManagedType != typeof(byte[])))
                {
                    var value = row[column.Value.Ordinal - 1];
                    if (value == null)
                    {
                        builder.Append("NULL,");
                        continue;
                    }

                    if (column.Value.ManagedType == typeof(string) || column.Value.ManagedType == typeof(DateTime))
                        builder.AppendFormat("'{0}',", value.ToString().Replace("'", "''"));
                    else
                        builder.Append(value + ",");
                }
                builder.Remove(builder.Length - 1, 1);
                builder.Append(");");
            }

            builder.AppendLine();
            return builder.ToString();
        }

        public static string GenerateDataScript(this IEnumerable<Table> tables, ISqlCeDatabase database)
        {
            var builder = new StringBuilder();

            foreach (var table in tables)
            {
                var script = GenerateDataScript(table, database);
                if (!string.IsNullOrEmpty(script))
                    builder.Append(script);
            }

            return builder.ToString();
        }

        public static string GenerateSchemaAndDataScript(this Table table, ISqlCeDatabase database)
        {
            var builder = new StringBuilder();

            builder.AppendLine(GenerateSchemaScript(table));
            builder.AppendLine(Environment.NewLine);
            builder.AppendLine(Environment.NewLine);
            builder.AppendLine(GenerateDataScript(table, database));

            return builder.ToString();
        }

        public static string GenerateSchemaAndDataScript(this IEnumerable<Table> tables, ISqlCeDatabase database)
        {
            var builder = new StringBuilder();

            foreach (var table in tables)
                builder.AppendLine(GenerateSchemaScript(table));

            builder.AppendLine(Environment.NewLine);
            builder.AppendLine(Environment.NewLine);

            foreach (var table in tables)
                builder.AppendLine(GenerateDataScript(table, database));

            return builder.ToString();
        }
    }
}