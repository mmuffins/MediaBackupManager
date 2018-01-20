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
        public FileIndex Index { get; }
        public string MountPoint { get => Drive.MountPoint; }

        public BackupSet() { }

        public BackupSet(FileIndex index, DirectoryInfo directory, LogicalVolume drive)
        {
            this.Index = index;
            this.Drive = drive;
            this.RootDirectory = new FileDirectory(directory.FullName, Drive, Index);
        }

        /// <summary>
        /// Scans all files below the root directory and adds them to the index.</summary>  
        public void ScanFiles()
        {
            RootDirectory.ScanFiles();
        }

        public override string ToString()
        {
            return MountPoint + " " + RootDirectory;
        }
    }
}
