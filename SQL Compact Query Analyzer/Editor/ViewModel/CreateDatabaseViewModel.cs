using System.Windows.Input;
using System.Windows.Forms;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class CreateDatabaseViewModel : ViewModelBase
    {
        public CreateDatabaseViewModel()
        {
            CreateDatabaseCommand = new SafeRelayCommand(CreateDatabase);
            OpenFileCommand = new SafeRelayCommand(OpenFile);
        }

        public ICommand CreateDatabaseCommand { get; private set; }
        public ICommand OpenFileCommand { get; private set; }

        private int selectedIndex;
        public int SelectedIndex
        {
            get { return selectedIndex; }
            set
            {
                selectedIndex = value;
                RaisePropertyChanged("SelectedIndex");
            }
        }

        private string filename;
        public string Filename
        {
            get { return filename; }
            set
            {
                filename = value;
                RaisePropertyChanged("Filename");
            }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                RaisePropertyChanged("Password");
            }
        }

        private int? maxDatabaseSize;
        public int? MaxDatabaseSize
        {
            get { return maxDatabaseSize; }
            set
            {
                maxDatabaseSize = value;
                RaisePropertyChanged("MaxDatabaseSize");
            }
        }

        public void CreateDatabase()
        {
            ISqlCeDatabase database = null;
            switch (SelectedIndex)
            {
                case 0:
                    database = SqlCeDatabaseFactory.Create(SupportedVersions.SqlCe31);
                    break;
                case 1:
                    database = SqlCeDatabaseFactory.Create(SupportedVersions.SqlCe35);
                    break;
                case 2:
                    database = SqlCeDatabaseFactory.Create(SupportedVersions.SqlCe40);
                    break;
            }

            if (database != null)
                database.CreateDatabase(Filename, Password, MaxDatabaseSize);
        }

        public void OpenFile()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Filter = "SQL Compact Databases|*.sdf";
                var result = dialog.ShowDialog();
                if (result == DialogResult.OK)
                    Filename = dialog.FileName;
            }
        }
    }
}
