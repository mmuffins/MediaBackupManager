using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using MediaBackupManager.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace MediaBackupManager.ViewModel
{
    class MainWindowViewModel : ViewModelBase
    {
        //TODO: In some cases, when removing all cases from the label, new archives can still be created
        //TODO: Reporting?
        //TODO: file deduplication feature?
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
            this.Title = "Media Backup Manager";

            this.Index = new FileIndexViewModel(new FileIndex());
            PrepareDatabaseAsync().Wait();

            ChangeViewModel(new ArchiveOverviewViewModel(Index));
            AppViewModels.Add(new DirectoryBrowserViewModel(Index));

            // Check what drives are connected every few seconds
            // to show the correct status in the archive overview
            DispatcherTimer timer = new DispatcherTimer(DispatcherPriority.Background);
            //TODO: Q-Any better way to do this?
            timer.Tick += RefreshVolumeStatus;
            timer.Interval = new TimeSpan(0, 0, 5);
            timer.Start();
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

                case "ShowCreateArchiveOverlay":
                    // Assigning a viewmodel to CurrentOverlay will automatically
                    // display it as overlay in the view
                    CurrentOverlay = new CreateArchiveViewModel(Index);
                    CurrentOverlay.Title = "Add Archive";
                    break;

                case "ShowExclusionListViewOverlay":
                    // We want to use a new viewmodel each time we open
                    // an overlay in order to have a fresh state,
                    // so don't use ChangeViewModel here
                    CurrentOverlay = new ExclusionListViewModel(Index);
                    CurrentOverlay.Title = "File Exlusions";
                    break;

                case "ShowUpdateArchiveOverlay":
                    if(e.Parameter != null && e.Parameter is ArchiveViewModel)
                    {
                        CurrentOverlay = new UpdateArchiveViewModel(Index, (ArchiveViewModel)e.Parameter);
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

                    // Directly open a archive if it was provided as parameter
                    if (e.Parameter is ArchiveViewModel)
                        browserView.SetDirectory(((ArchiveViewModel)e.Parameter).RootDirectory);

                    ChangeViewModel(browserView);
                    break;

                case "ShowArchiveOverview":
                    var archiveView = AppViewModels
                        .OfType<ArchiveOverviewViewModel>()
                        .FirstOrDefault(x => x is ArchiveOverviewViewModel);
                    if (archiveView is null)
                    {
                        archiveView = new ArchiveOverviewViewModel(index);
                        archiveView.Title = "Archives";
                    }

                    ChangeViewModel(archiveView);
                    break;

                default:
                    break;
            }
        }

        /// <summary>Makes sure that the backend database is created and in a good state.</summary>
        public async Task PrepareDatabaseAsync()
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

        /// <summary>
        /// Refreshes the connected status for all logical volumes in the file index.</summary>  
        private void RefreshVolumeStatus(object sender, EventArgs e)
        {
            if (index != null)
                Task.Run(() => index.RefreshVolumeStatus());
        }

        #endregion
    }
}
