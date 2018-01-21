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
        public HashSet<FileDirectory> Subdirectories { get; }
        public HashSet<FileNode> FileNodes { get; }
        public LogicalVolume Drive { get; }

        /// <summary>Full path name without mount point</summary>
        public string Name { get; }

        /// <summary>Name of the current directory</summary>
        public string DirectoryName { get => Path.GetDirectoryName(Name); }

        /// <summary>Full path name including mount point</summary>
        public string FullName { get => Path.Combine(Drive.MountPoint, Name); }


        public FileDirectory()
        {
            this.Subdirectories = new HashSet<FileDirectory>();
            this.FileNodes = new HashSet<FileNode>();
        }

        public FileDirectory(string path, LogicalVolume drive) : this()
        {
            this.Drive = drive;
            this.Name = path.Substring(Path.GetPathRoot(path).Length);
        }

        public void AddSubDirectory(string path)
        {
            Subdirectories.Add(new FileDirectory(path, Drive));
        }

        public void AddSubDirectory(FileDirectory subDirectory)
        {
            Subdirectories.Add(subDirectory);
        }

        public void AddFile(FileNode fileNode)
        {
            FileNodes.Add(fileNode);
        }

        public void ScanFiles()
        {
            foreach (var item in Directory.EnumerateDirectories(FullName))
            {
                var subDir = new FileDirectory(item, Drive);
                Subdirectories.Add(subDir);
                subDir.ScanFiles();
            }

            foreach (var file in Directory.EnumerateFiles(FullName))
            {
                BackupFile backupFile = FileIndex.IndexFile(file);
                var fileNode = new FileNode(file, this, backupFile);
                backupFile.AddNode(fileNode);
                FileNodes.Add(fileNode);
            }
        }

        /// <summary>
        /// Recursively removes all directories and file nodes from the directory.</summary>  
        public void Clear()
        {
            foreach (var item in FileNodes)
            {
                item.Remove();
            }

            FileNodes.Clear();

            foreach (var item in Subdirectories)
                item.Clear();

            Subdirectories.Clear();
        }

        public override string ToString()
        {
            return Name;
        }

        public override int GetHashCode()
        {
            return (Drive.VolumeName + Name).GetHashCode();
        }

        public bool Equals(FileDirectory other)
        {
            if (other == null)
                return false;

            return this.Name.Equals(other.Name) && this.Drive.Equals(other.Drive);
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
