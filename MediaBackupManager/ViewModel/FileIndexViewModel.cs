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
    public class FileIndexViewModel : ViewModelBase.ViewModelBase
    {
        #region Fields

        FileIndex index;
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

            Index.PropertyChanged += new PropertyChangedEventHandler(OnIndexPropertyChanged);
        }

        private void OnIndexPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "BackupSet":
                    UpdateBackupSets();
                    break;

                default:
                    break;
            }
        }

        public async Task LoadNewData(DirectoryInfo dir)
        {
            await Index.CreateBackupSetAsync(dir);
        }

        public async Task DeleteNewData()
        {
            var deleteSet = Index.BackupSets.FirstOrDefault(x => x.RootDirectory == "indexdir" && x.MountPoint == "C:\\");

            if (!(deleteSet is null))
            {
                await Index.RemoveBackupSetAsync(deleteSet);
            }
        }

        /// <summary>
        /// Syncronizes the local BackupSet collection with the model.</summary>  
        private void UpdateBackupSets()
        {
            for (int i = this.BackupSets.Count - 1; i >= 0; i--)
            {
                if (!Index.BackupSets.Contains(this.BackupSets.ElementAt(i).BackupSet))
                    this.BackupSets.RemoveAt(i); // set was removed from the model, update accordingly
            }

            var newSets = Index.BackupSets
                .Except(this.BackupSets.Select(x => x.BackupSet));

            foreach (var item in newSets)
            {
                this.BackupSets.Add(new BackupSetViewModel(item));
            }
        }

        /// <summary>
        /// Adds the default exclusions to the collection if they don't already exist.</summary>  
        public async Task RestoreDefaultExclusionsAsync()
        {
            await Index.RestoreDefaultExclusionsAsync();
        }

        /// <summary>
        /// Populates the index with data stored in the database.</summary>  
        public async Task LoadDataAsync()
        {
            await Index.LoadDataAsync();
        }

        #endregion

        #region Implementations

        #endregion


    }
}
