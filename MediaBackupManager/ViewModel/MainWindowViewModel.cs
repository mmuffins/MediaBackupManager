using MediaBackupManager.Model;
using MediaBackupManager.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MediaBackupManager.ViewModel
{
    class MainWindowViewModel
    {
        private FileIndex index;
        public FileIndex Index
        {
            get { return index; }
            set { index = value; }
        }

        public MainWindowViewModel(FileIndex index)
        {
            this.Index = index;
            PrepareDatabase(index);
            Index.LoadData();

            //TODO:Remove using directives for MediaBackupManager.View and System.Windows once done testing
            var testPage = new TestPage(index);
            testPage.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            testPage.Show();
        }

        private void PrepareDatabase(FileIndex index)
        {
            Database.Index = index;
            Database.CreateDatabase();
            Database.PrepareDatabase();
        }
    }
}
