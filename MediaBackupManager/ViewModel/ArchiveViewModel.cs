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
    public class ArchiveViewModel : ViewModelBase, IEquatable<ArchiveViewModel>
    {
        #region Fields

        Archive archive;
        FileDirectoryViewModel rootDirectory;
        FileIndexViewModel index;
        bool treeViewIsSelected;
        bool treeViewIsExpanded;
        bool renameMode;
        bool isConnected;
        string mountPoint;
        string serialNumber;

        RelayCommand renameArchiveCommand;

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the file Index containing the current Archive.</summary>  
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
        /// Gets the Archive for this viewmodel.</summary>
        public Archive Archive
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
        /// Gets the Guid of the current Archive.</summary>  
        public Guid Guid
        {
            get => archive.Guid;
        }

        /// <summary>
        /// Gets or sets the root directory of the current Archive.</summary>  
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
        /// Gets or sets the user defined label for the current Archive.</summary>
        public string Label
        {
            get
            {
                return  archive.Label;
            }
            set
            {
                if (value != archive.Label && !String.IsNullOrWhiteSpace(value))
                {
                    RenameArchiveCommand.Execute(value);
                    this.NotifyPropertyChanged();
                }

                // Always disable rename mode, even if the value remains 
                // the same, in case the user changed his mind
                this.RenameMode = false;
            }
        }

        /// <summary>
        /// Gets or sets the date the current Archive was last updated.</summary>  
        public DateTime LastScanDate
        {
            get => Archive.LastScanDate;
        }

        /// <summary>
        /// Gets or sets the logical volume of the current Archive.</summary>  
        public LogicalVolume Volume
        {
            get => Archive.Volume;
        }

        /// <summary>
        /// Gets or sets a value indicating if the logical volume containing the current Archive is currently connected to the host. To update, execute RefreshVolumeStatus.</summary>  
        public bool IsConnected
        {
            get { return isConnected; }
            private set
            {
                if (value != isConnected)
                {
                    isConnected = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets point or drive letter of the current Archive.</summary>  
        public string MountPoint
        {
            get { return mountPoint; }
            private set
            {
                if (value != mountPoint)
                {
                    mountPoint = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the volume serial number of the logical volume containing the current Archive.</summary>  
        public string SerialNumber
        {
            get { return serialNumber; }
            private set
            {
                if (value != serialNumber)
                {
                    serialNumber = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets the drive type of the logical volume containing the current Archive.</summary>  
        public DriveType DriveType
        {
            get => Volume is null ? DriveType.Unknown : Volume.Type;
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
        /// Gets or sets whether this archive is currently in renaming mode.
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

        public RelayCommand RenameArchiveCommand
        {
            get
            {
                if (renameArchiveCommand == null)
                {
                    renameArchiveCommand = new RelayCommand(
                        async p => {
                            RenameMode = false;
                            await archive.UpdateLabel(p.ToString());
                            },
                        p => !String.IsNullOrWhiteSpace(p.ToString()));
                }
                return renameArchiveCommand;
            }
        }

        /// <summary>
        /// Gets a value indicating if all subdirectories and child file nodes are related to more than one logical volume.</summary>  
        public bool HasMultipleBackups
        {
            get => RootDirectory is null ? true : RootDirectory.HasMultipleBackups;
        }

        #endregion

        #region Methods

        public ArchiveViewModel(Archive archive, FileIndexViewModel index)
        {
            this.Archive = archive;
            this.Index = index;
            this.RenameMode = false;
            this.isConnected = false;
            this.mountPoint = "";
            this.serialNumber = "";
            this.RootDirectory = new FileDirectoryViewModel(archive.RootDirectory, null, this);

            archive.PropertyChanged += Archive_PropertyChanged;

            if(archive.Volume != null)
            {
                archive.Volume.PropertyChanged += Volume_PropertyChanged;
                IsConnected = Volume.IsConnected;
                MountPoint = Volume.MountPoint;
                SerialNumber = Volume.SerialNumber;
            }
        }

        private void Archive_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "LastScanDate":
                    this.NotifyPropertyChanged("LastScanDate");
                    break;

                case "RootDirectory":
                    if (archive.RootDirectory is null)
                        RootDirectory = null;
                    else
                        RootDirectory = new FileDirectoryViewModel(archive.RootDirectory, null, this);
                    break;

                case "Volume":
                    if(archive.Volume != null)
                        archive.Volume.PropertyChanged += Volume_PropertyChanged;

                    this.NotifyPropertyChanged("Volume");
                    break;
            }
        }

        private void Volume_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsConnected":
                    IsConnected = Volume.IsConnected;
                    break;

                case "MountPoint":
                    MountPoint = Volume.MountPoint;
                    break;

                case "SerialNumber":
                    SerialNumber = Volume.SerialNumber;
                    break;

                case "Type":
                    this.NotifyPropertyChanged("Type");
                    break;

            }
        }

        /// <summary>
        /// Returns true if the provided object is the base object of the current viewmodel.</summary>  
        public bool IsViewFor(Archive archive)
        {
            return this.archive.Equals(archive);
        }

        /// <summary>
        /// Refreshes the status and mount point of the volume containing the archive.</summary>  
        public void RefreshVolumeStatus()
        {
            Volume.RefreshStatus();
        }

        /// <summary>
        /// Changes the label of the Archive.</summary>  
        public async Task UpdateLabel(string label)
        {
            await archive.UpdateLabel(label);
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
            return this.Archive.GetHashCode();
        }

        public virtual bool Equals(ArchiveViewModel other)
        {
            if (other == null)
                return false;

            return this.Archive.Equals(other.Archive);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            var otherObj = obj as ArchiveViewModel;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        #endregion

    }
}
