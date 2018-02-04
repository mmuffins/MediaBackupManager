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

        #region Properties

        /// <summary>
        /// Opens a FolderBrowserDialog and populates the path textbox with the selected directory.</summary>  
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
        /// Closes the overlay.</summary>  
        public RelayCommand.RelayCommand CancelCommand
        {
            get
            {
                if (cancelCommand == null)
                {
                    cancelCommand = new RelayCommand.RelayCommand(
                        p => MessageService.SendMessage(this, "DisposeOverlay",  null),
                        p => true);
                }
                return cancelCommand;
            }
        }

        /// <summary>
        /// Creates a Backup Set for the selected drive.</summary>  
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
                    System.Diagnostics.Debug.WriteLine(value);
                    scanProgress = value;
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

        private async void CreateBackupSet(object obj)
        {
            if (string.IsNullOrWhiteSpace(SelectedDirectory) || string.IsNullOrWhiteSpace(BackupSetLabel))
                return;

            var scanTokenSource = new CancellationTokenSource();
            var scanCancelToken = scanTokenSource.Token;
            var statusText = new Progress<string>(p => ScanStatusText = p);
            var scanProgress = new Progress<int>(p => ScanProgress = p);

            //TODO: Add some busy indicator while the directory is being scanned
            //TODO: Properly handle cancellation while the directory is being scanned
            await index.CreateBackupSetAsync(new DirectoryInfo(SelectedDirectory), scanCancelToken, scanProgress, statusText, BackupSetLabel);
            MessageService.SendMessage(this, "DisposeOverlay", null);
        }

        #endregion
    }
}
