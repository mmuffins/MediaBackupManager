using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
    /// A collection of directories and files below a root location.</summary>  
    public class Archive : IEquatable<Archive>, INotifyPropertyChanged, IDisposable
    {
        #region Fields

        public event PropertyChangedEventHandler PropertyChanged;
        FileIndex index;
        Guid guid;
        LogicalVolume volume;
        string rootDirectoryPath;
        FileDirectory rootDirectory;
        string label;
        DateTime lastScanDate;
        List<string> exclusions;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the file Index containing the current Archive.</summary>  
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

        /// <summary>
        /// Gets or sets the Guid of the current Archive.</summary>  
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

        /// <summary>
        /// Gets or sets the logical volume of the current Archive.</summary>  
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

        /// <summary>
        /// Gets or sets the root directory path of the current Archive.</summary>  
        public string RootDirectoryPath
        {
            get { return rootDirectoryPath; }
            set
            {
                if (value != rootDirectoryPath)
                {
                    rootDirectoryPath = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the root directory of the current Archive.</summary>  
        public FileDirectory RootDirectory
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

        /// <summary>
        /// Gets or sets the date the current Archive was last updated.</summary>  
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

        /// <summary>
        /// Gets the mount point or drive letter of the current Archive.</summary>  
        public string MountPoint { get => Volume.MountPoint; }

        /// <summary>
        /// Gets or sets the user defined label for the current Archive.</summary>
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
        public Archive()
        {
            this.Guid = Guid.NewGuid();
            //this.FileNodes = new ObservableHashSet<FileDirectory>();
            if (string.IsNullOrWhiteSpace(this.Label))
                this.Label = this.Guid.ToString();
        }

        public Archive(DirectoryInfo directory, LogicalVolume drive, List<string> exclusions) : this()
        {
            this.Volume = drive;
            var pathRoot = Path.GetPathRoot(directory.FullName);
            if(directory.FullName == pathRoot)
            {
                // Selected path is the root of a drive
                this.rootDirectoryPath = @"\";
            }
            else
            {
                this.RootDirectoryPath = directory.FullName.Substring(Path.GetPathRoot(directory.FullName).Length);
            }

            //this.RootDirectory = directory.FullName.Substring(Path.GetPathRoot(directory.FullName).Length);
            this.exclusions = exclusions;

            if (string.IsNullOrWhiteSpace(this.Label))
                this.Label = drive.MountPoint;
        }

        /// <summary>
        /// Changes the label of the Archive.</summary>  
        public async Task UpdateLabel(string label)
        {
            if (Label.Equals(label))
                return;

            await Database.UpdateArchiveLabel(this, label);
            Label = label;
        }

        /// <summary>
        /// Scans all files below the root directory and adds them to the index.</summary>  
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="processingFile">Progress object used to provide feedback over the file that is currently being hashed.</param>
        public async Task ScanFilesAsync(CancellationToken cancellationToken, IProgress<string> processingFile)
        {
            var scanPath = Path.Combine(MountPoint, RootDirectoryPath);
            if (RootDirectoryPath == @"\")
                scanPath = MountPoint;

            if (IsFileExcluded(scanPath))
            {
                MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("The root directory of the Archive is excluded from scanning due to a matching file exclusion."));
                return;
            }

            RootDirectory = new FileDirectory(new DirectoryInfo(scanPath), null, this);

            await Task.Factory.StartNew(() => RootDirectory.ScanSubDirectories(RootDirectory, cancellationToken, processingFile), cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            LastScanDate = DateTime.Now;
        }


        /// <summary>
        /// Generates hash for all files in the archive and adds them to the hash index.</summary>  
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="progress">Progress object used to report the progress of the operation.</param>
        /// <param name="processingFile">Progress object used to provide feedback over the file that is currently being hashed.</param>
        public async Task HashFilesAsync(CancellationToken cancellationToken, IProgress<int> progress, IProgress<string> processingFile)
        {
            await Task.Factory.StartNew(() =>
            {
                var scanNodes = GetFileNodes();
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
                    catch (Exception ex)
                    {
                        // The file couldn't be hashed for some reason, don't add it to the index
                        MessageService.SendMessage(node, "FileScanException", new ApplicationException("Could not hash " + node.FullName, ex));

                        continue;
                    }
                    finally
                    {
                        if (progress != null)
                            progress.Report((int)((double)currentNodeCount / nodeCount * 100));
                    }

                    node.Hash = new FileHash(node.FullSessionName, checkSum);
                    node.Checksum = checkSum;
                    node.Hash.AddNode(node);
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

            LastScanDate = DateTime.Now;
        }

        /// <summary>
        /// Determines whether a directory is already indexed in the archive.</summary>  
        public bool ContainsDirectory(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;

            return (dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length) + "\\").Contains(RootDirectoryPath + "\\");
        }

        /// <summary>
        /// Determines whether the archive is a child of the provided directory.</summary>  
        public bool IsParentDirectory(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;
            return (RootDirectoryPath + "\\").Contains((dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length)) + "\\");
        }

        /// <summary>
        /// Determines whether the provided file or directory is excluded based on the file exclusion list.</summary>  
        public bool IsFileExcluded(string path)
        {
            foreach (var item in exclusions)
            {
                if (Regex.IsMatch(path.Replace("\\\\", "\\"), item, RegexOptions.IgnoreCase))
                    return true;
           }
            return false;
        }

        /// <summary>
        /// Returns a list of all Hashes related to the nodes in the current Archive.</summary>  
        public List<FileHash> GetFileHashes()
        {
            return RootDirectory.GetAllFileNodes()
                .Where(x => x.Hash != null)
                .Select(x => x.Hash)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Returns a list of all File Nodes contained the current Archive.</summary>  
        public List<FileNode> GetFileNodes()
        {
            return RootDirectory.GetAllFileNodes();
        }

        /// <summary>
        /// Returns a list of all File Directories contained the current Archive.</summary>  
        public List<FileDirectory> GetFileDirectories()
        {
            var resultList = new List<FileDirectory>();
            resultList.Add(RootDirectory);
            resultList.AddRange(RootDirectory.GetAllSubdirectories());
            return resultList;
        }


        /// <summary>
        /// Removes all Elements from the collection.</summary>  
        public async Task ClearAsync()
        {
            var deleteNodes = RootDirectory.GetAllSubdirectories();
            deleteNodes.Add(RootDirectory);
            deleteNodes.AddRange(RootDirectory.GetAllFileNodes());
            await Database.BatchDeleteFileNodeAsync(deleteNodes);

            RootDirectory.Dispose();
            RootDirectory = null;
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return Guid.GetHashCode() ^
                Label.GetHashCode() ^ 
                RootDirectoryPath.GetHashCode();
        }

        public override string ToString()
        {
            return Label + " " + RootDirectoryPath;
        }

        public bool Equals(Archive other)
        {
            if (other == null)
                return false;

            return this.Guid.Equals(other.Guid) 
                && this.Label.Equals(other.Label)
                && this.RootDirectoryPath.Equals(other.RootDirectoryPath);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var otherObj = obj as Archive;
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

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    if(RootDirectory != null)
                        RootDirectory.Dispose();
                }

                // set large fields to null.
                Index = null;
                Volume = null;
                RootDirectory = null;

                disposedValue = true;
            }
        }

         ~Archive()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
