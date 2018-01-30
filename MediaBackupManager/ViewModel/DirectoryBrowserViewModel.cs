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

        private object currentDirectory;

        private BackupSetViewModel selectedSet;

        private event EventHandler<object> selectedDirectoryChangedEvent = delegate { };


        #endregion

        #region Properties

        public FileIndexViewModel Index
        {
            get { return index; }
            set { index = value; }
        }

        public BackupSetViewModel SelectedGridItem
        {
            get { return selectedSet; }
            set { selectedSet = value; }
        }

        public object CurrentDirectory
        {
            get { return currentDirectory; }
            set
            {
                currentDirectory = value;
                selectedDirectoryChangedEvent(this, new EventArgs());
            }
        }

        public EventHandler<object> SelectedDirectoryChangedEvent
        {
            get { return selectedDirectoryChangedEvent; }
            set { selectedDirectoryChangedEvent = value; }
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
