using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            MainViewModel = new MainViewModel();
            CreateDatabaseViewModel = new CreateDatabaseViewModel();
        }

        public MainViewModel MainViewModel { get; private set; }
        public CreateDatabaseViewModel CreateDatabaseViewModel { get; private set; }
    }
}
