using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents an index filesystem location.</summary>  
    class BackupSet
    {
        public LogicalVolume Drive { get; }
        public FileDirectory RootDirectory { get; }
        public string MountPoint { get => Drive.MountPoint; }

        public BackupSet() { }

        public BackupSet(DirectoryInfo directory, LogicalVolume drive)
        {
            this.Drive = drive;
            this.RootDirectory = new FileDirectory(directory.FullName, Drive);
        }

        /// <summary>
        /// Scans all files below the root directory and adds them to the index.</summary>  
        public void ScanFiles()
        {
            RootDirectory.ScanFiles();
        }

        /// <summary>
        /// Removes all Elements from the collection.</summary>  
        public void Clear()
        {
            RootDirectory.Clear();
        }

        /// <summary>
        /// Determines whether a directory is already indexed in the backup set.</summary>  
        public bool ContainsDirectory(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;

            return dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length).Contains(RootDirectory.Name);
        }

        /// <summary>
        /// Determines whether the backup set is a child of a directory.</summary>  
        public bool IsSubsetOf(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;
            return RootDirectory.Name.Contains(dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length));
        }

        public override string ToString()
        {
            return MountPoint + " " + RootDirectory;
        }
    }
}
