﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Represents an index filesystem location.</summary>  
    class BackupSet
    {
        public Guid Guid { get; set; }
        public LogicalVolume Drive { get; set; }
        public string RootDirectory { get; set; }
        public string MountPoint { get => Drive.MountPoint; }
        public HashSet<FileDirectory> FileNodes { get; }

        public BackupSet()
        {
            this.Guid = Guid.NewGuid();
            this.FileNodes = new HashSet<FileDirectory>();
        }

        public BackupSet(DirectoryInfo directory, LogicalVolume drive) : this()
        {
            this.Drive = drive;
            //this.RootDirectory = new FileDirectory(directory.FullName, Drive, null);
            this.RootDirectory = directory.FullName.Substring(Path.GetPathRoot(directory.FullName).Length);
        }

        /// <summary>
        /// Scans all files below the root directory and adds them to the index.</summary>  
        public void ScanFiles()
        {
            FileNodes.Add(new FileDirectory(new DirectoryInfo(Path.Combine(MountPoint, RootDirectory)), this));


            IndexDirectories(new DirectoryInfo(Path.Combine(MountPoint,RootDirectory)));

            foreach (var item in FileNodes)
            {
                if(item is FileNode)
                    Database.InsertFileNode(item as FileNode);
                else
                    Database.InsertFileNode(item as FileDirectory);

            }
        }

        private void IndexDirectories(DirectoryInfo directory)
        {
            foreach (var item in directory.GetDirectories())
            {
                IndexDirectories(item);
                FileNodes.Add(new FileDirectory(directory,this));
            }

            IndexFiles(directory);

        }

        private void IndexFiles(DirectoryInfo directory)
        {
            foreach (var file in directory.GetFiles())
            {
                BackupFile backupFile = FileIndex.IndexFile(file.FullName);
                var fileNode = new FileNode(file, this, backupFile);
                backupFile.AddNode(fileNode);
                FileNodes.Add(fileNode);
            }
        }

        /// <summary>
        /// Removes all Elements from the collection.</summary>  
        public void Clear()
        {
            foreach (var item in FileNodes)
            {
                item.Remove();
            }

            FileNodes.Clear();
        }

        /// <summary>
        /// Determines whether a directory is already indexed in the backup set.</summary>  
        public bool ContainsDirectory(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;

            return dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length).Contains(RootDirectory);
        }

        /// <summary>
        /// Determines whether the backup set is a child of a directory.</summary>  
        public bool IsSubsetOf(DirectoryInfo dir)
        {
            if (MountPoint != dir.Root.Name)
                return false;
            return RootDirectory.Contains(dir.FullName.Substring(Path.GetPathRoot(dir.FullName).Length));
        }

        public override string ToString()
        {
            return MountPoint + " " + RootDirectory;
        }
    }
}
