using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents the location of a BackupFile object in the file system.</summary>  

    class FileNode : IEquatable<FileNode>
    {
        // File Properties
        public string Name { get; set; }
        public string Extension { get; set; }
        public BackupFile File { get; set; }

        // Directory Properties
        public BackupSet BackupSet { get; set; }

        /// <summary>Name of the containing directory.</summary>
        public string Directory { get; set; }

        /// <summary>Full path name including volume serial.</summary>
        public string FullName { get => Path.Combine(BackupSet.Drive.VolumeSerialNumber, Directory, Name); }

        /// <summary>Full path name including mount point of the current session.</summary>
        public string FullSessionName { get => Path.Combine(BackupSet.Drive.MountPoint, Directory, Name); }


        public FileNode() { }

        public FileNode(FileInfo fileInfo, BackupSet backupSet, BackupFile file)
        {
            this.Name = fileInfo.Name;
            this.Extension = fileInfo.Extension;
            this.Directory = fileInfo.FullName.Substring(Path.GetPathRoot(fileInfo.FullName).Length);
            this.BackupSet = backupSet;
            this.File = file;
        }

        public FileNode(string fileName, BackupSet backupSet, BackupFile file)
            : this(new FileInfo(fileName), backupSet, file) { }


        public void Remove()
        {
            if(!(this.File is null)) // If the current object refers to a directory it has no file
                this.File.RemoveNode(this);
        }

        public override string ToString()
        {
            return FullName;
        }

        public override int GetHashCode()
        {
            return (Directory + Name).GetHashCode();
        }

        public bool Equals(FileNode other)
        {
            if (other == null)
                return false;

            return this.Name.Equals(other.Name)
                && this.Directory.Equals(other.Directory)
                && this.BackupSet.Drive.VolumeSerialNumber.Equals(other.BackupSet.Drive.VolumeSerialNumber);
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
    }
}
