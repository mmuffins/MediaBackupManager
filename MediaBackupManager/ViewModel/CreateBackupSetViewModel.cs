using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel
{
    public class CreateBackupSetViewModel : ViewModelBase.ViewModelBase
    {
        RelayCommand.RelayCommand selectDirectoryCommand;
        RelayCommand.RelayCommand cancelCommand;
        RelayCommand.RelayCommand confirmCommand;
        
        string selectedDirectory;
        string backupSetLabel;
        string scanStatusText;
        int scanProgress;
        FileIndexViewModel index;
        CancellationTokenSource tokenSource;

        #region Properties

        /// <summary>
        /// Command to open a FolderBrowserDialog and populate the path textbox with the selected directory.</summary>  
        public RelayCommand.RelayCommand SelectDirectoryCommand
        {
            get
            {
                if (selectDirectoryCommand == null)
                {
                    selectDirectoryCommand = new RelayCommand.RelayCommand(
                        SelectDirectoryCommand_Execute,
                        p => true);
                }
                return selectDirectoryCommand;
            }
        }

        /// <summary>
        /// Command to close the overlay.</summary>  
        public RelayCommand.RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand.RelayCommand(
                        CloseOverlay,
                        p => true);
                }
                return cancelCommand;
            }
        }

        /// <summary>
        /// Command to create a Backup Set for the selected drive.</summary>  
        public RelayCommand.RelayCommand ConfirmCommand
        {
            //TODO: Create Validation and error messages to make sure that all fields are filled
            get
            {
                if (confirmCommand == null)
                {
                    confirmCommand = new RelayCommand.RelayCommand(
                        CreateBackupSet,
                        p => true);
                }
                return confirmCommand;
            }
        }

        public string SelectedDirectory
        {
            get { return selectedDirectory; }
            set
            {
                if (value != selectedDirectory)
                {
                    selectedDirectory = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string BackupSetLabel
        {
            get { return backupSetLabel; }
            set
            {
                if (value != backupSetLabel)
                {
                    backupSetLabel = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Text displayed during directory scans, informing user of the current operation.</summary>  
        public string ScanStatusText
        {
            get { return scanStatusText; }
            set
            {
                if (value != scanStatusText)
                {
                    scanStatusText = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Numerical progress of the current scanning operation.</summary>  
        public int ScanProgress
        {
            get { return scanProgress; }
            set
            {
                if (value != scanProgress)
                {
                    scanProgress = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Token source for the scanning operation.</summary>  
        public CancellationTokenSource TokenSource
        {
            get
            {
                if (tokenSource is null)
                    tokenSource = new CancellationTokenSource();

                return tokenSource;
            }
            set
            {
                if (value != tokenSource)
                {
                    tokenSource = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        public CreateBackupSetViewModel(FileIndexViewModel index)
        {
            this.index = index;
        }

        /// <summary>
        /// Opens a FolderBrowserDialog and populates the path textbox with the selected directory.</summary>  
        private void SelectDirectoryCommand_Execute(object obj)
        {
            //TODO: Add function to read the drive label to auto-fill the label field
            var browser = new FolderBrowserDialog();
            browser.Description = "Please Select a folder";

            if (browser.ShowDialog() == DialogResult.OK)
            {
                SelectedDirectory = browser.SelectedPath;
            }
        }

        /// <summary>
        /// Creates a backup set for the currently selected directory.</summary>  
        private async void CreateBackupSet(object obj)
        {
            if (string.IsNullOrWhiteSpace(SelectedDirectory) || string.IsNullOrWhiteSpace(BackupSetLabel))
                return;

            var statusText = new Progress<string>(p => ScanStatusText = p);
            var scanProgress = new Progress<int>(p => ScanProgress = p);

            await index.CreateBackupSetAsync(new DirectoryInfo(SelectedDirectory), TokenSource.Token, scanProgress, statusText, BackupSetLabel);

            // All done, close the overlay
            CloseOverlay(null);
        }

        /// <summary>
        /// Closes the overlay and cancels all ongoing scanning operations.</summary>  
        private void CloseOverlay(object obj)
        {
            if (TokenSource != null)
            {
                TokenSource.Cancel();
                TokenSource.Dispose();
                TokenSource = null;
            }

            MessageService.SendMessage(this, "DisposeOverlay", null);
        }

        #endregion
    }
}
