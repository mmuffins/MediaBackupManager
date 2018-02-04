﻿using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class FileDirectoryViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        FileDirectory dir;
        BackupSetViewModel backupSet;
        FileDirectoryViewModel parent;
        bool treeViewIsSelected;
        bool treeViewIsExpanded;

        #endregion

        #region Properties

        public string Name
        {
            get => dir.Name;
        }

        public string DirectoryName
        {
            get => dir.DirectoryName;
        }

        public string FullName
        {
            get => dir.FullName;
        }

        public string FullSessionName
        {
            get => dir.FullSessionName;
        }

        //private string BaseParentDirectoryName
        //{
        //    get => dir.ParentDirectoryName;
        //}

        public FileDirectoryViewModel Parent
        {
            get
            {
                if (parent is null)
                {
                    this.parent = backupSet.GetDirectory(DirectoryName);
                    //NotifyPropertyChanged();
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

        public bool BackupStatus
        {
            get => dir.BackupStatus;
        }

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

        public virtual IEnumerable<FileDirectoryViewModel> SubDirectories
        {
            get => BackupSet.GetSubDirectories(Path.Combine(DirectoryName, Name));
        }

        public virtual IEnumerable<FileNodeViewModel> Files
        {
            get => BackupSet.GetFiles(Path.Combine(DirectoryName, Name));
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
                if (treeViewIsExpanded && Parent != null)
                    Parent.TreeViewIsExpanded = true;
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
        /// Returns true if the provided object is the base object of the current viewmodel.</summary>  
        public bool IsViewFor(FileDirectory fileDirectory)
        {
            return fileDirectory.Equals(dir);
        }

        public override string ToString()
        {
            return FullName;
        }

        #endregion
    }
}