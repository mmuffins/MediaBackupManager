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
    public class ArchiveOverviewViewModel :ViewModelBase
    {
        #region Fields

        FileIndexViewModel index;
        ArchiveViewModel selectedArchive;

        RelayCommand showCreateArchiveOverlayCommand;
        RelayCommand removeArchiveCommand;
        RelayCommand showUpdateArchiveOverlayCommand;
        RelayCommand showDirectoryBrowserViewCommand;
        RelayCommand showExclusionCommand;
        RelayCommand showCreateReportOverlayCommand;
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

        public ArchiveViewModel SelectedArchive
        {
            get { return selectedArchive; }
            set
            {
                if (value != selectedArchive)
                {
                    selectedArchive = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public RelayCommand ShowCreateArchiveOverlayCommand
        {
            get
            {
                if (showCreateArchiveOverlayCommand == null)
                {
                    // Messages the mainview to open the create archive overlay
                    showCreateArchiveOverlayCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowCreateArchiveOverlay", null),
                        p => !Index.IsOperationInProgress);
                }
                return showCreateArchiveOverlayCommand;
            }
        }

        public RelayCommand RemoveArchiveCommand
        {
            get
            {
                if (removeArchiveCommand == null)
                {
                    removeArchiveCommand = new RelayCommand(
                        async p => await RemoveArchive(p as ArchiveViewModel),
                        p => !Index.IsOperationInProgress);
                }
                return removeArchiveCommand;
            }
        }

        public RelayCommand ShowUpdateArchiveOverlayCommand
        {
            get
            {
                if (showUpdateArchiveOverlayCommand == null)
                {
                    showUpdateArchiveOverlayCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowUpdateArchiveOverlay", p as ArchiveViewModel),
                        p => !Index.IsOperationInProgress);
                }
                return showUpdateArchiveOverlayCommand;
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
                        p => !Index.IsOperationInProgress);
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

        public RelayCommand CreateReportOverlayCommand
        {
            get
            {
                if (showCreateReportOverlayCommand == null)
                {
                    showCreateReportOverlayCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowCreateReportOverlay", null),
                        p => !Index.IsOperationInProgress && Index.Archives.Count > 0);
                }
                return showCreateReportOverlayCommand;
            }
        }

        public RelayCommand EnableRenamingModeCommand
        {
            get
            {
                if (enableRenamingModeCommand == null)
                {
                    enableRenamingModeCommand = new RelayCommand(
                        p => ((ArchiveViewModel)p).RenameMode = true,
                        p => p is ArchiveViewModel);
                }
                return enableRenamingModeCommand;
            }
        }

        #endregion

        #region Methods

        public ArchiveOverviewViewModel(FileIndexViewModel index)
        {
            this.Index = index;
        }

        /// <summary>
        /// Removes the provided Archive from the file index.</summary>
        private async Task RemoveArchive(ArchiveViewModel archive)
        {
            var confirmDiag = new OKCancelPopupViewModel("Do you want to delete Archive " + archive.Label + "?", "", "Delete", "No");
            if (confirmDiag.ShowDialog() == DialogResult.Cancel)
                return;

            // User has confirmed the deletion, continue 
            if (archive != null && archive is ArchiveViewModel && archive.Archive != null)
                await Index.RemoveArchiveAsync(archive.Archive);
        }

        #endregion

    }
}
