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
        FileIndexViewModel index;
        CancellationTokenSource tokenSource;
        ObservableCollection<string> fileScanErrors;
        bool showPopup;
        ViewModelBase currentPopup;

        RelayCommand selectDirectoryCommand;
        RelayCommand cancelCommand;
        RelayCommand confirmCommand;

        #region Properties

        /// <summary>
        /// Command to open a FolderBrowserDialog and populate the path textbox with the selected directory.</summary>  
        public RelayCommand SelectDirectoryCommand
        {
            get
            {
                if (selectDirectoryCommand == null)
                {
                    selectDirectoryCommand = new RelayCommand(
                        SelectDirectoryCommand_Execute,
                        p => true);
                }
                return selectDirectoryCommand;
            }
        }

        /// <summary>
        /// Command to close the overlay.</summary>  
        public RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand(
                        CloseOverlay,
                        p => true);
                }
                return cancelCommand;
            }
        }

        /// <summary>
        /// Command to create a Backup Set for the selected drive.</summary>  
        public RelayCommand ConfirmCommand
        {
            //TODO: Create Validation and error messages to make sure that all fields are filled
            get
            {
                if (confirmCommand == null)
                {
                    confirmCommand = new RelayCommand(
                        //TODO:Q-Is calling Createbackupset like this still an async call?
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

        /// <summary>
        /// Collection containing a list of all errors that occured while scanning or hashing files.</summary>  
        public ObservableCollection<string> FileScanErrors
        {
            get { return fileScanErrors; }
            //set
            //{
            //    if (value != fileScanErrors)
            //    {
            //        fileScanErrors = value;
            //        NotifyPropertyChanged();
            //    }
            //}
        }

        /// <summary>
        /// Property controlling if a popup is currently displayed.</summary>  
        public bool ShowPopup
        {
            get { return showPopup; }
            set
            {
                if (value != showPopup)
                {
                    showPopup = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Contains the currently displayed viewmodel.</summary>  
        public ViewModelBase CurrentPopup
        {       
            get { return currentPopup; }
            set
            {
                if (value != currentPopup)
                {
                    currentPopup = value;
                    NotifyPropertyChanged();
                }
            }
        }

        #endregion

        #region Methods

        public CreateBackupSetViewModel(FileIndexViewModel index)
        {
            this.index = index;
            this.fileScanErrors = new ObservableCollection<string>();
        }

        /// <summary>
        /// Event handler for the global MessageService.</summary>
        protected override void OnMessageServiceMessage(object sender, MessageServiceEventArgs e)
        {
            switch (e.Property)
            {
                case "FileScanException":
                    // Sent during scanning or hashing operations
                    // add them to the error log to display to the user once done
                    if(e.Parameter is ApplicationException)
                    {
                        var errorMsg = $"{((ApplicationException)e.Parameter).Message}: {((ApplicationException)e.Parameter).InnerException.Message}";
                        FileScanErrors.Add(errorMsg);
                    }
                    break;

                default:
                    break;
            }
        }

        /// Opens a FolderBrowserDialog and populates the path textbox with the selected directory.</summary>  
        private void SelectDirectoryCommand_Execute(object obj)
        {
            //TODO: Add function to read the drive label to auto-fill the label field
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
        private async void CreateBackupSet(object obj)
        {
            this.FileScanErrors.Clear();

            if (string.IsNullOrWhiteSpace(SelectedDirectory) || string.IsNullOrWhiteSpace(BackupSetLabel))
                return;

            var statusText = new Progress<string>(p => ScanStatusText = p);
            var scanProgress = new Progress<int>(p => ScanProgress = p);

            await index.CreateBackupSetAsync(new DirectoryInfo(SelectedDirectory), TokenSource.Token, scanProgress, statusText, BackupSetLabel);

            //TODO: Show the error log before closing the overlay
            if(FileScanErrors.Count() > 0)
            {
                ShowPopup = true;
            }

            // All done, close the overlay
            CloseOverlay(null);
        }

        /// <summary>
        /// Closes the overlay and cancels all ongoing scanning operations.</summary>  
        private void CloseOverlay(object obj)
        {
            ShowOKCancelPopup("messagXe", "title");
            if (TokenSource != null)
            {
                TokenSource.Cancel();
                TokenSource.Dispose();
                TokenSource = null;
            }

            MessageService.SendMessage(this, "DisposeOverlay", null);
        }

        private bool ShowOKCancelPopup(string message, string title ="")
        {
            var popupVm = new OKCancelPopupViewModel(message, title);
            //currentPopup = popupVm;
            var result = popupVm.ShowDialog();

            return true;
        }

        #endregion
    }
}
