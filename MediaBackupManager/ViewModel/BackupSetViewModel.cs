using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    class BackupSetViewModel : ViewModelBase.ViewModelBase, IEquatable<BackupSetViewModel>
    {
        #region Fields

        private BackupSet backupSet;

        #endregion

        #region Properties

        public BackupSet BackupSet
        {
            get { return backupSet; }
            set
            {
                if (value != backupSet)
                {
                    backupSet = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        public Guid Guid
        {
            get { return backupSet.Guid; }
            set
            {
                if (value != backupSet.Guid)
                {
                    backupSet.Guid = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        public FileDirectory RootDirectoryObject { get; set; }

        public string RootDirectory
        {
            get { return backupSet.RootDirectory; }
            set
            {
                if (value != backupSet.RootDirectory)
                {
                    backupSet.RootDirectory = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        public ObservableCollection<FileDirectory> FileNodes { get; set; }

        #endregion

        #region Methods

        public BackupSetViewModel(BackupSet backupSet)
        {
            this.BackupSet = backupSet;
            this.FileNodes = new ObservableCollection<FileDirectory>(BackupSet.FileNodes);
            this.RootDirectoryObject = backupSet.GetRootDirectoryObject();

            //FileIndex.LoadDatau
            //this.backupSets = new ObservableCollection<BackupSet>(FileIndex.BackupSets);
            //var ab
        }

        public IEnumerable<FileDirectory> GetSubDirectories(FileDirectory parent)
        {
            return BackupSet.GetSubDirectories(parent);
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return this.Guid.GetHashCode();
        }

        public virtual bool Equals(BackupSetViewModel other)
        {
            if (other == null)
                return false;

            return this.Guid.Equals(other.Guid);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            BackupSetViewModel otherObj = obj as BackupSetViewModel;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        #endregion

    }
}
