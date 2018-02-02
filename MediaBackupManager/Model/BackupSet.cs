using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents an index filesystem location.</summary>  
    public class BackupSet : IEquatable<BackupSet>
    {
        #region Properties

        public FileIndex Index { get; set; }
        public Guid Guid { get; set; }
        public LogicalVolume Volume { get; set; }
        public string RootDirectory { get; set; }
        public string MountPoint { get => Volume.MountPoint; }
        public HashSet<FileDirectory> FileNodes { get; }
        public HashSet<string> Exclusions { get; set; }

        /// <summary>User defined label for the drive</summary>
        public string Label { get; set; }

        #endregion

        #region Methods
        public BackupSet()
        {
            this.Guid = Guid.NewGuid();
            this.FileNodes = new HashSet<FileDirectory>();
            if (string.IsNullOrWhiteSpace(this.Label))
                this.Label = this.Guid.ToString();
        }

        public BackupSet(DirectoryInfo directory, LogicalVolume drive, FileIndex fileIndex) : this()
        {
            //TODO: Is this constructor still needed?
            this.Volume = drive;
            //this.RootDirectory = new FileDirectory(directory.FullName, Drive, null);
            this.RootDirectory = directory.FullName.Substring(Path.GetPathRoot(directory.FullName).Length);
            this.Index = fileIndex;

            if (string.IsNullOrWhiteSpace(this.Label))
                this.Label = drive.MountPoint;
        }

        public BackupSet(DirectoryInfo directory, LogicalVolume drive, HashSet<string> exclusions) : this()
        {
            this.Volume = drive;
            //this.RootDirectory = new FileDirectory(directory.FullName, Drive, null);
            this.RootDirectory = directory.FullName.Substring(Path.GetPathRoot(directory.FullName).Length);
            this.Exclusions = exclusions;

            if (string.IsNullOrWhiteSpace(this.Label))
                this.Label = drive.MountPoint;
        }

        /// <summary>
        /// Scans all files below the root directory and adds them to the index.</summary>  
        public async Task ScanFilesAsync(CancellationToken cancellationToken)
        {
            if (IsFileExcluded((Path.Combine(MountPoint, RootDirectory)).ToString()))
                return;

            await Task.Run(()=>IndexDirectory(new DirectoryInfo(Path.Combine(MountPoint, RootDirectory))), cancellationToken);
            return;
        }

        /// <summary>
        /// Recursively adds the provided directory and subdirectories to the file index.</summary>
        private void IndexDirectory(DirectoryInfo directory)
        {
            if (IsFileExcluded(directory.FullName))
                return; //Don't index excluded directories at all

            // Call recursively to get all subdirectories
            foreach (var item in directory.GetDirectories())
                IndexDirectory(item);

            FileNodes.Add(new FileDirectory(directory, this));

            IndexFile(directory);
        }

        /// <summary>
        /// Scans all files found in the provided directory and adds them to the file index.</summary>
        private void IndexFile(DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
            {
                if (IsFileExcluded(file.FullName))
                    continue;

                // Make sure that the backup file is properly
                // added to the index before creating a file node
                //FileHash hash = Index.IndexFile(file.FullName);

                //if(!(hash is null))
                //{
                //    var fileNode = new FileNode(file, this, hash);
                //    hash.AddNode(fileNode);
                //    AddFileNode(fileNode);
                //}

                FileNodes.Add(new FileNode(file, this));
            }
        }

        /// <summary>
        /// Adds the specified element to the file node index.</summary>  
        private void AddFileNode(FileDirectory node)
        {
            //TODO: Not needed anymore?
            if (node is FileNode)
            {
                FileNodes.Add(node);
                //Database.InsertFileNode(node as FileNode);
            }
            else
            {
                FileNodes.Add(node);
                //Database.InsertFileNode(node as FileDirectory);
            }
        }

        /// <summary>
        /// Generates hash for all files in the backupset and adds them to the hash index.</summary>  
        public async Task HashFilesAsync(CancellationToken cancellationToken)
        {
            await Task.Run(() =>
            {
                foreach (FileNode node in FileNodes.OfType<FileNode>())
                {
                    string checkSum;
                    try { checkSum = FileHash.CalculateChecksum(node.FullSessionName); }
                    catch (Exception)
                    {
                        // The file couldn't be hashed for some reason, don't add it to the index
                        //TODO: Inform the user that something went wrong
                        continue;
                    }

                    node.Hash = new FileHash(node.FullSessionName, checkSum);
                    node.Hash.AddNode(node);

                    //FileHash hash;
                    //if (!Index.Hashes.TryGetValue(checkSum, out hash))
                    //{
                    //    // If a hash already exist use that, if not create a new one
                    //    hash = new FileHash(node.FullSessionName, checkSum);
                    //    Index.Hashes.Add(checkSum, hash);

                    //}
                    //if (!(hash is null))
                    //{
                    //    node.File = hash;
                    //    hash.AddNode(node);
                    //}
                }
            }, cancellationToken);
        }

        /// <summary>
        /// Removes all Elements from the collection.</summary>  
        public async Task ClearAsync()
        {
            foreach (var item in FileNodes.OfType<FileNode>())
            {
                item.RemoveFileReference();
                //await Database.DeleteFileNodeAsync (item);
            }
            await Database.BatchDeleteFileNodeAsync(FileNodes.ToList());
            FileNodes.Clear();
        }

        /// <summary>
        /// Determines whether a directory is already indexed in the backup set.</summary>  
        public bool ContainsDirectory(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;

            return (dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length) + "\\").Contains(RootDirectory + "\\");
        }

        /// <summary>
        /// Determines whether the backup set is a child of a directory.</summary>  
        public bool IsSubsetOf(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;
            return (RootDirectory + "\\").Contains((dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length)) + "\\");
        }

        /// <summary>
        /// Determines whether the provided file or directory is excluded based on the file exclusion list.</summary>  
        public bool IsFileExcluded(string path)
        {
            foreach (var item in Exclusions)
            {
                var ab = Regex.IsMatch(path.Replace("\\\\", "\\"), item, RegexOptions.IgnoreCase);
                if (Regex.IsMatch(path.Replace("\\\\", "\\"), item, RegexOptions.IgnoreCase))
                    return true;

                //var pathX = path.Replace("\\\\", "\\");
                //var itemX = item;
                //var dd = Regex.IsMatch("F:\\Archive", ".*\\\\archive.*", RegexOptions.IgnoreCase);
                //dd = Regex.IsMatch("F:\\SomeDir\\Archive", ".*\\\\archive.*", RegexOptions.IgnoreCase);
                //dd = Regex.IsMatch("F:\\SomeDir\\Archive\file.zip", ".*\\.zip.*", RegexOptions.IgnoreCase);
            }
            return false;
        }

        /// <summary>
        /// Returns a list of all Hashes related to the nodes in the current BackupSet.</summary>  
        public List<FileHash> GetFileHashes()
        {
            return FileNodes
                .OfType<FileNode>()
                .Select(x => x.Hash)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Returns an IEnumerable object of all elements below the provided directory.</summary>  
        public IEnumerable<FileDirectory> GetChildElements(FileDirectory parent)
        {
            return FileNodes.Where(x => x.ParentDirectoryName == parent.DirectoryName); 
        }

        /// <summary>
        /// Returns the root file directory object.</summary>  
        public FileDirectory GetRootDirectoryObject()
        {
            return FileNodes.FirstOrDefault(x => x.DirectoryName.Equals(RootDirectory));
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return Guid.GetHashCode() ^
                Label.GetHashCode() ^ 
                RootDirectory.GetHashCode();
        }

        public override string ToString()
        {
            return Label + " " + RootDirectory;
        }

        public bool Equals(BackupSet other)
        {
            if (other == null)
                return false;

            return this.Guid.Equals(other.Guid) 
                && this.Label.Equals(other.Label)
                && this.RootDirectory.Equals(other.RootDirectory);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            BackupSet otherObj = obj as BackupSet;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        #endregion
    }
}
