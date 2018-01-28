using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class FileHashViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        public FileHash hash = new FileHash();

        #endregion

        #region Properties

        public long Length
        {
            get { return hash.Length; }
            set
            {
                if (value != hash.Length)
                {
                    hash.Length = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        public DateTime CreationTime
        {
            get { return hash.CreationTime; }
            set
            {
                if (value != hash.CreationTime)
                {
                    hash.CreationTime = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        public DateTime LastWriteTime
        {
            get { return hash.LastWriteTime; }
            set
            {
                if (value != hash.LastWriteTime)
                {
                    hash.LastWriteTime = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        public string Checksum
        {
            get { return hash.Checksum; }
            set
            {
                if (value != hash.Checksum)
                {
                    hash.Checksum = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        public int NodeCount { get => hash.NodeCount; }

        public int BackupCount { get => hash.BackupCount; }

        #endregion

        #region Methods

        public FileHashViewModel(FileHash hash)
        {
            this.hash = hash;
        }

        #endregion

    }
}
