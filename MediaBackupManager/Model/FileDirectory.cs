using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents a directory in the file system.</summary>  
    public class FileDirectory : IEquatable<FileDirectory>, IComparable<FileDirectory>, INotifyPropertyChanged
    {
        #region Fields

        string name;
        string directoryName;
        BackupSet backupSet;
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion


        #region Properties

        public string Name
        {
            get { return name; }
            set
            {
                if (value != name)
                {
                    name = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public BackupSet BackupSet
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

        /// <summary>Name of the containing directory.</summary>
        public string DirectoryName
        {
            get { return directoryName; }
            set
            {
                if (value != directoryName)
                {
                    directoryName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>Full path name including volume serial.</summary>
        public virtual string FullName { get => Path.Combine(BackupSet.Label, DirectoryName, Name); }

        /// <summary>Full path name including mount point of the current session.</summary>
        public virtual string FullSessionName { get => Path.Combine(BackupSet.Volume.MountPoint, DirectoryName, Name); }

        ///// <summary>Returns the path of the parent directory.</summary>
        //public virtual string ParentDirectoryName {
        //    get
        //    {
        //        int lastIndex = DirectoryName.LastIndexOf("\\");
        //        return lastIndex >= 0 ? DirectoryName.Substring(0, DirectoryName.LastIndexOf("\\")) : null;
        //    }
        //}

        /// <summary>Returns true if all subdirectories or related file hashes have more than one related backup set.</summary>
        public virtual bool BackupStatus
        {
            get
            {
                return Children.Count() > 0 ? Children.All(x => x.BackupStatus.Equals(true)) : true;
            }
        }


        /// <summary>Returns a list of all directories below the current object.</summary>
        public virtual IEnumerable<FileDirectory> SubDirectories
        {
            get => this.BackupSet.GetChildElements(this)
                .OfType<FileDirectory>()
                .Where(x => x.GetType() == typeof(FileDirectory));
        }

        /// <summary>Returns a list of all files below the current object.</summary>
        public virtual IEnumerable<FileDirectory> Files { get => this.BackupSet.GetChildElements(this).OfType<FileNode>(); }

        /// <summary>Returns a list of all directories and files below the current object.</summary>
        public virtual IEnumerable<FileDirectory> Children { get => this.BackupSet.GetChildElements(this); }

        #endregion

        #region Methods

        public FileDirectory() { }

        public FileDirectory(DirectoryInfo directoryInfo, BackupSet backupSet)
        {
            this.Name = directoryInfo.Name;
            this.DirectoryName = directoryInfo.Parent.FullName.Substring(Path.GetPathRoot(directoryInfo.Parent.FullName).Length);
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


        public void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }
}


