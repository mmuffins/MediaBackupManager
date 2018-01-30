using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaBackupManager.ViewModel
{
    class CommandBarViewModel : ViewModelBase.ViewModelBase
    {

        #region Fields

        private FileIndexViewModel index;
        private RelayCommand.RelayCommand clearData;
        private RelayCommand.RelayCommand removeNewData;
        private RelayCommand.RelayCommand loadData;
        private RelayCommand.RelayCommand loadAdditionalData;
        private RelayCommand.RelayCommand scanNewData;
        private RelayCommand.RelayCommand createBackupSet;

        #endregion

        #region Properties

        public FileIndexViewModel Index
        {
            get { return index; }
            set { index = value; }
        }

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

        public RelayCommand.RelayCommand CreateBackupSet
        {
            get
            {
                if (createBackupSet == null)
                {
                    createBackupSet = new RelayCommand.RelayCommand(CreateBackupSet_Execute, param => true);
                }
                return createBackupSet;
            }
        }

        #endregion

        #region Methods

        public CommandBarViewModel(FileIndexViewModel index)
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
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"C:\indexdir"));
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

        public async void TestLoadAdditionalData_Execute(object obj)
        {

            //Index.Index.IndexDirectory(new DirectoryInfo(@"F:\NZB"));
            //await Index.Index.IndexDirectoryAsync(new DirectoryInfo(@"F:\Archive"));


            //await Database.BeginTransactionAsync();
            //await Index.Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\Archive\Anime\Anne Happy"));
            //await Index.Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\Archive"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"D:\indexdir\dd"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"D:\indexdir"));

            await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir\main\images"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"D:\indexdir\main\images2\b"));
            await Index.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir\main\images2"));

            //var stagingIndex = new FileIndex();
            //await stagingIndex.CreateBackupSetAsync(new DirectoryInfo(@"F:\indexdir"));
            //Index.Index.MergeFileIndex(stagingIndex);

        }

        #endregion
    }
}
