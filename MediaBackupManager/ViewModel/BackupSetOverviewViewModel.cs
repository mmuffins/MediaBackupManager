using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class BackupSetOverviewViewModel :ViewModelBase
    {
        #region Fields

        FileIndexViewModel index;
        BackupSetViewModel selectedBackupSet;

        RelayCommand showCreateBackupSetViewCommand;
        RelayCommand removeBackupSetCommand;
        RelayCommand showDirectoryBrowserViewCommand;
        RelayCommand showExclusionCommand;

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

        public BackupSetViewModel SelectedBackupset
        {
            get { return selectedBackupSet; }
            set
            {
                if (value != selectedBackupSet)
                {
                    selectedBackupSet = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public RelayCommand ShowCreateBackupSetViewCommand
        {
            get
            {
                if (showCreateBackupSetViewCommand == null)
                {
                    // Messages the mainview to open the create backupset overlay
                    showCreateBackupSetViewCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowCreateBackupSetOverlay", null),
                        p => !Index.IsOperationInProgress);
                }
                return showCreateBackupSetViewCommand;
            }
        }

        public RelayCommand RemoveBackupSetCommand
        {
            get
            {
                if (removeBackupSetCommand == null)
                {
                    removeBackupSetCommand = new RelayCommand(
                        p => RemoveBackupSet(p as BackupSetViewModel),
                        p => !Index.IsOperationInProgress);
                }
                return removeBackupSetCommand;
            }
        }

        public RelayCommand ShowDirectoryBrowserViewCommand
        {
            get
            {
                if (showDirectoryBrowserViewCommand == null)
                {
                    showDirectoryBrowserViewCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowDirectoryBrowserView", null),
                        p => true);
                }
                return showDirectoryBrowserViewCommand;
            }
        }

        public RelayCommand ShowExclusionOverlayCommand
        {
            get
            {
                if (showExclusionCommand == null)
                {
                    showExclusionCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowExclusionListViewOverlay", null),
                        p => true);
                }
                return showExclusionCommand;
            }
        }

        #endregion

        #region Methods

        public BackupSetOverviewViewModel(FileIndexViewModel index)
        {
            this.Index = index;
        }


        /// <summary>
        /// Removes the provided Backupset from the file index.</summary>
        private async void RemoveBackupSet(BackupSetViewModel backupSet)
        {

            if (backupSet != null && backupSet is BackupSetViewModel && backupSet.BackupSet != null)
            {
                await Index.RemoveBackupSetAsync(backupSet.BackupSet);
            }
        }

        #endregion

    }
}
