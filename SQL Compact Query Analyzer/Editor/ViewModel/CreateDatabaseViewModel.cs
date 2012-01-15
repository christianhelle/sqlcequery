using System.Windows.Input;
using System.Windows;
using System.Linq;
using System.Windows.Controls;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class CreateDatabaseViewModel : ViewModelBase
    {
        public CreateDatabaseViewModel()
        {
            CreateDatabaseCommand = new SafeRelayCommand(CreateDatabase);
        }

        public ICommand CreateDatabaseCommand { get; set; }

        public int SelectedIndex { get; set; }

        public string Filename { get; set; }

        public string Password { get; set; }

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
                database.CreateDatabase(Filename, Password);
        }
    }
}
