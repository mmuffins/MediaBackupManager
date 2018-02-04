using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
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
        private bool ignoreChanges = false;

        private ObservableCollection<BackupSetViewModel> backupSets;

        private FileDirectory currentDirectory;

        private ObservableHashSet<FileHashViewModel> hashes;

        #endregion

        #region Properties

        public FileIndex Index { get; }

        public FileDirectory CurrentDirectory
        {
            get { return currentDirectory; }
            set
            {
                if(currentDirectory != value)
                {
                    currentDirectory = value;
                    NotifyPropertyChanged();
                }
            }
        }

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

        public ObservableHashSet<FileHashViewModel> FileHashes
        {
            get { return hashes; }
            set
            {
                if (value != hashes)
                {
                    hashes = value;
                    NotifyPropertyChanged();
                }
            }
        }
        #endregion

        #region Methods

        public FileIndexViewModel(FileIndex index)
        {
            this.Index = index;
            this.BackupSets = new ObservableCollection<BackupSetViewModel>();
            this.FileHashes = new ObservableHashSet<FileHashViewModel>();
            foreach (var hash in Index.Hashes)
                this.FileHashes.Add(new FileHashViewModel(hash));

            foreach (var set in Index.BackupSets)
                this.BackupSets.Add(new BackupSetViewModel(set, this));

            //Index.PropertyChanged += new PropertyChangedEventHandler(OnIndexPropertyChanged);
            Index.Hashes.CollectionChanged += new NotifyCollectionChangedEventHandler(FileHashes_CollectionChanged);
            Index.BackupSets.CollectionChanged += new NotifyCollectionChangedEventHandler(BackupSets_CollectionChanged);
        }

        private void FileHashes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            ignoreChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                FileHashes.Clear();

                foreach (var hash in Index.Hashes)
                    hashes.Add(new FileHashViewModel(hash));
            }
            else
            {
                // Remove items from collection.
                var toRemove = new List<FileHashViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                        foreach (var existingItem in FileHashes)
                            if (existingItem.IsViewFor((FileHash)item))
                                toRemove.Add(existingItem);

                foreach (var item in toRemove)
                    FileHashes.Remove(item);

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                        FileHashes.Add(new FileHashViewModel((FileHash)item));
            }
            ignoreChanges = false;
            NotifyPropertyChanged("FileHashes");
        }

        private void BackupSets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            ignoreChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                BackupSets.Clear();

                foreach (var set in Index.BackupSets)
                    BackupSets.Add(new BackupSetViewModel(set, this));
            }
            else
            {
                // Remove items from collection.
                var toRemove = new List<BackupSetViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                        foreach (var existingItem in BackupSets)
                            if (existingItem.IsViewFor((BackupSet)item))
                                toRemove.Add(existingItem);

                foreach (var item in toRemove)
                    BackupSets.Remove(item);

                // Add new items to the collection.
                if (null != e.NewItems && e.NewItems.Count > 0)
                    foreach (var item in e.NewItems)
                        BackupSets.Add(new BackupSetViewModel((BackupSet)item, this));
            }
            ignoreChanges = false;
        }

        public async Task CreateBackupSetAsync(DirectoryInfo dir, string label = "")
        {
            await Index.CreateBackupSetAsync(dir, label);
        }

        public async Task RemoveBackupSetAsync(BackupSet backupSet)
        {
            await Index.RemoveBackupSetAsync(backupSet, true);
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

        /// <summary>
        /// Returns the FileHashViewModel object for the provided string.</summary>  
        public FileHashViewModel GetFileHashViewModel(string hash)
        {
            return FileHashes.FirstOrDefault(x => x.Checksum.Equals(hash));
        }


        #endregion

        #region Implementations

        #endregion


    }
}
