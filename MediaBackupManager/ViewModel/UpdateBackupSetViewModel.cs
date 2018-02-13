using MediaBackupManager.SupportingClasses;
using MediaBackupManager.ViewModel.Popups;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel
{
    public class UpdateBackupSetViewModel : ViewModelBase
    {
        string scanStatusText;
        int scanProgress;
        string cancelButtonCaption;
        bool isScanInProgressOrCompleted;
        bool isScanCompleted;
        BackupSetViewModel updateSet;

        FileIndexViewModel index;
        CancellationTokenSource tokenSource;
        string fileScanErrorString;
        Task directoryScan;

        RelayCommand cancelCommand;
        RelayCommand startCommand;

        #region Properties


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
        /// Gets or sets the command to start the update process.</summary>  
        public RelayCommand StartCommand
        {
            get
            {
                if (startCommand == null)
                {
                    startCommand = new RelayCommand(
                        async p => await UpdateBackupSet(UpdateSet),
                        p => true);
                }
                return startCommand;
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
        /// Gets a value indicating whether a file scan was successfully completed.</summary>  
        public bool IsScanCompleted
        {
            // Needed to simplify binding
            get { return isScanCompleted; }
            set
            {
                if (value != isScanCompleted)
                {
                    isScanCompleted = value;
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
        /// Gets or sets Backup Set that is being updated.</summary>  
        public BackupSetViewModel UpdateSet
        {
            get { return updateSet; }
            set
            {
                if (value != updateSet)
                {
                    updateSet = value;
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

        public UpdateBackupSetViewModel(FileIndexViewModel index, BackupSetViewModel backupSet)
        {
            this.index = index;
            this.CancelButtonCaption = "Cancel";
            this.IsScanInProgressOrCompleted = false;
            this.IsScanCompleted = false;
            this.FileScanErrorString = "";
            this.UpdateSet = backupSet;
            this.Title = "Updating Backup Set" + backupSet.Label;
            StartCommand.Execute(null);
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
                    if (e.Parameter is ApplicationException)
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

        /// <summary>
        /// Updates the provided Backup Set.</summary>  
        private async Task UpdateBackupSet(BackupSetViewModel backupSet)
        {
            if (directoryScan != null && !directoryScan.IsCompleted)
            {
                // a scan is currently running, ignore the ok button
                return;
            }

            FileScanErrorString = "";

            var statusText = new Progress<string>(p => ScanStatusText = p);
            var scanProgress = new Progress<int>(p => ScanProgress = p);

            // Clean up previous tasks if the user has canceled it
            if (directoryScan != null && directoryScan.IsCompleted)
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

            directoryScan = index.UpdateBackupSetAsync(backupSet, TokenSource.Token, scanProgress, statusText);
            await directoryScan;

            if (tokenSource.IsCancellationRequested)
                IsScanInProgressOrCompleted = false;
            else
            {
                // Indicate to the user that the progress is fully completed and that he can now
                // close the window by changing the button caption
                CancelButtonCaption = "Done";
                IsScanCompleted = true;

            }
        }

        /// <summary>
        /// Cancels all ongoing scanning operations and closes the overlay.</summary>  
        private void CancelCommand_Execute(object obj)
        {
            // IsCompleted also includes IsFaulted and IsCanceled
            if (directoryScan is null || directoryScan.IsCompleted)
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
            var confirmDiag = new OKCancelPopupViewModel("Do you want to abort the update?", "", "Continue", "Abort");
            if (confirmDiag.ShowDialog() == DialogResult.Cancel)
            {
                // User has clicked abort, cancel the token 
                TokenSource.Cancel();
            }
        }

        #endregion
    }
}
