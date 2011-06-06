using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Policy;
using System.Data;
using System.Text;

namespace ChristianHelle.DatabaseTools.SqlCe
{
    public interface ISqlCeDatabase
    {
        string ConnectionString { get; }
        string Namespace { get; set; }
        List<Table> Tables { get; set; }
        void AnalyzeDatabase();
        void Verify();
        void Shrink();
        void Compact();
        void Upgrade();
        object GetTableData(Table table);
        object GetTableData(string tableName, string columnName);
        void SaveTableDataChanges(DataTable TableData);
        DataTable ExecuteQuery(string query, StringBuilder errors, StringBuilder messages);
    }    
}
