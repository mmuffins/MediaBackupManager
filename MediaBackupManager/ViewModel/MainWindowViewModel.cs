using MediaBackupManager.Model;
using MediaBackupManager.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ViewModelBase;

namespace MediaBackupManager.ViewModel
{
    class MainWindowViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        private RelayCommand.RelayCommand changePageCommand;
        private ViewModelBase.ViewModelBase currentAppViewModel = new ViewModelBase.ViewModelBase();
        private List<ViewModelBase.ViewModelBase> appViewModels = new List<ViewModelBase.ViewModelBase>();
        private FileIndexViewModel index;

        #endregion

        #region Properties

        public FileIndexViewModel Index
        {
            get { return index; }
            set { index = value; }
        }

        public List<ViewModelBase.ViewModelBase> AppViewModels { get => appViewModels; }

        /// <summary>
        /// The viewmodel used to present content in the main window</summary>
        public ViewModelBase.ViewModelBase CurrentAppViewModel
        {
            get { return currentAppViewModel; }
            set
            {
                if (value != currentAppViewModel)
                {
                    currentAppViewModel = value;
                    NotifyPropertyChanged("");
                }
            }
        }

        public RelayCommand.RelayCommand ChangePageCommand
        {
            get
            {
                if (changePageCommand is null)
                {
                    changePageCommand = new RelayCommand.RelayCommand(
                        p => ChangeViewModel((ViewModelBase.ViewModelBase)p), 
                        p => p is ViewModelBase.ViewModelBase);
                }
                return changePageCommand;
            }
        }

        #endregion

        #region Methods

        public MainWindowViewModel()
        {
            //TODO: Support cancelling a scan in progress
            //TODO: Add a progress bar for scanning operations
            //TODO: Replace the item gridview with something nicer looking
            //TODO: Show a view containing a label and path field after clicking the add directory button
            //TODO: Add an options view
            //TODO: Add an option to trigger highlighting nodes without backup
            //TODO: Add a function to add and remove exclusions
            //TODO: Add a filter bar
            //TODO: Add a function to update existing backup sets
            //TODO: When double clicking a file, show a view containing all other nodes of the current file hash
            //TODO: Make sure all major functions have a description
            //TODO: Add different highlighting for directories in the file grid
            //TODO: Add breadcrumb navigation for the file grid


            this.Index = new FileIndexViewModel(new FileIndex());
            PrepareDatabaseAsync(Index.Index).Wait();

            appViewModels.Add(new DirectoryBrowserViewModel(Index));
            CurrentAppViewModel = appViewModels[0];
        }

        /// <summary>Makes sure that the backend database is created and in a good state.</summary>
        private async Task PrepareDatabaseAsync(FileIndex index)
        {
            Database.Index = index;
            bool newDB = Database.CreateDatabase();
            await Database.PrepareDatabaseAsync();

            // Add the default exclusions if a new db was created
            if (newDB)
                await index.RestoreDefaultExclusionsAsync();

            await index.LoadDataAsync();
        }

        /// <summary>Changes the currently displayed viewmodel.</summary>
        private void ChangeViewModel(ViewModelBase.ViewModelBase viewModel)
        {
            if (!appViewModels.Contains(viewModel))
                appViewModels.Add(viewModel);

            CurrentAppViewModel = viewModel;
        }


        #endregion

        #region Implementations

        #endregion


    }
}
