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
        }

        /// <summary>
        /// Gets or sets the root directory of the current Backup Set.</summary>  
        public FileDirectoryViewModel RootDirectory
        {
            get { return rootDirectory; }

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
            this.Index = index;
            this.RenameMode = false;
            this.RootDirectory = new FileDirectoryViewModel(backupSet.RootDirectory, null, this);

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

                case "RootDirectory":
                    if (backupSet.RootDirectory is null)
                        RootDirectory = null;
                    else
                        RootDirectory = new FileDirectoryViewModel(backupSet.RootDirectory, null, this);
                    break;

                case "Volume":
                    if(backupSet.Volume != null)
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
        /// Returns a list of all file nodes matching the provided search term.</summary>  
        public IEnumerable<FileNodeViewModel> FindFileNodes(string searchTerm)
        {
            return RootDirectory.GetAllFileNodes()
                .Where(x => x.FullName.ToUpper().Contains(searchTerm.ToUpper()));
        }

        /// <summary>
        /// Returns a list of all directories matching the provided search term.</summary>  
        public IEnumerable<FileDirectoryViewModel> FindDirectories(string searchTerm)
        {
            return RootDirectory.GetAllSubdirectories()
                .Where(x => x.FullName.ToUpper().Contains(searchTerm.ToUpper()));
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
