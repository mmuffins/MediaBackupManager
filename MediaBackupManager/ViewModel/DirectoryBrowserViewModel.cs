using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
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
    class DirectoryBrowserViewModel : ViewModelBase
    {
        #region Fields

        FileIndexViewModel index;
        FileDirectoryViewModel currentDirectory;
        object selectedDirectoryTreeItem;
        object selectedFileGridItem;
        string searchBarText;
        bool showSearchResults = false;
        bool highlightMissingBackupFiles =true;

        ObservableCollection<object> searchResults;

        RelayCommand clearDataCommand;
        RelayCommand removeNewData;
        RelayCommand removeBackupSetCommand;
        RelayCommand loadData;
        RelayCommand loadAdditionalData;
        RelayCommand scanNewData;
        RelayCommand showCreateBackupSetViewCommand;
        RelayCommand searchFilesCommand;
        RelayCommand clearSearchResultsCommand;
        RelayCommand showExclusionCommand;
        RelayCommand showBackupSetOverviewCommand;


        #endregion

        #region Properties

        public FileIndexViewModel Index
        {
            get { return index; }
            set { index = value; }
        }

        public object SelectedFileGridItem
        {
            get { return selectedFileGridItem; }
            set { selectedFileGridItem = value; }
        }

        public object SelectedDirectoryTreeItem
        {
            get { return selectedDirectoryTreeItem; }
            set
            {
                if (value != selectedDirectoryTreeItem)
                {
                    selectedDirectoryTreeItem = value;
                    if (value is BackupSetViewModel)
                        CurrentDirectory = ((BackupSetViewModel)value).RootDirectory;
                    else
                        CurrentDirectory = (FileDirectoryViewModel)value;

                    NotifyPropertyChanged();
                }
            }
        }

        public ObservableCollection<object> SearchResults
        {
            get { return searchResults; }
            set
            {
                if (value != searchResults)
                {
                    searchResults = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool ShowSearchResults
        {
            get { return showSearchResults; }
            set
            {
                if (value != showSearchResults)
                {
                    showSearchResults = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool HighlightMissingBackupFiles
        {
            get { return highlightMissingBackupFiles; }
            set
            {
                if (value != highlightMissingBackupFiles)
                {
                    highlightMissingBackupFiles = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string SearchBarText
        {
            get { return searchBarText; }
            set
            {
                if (value != searchBarText)
                {
                    searchBarText = value;
                    if (string.IsNullOrWhiteSpace(value))
                        ClearSearchResults();
                    else
                        PerformFileSearch(value);

                    NotifyPropertyChanged();
                }
            }
        }

        public FileDirectoryViewModel CurrentDirectory
        {
            get { return currentDirectory; }
            set
            {
                ClearSearchResults();
                if(value != currentDirectory)
                {
                    currentDirectory = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public RelayCommand ClearDataCommand
        {
            get
            {
                if (clearDataCommand == null)
                {
                    clearDataCommand = new RelayCommand(
                        ClearData_Execute,
                        p => !Index.IsOperationInProgress);
                }
                return clearDataCommand;
            }
        }

        public RelayCommand RemoveNewData
        {
            get
            {
                if (removeNewData == null)
                {
                    removeNewData = new RelayCommand(RemoveNewData_Execute, param => true);
                }
                return removeNewData;
            }
        }

        public RelayCommand LoadData
        {
            get
            {
                if (loadData == null)
                {
                    loadData = new RelayCommand(LoadData_Execute, param => true);
                }
                return loadData;
            }
        }

        public RelayCommand LoadAdditionalData
        {
            get
            {
                if (loadAdditionalData == null)
                {
                    loadAdditionalData = new RelayCommand(TestLoadAdditionalData_Execute, param => true);
                }
                return loadAdditionalData;
            }
        }

        public RelayCommand ScanNewData
        {
            get
            {
                if (scanNewData == null)
                {
                    scanNewData = new RelayCommand(ScanNewData_Execute, param => true);
                }
                return scanNewData;
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

        public RelayCommand SearchFilesCommand
        {
            get
            {
                if (searchFilesCommand == null)
                {
                    searchFilesCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "SearchFiles", null),
                        p => true);
                }
                return searchFilesCommand;
            }
        }

        public RelayCommand ClearSearchResultsCommand
        {
            get
            {
                if (clearSearchResultsCommand == null)
                {
                    clearSearchResultsCommand = new RelayCommand(
                        p => ClearSearchResults(),
                        p => true);
                }
                return clearSearchResultsCommand;
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

        public RelayCommand ShowBackupSetOverviewCommand
        {
            get
            {
                if (showBackupSetOverviewCommand == null)
                {
                    showBackupSetOverviewCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "ShowBackupSetOverview", null),
                        p => true);
                }
                return showBackupSetOverviewCommand;
            }
        }

        #endregion

        #region Methods

        public DirectoryBrowserViewModel(FileIndexViewModel index)
        {
            this.Index = index;
            searchResults = new ObservableCollection<object>();
            //MessageService.RoutedMessage += new EventHandler<MessageServiceEventArgs>(OnMessageServiceMessage);
        }

        /// <summary>
        /// Event handler for the global MessageService.</summary>
        protected override void OnMessageServiceMessage(object sender, MessageServiceEventArgs e)
        {
            switch (e.Property)
            {
                case "GridFiles_MouseDoubleClick":
                    if (e.Parameter is FileDirectoryViewModel)
                        SetDirectory((FileDirectoryViewModel)e.Parameter);
                    else if (e.Parameter is FileNodeViewModel)
                        ShowRelatedFileNodes(((FileNodeViewModel)e.Parameter));

                    break;

                case "BreadcrumbBar_MouseUp":
                    if (e.Parameter is FileDirectoryViewModel)
                        SetDirectory((FileDirectoryViewModel)e.Parameter);
                    break;

                default:
                    break;
            }
        }

        private async void ClearData_Execute(object obj)
        {
            for (int i = Index.Index.BackupSets.Count - 1; i >= 0; i--)
            {
                var deleteElement = Index.Index.BackupSets.ElementAt(i);
                await Index.Index.RemoveBackupSetAsync(deleteElement, true);
            }
        }

        private async void RemoveNewData_Execute(object obj)
        {
            var deleteSet = Index.Index.BackupSets.FirstOrDefault(x => x.RootDirectory == "indexdir" && x.MountPoint == "C:\\");

            if (!(deleteSet is null))
            {
                await Index.RemoveBackupSetAsync(deleteSet);
            }
        }

        private async void LoadData_Execute(object obj)
        {
            await Index.Index.LoadDataAsync();
        }

        private async void ScanNewData_Execute(object obj)
        {
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"C:\Portable Apps"), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "PortableApps");
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

        public async void TestLoadAdditionalData_Execute(object obj)
        {
            //await Index.CreateBackupSetAsync(new DirectoryInfo(@"C:\indexdir\dd"));
            //await Index.CreateBackupSetAsync(new DirectoryInfo(@"C:\indexdir"));

            await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir\main"), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "fMain");
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"D:\indexdir\main\images"), new CancellationTokenSource().Token, new Progress<int>(), new Progress<string>(), "dMainImages");
            //await Index.CreateBackupSetAsync(new DirectoryInfo(@"C:\indexdir\main\images2\b"));
            //await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir\main\images2"));

        }

        /// <summary>
        /// Sets the provided FileDirectoryViewModel as current directory.</summary>  
        public void SetDirectory(FileDirectoryViewModel directory)
        {
            ClearSearchResults();
            CurrentDirectory = directory;
            CurrentDirectory.TreeViewIsExpanded = true;
            CurrentDirectory.TreeViewIsSelected = true;
        }

        /// <summary>
        /// Resets the search Results.</summary>  
        private void ClearSearchResults()
        {
            this.ShowSearchResults = false;
            SearchResults.Clear();
        }

        /// <summary>
        /// Displays file nodes and directories containing the given search term in the file grid.</summary>  
        private void PerformFileSearch(string searchTerm)
        {
            SearchResults.Clear();
            var nodes = Index.FindFileNodes(searchTerm);
            var dirs = Index.FindDirectories(searchTerm);

            foreach (var item in dirs)
                SearchResults.Add(item);

            foreach (var item in nodes)
                SearchResults.Add(item);

            ShowSearchResults = true;
        }

        /// <summary>
        /// Gets all file nodes that have the same hash as the provided node and displays them in the file grid.</summary>  
        private void ShowRelatedFileNodes(FileNodeViewModel node)
        {
            SearchResults.Clear();
            var relatedNodes = node.GetRelatedNodes();

            foreach (var item in relatedNodes)
                SearchResults.Add(item);

            ShowSearchResults = true;
        }

        #endregion
    }
}
