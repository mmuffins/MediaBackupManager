using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class FileNodeViewModel : ViewModelBase
    {
    
        #region Fields

        FileNode node;
        BackupSetViewModel backupSet;
        FileHashViewModel hash;

        #endregion

        #region Properties

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

        public FileHashViewModel Hash
        {
            get
            {
                if(hash is null)
                {
                    // If the hashes and nodes of the base collection were
                    // not loaded in order it's possible that the related hash
                    // is not filled so make sure to check if a new one is available
                    this.hash = RefreshHash();
                    //NotifyPropertyChanged();
                }

                return hash;
            }
            set
            {
                if (value != hash)
                {
                    hash = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Checksum
        {
            get => node.Checksum;
        }

        public string Name
        {
            get => node.Name;
        }

        public string Extension
        {
            get => node.Extension;
        }

        public string DirectoryName
        {
            get => node.DirectoryName;
        }

        public string FullName
        {
            get => node.FullName;
        }

        public string FullSessionName
        {
            get => node.FullSessionName;
        }

        public bool HasMultipleBackups
        {
            get => Hash is null ? false : Hash.BackupCount > 1;
        }


        #endregion

        #region Methods

        public FileNodeViewModel(FileNode fileNode, BackupSetViewModel backupSet)
        {
            this.node = fileNode;
            this.backupSet = backupSet;
            this.Hash = RefreshHash();

            fileNode.PropertyChanged += Node_PropertyChanged; 
        }

        private void Node_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Hash":
                    this.hash = RefreshHash();
                    break;

                case "Checksum":
                    this.hash = RefreshHash();
                    break;
            }
        }

        /// <summary>
        /// Returns true if the provided object is the base object of the current viewmodel.</summary>  
        public bool IsViewFor(FileNode fileNode)
        {
            return node.Equals(fileNode);
        }

        /// <summary>
        /// Gets the related FileHash from the provided FileIndexViewModel.</summary>  
        private FileHashViewModel RefreshHash()
        {
            if (hash != null)
                hash.RemoveNode(this);

            if(BackupSet is null || string.IsNullOrWhiteSpace(Checksum))
                return null;

            var newHash = BackupSet.Index.GetFileHashViewModel(Checksum);

            if (newHash != null)
                newHash.AddNode(this);

            return newHash;
        }

        #endregion

        #region Implementations

        public override string ToString()
        {
            return FullName;
        }

        #endregion

    }
}
