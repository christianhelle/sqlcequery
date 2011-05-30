using System;
using System.Collections.Generic;
using System.Data.SqlServerCe;

namespace ChristianHelle.DatabaseTools.SqlCe.CodeGenCore
{
    public static class SqlCeEngineExtensions
    {
        public static bool DoesTableExist(this SqlCeEngine source, string tablename)
        {
            using (var conn = new SqlCeConnection(source.LocalConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT COUNT(TABLE_NAME) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME=@Name";
                    cmd.Parameters.AddWithValue("@Name", tablename);
                    return Convert.ToBoolean(cmd.ExecuteScalar());
                }
            }
        }

        public static string[] GetTables(this SqlCeEngine source)
        {
            using (var conn = new SqlCeConnection(source.LocalConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES";
                    return PopulateStringList(cmd);
                }
            }
        }

        public static string[] GetTableConstraints(this SqlCeEngine source)
        {
            using (var conn = new SqlCeConnection(source.LocalConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS";
                    return PopulateStringList(cmd);
                }
            }
        }

        public static string[] GetTableConstraints(this SqlCeEngine source, string tablename)
        {
            using (var conn = new SqlCeConnection(source.LocalConnectionString))
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText =
                        @"SELECT CONSTRAINT_NAME FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME=@Name";
                    cmd.Parameters.AddWithValue("@Name", tablename);
                    return PopulateStringList(cmd);
                }
            }
        }

        private static string[] PopulateStringList(SqlCeCommand cmd)
        {
            var list = new List<string>();
            using (var reader = cmd.ExecuteReader())
                while (reader.Read())
                    list.Add(reader.GetString(0));

            return list.ToArray();
        }
    }
}