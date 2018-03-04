using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class FileDirectoryViewModel : ViewModelBase
    {
        #region Fields

        FileDirectory dir;
        ArchiveViewModel archive;
        FileDirectoryViewModel parent;
        ObservableCollection<FileDirectoryViewModel> subDirectories;
        ObservableCollection<FileNodeViewModel> fileNodes;
        bool treeViewIsSelected;
        bool treeViewIsExpanded;
        private bool ignoreSubdirectoryChanges = false;
        private bool ignoreFileChanges = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the Name of the current directory.</summary>  
        public string Name
        {
            get => dir.Name;
        }

        /// <summary>
        /// Gets or sets the Name of the directory containing the current directory.</summary>  
        public string DirectoryName
        {
            get => dir.DirectoryName;
        }

        /// <summary>
        /// Gets the full path Name from the pof the current directory, with its parent Archive as root.</summary>  
        public string FullName
        {
            get => dir.FullName;
        }

        /// <summary>
        /// Gets the full path Name from the pof the current directory, with its current mount point as root.</summary>  
        public string FullSessionName
        {
            get => dir.FullSessionName;
        }

        /// <summary>
        /// Gets or sets the parent directory object of the current directory.</summary>  
        public FileDirectoryViewModel Parent
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
        /// Gets a value indicating if all subdirectories and child file nodes are related to more than one logical volume.</summary>  
        public bool HasMultipleBackups
        {
            get
            {
                if((SubDirectories != null && FileNodes != null) || (SubDirectories.Count() > 0 && FileNodes.Count() > 0))
                    return SubDirectories.All(x => x.HasMultipleBackups == true) && FileNodes.All(x => x.HasMultipleBackups == true);
                else
                    return true;
            }
        }

        /// <summary>
        /// Gets or sets the Archive containing the current directory.</summary>  
        public ArchiveViewModel Archive
            {
                get { return archive; }
                set
                {
                    if (value != archive)
                    {
                        archive = value;
                        NotifyPropertyChanged();
                    }
                }
            }

        /// <summary>
        /// <summary>Gets the subdirectories of the current directory.</summary>
        public ObservableCollection<FileDirectoryViewModel> SubDirectories
        {
            get => subDirectories; 
        }

        /// <summary>Gets a list of all file nodes below the current object.</summary>
        public ObservableCollection<FileNodeViewModel> FileNodes
        {
            get => fileNodes;
        }


        public List<object> ChildElements
        {
            get
            {
                return SubDirectories
                    .AsQueryable<object>()
                    .Concat(FileNodes.AsQueryable<object>())
                    .ToList();
            }
        }

        /// <summary>
        /// Gets an IEnumerable containing all directories 
        /// from the Archive root to the current directory
        /// </summary>
        public IEnumerable<FileDirectoryViewModel> BreadCrumbList
        {
            get => GetBreadCrumbList();
        }

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

                // Expand all the way up to the root.
                if (treeViewIsExpanded)
                {
                    this.Archive.TreeViewIsExpanded = true;
                    if(parent != null)
                    {
                        Parent.TreeViewIsExpanded = true;
                    }
                }
            }
        }

        /// <summary>
        /// Gets the checksum of the file hash 
        /// related to the current file directory.</summary>  
        public string Checksum
        {
            // Only added for compatibility reasons in the directory
            // browser, this does not contain any actual data
            get => "";
        }

        /// <summary>
        /// Gets or sets the file extension of the current file directory.</summary>  
        public string Extension
        {
            // Only added for compatibility reasons in the directory
            // browser, this does not contain any actual data
            get => "";
        }

        /// <summary>
        /// Gets or sets the file hash related to the current file directory.</summary>  
        public FileHashViewModel Hash
        {
            // Only added for compatibility reasons in the directory
            // browser, this does not contain any actual data
            get => null;
        }



        #endregion

        #region Methods

        public FileDirectoryViewModel(FileDirectory fileDirectory, FileDirectoryViewModel parent, ArchiveViewModel archive)
        {
            this.dir = fileDirectory;
            this.archive = archive;
            this.Parent = parent;

            this.subDirectories = new ObservableCollection<FileDirectoryViewModel>();
            this.fileNodes = new ObservableCollection<FileNodeViewModel>();

            foreach (var item in dir.SubDirectories)
                this.SubDirectories.Add(new FileDirectoryViewModel(item, this, archive));

            foreach (var item in dir.FileNodes)
            {
                this.FileNodes.Add(new FileNodeViewModel(item, this, archive));
            }

            dir.SubDirectories.CollectionChanged += SubDirectories_CollectionChanged;
            dir.FileNodes.CollectionChanged += FileNodes_CollectionChanged;

        }

        private void SubDirectories_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ignoreSubdirectoryChanges)
                return;

            ignoreSubdirectoryChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                SubDirectories.Clear();

                foreach (var node in dir.SubDirectories)
                    SubDirectories.Add(new FileDirectoryViewModel(node, null, Archive));
            }
            else
            {
                // Remove items from collection.
                var toRemoveDirs = new List<FileDirectoryViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                    {
                        foreach (var existingItem in SubDirectories)
                        {
                            if (existingItem.IsViewFor((FileDirectory)item))
                                toRemoveDirs.Add(existingItem);
                        }
                    }

                foreach (var item in toRemoveDirs)
                    SubDirectories.Remove(item);

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                        SubDirectories.Add(new FileDirectoryViewModel((FileDirectory)item, this, Archive));
            }

            ignoreSubdirectoryChanges = false;
        }

        private void FileNodes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (ignoreFileChanges)
                return;

            ignoreFileChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                SubDirectories.Clear();

                foreach (var node in dir.FileNodes)
                    FileNodes.Add(new FileNodeViewModel(node, this, Archive));
            }
            else
            {
                // Remove items from collection.
                var toRemoveNodes = new List<FileNodeViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                    {
                        foreach (var existingItem in FileNodes)
                        {
                            if (existingItem.IsViewFor((FileNode)item))
                                toRemoveNodes.Add(existingItem);
                        }
                    }

                foreach (var item in toRemoveNodes)
                    FileNodes.Remove(item);

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                        FileNodes.Add(new FileNodeViewModel((FileNode)item, this, Archive));
            }

            ignoreFileChanges = false;
        }

        /// <summary>
        /// Gets a list of all parent directories up to the root node.</summary>
        public List<FileDirectoryViewModel> GetBreadCrumbList()
        {
            var parentList = new List<FileDirectoryViewModel>();
            parentList.Add(this);

            var currentDirectory = this;

            while (currentDirectory.Parent != null)
            {
                parentList.Add(currentDirectory.Parent);
                currentDirectory = currentDirectory.Parent;
            }

            parentList.Reverse();

            return parentList;
        }


        /// <summary>
        /// Returns a recursive list of all subdirectories of the current directory.</summary>
        public List<FileDirectoryViewModel> GetAllSubdirectories()
        {
            var resultList = new List<FileDirectoryViewModel>(SubDirectories);
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
        public List<FileNodeViewModel> GetAllFileNodes()
        {
            var resultList = new List<FileNodeViewModel>(FileNodes);
            GetAllSubdirectories().ForEach(x => resultList.AddRange(x.FileNodes));
            return resultList;
        }

        /// <summary>
        /// Returns true if the provided object is the base object of the current viewmodel.</summary>  
        public bool IsViewFor(FileDirectory fileDirectory)
        {
            return fileDirectory.Equals(dir);
        }

        #endregion

        #region Implementations

        public override string ToString()
        {
            return FullName;
        }

        #endregion
    }
}
