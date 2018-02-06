﻿using MediaBackupManager.Model;
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
            //TODO: Replace the item gridview with something nicer looking
            //TODO: Show a view containing a label and path field after clicking the add directory button
            //TODO: Add an options view
            //TODO: Add an option to trigger highlighting nodes without backup
            //TODO: Add a function to update existing backup sets
            //TODO: When double clicking a file, show a view containing all other nodes of the current file hash
            //TODO: Make sure all major functions have a description
            //TODO: Add different highlighting for directories in the file grid
            //TODO: Support sorting in the file grid
            //TODO: Add context menu support for deleting/updating backup sets
            //TODO: Make sure that all commands have valid execution conditions
            //TODO: ANIMATIONS!
            //TODO: Change Properties to get/set style help texts => See popups

            this.Index = new FileIndexViewModel(new FileIndex());
            PrepareDatabaseAsync(Index.Index).Wait();

            appViewModels.Add(new DirectoryBrowserViewModel(Index));
            CurrentAppViewModel = appViewModels[0];

            //CurrentOverlay = new CreateBackupSetViewModel();
            
            //MessageService.RoutedMessage += new EventHandler<MessageServiceEventArgs>(OnMessageServiceMessage);
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

                case "CreateBackupSet":
                    // Assigning a viewmodel to CurrentOverlay will automatically
                    // display it as overlay in the view
                    CurrentOverlay = new CreateBackupSetViewModel(Index);
                    break;

                case "ShowExclusionList":
                    CurrentOverlay = new ExclusionListViewModel(Index);
                    break;

                default:
                    break;
            }
        }


        /// <summary>Makes sure that the backend database is created and in a good state.</summary>
        public async Task PrepareDatabaseAsync(FileIndex index)
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
        private void ChangeViewModel(ViewModelBase viewModel)
        {
            if (!appViewModels.Contains(viewModel))
                appViewModels.Add(viewModel);

            CurrentAppViewModel = viewModel;
        }

        ///// <summary>Displays the CreateBackupSetView overlay.</summary>
        //private void CreateBackupSet_Execute(object obj)
        //{
        //    //var browser = new FolderBrowserDialog();
        //    //browser.Description = "Please Select a folder";

        //    //if (browser.ShowDialog() == DialogResult.OK)
        //    //{
        //    //    await Index.CreateBackupSetAsync(new DirectoryInfo(browser.SelectedPath));
        //    //}
        //    //MessageService.SendMessage(this, "OpenFile", "open the file dude");
        //}


        #endregion

        #region Implementations

        #endregion


    }
}
