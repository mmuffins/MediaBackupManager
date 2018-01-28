using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    class FileIndexViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        FileIndex index = new FileIndex();
        private ObservableCollection<BackupSetViewModel> backupSets;

        #endregion

        #region Properties

        public FileIndex Index { get; set; }

        public ObservableCollection<BackupSetViewModel> BackupSets
        {
            get { return backupSets; }
            set
            {
                if (value != backupSets)
                {
                    backupSets = value;
                    NotifyPropertyChanged("");
                }
            }
        }


        #endregion

        #region Methods

        public FileIndexViewModel(FileIndex index)
        {
            this.Index = index;
            this.BackupSets = new ObservableCollection<BackupSetViewModel>();
            UpdateBackupSets();

            //PropertyChanged += (obj, args) => { System.Diagnostics.Debug.WriteLine("Property " + args.PropertyName + " changed"); };
            Index.PropertyChanged += new PropertyChangedEventHandler(Index_PropertyChanged);
            GetSubDirs();
        }

        public void GetSubDirs()
        {
            //var parent1 = BackupSets[0].FileNodes[10];
            //var ab = BackupSets[0].GetSubDirectories(parent1);

            //var parent2 = BackupSets[0].FileNodes[20];
            //var ab2 = BackupSets[0].GetSubDirectories(parent2);

        }

        private void Index_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "BackupSets")
            {
                UpdateBackupSets();
            }
        }


        public async Task LoadNewData(DirectoryInfo dir)
        {
            await Index.CreateBackupSetAsync(dir);
            //UpdateBackupSets();
        }

        public async Task DeleteNewData()
        {
            var deleteSet = Index.BackupSets.FirstOrDefault(x => x.RootDirectory == "indexdir" && x.MountPoint == "C:\\");

            if (!(deleteSet is null))
            {
                await Index.RemoveBackupSetAsync(deleteSet);
            }
        }

        private void UpdateBackupSets()
        {
            foreach (var item in Index.BackupSets)
            {
                var itm = new BackupSetViewModel(item);

                if (!this.BackupSets.Contains(itm))
                {
                    this.BackupSets.Add(itm);
                }
            }
        }

        #endregion

        #region Implementations

        #endregion

        
    }
}
