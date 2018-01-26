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
        #region Fields

        #endregion

        #region Properties

        #endregion

        #region Methods

        #endregion

        #region Implementations

        #endregion

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
            bool newDB = Database.CreateDatabase();
            Database.PrepareDatabase();

            // Add the default exclusions if a new db was created
            if (newDB)
                index.RestoreDefaultExclusions();
        }
    }
}
