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
        public FileDirectory Directory { get; }
        public string Name { get; }
        public string Extension { get; }
        public BackupFile File { get; }

        public FileNode(FileInfo fileInfo, FileDirectory directory, BackupFile file)
        {
            this.Name = fileInfo.Name;
            this.Extension = fileInfo.Extension;
            this.Directory = directory;
            this.File = file;
        }

        public FileNode(string fileName, FileDirectory directory, BackupFile file) 
            : this(new FileInfo(fileName), directory, file) { }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return (Directory + Name).GetHashCode();
        }

        public bool Equals(FileNode other)
        {
            if (other == null)
                return false;

            return this.Name.Equals(other.Name) && this.Directory.Equals(other.Directory);
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
