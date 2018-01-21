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
    class FileDirectory : IEquatable<FileDirectory>
    {
        // Directory Properties
        public BackupSet BackupSet { get; set; }

        /// <summary>Name of the containing directory.</summary>
        public string DirectoryName { get; set; }

        /// <summary>Full path name including volume serial.</summary>
        public virtual string FullName { get => Path.Combine(BackupSet.Drive.VolumeSerialNumber, DirectoryName); }

        /// <summary>Full path name including mount point of the current session.</summary>
        public virtual string FullSessionName { get => Path.Combine(BackupSet.Drive.MountPoint, DirectoryName); }


        public FileDirectory() { }

        public FileDirectory(DirectoryInfo directoryInfo, BackupSet backupSet)
        {
            this.DirectoryName = directoryInfo.FullName.Substring(Path.GetPathRoot(directoryInfo.FullName).Length);
            this.BackupSet = backupSet;
        }

        public FileDirectory(string directoryName, BackupSet backupSet)
            : this(new DirectoryInfo(directoryName), backupSet) { }


        public virtual void Remove() { }

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
    }
}


