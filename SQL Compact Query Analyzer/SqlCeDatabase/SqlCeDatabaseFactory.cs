using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Policy;
using System.Reflection;
using System.Data.SqlClient;
using System.IO;

namespace ChristianHelle.DatabaseTools.SqlCe
{
    public class SqlCeDatabaseFactory
    {
        public static ISqlCeDatabase Create(string connectionString)
        {
            var type = GetImplementation(connectionString);
            var database = Activator.CreateInstance(type, connectionString) as ISqlCeDatabase;
            return AnalyzeDatabase(database);
        }

        public static ISqlCeDatabase Create(string defaultNamespace, string connectionString)
        {
            var type = GetImplementation(connectionString);
            var database = Activator.CreateInstance(type, defaultNamespace, connectionString) as ISqlCeDatabase;
            return AnalyzeDatabase(database);
        }

        private static ISqlCeDatabase AnalyzeDatabase(ISqlCeDatabase database)
        {
            if (database != null)
                database.AnalyzeDatabase();
            return database;
        }

        private static Type GetImplementation(string connectionString)
        {
            var file = new SqlConnectionStringBuilder(connectionString).DataSource;
            var version = GetVersion(file);

            switch (version)
            {
                case SupportedVersions.SqlCe31:
                    return LoadSqlCe31();
                case SupportedVersions.SqlCe35:
                    return LoadSqlCe35();
                case SupportedVersions.SqlCe40:
                    return LoadSqlCe40();
                default:
                    throw new NotSupportedException();
            }
        }

        private static string GetExecutingAssemblyPath()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        private static Type LoadSqlCe31()
        {
            var file = Path.Combine(GetExecutingAssemblyPath(), "SqlCe31");
            var assembly = Assembly.LoadFile(Path.Combine(file, "SqlCeDatabase31.dll"));
            var type = assembly.GetType("ChristianHelle.DatabaseTools.SqlCe.SqlCeDatabase");
            return type;
        }

        private static Type LoadSqlCe35()
        {
            var file = Path.Combine(GetExecutingAssemblyPath(), "SqlCe35");
            var assembly = Assembly.LoadFile(Path.Combine(file, "SqlCeDatabase35.dll"));
            var type = assembly.GetType("ChristianHelle.DatabaseTools.SqlCe.SqlCeDatabase");
            return type;
        }

        private static Type LoadSqlCe40()
        {
            var file = Path.Combine(GetExecutingAssemblyPath(), "SqlCe40");
            var assembly = Assembly.LoadFile(Path.Combine(file, "SqlCeDatabase40.dll"));
            var type = assembly.GetType("ChristianHelle.DatabaseTools.SqlCe.SqlCeDatabase");
            return type;
        }

        static SupportedVersions GetVersion(string file)
        {
            using (FileStream fs = new FileStream(file, FileMode.Open))
            {
                fs.Seek(16, SeekOrigin.Begin);
                var reader = new BinaryReader(fs);
                var signature = reader.ReadInt32();

                switch (signature)
                {
                    case 0x73616261: return SupportedVersions.SqlCe20;
                    case 0x002dd714: return SupportedVersions.SqlCe31;
                    case 0x00357b9d: return SupportedVersions.SqlCe35;
                    case 0x003D0900: return SupportedVersions.SqlCe40;
                    default: return SupportedVersions.Unsupported;
                }
            }
        }

        enum SupportedVersions
        {
            Unsupported,
            SqlCe20,
            SqlCe31,
            SqlCe35,
            SqlCe40
        }
    }
}
