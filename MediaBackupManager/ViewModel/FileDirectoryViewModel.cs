using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class FileDirectoryViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        FileDirectory dir;
        BackupSetViewModel backupSet;

        #endregion

        #region Properties

        public string Name
        {
            get => dir.Name;
        }

        public string DirectoryName
        {
            get => dir.DirectoryName;
        }

        public string FullName
        {
            get => dir.FullName;
        }

        public string FullSessionName
        {
            get => dir.FullSessionName;
        }

        public string ParentDirectoryName
        {
            get => dir.ParentDirectoryName;
        }

        public bool BackupStatus
        {
            get => dir.BackupStatus;
        }

        public BackupSetViewModel BackupSet
        {
            get { return backupSet; }
            set
            {
                if (value != backupSet)
                {
                    backupSet = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public virtual IEnumerable<FileDirectoryViewModel> SubDirectories
        {
            get => BackupSet.GetSubDirectories(this);
        }

        public virtual IEnumerable<FileNodeViewModel> Files
        {
            get => BackupSet.GetFiles(this);
        }

        #endregion

        #region Methods

        public FileDirectoryViewModel(FileDirectory fileDirectory, BackupSetViewModel backupSet)
        {
            this.dir = fileDirectory;
            this.backupSet = backupSet;
        }

        /// <summary>
        /// Returns true if the provided object is the base object of the current viewmodel.</summary>  
        public bool IsViewFor(FileDirectory fileDirectory)
        {
            return fileDirectory.Equals(dir);
        }

        #endregion
    }
}
