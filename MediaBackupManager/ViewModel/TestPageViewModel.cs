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
                    clearData = new RelayCommand.RelayCommand(ClearData_Execute, ClearData_CanExecute);
                }
                return clearData;
            }
        }

        private bool ClearData_CanExecute(object obj)
        {
            return true;
        }

        private async void ClearData_Execute(object obj)
        {
            for (int i = Index.BackupSets.Count - 1; i >= 0; i--)
            {
                var deleteElement = Index.BackupSets.ElementAt(i);
                await Index.RemoveBackupSetAsync (deleteElement);
            }
        }

        private RelayCommand.RelayCommand loadData;
        public RelayCommand.RelayCommand LoadData
        {
            get
            {
                if (loadData == null)
                {
                    loadData = new RelayCommand.RelayCommand(LoadData_Execute, LoadData_CanExecute);
                }
                return loadData;
            }
        }

        private bool LoadData_CanExecute(object obj)
        {
            return true;
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
                    loadAdditionalData = new RelayCommand.RelayCommand(LoadAdditionalData_Execute, LoadAdditionalData_CanExecute);
                }
                return loadAdditionalData;
            }
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

        private bool LoadAdditionalData_CanExecute(object obj)
        {
            return true;
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
