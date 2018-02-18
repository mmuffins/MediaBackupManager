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
    public class FileNodeViewModel : ViewModelBase, IEquatable<FileNodeViewModel>
    {
    
        #region Fields

        FileNode node;
        BackupSetViewModel backupSet;
        FileHashViewModel hash;
        FileDirectoryViewModel parent;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Backup Set containing the current directory.</summary>  
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

        /// <summary>
        /// Gets or sets the file hash related to the current file node.</summary>  
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

        /// <summary>
        /// Gets or sets the checksum of the file hash related to the current file node.</summary>  
        public string Checksum
        {
            get => node.Checksum;
        }

        /// <summary>
        /// Gets or sets the Name of the current file node.</summary>  
        public string Name
        {
            get => node.Name;
        }

        /// <summary>
        /// Gets or sets the file extension of the current file node.</summary>  
        public string Extension
        {
            get => node.Extension;
        }

        /// <summary>
        /// Gets or sets the Name of the file node containing the current directory.</summary>  
        public string DirectoryName
        {
            get => node.DirectoryName;
        }

        /// <summary>
        /// Gets the full path Name from the of the file node, with its parent Backup Set as root.</summary>  
        public string FullName
        {
            get => node.FullName;
        }

        /// <summary>
        /// Gets the full path Name from the of the current file node, with its current mount point as root.</summary>  
        public string FullSessionName
        {
            get => node.FullSessionName;
        }

        /// <summary>
        /// Gets a value indicating if the file hash related to this file node is contained on more than one logical volumes.</summary>  
        public bool HasMultipleBackups
        {
            get => Hash is null ? false : Hash.BackupCount > 1;
        }

        /// <summary>
        /// Gets or sets the parent directory object of the current file node.</summary>  
        public FileDirectoryViewModel Parent
        {
            get { return parent; }
            set
            {
                if (value != parent)
                {
                    parent = value;
                    NotifyPropertyChanged();
                }
            }
        }


        #endregion

        #region Methods

        public FileNodeViewModel(FileNode fileNode, FileDirectoryViewModel parent, BackupSetViewModel backupSet)
        {
            this.node = fileNode;
            this.backupSet = backupSet;
            this.Hash = RefreshHash();
            this.Parent = parent;

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

        /// <summary>
        /// Returns a list of all file nodes with the same file hash.</summary>  
        public List<FileNodeViewModel> GetRelatedNodes()
        {
            if (Hash is null)
                return null;

            return Hash.FileNodes.ToList();
        }

        #endregion

        #region Implementations

        public override string ToString()
        {
            return FullName;
        }

        public override int GetHashCode()
        {
            return node.GetHashCode();
        }

        public bool Equals(FileNodeViewModel other)
        {
            if (other == null)
                return false;

            //return this.Name.Equals(other.Name)
            //    && this.DirectoryName.Equals(other.DirectoryName)
            //    && this.BackupSet.Equals(other.BackupSet);

            return this.node.Equals(other.node);

        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var otherObj = obj as FileNodeViewModel;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        #endregion

    }
}
