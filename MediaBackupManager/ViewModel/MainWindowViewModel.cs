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
            //TODO:Properly implement the cancellation token
            //var token = new System.Threading.CancellationToken();
            //App.Current.Properties["cancelToken"] = token;

            this.Index = new FileIndexViewModel(new FileIndex());
            PrepareDatabaseAsync(Index.Index).Wait();

            //TODO: Implement view showing a list of hashes related to a single node
            
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
