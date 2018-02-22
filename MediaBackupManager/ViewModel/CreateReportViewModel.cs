using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using MediaBackupManager.ViewModel.Popups;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel
{
    /// <summary>
    /// Struct containing Name, Description and action of a report.</summary>
    struct ReportObject
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public Func<List<Archive>, string, Task<string>> ReportFunction { get; set; }
    }

    class CreateReportViewModel : ViewModelBase
    {
        #region Fields

        FileIndexViewModel index;
        string reportPath;
        bool isReportInProgress;
        ObservableCollection<ReportObject> reportList;
        ReportObject? selectedReport;
        List<Archive> exportArchives;

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
                        async p => await CreateReport(),
                        p => SelectedReport != null
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
        public ReportObject? SelectedReport
        {
            get { return selectedReport; }
            set
            {
                selectedReport = value;
                NotifyPropertyChanged();
            }
        }

        /// <summary>
        /// Gets a list of possible report types.</summary>  
        public ObservableCollection<ReportObject> ReportList
        {
            get { return reportList; }
            set { reportList = value; }
        }


        #endregion

        #region Methods

        public CreateReportViewModel(FileIndexViewModel index, List<ArchiveViewModel> exportArchives)
        {
            this.index = index;
            this.IsReportInProgress = false;
            this.ReportList = new ObservableCollection<ReportObject>(GetReportList());
            this.exportArchives = exportArchives.Select(x => x.Archive).ToList();

            this.Title = "Create Report for all archives";
            if(exportArchives.Count > 1)
                this.Title = "Create Report for archive " + exportArchives.FirstOrDefault().Label;
        }

        /// <summary>
        /// Gets a list of all available report types.</summary>  
        private List<ReportObject> GetReportList()
        {
            var rList = new List<ReportObject>();
            rList.Add(new ReportObject()
            {
                Name = "Type 1",
                Description = "Type 1 Desc",
                ReportFunction = ReportWriter.GenerateFileListReport
            });
            rList.Add(new ReportObject()
            {
                Name = "Type 2",
                Description = "Type 2 Desc",
                ReportFunction = ReportWriter.GenerateFileListReport
            });

            return rList;
        }

        /// <summary>
        /// Generates the selected report and exports it.</summary>  
        private async Task CreateReport()
        {
            if (exportArchives.Count == 0)
            {
                var confirmDiag = new OKCancelPopupViewModel("An error occured while creating the report: No archives were selected", "", "OK", "")
                {
                    ShowCancelButton = false
                };
                confirmDiag.ShowDialog();
                return;
            }

            IsReportInProgress = true;

            try
            {
                // the report writer returns the path the file was exported to if the process was successful
                var resultPath = await SelectedReport.Value.ReportFunction(exportArchives, ReportPath);
                IsReportInProgress = false;

                if (File.Exists(resultPath))
                    System.Diagnostics.Process.Start(resultPath);
            }
            catch (Exception ex)
            {
                var confirmDiag = new OKCancelPopupViewModel("An error occured while creating the report: " + ex.InnerException.Message, "", "OK", "")
                {
                    ShowCancelButton = false
                };
                confirmDiag.ShowDialog();
            }
            finally
            {
                IsReportInProgress = false;
                CancelCommand_Execute(null);
            }
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
