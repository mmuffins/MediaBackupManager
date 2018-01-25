using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents a directory in the file system.</summary>  
    public class FileDirectory : IEquatable<FileDirectory>, IComparable<FileDirectory>
    {
        // Directory Properties
        public BackupSet BackupSet { get; set; }

        /// <summary>Name of the containing directory.</summary>
        public string DirectoryName { get; set; }

        /// <summary>Full path name including volume serial.</summary>
        public virtual string FullName { get => Path.Combine(BackupSet.Volume.SerialNumber, DirectoryName); }

        /// <summary>Full path name including mount point of the current session.</summary>
        public virtual string FullSessionName { get => Path.Combine(BackupSet.Volume.MountPoint, DirectoryName); }


        public FileDirectory() { }

        public FileDirectory(DirectoryInfo directoryInfo, BackupSet backupSet)
        {
            this.DirectoryName = directoryInfo.FullName.Substring(Path.GetPathRoot(directoryInfo.FullName).Length);
            this.BackupSet = backupSet;
        }

        public FileDirectory(string directoryName, BackupSet backupSet)
            : this(new DirectoryInfo(directoryName), backupSet) { }

        // FileDirectory objects don't have any FileHash references,
        // so the class is not implemented here, but still needed for
        // compatibility reasons
        /// <summary>Removes the reference to this node from the linked FileHash object.</summary>
        public virtual void RemoveFileReference() { }

        public override string ToString()
        {
            return FullName;
        }

        public override int GetHashCode()
        {
            return (BackupSet.Guid + DirectoryName).GetHashCode();
        }

        public virtual bool Equals(FileDirectory other)
        {
            if (other == null)
                return false;

            return this.DirectoryName.Equals(other.DirectoryName)
                && this.BackupSet.Guid.Equals(other.BackupSet.Guid);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            FileDirectory otherObj = obj as FileDirectory;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        public int CompareTo(FileDirectory other)
        {
            return DirectoryName.CompareTo(other.DirectoryName);
        }
    }
}


