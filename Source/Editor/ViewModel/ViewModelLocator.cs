using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChristianHelle.DatabaseTools.SqlCe.QueryAnalyzer.ViewModel
{
    public class ViewModelLocator
    {
        public MainViewModel MainViewModel
        {
            get { return new MainViewModel(); }
        }

        public CreateDatabaseViewModel CreateDatabaseViewModel
        {
            get { return new CreateDatabaseViewModel(); }
        }
    }
}
