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
    public class FileNode : FileDirectory
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

        /// <summary>Full path name including volume serial.</summary>
        public override string FullName { get => Path.Combine(BackupSet.Label, DirectoryName, Name); }

        /// <summary>Full path name including mount point of the current session.</summary>
        public override string FullSessionName { get => Path.Combine(BackupSet.Volume.MountPoint, DirectoryName, Name); }

        /// <summary>Returns true if all subdirectories or related file hashes have more than one related backup set.</summary>
        public override bool BackupStatus { get => Hash is null ? false : Hash.BackupCount > 1; }

        /// <summary>Returns a list of all subdirectories of the current file node.</summary>
        public override IEnumerable<FileDirectory> SubDirectories { get => null; }

        #endregion

        #region Methods

        public FileNode() { }

        public FileNode(FileInfo fileInfo, BackupSet backupSet)
        {
            this.Name = fileInfo.Name;
            this.Extension = fileInfo.Extension;
            this.DirectoryName = fileInfo.DirectoryName.Substring(Path.GetPathRoot(fileInfo.DirectoryName).Length);
            this.BackupSet = backupSet;
        }

        public FileNode(FileInfo fileInfo, BackupSet backupSet, FileHash file) : this (fileInfo, backupSet)
        {
            this.Hash = file;
            this.Checksum = file.Checksum;
        }

        public FileNode(string fileName, BackupSet backupSet, FileHash file)
            : this(new FileInfo(fileName), backupSet, file) { }

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
    }
}
