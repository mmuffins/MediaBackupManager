using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// The location of a file hash object in the file system.</summary>  
    public class FileNode : FileDirectory, IDisposable
    {

        #region Fields

        private FileHash hash;
        private string checkSum;
        private string extension;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the file extension of the current file node.</summary>  
        public string Extension
        {
            get { return extension; }
            set
            {
                if (value != extension)
                {
                    extension = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the file hash related to the current file node.</summary>  
        public FileHash Hash
        {
            get { return hash; }
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
            get { return checkSum; }
            set
            {
                if (value != checkSum)
                {
                    checkSum = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the full path Name from the of the file node, with its parent Backup Set as root.</summary>  
        public override string FullName { get => Path.Combine(BackupSet.Label, DirectoryName, Name); }

        /// <summary>
        /// Gets the full path Name from the of the current file node, with its current mount point as root.</summary>  
        public override string FullSessionName
        {
            get
            {
                if(DirectoryName == @"\")
                    return Path.Combine(BackupSet.Volume.MountPoint, Name);
                else 
                    return Path.Combine(BackupSet.Volume.MountPoint, DirectoryName, Name);
            }
        }

        /// <summary>
        /// Returns true if all subdirectories or related file hashes have more than one related backup set.</summary>
        public override bool BackupStatus { get => Hash is null ? false : Hash.BackupCount > 1; }

        #endregion

        #region Methods

        public FileNode() { }

        public FileNode(FileInfo fileInfo, BackupSet backupSet, FileDirectory parent)
        {
            this.Name = fileInfo.Name;
            this.Extension = fileInfo.Extension;
            this.BackupSet = backupSet;
            this.Parent = parent;

            //Set root directory to \
            if (fileInfo.Directory is null || fileInfo.Directory.FullName == fileInfo.Directory.Root.FullName)
                this.DirectoryName = @"\";
            else
                this.DirectoryName = fileInfo.DirectoryName.Substring(Path.GetPathRoot(fileInfo.DirectoryName).Length);
        }

        public FileNode(FileInfo fileInfo, BackupSet backupSet, FileDirectory parent, FileHash file) : this (fileInfo, backupSet, parent)
        {
            this.Hash = file;
            this.Checksum = file.Checksum;
        }

        /// <summary>
        /// Removes the reference to this node from the related file hash.</summary>
        public override void RemoveFileReference()
        {
            if (!(this.Hash is null)) // If the current object refers to a directory it has no file
                this.Hash.RemoveNode(this);
                //this.BackupSet.Index.RemoveFileNode(this);
                //this.File.RemoveNode(this);
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return (BackupSet.Guid + DirectoryName + Name).GetHashCode();
        }

        public bool Equals(FileNode other)
        {
            if (other == null)
                return false;

            return this.Name.Equals(other.Name)
                && this.DirectoryName.Equals(other.DirectoryName)
                && this.BackupSet.Equals(other.BackupSet);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var otherObj = obj as FileNode;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        public int CompareTo(FileNode other)
        {
            return (DirectoryName + Name).CompareTo((other.DirectoryName + other.Name));
        }

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                }

                // set large fields to null.
                RemoveFileReference();
                Parent = null;

                disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileDirectory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public override void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
