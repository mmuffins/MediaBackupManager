using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents the location of a FileHash object in the file system.</summary>  

    public class FileNode : FileDirectory
    {

        #region Properties

        // File Properties
        public string Name { get; set; }
        public string Extension { get; set; }
        public FileHash Hash { get; set; }
        public string Checksum { get; set; }

        /// <summary>Full path name including volume serial.</summary>
        public override string FullName { get => Path.Combine(BackupSet.Volume.SerialNumber, DirectoryName, Name); }

        /// <summary>Full path name including mount point of the current session.</summary>
        public override string FullSessionName { get => Path.Combine(BackupSet.Volume.MountPoint, DirectoryName, Name); }

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

        /// <summary>Removes the reference to this node from the linked FileHash object.</summary>
        public override async Task RemoveFileReferenceAsync()
        {
            if (!(this.Hash is null)) // If the current object refers to a directory it has no file
                await this.BackupSet.Index.RemoveFileNodeAsync(this);
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
                && this.BackupSet.Volume.SerialNumber.Equals(other.BackupSet.Volume.SerialNumber);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            FileNode otherObj = obj as FileNode;
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
