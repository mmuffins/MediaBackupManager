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
        bool showSearchResults;
        bool highlightMissingBackupFiles;

        ObservableCollection<object> searchResults;

        RelayCommand removeBackupSetCommand;
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
            this.HighlightMissingBackupFiles = false;
            this.ShowSearchResults = false;
            searchResults = new ObservableCollection<object>();
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

        /// <summary>
        /// Removes the provided Backupset from the file index.</summary>
        private async void RemoveBackupSet(BackupSetViewModel backupSet)
        {
            if (backupSet != null && backupSet is BackupSetViewModel && backupSet.BackupSet != null)
            {
                await Index.RemoveBackupSetAsync(backupSet.BackupSet);
            }
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
