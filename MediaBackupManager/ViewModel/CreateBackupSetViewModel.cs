using MediaBackupManager.SupportingClasses;
using MediaBackupManager.ViewModel.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel
{
    public class CreateBackupSetViewModel : ViewModelBase
    {
        string selectedDirectory;
        string backupSetLabel;
        string scanStatusText;
        int scanProgress;
        string cancelButtonCaption;
        bool isScanInProgressOrCompleted;

        FileIndexViewModel index;
        CancellationTokenSource tokenSource;
        string fileScanErrorString;
        Task directoryScan;

        RelayCommand selectDirectoryCommand;
        RelayCommand cancelCommand;
        RelayCommand startCommand;

        #region Properties

        /// <summary>
        /// Gets or sets the command to open a Dialog that lets the user a directory for the backup set.</summary>  
        public RelayCommand SelectDirectoryCommand
        {
            get
            {
                if (selectDirectoryCommand == null)
                {
                    selectDirectoryCommand = new RelayCommand(
                        SelectDirectoryCommand_Execute,
                        p => !IsScanInProgressOrCompleted);
                }
                return selectDirectoryCommand;
            }
        }

        /// <summary>
        /// Gets or sets the command to close the overlay.</summary>  
        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(
                        CancelCommand_Execute,
                        p => true);
                }
                return cancelCommand;
            }
        }

        /// <summary>
        /// Gets or sets the command to create a Backup Set for the selected drive.</summary>  
        public RelayCommand StartCommand
        {
            get
            {
                if (startCommand == null)
                {
                    startCommand = new RelayCommand(
                        async p => await CreateBackupSet(),
                        p => !String.IsNullOrWhiteSpace(BackupSetLabel) 
                        && !String.IsNullOrWhiteSpace(SelectedDirectory)
                        && !IsScanInProgressOrCompleted);
                }
                return startCommand;
            }
        }

        /// <summary>
        /// Gets or sets the currently selected directory.</summary>  
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

        /// <summary>
        /// Gets or sets the label of the new backup set.</summary>  
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
        /// Gets or sets the caption for the Cancel button.</summary>  
        public string CancelButtonCaption
        {
            get { return cancelButtonCaption; }
            set
            {
                if (value != cancelButtonCaption)
                {
                    cancelButtonCaption = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text displayed during directory scans, informing user of the current operation.</summary>  
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
        /// Gets a value indicating whether a file scan is in progress or was successfully completed.</summary>  
        public bool IsScanInProgressOrCompleted
        {
            // Needed to simplify binding
            get { return isScanInProgressOrCompleted; }
            set
            {
                if (value != isScanInProgressOrCompleted)
                {
                    isScanInProgressOrCompleted = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the numerical progress of the current scanning operation.</summary>  
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
        /// Gets or sets the token source for the scanning operation.</summary>  
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

        /// <summary>
        /// Gets a list of all errors that occured while scanning or hashing files.</summary>  
        public string FileScanErrorString
        {
            get { return fileScanErrorString; }

            set
            {
                if (value != fileScanErrorString)
                {
                    fileScanErrorString = value;
                    NotifyPropertyChanged();
                    NotifyPropertyChanged("HasScanErrors");
                }
            }
        }
        /// <summary>
        /// Gets a value indicating whether errors occured during a file scan process.</summary>  
        public bool HasScanErrors
        {
            get => FileScanErrorString.Length > 0;
        }

        #endregion

        #region Methods

        public CreateBackupSetViewModel(FileIndexViewModel index)
        {
            this.index = index;
            this.CancelButtonCaption = "Cancel";
            this.IsScanInProgressOrCompleted = false;
            this.FileScanErrorString = "";
        }

        /// <summary>
        /// Event handler for the global MessageService.</summary>
        protected override void OnMessageServiceMessage(object sender, MessageServiceEventArgs e)
        {
            switch (e.Property)
            {
                case "ScanLogicException":
                // General exception in the scanning logic procedure
                case "FileScanException":
                    // Sent during scanning or hashing operations
                    // add them to the error log to display to the user once done
                    if(e.Parameter is ApplicationException)
                    {
                        var errorMsg = $"{((ApplicationException)e.Parameter).Message}";
                        if (((ApplicationException)e.Parameter).InnerException != null)
                            errorMsg += $": { ((ApplicationException)e.Parameter).InnerException.Message}";

                        errorMsg += "\n";

                        FileScanErrorString += errorMsg;
                    }
                    break;

                default:
                    break;
            }
        }

        /// Opens a FolderBrowserDialog and populates the path textbox with the selected directory.</summary>  
        private void SelectDirectoryCommand_Execute(object obj)
        {
            var browser = new FolderBrowserDialog
            {
                Description = "Please Select a folder"
            };

            if (browser.ShowDialog() == DialogResult.OK)
            {
                SelectedDirectory = browser.SelectedPath;
            }
        }

        /// <summary>
        /// Creates a backup set for the currently selected directory.</summary>  
        private async Task CreateBackupSet()
        {
            if(directoryScan != null && !directoryScan.IsCompleted)
            {
                // a scan is currently running, ignore the ok button
                return;
            }

            FileScanErrorString = "";

            if (string.IsNullOrWhiteSpace(SelectedDirectory) || string.IsNullOrWhiteSpace(BackupSetLabel))
                return;

            var statusText = new Progress<string>(p => ScanStatusText = p);
            var scanProgress = new Progress<int>(p => ScanProgress = p);

            // Clean up previous tasks if the user has canceled it
            if(directoryScan != null && directoryScan.IsCompleted)
            {
                try
                {
                    directoryScan.Dispose();
                }
                catch (Exception)
                {
                    // No error handling required, the task only needs to
                    // be wrapped in case it's not yet ready to be disposed and throws
                    // an exception, but it will be set to null anyway, so there is no issue
                }
            }

            directoryScan = null;

            // Don't use the property here since it would generate a new tokensource
            if (tokenSource != null)
            {
                TokenSource.Dispose();
                TokenSource = null;
            }

            IsScanInProgressOrCompleted = true;

            directoryScan = index.CreateBackupSetAsync(new DirectoryInfo(SelectedDirectory), TokenSource.Token, scanProgress, statusText, BackupSetLabel);
            await directoryScan;

            if (tokenSource.IsCancellationRequested)
                IsScanInProgressOrCompleted = false;
            else
            {
                // Indicate to the user that the progress is fully completed and that he can now
                // close the window by changing the button caption
                CancelButtonCaption = "Done";
                
            }
        }

        /// <summary>
        /// Cancels all ongoing scanning operations and closes the overlay.</summary>  
        private void CancelCommand_Execute(object obj)
        {
            // IsCompleted also includes IsFaulted and IsCanceled
            if(directoryScan is null || directoryScan.IsCompleted)
            {
                // No scan in progress, close the overlay

                if (directoryScan != null)
                    directoryScan.Dispose();

                if (TokenSource != null)
                {
                    TokenSource.Cancel();
                    TokenSource.Dispose();
                    TokenSource = null;
                }

                MessageService.SendMessage(this, "DisposeOverlay", null);
                return;
            }

            // A scan is in progress, prompt the user for confirmation
            // and abort the scan, but don't close the overlay
            var confirmDiag = new OKCancelPopupViewModel("Do you want to abort the scan in progress?", "", "Continue", "Abort");
            if(confirmDiag.ShowDialog() == DialogResult.Cancel)
            {
                // User has clicked abort, cancel the token 
                TokenSource.Cancel();
            }
        }

        #endregion
    }
}
