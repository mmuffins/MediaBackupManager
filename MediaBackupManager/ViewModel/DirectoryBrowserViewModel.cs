using MediaBackupManager.Model;
using MediaBackupManager.SupportingClasses;
using System;
using System.Collections.Generic;
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

        private FileIndexViewModel index;
        private FileDirectoryViewModel currentDirectory;
        private object selectedDirectoryTreeItem;
        private object selectedFileGridItem;

        private RelayCommand clearDataCommand;
        private RelayCommand removeNewData;
        private RelayCommand removeBackupSetCommand;
        private RelayCommand loadData;
        private RelayCommand loadAdditionalData;
        private RelayCommand scanNewData;
        private RelayCommand createBackupSetCommand;
        
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

        public RelayCommand ClearDataCommand
        {
            get
            {
                if (clearDataCommand == null)
                {
                    clearDataCommand = new RelayCommand(ClearData_Execute, param => true);
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

        public RelayCommand CreateBackupSetCommand
        {
            get
            {
                if (createBackupSetCommand == null)
                {
                    createBackupSetCommand = new RelayCommand(
                        p => MessageService.SendMessage(this, "CreateBackupSet", null), 
                        p => true);
                }
                return createBackupSetCommand;
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
                        p => p is BackupSetViewModel);
                }
                return removeBackupSetCommand;
            }
        }

        #endregion

        #region Methods

        public DirectoryBrowserViewModel(FileIndexViewModel index)
        {
            this.Index = index;
            MessageService.RoutedMessage += new EventHandler<MessageServiceEventArgs>(OnMessageServiceMessage);
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
                CurrentDirectory = directory;
                CurrentDirectory.TreeViewIsExpanded = true;
                CurrentDirectory.TreeViewIsSelected = true;
        }

        /// <summary>
        /// Event handler for the global MessageService.</summary>
        private void OnMessageServiceMessage(object sender, MessageServiceEventArgs e)
        {
            switch (e.Property)
            {
                case "BreadcrumbBar_MouseUp":
                    if (e.Parameter is FileDirectoryViewModel)
                        SetDirectory((FileDirectoryViewModel)e.Parameter);
                    break;

                case "GridFiles_MouseDoubleClick":
                    if (e.Parameter is FileDirectoryViewModel)
                        SetDirectory((FileDirectoryViewModel)e.Parameter);
                    break;

                default:
                    break;
            }
        }


        #endregion
    }
}
