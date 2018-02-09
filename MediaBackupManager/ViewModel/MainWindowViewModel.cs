using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using MediaBackupManager.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MediaBackupManager.ViewModel
{
    class MainWindowViewModel : ViewModelBase
    {
        #region Fields

        private RelayCommand changePageCommand;
        private ViewModelBase currentAppViewModel = new ViewModelBase();
        private ViewModelBase currentOverlay;
        private List<ViewModelBase> appViewModels = new List<ViewModelBase>();
        private FileIndexViewModel index;

        #endregion

        #region Properties

        public FileIndexViewModel Index
        {
            get { return index; }
            set { index = value; }
        }

        /// <summary>
        /// Gets a list of all viemodels in the application.</summary>
        public List<ViewModelBase> AppViewModels { get => appViewModels; }

        /// <summary>
        /// The viewmodel used to present content in the main window.</summary>
        public ViewModelBase CurrentAppViewModel
        {
            get { return currentAppViewModel; }
            set
            {
                if (value != currentAppViewModel)
                {
                    currentAppViewModel = value;
                    NotifyPropertyChanged();
                }
            }
        }

        /// <summary>
        /// The viewmodel currently displayed as overlay.</summary>
        public ViewModelBase CurrentOverlay
        {
            get { return currentOverlay; }
            set
            {
                if (value != currentOverlay)
                {
                    currentOverlay = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public RelayCommand ChangePageCommand
        {
            get
            {
                if (changePageCommand is null)
                {
                    changePageCommand = new RelayCommand(
                        p => ChangeViewModel((ViewModelBase)p), 
                        p => p is ViewModelBase);
                }
                return changePageCommand;
            }
        }

        #endregion

        #region Methods

        public MainWindowViewModel()
        {
            //TODO: Make the textbox slide open when clicking the add button for exclusion
            //TODO: Add the current tab to the control bar and highlight it

            this.Index = new FileIndexViewModel(new FileIndex());
            PrepareDatabaseAsync(Index.Index).Wait();

            ChangeViewModel(new BackupSetOverviewViewModel(Index));
            AppViewModels.Add(new DirectoryBrowserViewModel(Index));
        }

        /// <summary>
        /// Event handler for the global MessageService.</summary>
        protected override void OnMessageServiceMessage(object sender, MessageServiceEventArgs e)
        {
            switch (e.Property)
            {
                case "DisposeOverlay":
                    // Setting the CurrentOverlay property to null 
                    // will remove the overlay from the view
                    CurrentOverlay = null;
                    break;

                case "ShowCreateBackupSetOverlay":
                    // Assigning a viewmodel to CurrentOverlay will automatically
                    // display it as overlay in the view
                    CurrentOverlay = new CreateBackupSetViewModel(Index);
                    CurrentOverlay.Title = "Add Backup Set";
                    break;

                case "ShowExclusionListViewOverlay":
                    // We want to use a new viewmodel each time we open
                    // an overlay in order to have a fresh state,
                    // so don't use ChangeViewModel here
                    CurrentOverlay = new ExclusionListViewModel(Index);
                    CurrentOverlay.Title = "File Exlusions";
                    break;

                case "ShowUpdateBackupSetOverlay":
                    if(e.Parameter != null && e.Parameter is BackupSetViewModel)
                    {
                        CurrentOverlay = new UpdateBackupSetViewModel(Index, (BackupSetViewModel)e.Parameter);
                        CurrentOverlay.Title = "Update Backup Set";
                    }

                    break;

                case "ShowDirectoryBrowserView":
                    var browserView = AppViewModels
                        .OfType<DirectoryBrowserViewModel>()
                        .FirstOrDefault(x => x is DirectoryBrowserViewModel);
                    if (browserView is null)
                    {
                        browserView = new DirectoryBrowserViewModel(index);
                        browserView.Title = "Directory Browser";
                    }

                    // Directly open a backup set if it was provided as parameter
                    if (e.Parameter is BackupSetViewModel)
                        browserView.SetDirectory(((BackupSetViewModel)e.Parameter).RootDirectory);

                    ChangeViewModel(browserView);
                    break;

                case "ShowBackupSetOverview":
                    var backupSetView = AppViewModels
                        .OfType<BackupSetOverviewViewModel>()
                        .FirstOrDefault(x => x is BackupSetOverviewViewModel);
                    if (backupSetView is null)
                    {
                        backupSetView = new BackupSetOverviewViewModel(index);
                        backupSetView.Title = "Backup Sets";
                    }

                    ChangeViewModel(backupSetView);
                    break;

                default:
                    break;
            }
        }


        /// <summary>Makes sure that the backend database is created and in a good state.</summary>
        public async Task PrepareDatabaseAsync(FileIndex index)
        {
            bool newDB = Database.CreateDatabase();
            await Database.PrepareDatabaseAsync();

            // Add the default exclusions if a new db was created
            if (newDB)
                await index.RestoreDefaultExclusionsAsync();

            await index.LoadDataAsync();
        }

        /// <summary>Changes the currently displayed viewmodel.</summary>
        private void ChangeViewModel(ViewModelBase viewModel)
        {
            if (!appViewModels.Contains(viewModel))
                appViewModels.Add(viewModel);

            CurrentAppViewModel = viewModel;
        }

        #endregion
    }
}
