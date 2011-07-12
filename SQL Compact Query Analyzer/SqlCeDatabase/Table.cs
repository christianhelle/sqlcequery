using System;
using System.Collections.Generic;
using System.Data;

namespace ChristianHelle.DatabaseTools.SqlCe
{
    public class Table
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string ClassName { get; set; }
        public Dictionary<string, Column> Columns { get; set; }
        public List<Index> Indexes { get; set; }
        public string PrimaryKeyColumnName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class Column
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string FieldName { get; set; }
        public int? MaxLength { get; set; }
        public Type ManagedType { get; set; }
        public string DatabaseType { get; set; }
        public bool AllowsNull { get; set; }
        public bool IsPrimaryKey { get; set; }
        public long? AutoIncrement { get; set; }
        public long? AutoIncrementSeed { get; set; }
        public bool IsForeignKey { get; set; }
        public int Ordinal { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class Index
    {
        public Column Column { get; set; }
        public string Name { get; set; }
        public bool Unique { get; set; }
        public bool Clustered { get; set; }
    }
}
