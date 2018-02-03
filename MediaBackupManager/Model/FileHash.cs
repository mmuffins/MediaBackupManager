﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MediaBackupManager.Model
{
    /// <summary>
    /// Virtual representation of a file in one or multiple backup set.</summary>  
    public class FileHash : IEquatable<FileHash>
    {
        #region Properties

        public long Length { get; set; }
        public DateTime CreationTime { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string Checksum { get; set; }
        public ObservableHashSet<FileNode> Nodes { get; }
        public int NodeCount { get => Nodes.Count; }
        public int BackupCount { get => Nodes.Select(x => x.BackupSet.Volume).Distinct().Count(); }

        #endregion

        #region Methods

        public FileHash()
        {
            this.Nodes = new ObservableHashSet<FileNode>();
        }

        public FileHash(FileInfo fileInfo, string checkSum) : this()
        {
            this.Length = fileInfo.Length;
            this.CreationTime = fileInfo.CreationTime;
            this.LastWriteTime = fileInfo.LastWriteTime;
            this.Checksum = checkSum;
        }

        public FileHash(string fileName, string checkSum) : this(new FileInfo(fileName), checkSum) { }

        /// <summary>Generates MD5 hash</summary>  
        /// <param name="filePath">The fully qualified or relative name of a physical file</param>
        public static string CalculateChecksum(string filePath)
        {
            //TODO: Maybe implement option to use another hashing algorithm to gain speed but have a higher chance of duplicates?
            //TODO: Speed up md5 -> https://stackoverflow.com/a/1177712/8401023
            //TODO: run this function in parallel to speed it up?
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", String.Empty); ;
                }
            }
        }

        /// <summary>Adds a reference to a physical location for the file.</summary>  
        public void AddNode(FileNode node)
        {
            Nodes.Add(node);
        }

        /// <summary>Removes reference to a phyisical location for the file.</summary>  
        public void RemoveNode(FileNode node)
        {
            Nodes.Remove(node);

            //if(Nodes.Count == 0)
            //    FileIndex.RemoveFile(this);
        }

        #endregion

        #region Implementations

        public override int GetHashCode()
        {
            return Checksum.GetHashCode();
        }

        public bool Equals(FileHash other)
        {
            if (other == null)
                return false;

            return this.Checksum.Equals(other.Checksum);
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            FileHash otherObj = obj as FileHash;
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
