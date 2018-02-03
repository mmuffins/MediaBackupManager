﻿using MediaBackupManager.Model;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.ViewModel
{
    public class FileHashViewModel : ViewModelBase.ViewModelBase, IEquatable<FileHashViewModel>
    {
        #region Fields

        private FileHash hash = new FileHash();
        private ObservableCollection<FileNodeViewModel> fileNodes = new ObservableCollection<FileNodeViewModel>();
        private bool ignoreChanges = false;

        #endregion

        #region Properties

        public long Length
        {
            get { return hash.Length; }
            set
            {
                if (value != hash.Length)
                {
                    hash.Length = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DateTime CreationTime
        {
            get { return hash.CreationTime; }
            set
            {
                if (value != hash.CreationTime)
                {
                    hash.CreationTime = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public DateTime LastWriteTime
        {
            get { return hash.LastWriteTime; }
            set
            {
                if (value != hash.LastWriteTime)
                {
                    hash.LastWriteTime = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string Checksum
        {
            get { return hash.Checksum; }
            set
            {
                if (value != hash.Checksum)
                {
                    hash.Checksum = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int NodeCount { get => hash.NodeCount; }

        public int BackupCount { get => hash.BackupCount; }

        public ObservableCollection<FileNodeViewModel> FileNodes
        {
            get { return fileNodes; }
        }

        #endregion

        #region Methods

        public FileHashViewModel(FileHash hash)
        {
            this.hash = hash;
            this.fileNodes = new ObservableCollection<FileNodeViewModel>();
            //foreach (var item in hash.Nodes)
            //    fileNodes.Add(new FileNodeViewModel(item));

            fileNodes.CollectionChanged += new NotifyCollectionChangedEventHandler(FileNodes_CollectionChanged);
        }

        private void FileNodes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (ignoreChanges)
                return;

            ignoreChanges = true;

            // If the collection was reset, then e.OldItems is empty. Just clear and reload.
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                FileNodes.Clear();

                //foreach (var node in hash.Nodes)
                //    fileNodes.Add(new FileNodeViewModel(node));
            }
            else
            {
                // Remove items from collection.
                var toRemove = new List<FileNodeViewModel>();

                if (null != e.OldItems && e.OldItems.Count > 0)
                    foreach (var item in e.OldItems)
                        foreach (var existingItem in FileNodes)
                            if (existingItem.IsViewFor((FileNode)item))
                                toRemove.Add(existingItem);

                foreach (var item in toRemove)
                    FileNodes.Remove(item);

                // Add new items to the collection.
                //if (null != e.NewItems && e.NewItems.Count > 0)
                    //foreach (var item in e.NewItems)
                    //    FileNodes.Add(new FileNodeViewModel((FileNode)item));
            }
            ignoreChanges = false;
        }

        /// <summary>
        /// Returns true if the provided object is the base object of the current viewmodel.</summary>  
        public bool IsViewFor(FileHash fileHash)
        {
            return hash.Equals(fileHash);
        }

        /// <summary>Adds a reference to a file node for the file.</summary>  
        public void AddNode(FileNodeViewModel node)
        {
            FileNodes.Add(node);
        }

        /// <summary>Removes reference to a file node for the file.</summary>  
        public void RemoveNode(FileNodeViewModel node)
        {
            FileNodes.Remove(node);
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return Checksum.GetHashCode();
        }

        public bool Equals(FileHashViewModel other)
        {
            if (other == null)
                return false;

            return this.hash.Equals(other.hash);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            FileHashViewModel otherObj = obj as FileHashViewModel;
            if (otherObj == null)
                return false;
            else
                return Equals(otherObj);
        }

        public override string ToString()
        {
            return Checksum;
        }

        #endregion


    }
}
