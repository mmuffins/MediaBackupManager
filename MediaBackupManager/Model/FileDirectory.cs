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

        #region Properties

        public string Name { get; set; }
        public BackupSet BackupSet { get; set; }

        /// <summary>Name of the containing directory.</summary>
        public string DirectoryName { get; set; }

        /// <summary>Full path name including volume serial.</summary>
        public virtual string FullName { get => Path.Combine(BackupSet.Label, DirectoryName); }

        /// <summary>Full path name including mount point of the current session.</summary>
        public virtual string FullSessionName { get => Path.Combine(BackupSet.Volume.MountPoint, DirectoryName); }

        /// <summary>Returns the path of the parent directory.</summary>
        public virtual string ParentDirectoryName {
            get
            {
                int lastIndex = DirectoryName.LastIndexOf("\\");
                return lastIndex >= 0 ? DirectoryName.Substring(0, DirectoryName.LastIndexOf("\\")) : null;
            }
        }

        /// <summary>Returns a list of all subdirectories of the current object.</summary>
        public virtual IEnumerable<FileDirectory> SubDirectories { get => this.BackupSet.GetSubDirectories(this); }

        #endregion

        #region Methods

        public FileDirectory() { }

        public FileDirectory(DirectoryInfo directoryInfo, BackupSet backupSet)
        {
            this.Name = directoryInfo.Name;
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

        #endregion

        #region Implementations

        public override string ToString()
        {
            return FullName;
        }

        public override int GetHashCode()
        {
            return (BackupSet.Guid + DirectoryName + Name).GetHashCode();
        }

        public virtual bool Equals(FileDirectory other)
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

        #endregion
    }
}


