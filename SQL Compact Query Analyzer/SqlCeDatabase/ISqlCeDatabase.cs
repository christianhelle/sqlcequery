using System.Collections.Generic;
using System.Data;
using System.Text;

namespace ChristianHelle.DatabaseTools.SqlCe
{
    public interface ISqlCeDatabase
    {
        string ConnectionString { get; set; }
        List<Table> Tables { get; set; }
        bool VerifyConnectionStringPassword();
        void AnalyzeDatabase();
        void Verify();
        void Shrink();
        void Compact();
        void Upgrade();
        void Rename(Table table, string newName);
        void Rename(Column column, string newName);
        object GetTableData(Table table);
        object GetTableData(string tableName, string columnName);
        object GetTableProperties(Table table);
        void SaveTableDataChanges(DataTable tableData);
        object ExecuteQuery(string query, StringBuilder errors, StringBuilder messages);
        void CreateDatabase(string filename, string password, int? maxDatabaseSize);
    }
}
