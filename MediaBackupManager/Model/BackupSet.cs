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
    public class BackupSet
    {
        #region Properties

        public FileIndex Index { get; set; }
        public Guid Guid { get; set; }
        public LogicalVolume Volume { get; set; }
        public string RootDirectory { get; set; }
        public string MountPoint { get => Volume.MountPoint; }
        public HashSet<FileDirectory> FileNodes { get; }

        #endregion

        #region Methods
        public BackupSet()
        {
            this.Guid = Guid.NewGuid();
            this.FileNodes = new HashSet<FileDirectory>();
        }

        public BackupSet(DirectoryInfo directory, LogicalVolume drive, FileIndex fileIndex) : this()
        {
            this.Volume = drive;
            //this.RootDirectory = new FileDirectory(directory.FullName, Drive, null);
            this.RootDirectory = directory.FullName.Substring(Path.GetPathRoot(directory.FullName).Length);
            this.Index = fileIndex;
        }

        /// <summary>
        /// Scans all files below the root directory and adds them to the index.</summary>  
        public void ScanFiles()
        {
            IndexDirectory(new DirectoryInfo(Path.Combine(MountPoint,RootDirectory)));
            //TODO:Write more efficient function to mass-add indexed files to the DB
        }

        /// <summary>
        /// Recursively adds the provided directory and subdirectories to the file index.</summary>
        private void IndexDirectory(DirectoryInfo directory)
        {
            if (Index.IsFileExcluded(directory.FullName))
                return; //Don't index excluded directories at all

            // Call recursively to get all subdirectories
            foreach (var item in directory.GetDirectories())
                IndexDirectory(item);

            var dir = new FileDirectory(directory, this);
            AddFileNode(dir);

            IndexFile(directory);
        }

        /// <summary>
        /// Scans all files found in the provided directory and adds them to the file index.</summary>
        private void IndexFile(DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
            {
                if (Index.IsFileExcluded(file.FullName))
                    continue;

                // Make sure that the backup file is properly
                // added to the index before creating a file node
                FileHash hash = Index.IndexFile(file.FullName);

                if(!(hash is null))
                {
                    var fileNode = new FileNode(file, this, hash);
                    hash.AddNode(fileNode);
                    AddFileNode(fileNode);
                }
            }
        }

        /// <summary>
        /// Adds the specified element to the file node index.</summary>  
        private void AddFileNode(FileDirectory node)
        {
            if (node is FileNode)
            {
                FileNodes.Add(node);
                Database.InsertFileNode(node as FileNode);
            }
            else
            {
                FileNodes.Add(node);
                Database.InsertFileNode(node as FileDirectory);
            }
        }

        /// <summary>
        /// Removes all Elements from the collection.</summary>  
        public void Clear()
        {
            foreach (var item in FileNodes)
            {
                item.RemoveFileReference();
                Database.DeleteFileNode(item);
            }

            FileNodes.Clear();
        }

        /// <summary>
        /// Determines whether a directory is already indexed in the backup set.</summary>  
        public bool ContainsDirectory(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;

            return dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length).Contains(RootDirectory);
        }

        /// <summary>
        /// Determines whether the backup set is a child of a directory.</summary>  
        public bool IsSubsetOf(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;
            return RootDirectory.Contains(dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length));
        }

        #endregion

        #region Implementations

        public override string ToString()
        {
            return MountPoint + " " + RootDirectory;
        }

        #endregion
    }
}
