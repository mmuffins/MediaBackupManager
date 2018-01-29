using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class BackupSetViewModel : ViewModelBase.ViewModelBase, IEquatable<BackupSetViewModel>
    {
        #region Fields

        private BackupSet backupSet;
        private FileDirectory rootDirectory;

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

        public FileDirectory RootDirectory
        {
            get
            {
                if(this.rootDirectory is null)
                    this.rootDirectory = backupSet.GetRootDirectoryObject();

                return this.rootDirectory;
            }
            set
            {
                if (value != this.RootDirectory)
                {
                    this.RootDirectory = value;
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
            this.RootDirectory = backupSet.GetRootDirectoryObject();
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return this.BackupSet.GetHashCode();
        }

        public virtual bool Equals(BackupSetViewModel other)
        {
            if (other == null)
                return false;

            return this.BackupSet.Equals(other.BackupSet);
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
