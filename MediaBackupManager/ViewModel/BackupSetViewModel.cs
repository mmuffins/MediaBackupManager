using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class BackupSetViewModel : ViewModelBase, IEquatable<BackupSetViewModel>
    {
        #region Fields

        BackupSet backupSet;
        FileDirectoryViewModel rootDirectory;
        FileIndexViewModel index;
        private bool ignoreChanges = false;
        bool treeViewIsSelected;
        bool treeViewIsExpanded;

        #endregion

        #region Properties

        public FileIndexViewModel Index
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

        public Guid Guid
        {
            get => backupSet.Guid;
            //set
            //{
            //    if (value != backupSet.Guid)
            //    {
            //        backupSet.Guid = value;
            //        NotifyPropertyChanged();
            //    }
            //}
        }

        public FileDirectoryViewModel RootDirectory
        {
            get
            {
                if (this.rootDirectory is null)
                {
                    this.rootDirectory = GetRootDirectoryObject();
                    //NotifyPropertyChanged();
                }

                return this.rootDirectory;
            }
            set
            {
                if (value != this.rootDirectory)
                {
                    this.rootDirectory = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Label
        {
            get => BackupSet.Label;
        }

        public DateTime LastScanDate
        {
            get => BackupSet.LastScanDate;
        }

        public LogicalVolume Volume
        {
            get => BackupSet.Volume;
        }

        public bool IsConnected
        {
            get => Volume.IsConnected;
        }

        public string MountPoint
        {
            get => BackupSet.MountPoint;
        }

        public string SerialNumber
        {
            get => Volume.SerialNumber;
        }

        public DriveType DriveType
        {
            get => Volume.Type;
        }

        public ObservableCollection<FileDirectoryViewModel> Directories { get; set; }

        public ObservableCollection<FileNodeViewModel> FileNodes { get; set; }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is selected.
        /// </summary>
        public bool TreeViewIsSelected
        {
            get { return treeViewIsSelected; }
            set
            {
                if (value != treeViewIsSelected)
                {
                    treeViewIsSelected = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets/sets whether the TreeViewItem 
        /// associated with this object is expanded.
        /// </summary>
        public bool TreeViewIsExpanded
        {
            get { return treeViewIsExpanded; }
            set
            {
                if (value != treeViewIsExpanded)
                {
                    treeViewIsExpanded = value;
                    this.NotifyPropertyChanged();
                }

                // Backupsets are always a root node, so no need to check a parent
                //if (treeViewIsExpanded && Parent != null)
                //    Parent.TreeViewIsExpanded = true;
            }
        }


        #endregion

        #region Methods

        public BackupSetViewModel(BackupSet backupSet, FileIndexViewModel index)
        {
            this.BackupSet = backupSet;
            this.Directories = new ObservableCollection<FileDirectoryViewModel>();
            this.FileNodes = new ObservableCollection<FileNodeViewModel>();
            this.Index = index;


            foreach (var node in BackupSet.FileNodes)
            {
                if(node is FileNode)
                    this.FileNodes.Add(new FileNodeViewModel((FileNode)node, this));
                else
                    this.Directories.Add(new FileDirectoryViewModel(node, this));
            }

            backupSet.FileNodes.CollectionChanged += FileNodes_CollectionChanged;
        }


        private void FileNodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            ignoreChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                Directories.Clear();
                FileNodes.Clear();

                foreach (var node in backupSet.FileNodes)
                {
                    if (node is FileNode)
                        FileNodes.Add(new FileNodeViewModel((FileNode)node, this));
                    else
                        Directories.Add(new FileDirectoryViewModel(node, this));
                }
            }
            else
            {
                // Remove items from collection.
                var toRemoveDirs = new List<FileDirectoryViewModel>();
                var toRemoveFiles = new List<FileNodeViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                    {
                        if (item is FileNode)
                        {
                            foreach (var existingItem in FileNodes)
                            {
                                if (existingItem.IsViewFor((FileNode)item))
                                    toRemoveFiles.Add(existingItem);
                            }
                        }
                        else
                        {
                            foreach (var existingItem in Directories)
                            {
                                if (existingItem.IsViewFor((FileDirectory)item))
                                    toRemoveDirs.Add(existingItem);
                            }
                        }
                    }

                foreach (var item in toRemoveFiles)
                    FileNodes.Remove(item);

                foreach (var item in toRemoveDirs)
                    Directories.Remove(item);

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                    {
                        if (item is FileNode)
                            FileNodes.Add(new FileNodeViewModel((FileNode)item, this));
                        else
                            Directories.Add(new FileDirectoryViewModel((FileDirectory)item, this));
                    }
            }
            ignoreChanges = false;
            this.rootDirectory = GetRootDirectoryObject();

            NotifyPropertyChanged("FileNodes");
            NotifyPropertyChanged("Directories");
            NotifyPropertyChanged("RootDirectory");
        }

        /// <summary>
        /// Returns true if the provided object is the base object of the current viewmodel.</summary>  
        public bool IsViewFor(BackupSet backupSet)
        {
            return this.backupSet.Equals(backupSet);
        }

        /// <summary>
        /// Refreshes the status and mount point of the volume containing the backup set.</summary>  
        public void RefreshVolumeStatus()
        {
            Volume.RefreshStatus();
        }

        /// <summary>
        /// Returns an IEnumerable object of all directories below the provided directory.</summary>  
        public IEnumerable<FileDirectoryViewModel> GetSubDirectories(string path)
        {
            //return Directories.Where(x => x.Parent != null && x.Parent.DirectoryName == path);
            return Directories.Where(x => x.DirectoryName == path);
        }

        /// <summary>
        /// Returns an IEnumerable object of all file nodes below the provided directory.</summary>  
        public IEnumerable<FileNodeViewModel> GetFiles(string path)
        {
            return FileNodes.Where(x => x.DirectoryName == path);
        }

        /// <summary>
        /// Returns the directory object for the provided path.</summary>  
        public FileDirectoryViewModel GetDirectory(string directory)
        {
            return Directories.FirstOrDefault(x => Path.Combine(x.DirectoryName, x.Name) == directory);
        }

        /// <summary>
        /// Returns the root file directory object.</summary>  
        private FileDirectoryViewModel GetRootDirectoryObject()
        {
            return Directories
                .FirstOrDefault(x => Path.Combine(x.DirectoryName, x.Name) .Equals(backupSet.RootDirectory));
        }

        /// <summary>
        /// Returns a list of all file nodes matching the provided search term.</summary>  
        public IEnumerable<FileNodeViewModel> FindFileNodes(string searchTerm)
        {
            return FileNodes.Where(x => x.FullName.ToUpper().Contains(searchTerm.ToUpper()));
        }

        /// <summary>
        /// Returns a list of all directories matching the provided search term.</summary>  
        public IEnumerable<FileDirectoryViewModel> FindDirectories(string searchTerm)
        {
            return Directories.Where(x => x.FullName.ToUpper().Contains(searchTerm.ToUpper()));
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return this.BackupSet.GetHashCode();
        }

        public virtual bool Equals(BackupSetViewModel other)
        {
            if (other == null)
                return false;

            return this.BackupSet.Equals(other.BackupSet);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var otherObj = obj as BackupSetViewModel;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        #endregion

    }
}
