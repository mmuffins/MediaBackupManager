using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace MediaBackupManager.ViewModel
{
    class TestPageViewModel : ViewModelBase.ViewModelBase
    {
        private FileIndex index;
        public FileIndex Index
        {
            get { return index; }
            set { index = value; }
        }

        BackupSetViewModel backupSets = new BackupSetViewModel();
        public BackupSetViewModel BackupSets { get => backupSets; }

        //ObservableCollection<BackupSet> backupSets = new ObservableCollection<BackupSet>();
        //public ObservableCollection<BackupSet> BackupSets { get => backupSets; }

        private RelayCommand.RelayCommand clearData;
        public RelayCommand.RelayCommand ClearData
        {
            get
            {
                if (clearData == null)
                {
                    clearData = new RelayCommand.RelayCommand(ClearData_Execute, param => true);
                }
                return clearData;
            }
        }
        private async void ClearData_Execute(object obj)
        {
            for (int i = Index.BackupSets.Count - 1; i >= 0; i--)
            {
                var deleteElement = Index.BackupSets.ElementAt(i);
                await Index.RemoveBackupSetAsync(deleteElement);
            }
        }


        private RelayCommand.RelayCommand removeNewData;
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
        private async void RemoveNewData_Execute(object obj)
        {
            var deleteSet = Index.BackupSets.FirstOrDefault(x => x.RootDirectory == "\\indexdir");

            if(!(deleteSet is null))
            {
                await Index.RemoveBackupSetAsync(deleteSet);
            }
        }

        private RelayCommand.RelayCommand loadData;
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
        private async void LoadData_Execute(object obj)
        {
            await Index.LoadDataAsync();
        }

        private RelayCommand.RelayCommand loadAdditionalData;
        public RelayCommand.RelayCommand LoadAdditionalData
        {
            get
            {
                if (loadAdditionalData == null)
                {
                    loadAdditionalData = new RelayCommand.RelayCommand(LoadAdditionalData_Execute, param => true);
                }
                return loadAdditionalData;
            }
        }

        private RelayCommand.RelayCommand scanNewData;
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
        private async void ScanNewData_Execute(object obj)
        {
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"C:\indexdir"));
        }

        private async void LoadAdditionalData_Execute(object obj)
        {

            //Index.IndexDirectory(new DirectoryInfo(@"F:\NZB"));
            //await Index.IndexDirectoryAsync(new DirectoryInfo(@"F:\Archive"));

            var token = new CancellationToken();
            App.Current.Properties["cancelToken"] = token;


            //await Database.BeginTransactionAsync();
            //await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\Archive\Anime\Anne Happy"));
            //await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\Archive"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"D:\indexdir\dd"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"D:\indexdir"));

            await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir\main\images"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"D:\indexdir\main\images2\b"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir\main\images2"));

            //var stagingIndex = new FileIndex();
            //await stagingIndex.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir"));
            //Index.MergeFileIndex(stagingIndex);

        }

        public TestPageViewModel(FileIndex index)
        {
            this.Index = index;
            //FileIndex.LoadData();
            //this.backupSets = new ObservableCollection<BackupSet>(FileIndex.BackupSets);

            //FileIndex.IndexDirectory(new DirectoryInfo(@"D:\indexdir\dd"));
            //FileIndex.IndexDirectory(new DirectoryInfo(@"D:\indexdir"));

            //FileIndex.IndexDirectory(new DirectoryInfo(@"F:\indexdir\main\images"));
            //FileIndex.IndexDirectory(new DirectoryInfo(@"F:\indexdir\main\images2"));


            //for (int i = FileIndex.BackupSets.Count - 1; i >= 0; i--)
            //{
            //    var deleteElement = FileIndex.BackupSets.ElementAt(i);
            //    FileIndex.RemoveBackupSet(deleteElement);
            //}

        }
    }
}
