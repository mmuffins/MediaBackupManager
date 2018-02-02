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

        private object selectedDirectoryTreeItem;

        private FileDirectory selectedFileGridItem;

        //private event EventHandler<object> selectedDirectoryChangedEvent = delegate { };

        #endregion

        #region Properties

        public FileIndexViewModel Index
        {
            get { return index; }
            set { index = value; }
        }

        public FileDirectory SelectedFileGridItem
        {
            get { return selectedFileGridItem; }
            set { selectedFileGridItem = value; }
        }

        public object SelectedDirectoryTreeItem
        {
            get { return selectedDirectoryTreeItem; }
            set
            {
                if (value != selectedDirectoryTreeItem)
                {
                    selectedDirectoryTreeItem = value;
                    if (value is BackupSetViewModel)
                        CurrentDirectory = ((BackupSetViewModel)value).RootDirectory;
                    else
                    {
                        CurrentDirectory = (FileDirectory)value;
                        var ab = currentDirectory.SubDirectories;
                        var de = currentDirectory.Files;
                        var ab2 = ab.OfType<FileDirectory>();
                        var de2 = de.OfType<FileNode>();

                    }

                    NotifyPropertyChanged();
                }
            }
        }

        public FileDirectory CurrentDirectory
        {
            get { return currentDirectory; }
            set
            {
                if(value != currentDirectory)
                {
                    currentDirectory = value;
                    NotifyPropertyChanged();
                    //selectedDirectoryChangedEvent(this, new EventArgs());
                }
            }
        }

        //public EventHandler<object> SelectedDirectoryChangedEvent
        //{
        //    get { return selectedDirectoryChangedEvent; }
        //    set { selectedDirectoryChangedEvent = value; }
        //}
        
        #endregion

        #region Methods

        public DirectoryBrowserViewModel(FileIndexViewModel index)
        {
            this.Index = index;
        }


        #endregion
    }
}
