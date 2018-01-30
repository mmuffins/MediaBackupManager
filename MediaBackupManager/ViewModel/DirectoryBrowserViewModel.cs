using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    class DirectoryBrowserViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        private FileIndexViewModel index;
        private FileDirectory currentDirectory;
        private BackupSetViewModel selectedSet;

        #endregion

        #region Properties

        public FileIndexViewModel Index
        {
            get { return index; }
            set { index = value; }
        }

        public FileDirectory CurrentDirectory
        {
            get { return currentDirectory; }
            set { currentDirectory = value; }
        }

        public BackupSetViewModel SelectedGridItem
        {
            get { return selectedSet; }
            set { selectedSet = value; }
        }
        #endregion

        #region Methods

        public DirectoryBrowserViewModel(FileIndexViewModel index)
        {
            this.Index = index;
        }


        #endregion
    }
}
