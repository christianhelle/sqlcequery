using System.Windows;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class RenameObjectViewModel : ViewModelBase
    {
        private object objectToRename;
        private ISqlCeDatabase database;

        private RenameObjectViewModel(ISqlCeDatabase database, string objectName)
        {
            this.database = database;
            Name = objectName;
        }

        public RenameObjectViewModel(ISqlCeDatabase database, Table table)
            : this(database, table.Name)
        {
            objectToRename = table;
        }

        public RenameObjectViewModel(ISqlCeDatabase database, Column column)
            : this(database, column.Name)
        {
            objectToRename = column;
        }

        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged("Name");
            }
        }

        private string newName;
        public string NewName
        {
            get { return newName; }
            set
            {
                newName = value;
                RaisePropertyChanged("NewName");
            }
        }

        public void Rename()
        {
            MessageBox.Show("This feature is yet to be implemented...");
        }
    }
}
