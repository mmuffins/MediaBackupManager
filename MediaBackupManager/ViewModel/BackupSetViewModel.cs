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
        bool renameMode;

        RelayCommand renameBackupSetCommand;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the file Index containing the current Backup Set.</summary>  
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

        /// <summary>
        /// Gets the Backup set for this viewmodel.</summary>
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
        /// Gets the Guid of the current Backup Set.</summary>  
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

        /// <summary>
        /// Gets or sets the root directory of the current Backup Set.</summary>  
        public FileDirectoryViewModel RootDirectory
        {
            get
            {
                if (this.rootDirectory is null)
                {
                    this.rootDirectory = GetRootDirectoryObject();
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

        /// <summary>
        /// Gets or sets the user defined label for the current Backup Set.</summary>
        public string Label
        {
            get
            {
                return  backupSet.Label;
            }
            set
            {
                if (value != backupSet.Label && !String.IsNullOrWhiteSpace(value))
                {
                    RenameBackupSetCommand.Execute(value);
                    this.NotifyPropertyChanged();
                }

                // Always disable rename mode, even if the value remains 
                // the same, in case the user changed his mind
                this.RenameMode = false;
            }
        }

        /// <summary>
        /// Gets or sets the date the current Backup Set was last updated.</summary>  
        public DateTime LastScanDate
        {
            get => BackupSet.LastScanDate;
        }

        /// <summary>
        /// Gets or sets the logical volume of the current Backup Set.</summary>  
        public LogicalVolume Volume
        {
            get => BackupSet.Volume;
        }

        /// <summary>
        /// Gets or sets a value indicating if the logical volume containing the current Backup Set is currently connected to the host. To update, execute RefreshVolumeStatus.</summary>  
        public bool IsConnected
        {
            get => Volume.IsConnected;
        }

        /// <summary>
        /// Gets point or drive letter of the current Backup Set.</summary>  
        public string MountPoint
        {
            get => BackupSet.MountPoint;
        }

        /// <summary>
        /// Gets the volume serial number of the logical volume containing the current Backup Set.</summary>  
        public string SerialNumber
        {
            get => Volume.SerialNumber;
        }

        /// <summary>
        /// Gets the drive type of the logical volume containing the current Backup Set.</summary>  
        public DriveType DriveType
        {
            get => Volume.Type;
        }

        /// <summary>
        /// Gets a collection of directories contained in the current Backup Set.</summary>  
        public ObservableCollection<FileDirectoryViewModel> Directories { get; set; }

        /// <summary>
        /// Gets a collection of file nodes contained in the current Backup Set.</summary>  
        public ObservableCollection<FileNodeViewModel> FileNodes { get; set; }

        /// <summary>
        /// Gets a collection of file nodes and directories contained in the current Backup Set.</summary>  
        public List<object> ChildElements
        {
            get
            {
                return Directories.AsQueryable<object>().Concat(FileNodes.AsQueryable<object>()).ToList();
            }
        }

        /// <summary>
        /// Gets orsets whether the TreeViewItem 
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
        /// Gets or sets whether the TreeViewItem 
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
            }
        }

        /// <summary>
        /// Gets or sets whether this backup set is currently in renaming mode.
        /// </summary>
        public bool RenameMode
        {
            get { return renameMode; }
            set
            {
                if (value != renameMode)
                {
                    renameMode = value;
                    this.NotifyPropertyChanged();
                }
            }
        }

        public RelayCommand RenameBackupSetCommand
        {
            get
            {
                if (renameBackupSetCommand == null)
                {
                    renameBackupSetCommand = new RelayCommand(
                        async p => {
                            RenameMode = false;
                            await backupSet.UpdateLabel(p.ToString());
                            },
                        p => !String.IsNullOrWhiteSpace(p.ToString()));
                }
                return renameBackupSetCommand;
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
            this.RenameMode = false;

            foreach (var node in BackupSet.FileNodes)
            {
                if(node is FileNode)
                    this.FileNodes.Add(new FileNodeViewModel((FileNode)node, this));
                else
                    this.Directories.Add(new FileDirectoryViewModel(node, this));
            }

            RebuildDirectoryTree();
            backupSet.FileNodes.CollectionChanged += FileNodes_CollectionChanged;
            backupSet.PropertyChanged += BackupSet_PropertyChanged;
        }

        private void BackupSet_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            //TODO: Q-Any way to directly pass notifications from model to view instead of hooking up the event and manually forward it?
            switch (e.PropertyName)
            {
                case "MountPoint":
                    this.NotifyPropertyChanged("MountPoint");
                    break;

                case "LastScanDate":
                    this.NotifyPropertyChanged("DriveType");
                    break;

                case "Volume":
                    backupSet.Volume.PropertyChanged += Volume_PropertyChanged;
                    this.NotifyPropertyChanged("Volume");
                    break;
            }
        }

        private void Volume_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsConnected":
                    this.NotifyPropertyChanged("IsConnected");
                    break;

                case "SerialNumber":
                    this.NotifyPropertyChanged("SerialNumber");
                    break;

                case "Type":
                    this.NotifyPropertyChanged("Type");
                    break;

            }
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
            //this.rootDirectory = GetRootDirectoryObject();
            // Rebuilding the tree after every change of the collection is very expensive,
            // and generally not necessary since nodes are scanned in sequence
            //RebuildDirectoryTree();
            //RebuildNodeTree();

            ignoreChanges = false;

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
        /// Changes the label of the BackupSet.</summary>  
        public async Task UpdateLabel(string label)
        {
            await backupSet.UpdateLabel(label);
        }

        /// <summary>
        /// Returns an IEnumerable object of all directories below the provided directory.</summary>  
        public IEnumerable<FileDirectoryViewModel> GetSubDirectories(string path)
        {
            if (path == @"\")
                return Directories.Where(x => x.DirectoryName == path && x.Name != path);
            else 
                return Directories.Where(x => x.DirectoryName == path);
        }

        /// <summary>
        /// Returns an IEnumerable object of all file nodes below the provided directory.</summary>  
        public IEnumerable<FileNodeViewModel> GetFileNodes(string path)
        {
            if(path == @"\")
                return FileNodes.Where(x => x.DirectoryName == path);
            else
                return FileNodes.Where(x => x.DirectoryName.TrimStart('\\') == path);
        }

        /// <summary>
        /// Returns the directory object for the provided path.</summary>  
        public FileDirectoryViewModel GetDirectory(string directory)
        {
            if (directory == @"\")
                return Directories.FirstOrDefault(x => Path.Combine(x.DirectoryName, x.Name) == directory);
            else
                return Directories.FirstOrDefault(x => Path.Combine(x.DirectoryName.TrimStart('\\'), x.Name) == directory);
        }

        /// <summary>
        /// Returns the root file directory object.</summary>  
        private FileDirectoryViewModel GetRootDirectoryObject()
        {
            if (backupSet.RootDirectory == @"\")
                return Directories
                    .FirstOrDefault(x => x.DirectoryName == backupSet.RootDirectory && x.Name == backupSet.RootDirectory);
            else
                return Directories
                    .FirstOrDefault(x => Path.Combine(x.DirectoryName.TrimStart('\\'), x.Name)
                    .Equals(backupSet.RootDirectory));
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

        /// <summary>
        /// Rebuilds the parent/child relationship for all directories in the backup set.</summary>  
        public void RebuildDirectoryTree()
        {
            // Make sure that each element except root directories have a parent
            foreach (var dir in Directories.Where(x => x.Parent is null && x.Name != @"\" && x.DirectoryName != @"\"))
                dir.Parent = GetDirectory(dir.DirectoryName);

            // All elements except the root directory now have a parent,
            // with this we can rebuild the children
            foreach (var dir in Directories)
                dir.SubDirectories.Clear();

            foreach (var dir in Directories.Where(x => x.Parent != null))
                dir.Parent.SubDirectories.Add(dir);
        }

        /// <summary>
        /// Rebuilds the parent/child relationship for all file nodes in the backup set.</summary>  
        public void RebuildNodeTree()
        {
            // Make sure that each element has a parent
            foreach (var node in FileNodes.Where(x => x.Parent is null))
                node.Parent = GetDirectory(node.DirectoryName);

            foreach (var dir in Directories)
                dir.FileNodes.Clear();

            foreach (var node in FileNodes.Where(x => x.Parent != null))
                node.Parent.FileNodes.Add(node);
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
