using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel
{
    class CreateReportViewModel : ViewModelBase
    {
        #region Fields

        FileIndexViewModel index;
        string reportPath;
        string selectedReport;
        bool isReportInProgress;
        ObservableCollection<string> reportList;

        RelayCommand cancelCommand;
        RelayCommand createReportCommand;
        RelayCommand selectDirectoryCommand;

        #endregion

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
        /// Gets or sets the command to create a report based on the selected options.</summary>  
        public RelayCommand CreateReportCommand
        {
            get
            {
                if (createReportCommand == null)
                {
                    createReportCommand = new RelayCommand(
                        async p => await ExportReport(),
                        p => !String.IsNullOrWhiteSpace(SelectedReport)
                        && !String.IsNullOrWhiteSpace(ReportPath)
                        && !isReportInProgress);
                }
                return createReportCommand;
            }
        }

        /// <summary>
        /// Gets or sets the report path Directory.</summary>  
        public string ReportPath
        {
            get { return reportPath; }
            set
            {
                if (value != reportPath)
                {
                    reportPath = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the command to open a Dialog that lets the user a directory for the archive.</summary>  
        public RelayCommand SelectDirectoryCommand
        {
            get
            {
                if (selectDirectoryCommand == null)
                {
                    selectDirectoryCommand = new RelayCommand(
                        SelectDirectoryCommand_Execute,
                        p => !IsReportInProgress);
                }
                return selectDirectoryCommand;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether an report creation operation is in progress.</summary>  
        public bool IsReportInProgress
        {
            // Needed to simplify binding
            get { return isReportInProgress; }
            set
            {
                if (value != isReportInProgress)
                {
                    isReportInProgress = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected report type.</summary>  
        public string SelectedReport
        {
            get { return selectedReport; }
            set
            {
                if (value != selectedReport)
                {
                    selectedReport = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// Gets a list of possible report types.</summary>  
        public ObservableCollection<string> ReportList
        {
            get { return reportList; }
            set { reportList = value; }
        }


        #endregion

        #region Methods

        public CreateReportViewModel(FileIndexViewModel index)
        {
            this.index = index;
            this.IsReportInProgress = false;
            this.ReportList = new ObservableCollection<string>();
            ReportList.Add("Type 1");
            ReportList.Add("Type 2");
        }

        /// <summary>
        /// Generates the selected report and exports it.</summary>  
        private async Task ExportReport()
        {
            await ReportWriter.GenerateArchiveReport(index.Archives.ElementAt(0).Archive);
        }

        /// <summary>
        /// Opens a FolderBrowserDialog and populates the path textbox with the selected directory.</summary>  
        private void SelectDirectoryCommand_Execute(object obj)
        {

            var browser = new SaveFileDialog
            {
                Title = "Please Select a file name",
                CheckPathExists = true,
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
                AddExtension = true,
                DefaultExt = "html",
                Filter = "HTML files (*.html)|*.html",
                FilterIndex = 0,
                OverwritePrompt = true,
                FileName = "report.html"
            };

            if (browser.ShowDialog() == DialogResult.OK)
            {
                ReportPath = browser.FileName;
            }
        }

        /// <summary>
        /// Closes the overlay.</summary>  
        private void CancelCommand_Execute(object obj)
        {
            // the report creation is close to instant,
            // so there is no real benefit to checking the status of the progress
            // and conditionally aborting it, especially since the task creation
            // runs in a separate async thread that can finish even the caller is disposed 
            MessageService.SendMessage(this, "DisposeOverlay", null);
        }

        #endregion
    }
}
