using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MediaBackupManager.ViewModel
{
    class TestPageViewModel : ViewModelBase.ViewModelBase
    {
        FileIndex fIndex = new FileIndex();
        public FileIndex Index { get => fIndex; }

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

        private void ClearData_Execute(object obj)
        {
            for (int i = Index.BackupSets.Count - 1; i >= 0; i--)
            {
                var deleteElement = Index.BackupSets.ElementAt(i);
                Index.RemoveBackupSet(deleteElement);
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

        private void LoadData_Execute(object obj)
        {
            Index.LoadData();
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

        private void LoadAdditionalData_Execute(object obj)
        {
            Index.IndexDirectory(new DirectoryInfo(@"D:\indexdir\dd"));
            Index.IndexDirectory(new DirectoryInfo(@"D:\indexdir"));

            Index.IndexDirectory(new DirectoryInfo(@"F:\indexdir\main\images"));
            Index.IndexDirectory(new DirectoryInfo(@"F:\indexdir\main\images2"));
        }

        private bool LoadAdditionalData_CanExecute(object obj)
        {
            return true;
        }

        public TestPageViewModel()
        {
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

        private void btnClearData_Click(object sender, RoutedEventArgs e)
        {

        }

        private void btnLoadAdditionalData_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
