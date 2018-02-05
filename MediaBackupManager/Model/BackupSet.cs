using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents an index filesystem location.</summary>  
    public class BackupSet : IEquatable<BackupSet>, INotifyPropertyChanged
    {
        #region Fields

        public event PropertyChangedEventHandler PropertyChanged;
        FileIndex index;
        Guid guid;
        LogicalVolume volume;
        string rootDirectory;
        string label;
        DateTime lastScanDate;
        List<string> exclusions;

        #endregion

        #region Properties

        public FileIndex Index
        {
            get { return index; }
            set
            {
                if (value != index)
                {
                    index = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public Guid Guid
        {
            get { return guid; }
            set
            {
                if (value != guid)
                {
                    guid = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public LogicalVolume Volume
        {
            get { return volume; }
            set
            {
                if (value != volume)
                {
                    volume = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string RootDirectory
        {
            get { return rootDirectory; }
            set
            {
                if (value != rootDirectory)
                {
                    rootDirectory = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DateTime LastScanDate
        {
            get { return lastScanDate; }
            set
            {
                if (value != lastScanDate)
                {
                    lastScanDate = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string MountPoint { get => Volume.MountPoint; }

        public ObservableHashSet<FileDirectory> FileNodes { get; }

        /// <summary>User defined label for the drive</summary>
        public string Label
        {
            get { return label; }
            set
            {
                if (value != label)
                {
                    label = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods
        public BackupSet()
        {
            this.Guid = Guid.NewGuid();
            this.FileNodes = new ObservableHashSet<FileDirectory>();
            if (string.IsNullOrWhiteSpace(this.Label))
                this.Label = this.Guid.ToString();
        }

        public BackupSet(DirectoryInfo directory, LogicalVolume drive, List<string> exclusions) : this()
        {
            this.Volume = drive;
            this.RootDirectory = directory.FullName.Substring(Path.GetPathRoot(directory.FullName).Length);
            this.exclusions = exclusions;

            if (string.IsNullOrWhiteSpace(this.Label))
                this.Label = drive.MountPoint;
        }

        /// <summary>
        /// Scans all files below the root directory and adds them to the index.</summary>  
        public async Task ScanFilesAsync(CancellationToken cancellationToken)
        {
            if (IsFileExcluded((Path.Combine(MountPoint, RootDirectory)).ToString()))
                return;

            await Task.Run(()=>IndexDirectory(new DirectoryInfo(Path.Combine(MountPoint, RootDirectory)), cancellationToken), cancellationToken);
            LastScanDate = DateTime.Now;
        }

        /// <summary>
        /// Recursively adds the provided directory and subdirectories to the file index.</summary>
        private void IndexDirectory(DirectoryInfo directory, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (IsFileExcluded(directory.FullName))
                return; //Don't index excluded directories at all

            // Call recursively to get all subdirectories
            try
            {
                foreach (var item in directory.GetDirectories())
                    IndexDirectory(item, cancellationToken);

                FileNodes.Add(new FileDirectory(directory, this));

                IndexFile(directory, cancellationToken);
            }
            catch (Exception)
            {
                //TODO: Inform the user that something went wrong 
            }

        }

        /// <summary>
        /// Scans all files found in the provided directory and adds them to the file index.</summary>
        private void IndexFile(DirectoryInfo directory, CancellationToken cancellationToken)
        {

            foreach (var file in directory.GetFiles())
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

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

                try
                {
                    FileNodes.Add(new FileNode(file, this));
                }
                catch (Exception)
                {
                    //TODO: Inform the user that something went wrong 
                }

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
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="processingFile">Progress object used to indicate the file that is currently being hashed.</param>
        public async Task HashFilesAsync(CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> processingFile)
        {
            await Task.Run(() =>
            {
                var scanNodes = FileNodes.OfType<FileNode>();
                var nodeCount = scanNodes.Count();
                int currentNodeCount = 0;

                foreach (FileNode node in scanNodes)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    // Report the current progress
                    currentNodeCount++;
                    if(processingFile != null)
                        processingFile.Report(node.FullSessionName);

                    string checkSum;
                    try { checkSum = FileHash.CalculateChecksum(node.FullSessionName); }
                    catch (Exception)
                    {
                        // The file couldn't be hashed for some reason, don't add it to the index
                        //TODO: Inform the user that something went wrong
                        continue;
                    }
                    node.Hash = new FileHash(node.FullSessionName, checkSum);
                    node.Checksum = checkSum;
                    node.Hash.AddNode(node);

                    if(progress != null)
                        progress.Report((int)((double)currentNodeCount / nodeCount * 100));
                }
            }, cancellationToken);

            LastScanDate = DateTime.Now;
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
            foreach (var item in exclusions)
            {
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
                .Where(x => x.Hash != null)
                .Select(x => x.Hash)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Returns an IEnumerable object of all elements below the provided directory.</summary>  
        public IEnumerable<FileDirectory> GetChildElements(FileDirectory parent)
        {
            return FileNodes.Where(x => Path.Combine(x.DirectoryName, x.Name) == parent.DirectoryName); 
        }

        /// <summary>
        /// Returns the root file directory object.</summary>  
        public FileDirectory GetRootDirectoryObject()
        {
            return FileNodes
                .OfType<FileDirectory>()
                .FirstOrDefault(x => Path.Combine(x.DirectoryName, x.Name).Equals(RootDirectory) && x.GetType() == typeof(FileDirectory));
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

            var otherObj = obj as BackupSet;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
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
