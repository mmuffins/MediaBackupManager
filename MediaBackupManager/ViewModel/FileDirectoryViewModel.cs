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
        BackupSetViewModel backupSet;
        FileDirectoryViewModel parent;
        ObservableCollection<FileDirectoryViewModel> subDirectories;
        ObservableCollection<FileNodeViewModel> fileNodes;
        bool treeViewIsSelected;
        bool treeViewIsExpanded;

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
        /// Gets the full path Name from the pof the current directory, with its parent Backup Set as root.</summary>  
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
            get
            {
                if (parent is null)
                {
                    if (this.Name == @"\" && this.DirectoryName == @"\")
                        this.parent = null;
                    else
                        this.parent = backupSet.GetDirectory(DirectoryName);
                }

                return parent;
            }
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
        /// Gets a value indicating if all subdirectories and child file nodes have are related to more than one logical volumes.</summary>  
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
        /// Gets or sets the Backup Set containing the current directory.</summary>  
        public BackupSetViewModel BackupSet
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
        /// <summary>Gets the subdirectories of the current directory.</summary>
        public ObservableCollection<FileDirectoryViewModel> SubDirectories
        {
            get
            {
                if (subDirectories is null)
                {
                    this.subDirectories = new ObservableCollection<FileDirectoryViewModel>(BackupSet
                        .GetSubDirectories(Path.Combine(DirectoryName, Name)));
                }

                return subDirectories;
            }
        }

        /// <summary>Gets a list of all file nodes below the current object.</summary>
        public ObservableCollection<FileNodeViewModel> FileNodes
        {
            get
            {
                if (fileNodes is null)
                {
                    this.fileNodes = new ObservableCollection<FileNodeViewModel>(BackupSet
                        .GetFileNodes(Path.Combine(DirectoryName, Name)));
                }

                return fileNodes;
            }
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
                    this.BackupSet.TreeViewIsExpanded = true;
                    if(parent != null)
                    {
                        Parent.TreeViewIsExpanded = true;
                    }
                }
            }
        }
        #endregion

        #region Methods

        public FileDirectoryViewModel(FileDirectory fileDirectory, BackupSetViewModel backupSet)
        {
            this.dir = fileDirectory;
            this.backupSet = backupSet;
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
