using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel
{
    class DirectoryBrowserViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        private FileIndexViewModel index;
        private FileDirectoryViewModel currentDirectory;
        private object selectedDirectoryTreeItem;
        private object selectedFileGridItem;

        private RelayCommand.RelayCommand clearDataCommand;
        private RelayCommand.RelayCommand removeNewData;
        private RelayCommand.RelayCommand removeBackupSetCommand;
        private RelayCommand.RelayCommand loadData;
        private RelayCommand.RelayCommand loadAdditionalData;
        private RelayCommand.RelayCommand scanNewData;
        private RelayCommand.RelayCommand createBackupSetCommand;
        private RelayCommand.RelayCommand openDirectoryCommand;
        
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

        public FileDirectoryViewModel CurrentDirectory
        {
            get { return currentDirectory; }
            set
            {
                if(value != currentDirectory)
                {
                    currentDirectory = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public RelayCommand.RelayCommand ClearDataCommand
        {
            get
            {
                if (clearDataCommand == null)
                {
                    clearDataCommand = new RelayCommand.RelayCommand(ClearData_Execute, param => true);
                }
                return clearDataCommand;
            }
        }

        public RelayCommand.RelayCommand RemoveNewData
        {
            get
            {
                if (removeNewData == null)
                {
                    removeNewData = new RelayCommand.RelayCommand(RemoveNewData_Execute, param => true);
                }
                return removeNewData;
            }
        }

        public RelayCommand.RelayCommand LoadData
        {
            get
            {
                if (loadData == null)
                {
                    loadData = new RelayCommand.RelayCommand(LoadData_Execute, param => true);
                }
                return loadData;
            }
        }

        public RelayCommand.RelayCommand LoadAdditionalData
        {
            get
            {
                if (loadAdditionalData == null)
                {
                    loadAdditionalData = new RelayCommand.RelayCommand(TestLoadAdditionalData_Execute, param => true);
                }
                return loadAdditionalData;
            }
        }

        public RelayCommand.RelayCommand ScanNewData
        {
            get
            {
                if (scanNewData == null)
                {
                    scanNewData = new RelayCommand.RelayCommand(ScanNewData_Execute, param => true);
                }
                return scanNewData;
            }
        }

        public RelayCommand.RelayCommand CreateBackupSetCommand
        {
            get
            {
                if (createBackupSetCommand == null)
                {
                    createBackupSetCommand = new RelayCommand.RelayCommand(CreateBackupSet_Execute, param => true);
                }
                return createBackupSetCommand;
            }
        }

        public RelayCommand.RelayCommand RemoveBackupSetCommand
        {
            get
            {
                if (removeBackupSetCommand == null)
                {
                    removeBackupSetCommand = new RelayCommand.RelayCommand(
                        p => RemoveBackupSet(p as BackupSetViewModel), 
                        p => p is BackupSetViewModel);
                }
                return removeBackupSetCommand;
            }
        }

        public RelayCommand.RelayCommand OpenDirectoryCommand
        {
            get
            {
                if (openDirectoryCommand == null)
                {
                    openDirectoryCommand = new RelayCommand.RelayCommand(
                        p => RemoveBackupSet(p as BackupSetViewModel),
                        p => p is BackupSetViewModel);
                }
                return openDirectoryCommand;
            }
        }

        #endregion

        #region Methods

        public DirectoryBrowserViewModel(FileIndexViewModel index)
        {
            this.Index = index;
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
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"C:\Portable Apps"));
        }

        private async void CreateBackupSet_Execute(object obj)
        {
            var browser = new FolderBrowserDialog();
            browser.Description = "Please Select a folder";

            if (browser.ShowDialog() == DialogResult.OK)
            {
                await Index.CreateBackupSetAsync(new DirectoryInfo(browser.SelectedPath));
            }
        }

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

            await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir\main"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"D:\indexdir\main\images"));
            //await Index.CreateBackupSetAsync(new DirectoryInfo(@"C:\indexdir\main\images2\b"));
            //await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir\main\images2"));

        }

        /// <summary>
        /// Handler for Double Click events on the file grid from the view.</summary>  
        public void GridFiles_MouseDoubleClick(object sender)
        {
            if(sender is FileDirectoryViewModel)
            {
                CurrentDirectory = (FileDirectoryViewModel)sender;
                CurrentDirectory.TreeViewIsExpanded = true;
                CurrentDirectory.TreeViewIsSelected = true;
            }
        }

        #endregion
    }
}
