using MediaBackupManager.SupportingClasses;
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
    /// A collection of directories and files below a root location.</summary>  
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

        /// <summary>
        /// Gets or sets the file Index containing the current Backup Set.</summary>  
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
        /// Gets or sets the Guid of the current Backup Set.</summary>  
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
        /// Gets or sets the logical volume of the current Backup Set.</summary>  
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
        /// Gets or sets the root directory of the current Backup Set.</summary>  
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

        /// <summary>
        /// Gets or sets the date the current Backup Set was updated.</summary>  
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
        /// Gets the mount point of the current Backup Set.</summary>  
        public string MountPoint { get => Volume.MountPoint; }

        /// <summary>
        /// Gets a collection of file nodes contained in the current Backup Set.</summary>  
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
        /// Changes the label of the BackupSet.</summary>  
        public async Task UpdateLabel(string label)
        {
            if (Label.Equals(label))
                return;

            await Database.UpdateBackupSetLabel(this, label);
            Label = label;
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
            catch (Exception ex)
            {
                MessageService.SendMessage(directory, "FileScanException", new ApplicationException("Could not scan " + directory.FullName, ex));
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

                try
                {
                    FileNodes.Add(new FileNode(file, this));
                }
                catch (Exception ex)
                {
                    MessageService.SendMessage(file, "FileScanException", new ApplicationException("Could not scan " + file.FullName,ex));
                }

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
                    catch (Exception ex)
                    {
                        // The file couldn't be hashed for some reason, don't add it to the index
                        MessageService.SendMessage(node, "FileScanException", new ApplicationException("Could not hash " + node.FullName, ex));

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
