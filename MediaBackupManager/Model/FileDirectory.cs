using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents a directory in the file system.</summary>  
    public class FileDirectory : IEquatable<FileDirectory>, IComparable<FileDirectory>, INotifyPropertyChanged, IDisposable
    {
        #region Fields

        string name;
        string directoryName;
        BackupSet backupSet;
        FileDirectory parent;
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Name of the current directory.</summary>  
        public string Name
        {
            get { return name; }
            set
            {
                if (value != name)
                {
                    name = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Backup Set containing the current directory.</summary>  
        public BackupSet BackupSet
        {
            get { return backupSet; }
            set
            {
                if (value != backupSet)
                {
                    backupSet = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the Name of the directory containing the current directory.</summary>  
        public string DirectoryName
        {
            get { return directoryName; }
            set
            {
                if (value != directoryName)
                {
                    directoryName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the full path Name from the of the current directory, with its parent Backup Set as root.</summary>  
        public virtual string FullName { get => Path.Combine(BackupSet.Label, DirectoryName, Name); }

        /// <summary>
        /// Gets the full path Name from the of the current directory, with its current mount point as root.</summary>  
        public virtual string FullSessionName { get => Path.Combine(BackupSet.Volume.MountPoint, DirectoryName, Name); }

        /// <summary>
        /// Gets a value indicating if all subdirectories and child file nodes are related to more than one logical volumes.</summary>  
        public virtual bool BackupStatus
        {
            get
            {
                if(FileNodes.Count > 0)
                    if (!FileNodes.All(x => x.BackupStatus.Equals(true)))
                        return false;

                if(SubDirectories.Count > 0)
                    if (!SubDirectories.All(x => x.BackupStatus.Equals(true)))
                        return false;

                return true;
            }
        }

        /// <summary>
        /// Gets or sets the parent directory object of the current file directory.</summary>  
        public FileDirectory Parent
        {
            get { return parent; }
            set
            {
                if (value != parent)
                {
                    parent = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the subdirectories of the current directory.</summary>
        public ObservableCollection<FileDirectory> SubDirectories { get; /*set;*/ }

        /// <summary>
        /// Gets a list of all file nodes below the current object.</summary>
        public ObservableCollection<FileNode> FileNodes { get; /*set;*/ }

        #endregion

        #region Methods

        public FileDirectory()
        {
            this.SubDirectories = new ObservableCollection<FileDirectory>();
            this.FileNodes = new ObservableCollection<FileNode>();
        }

        public FileDirectory(DirectoryInfo directoryInfo, FileDirectory parent, BackupSet backupSet) : this()
        {
            this.BackupSet = backupSet;
            this.Parent = parent;

            // set name to \ if the this is the root directory
            if (directoryInfo.FullName == directoryInfo.Root.FullName)
                this.name = @"\";
            else
                this.name = directoryInfo.Name;

            // Set root directory to \
            if (directoryInfo.Parent is null || directoryInfo.Parent.FullName == directoryInfo.Root.FullName)
                this.directoryName = @"\";
            else
                this.DirectoryName = directoryInfo.Parent.FullName.Substring(Path.GetPathRoot(directoryInfo.Parent.FullName).Length);


        }

        /// <summary>
        /// Recursively adds the provided directory and subdirectories to the file index.</summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="processingDirectory">Progress object used to provide feedback over the current status of the operation.</param>
        public void ScanSubDirectories(FileDirectory parentDirectory, CancellationToken cancellationToken, IProgress<string> processingDirectory)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            var directory = new DirectoryInfo(FullSessionName);

            ScanDirectoryFiles(directory, this, cancellationToken, processingDirectory);

            // Add all subdirectories of the current directory to the subdirectory collection an
            // recursively scan their respective subdirectories
            try
            {
                foreach (var item in directory.GetDirectories())
                {
                    if (BackupSet.IsFileExcluded(directory.FullName))
                    {
                        //MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("Directory " + directory.FullName + " is excluded from scanning due to a matching file exclusion."));
                        continue;
                    }

                    var newSubDirectory = new FileDirectory(item, this, BackupSet);
                    SubDirectories.Add(newSubDirectory);
                    newSubDirectory.ScanSubDirectories(this, cancellationToken, processingDirectory);
                }
            }
            catch (Exception ex)
            {
                MessageService.SendMessage(directory, "FileScanException", new ApplicationException("Could not scan " + directory.FullName, ex));
            }

        }

        /// <summary>
        /// Scans all files found in the provided directory and adds them to the file index.</summary>
        /// <param name="cancellationToken">Cancellation token for the async operation.</param>
        /// <param name="processingFile">Progress object used to provide feedback over the file that is currently being hashed.</param>
        private void ScanDirectoryFiles(DirectoryInfo directory, FileDirectory parent, CancellationToken cancellationToken, IProgress<string> processingFile)
        {
            foreach (var file in directory.GetFiles())
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                if (BackupSet.IsFileExcluded(file.FullName))
                {
                    //MessageService.SendMessage(this, "ScanLogicException", new ApplicationException("File " + file.FullName + " is excluded from scanning due to a matching file exclusion."));
                    continue;
                }

                // Report the current progress
                if (processingFile != null)
                    processingFile.Report(file.FullName);

                try
                {
                    FileNodes.Add(new FileNode(file, BackupSet, this));
                }
                catch (Exception ex)
                {
                    MessageService.SendMessage(file, "FileScanException", new ApplicationException("Could not scan " + file.FullName, ex));
                }

            }
        }

        /// <summary>
        /// Returns a recursive list of all subdirectories of the current directory.</summary>
        public List<FileDirectory> GetAllSubdirectories()
        {
            var resultList = new List<FileDirectory>(SubDirectories);
            if (resultList is null || resultList.Count == 0)
                return resultList;

            foreach (var item in SubDirectories)
            {
                var subDirs = item.GetAllSubdirectories();
                if (subDirs != null && subDirs.Count > 0)
                    resultList.AddRange(subDirs);
            }

            return resultList;
        }

        /// <summary>
        /// Returns a recursive list of all file nodes below the current directory.</summary>
        public List<FileNode> GetAllFileNodes()
        {
            var resultList = new List<FileNode>(FileNodes);
            GetAllSubdirectories().ForEach(x => resultList.AddRange(x.FileNodes));
            return resultList;
        }

        // FileDirectory objects don't have any FileHash references,
        // so the class is not implemented here, but still needed for
        // compatibility reasons
        /// <summary>
        /// Removes the reference to this node from the linked FileHash object.</summary>
        public virtual void RemoveFileReference() { }

        #endregion

        #region Implementations

        public override string ToString()
        {
            return FullName;
        }

        public override int GetHashCode()
        {
            return (BackupSet.Guid + DirectoryName + Name).GetHashCode();
        }

        public virtual bool Equals(FileDirectory other)
        {
            if (other == null)
                return false;

            return this.Name.Equals(other.Name)
                && this.DirectoryName.Equals(other.DirectoryName)
                && this.BackupSet.Equals(other.BackupSet);
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

        public int CompareTo(FileDirectory other)
        {
            return (DirectoryName + Name).CompareTo(other.DirectoryName + other.Name);
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

                    foreach (var node in FileNodes)
                        node.Dispose();

                    FileNodes.Clear();

                    foreach (var dir in SubDirectories)
                        dir.Dispose();

                    SubDirectories.Clear();
                }

                // set large fields to null.
                Parent = null;
                BackupSet = null;
                disposedValue = true;
            }
        }

        // override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FileDirectory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public virtual void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}


