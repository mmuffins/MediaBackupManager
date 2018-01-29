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

        public MainWindowViewModel(FileIndex index)
        {
            this.Index = new FileIndexViewModel(index);
            PrepareDatabaseAsync(Index).Wait();

            appViewModels.Add(new DirectoryBrowserViewModel(Index.Index));
            CurrentAppViewModel = appViewModels[0];

            //TODO:Remove using directives for MediaBackupManager.View and System.Windows once done testing
            var testPage = new TestPage(Index.Index);
            testPage.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            testPage.Show();
        }

        /// <summary>Makes sure that the backend database is created and in a good state.</summary>
        private async Task PrepareDatabaseAsync(FileIndexViewModel index)
        {
            Database.Index = Index.Index;
            bool newDB = Database.CreateDatabase();
            await Database.PrepareDatabaseAsync();

            // Add the default exclusions if a new db was created
            if (newDB)
                await this.Index.RestoreDefaultExclusionsAsync();

            await Index.LoadDataAsync();
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
