using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using MediaBackupManager.ViewModel.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel
{
    public class BackupSetOverviewViewModel :ViewModelBase
    {
        #region Fields

        FileIndexViewModel index;
        BackupSetViewModel selectedBackupSet;

        RelayCommand showCreateBackupSetOverlayCommand;
        RelayCommand removeBackupSetCommand;
        RelayCommand showUpdateBackupSetOverlayCommand;
        RelayCommand showDirectoryBrowserViewCommand;
        RelayCommand showExclusionCommand;
        RelayCommand enableRenamingModeCommand;


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

        public RelayCommand ShowCreateBackupSetOverlayCommand
        {
            get
            {
                if (showCreateBackupSetOverlayCommand == null)
                {
                    // Messages the mainview to open the create backupset overlay
                    showCreateBackupSetOverlayCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowCreateBackupSetOverlay", null),
                        p => !Index.IsOperationInProgress);
                }
                return showCreateBackupSetOverlayCommand;
            }
        }

        public RelayCommand RemoveBackupSetCommand
        {
            get
            {
                if (removeBackupSetCommand == null)
                {
                    removeBackupSetCommand = new RelayCommand(
                        async p => await RemoveBackupSet(p as BackupSetViewModel),
                        p => !Index.IsOperationInProgress);
                }
                return removeBackupSetCommand;
            }
        }

        public RelayCommand ShowUpdateBackupSetOverlayCommand
        {
            get
            {
                if (showUpdateBackupSetOverlayCommand == null)
                {
                    showUpdateBackupSetOverlayCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowUpdateBackupSetOverlay", p as BackupSetViewModel),
                        p => !Index.IsOperationInProgress);
                }
                return showUpdateBackupSetOverlayCommand;
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

        public RelayCommand EnableRenamingModeCommand
        {
            get
            {
                if (enableRenamingModeCommand == null)
                {
                    enableRenamingModeCommand = new RelayCommand(
                        p => ((BackupSetViewModel)p).RenameMode = true,
                        p => p is BackupSetViewModel);
                }
                return enableRenamingModeCommand;
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
        private async Task RemoveBackupSet(BackupSetViewModel backupSet)
        {

            var confirmDiag = new OKCancelPopupViewModel("Do you want to remove Backup Set " + backupSet.Label + "?", "", "Delete", "No");
            if (confirmDiag.ShowDialog() == DialogResult.Cancel)
                return;

            // User has clicked abort, remve the set 
            if (backupSet != null && backupSet is BackupSetViewModel && backupSet.BackupSet != null)
            {
                await Index.RemoveBackupSetAsync(backupSet.BackupSet);
            }

        }

        #endregion

    }
}
